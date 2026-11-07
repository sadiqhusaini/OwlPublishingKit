using System.Collections.Generic;
using UnityEngine;
using Owl.Utility;
#if USE_FACEBOOK_SDK
using Facebook.Unity;
#endif

namespace Owl.Analytics
{
    /// <summary>Initializes optional Meta events after Owl collection consent is granted.</summary>
    public sealed class FacebookController : MonoBehaviour
    {
        private bool _initialized;
        public static bool IsInitialized { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState() => IsInitialized = false;

        public void Initialize()
        {
#if USE_FACEBOOK_SDK
            if (_initialized || !CanSendEvents) return;
            _initialized = true;
            DontDestroyOnLoad(gameObject);
            if (FB.IsInitialized)
            {
                IsInitialized = true;
                FB.ActivateApp();
            }
            else
                FB.Init(OnInitialized, OnHideUnity);
#endif
        }

#if USE_FACEBOOK_SDK
        private static bool CanSendEvents =>
            ConsentManager.CanCollectAnalytics && ConsentManager.CanCollectAttribution;

        private static void OnInitialized()
        {
            if (!FB.IsInitialized)
            {
                IsInitialized = false;
                Debug.LogWarning("Meta Unity SDK initialization failed.");
                return;
            }

            IsInitialized = true;
            if (CanSendEvents)
                FB.ActivateApp();
            Debug.Log("Meta Unity SDK initialized.");
        }

        private static void OnHideUnity(bool isGameShown)
        {
            Time.timeScale = isGameShown ? 1f : 0f;
        }

        private void OnApplicationPause(bool paused)
        {
            if (!paused && CanSendEvents && FB.IsInitialized)
                FB.ActivateApp();
        }
#endif
    }

    /// <summary>Consent-gated Meta App Events used by Owl's purchase and gameplay hooks.</summary>
    public static class FacebookEvents
    {
        private static bool CanLog
        {
            get
            {
#if USE_FACEBOOK && USE_FACEBOOK_SDK
                return ConsentManager.CanCollectAnalytics && ConsentManager.CanCollectAttribution && FB.IsInitialized;
#else
                return false;
#endif
            }
        }

        public static void Purchase(float amount, string currency)
        {
#if USE_FACEBOOK && USE_FACEBOOK_SDK
            if (!CanLog || amount <= 0f) return;
            FB.LogAppEvent(AppEventName.Purchased, amount,
                new Dictionary<string, object> { { "currency", currency ?? "USD" } });
#endif
        }

        public static void LevelCompleted(int level)
        {
#if USE_FACEBOOK && USE_FACEBOOK_SDK
            if (!CanLog) return;
            FB.LogAppEvent(AppEventName.AchievedLevel, null,
                new Dictionary<string, object> { { "level", level } });
#endif
        }

#if USE_ADMOB && USE_FACEBOOK && USE_FACEBOOK_SDK
        public static void AdMobImpression(GoogleMobileAds.Api.AdValue adValue,
            string adFormat, string adUnitId, string placement)
        {
            if (!CanLog || adValue == null || adValue.Value <= 0) return;
            var revenue = adValue.Value / 1_000_000f;
            var parameters = new Dictionary<string, object>
            {
                { "fb_ad_network", "AdMob" },
                { "fb_ad_placement", placement ?? string.Empty },
                { "fb_ad_format", adFormat ?? string.Empty },
                { "currency", adValue.CurrencyCode ?? "USD" }
            };
            FB.LogAppEvent("AdImpression", revenue, parameters);
        }
#endif

#if USE_MAXADS && USE_FACEBOOK && USE_FACEBOOK_SDK
        public static void MaxImpression(string adUnitId, string adFormat,
            string placement, MaxSdkBase.AdInfo adInfo)
        {
            if (!CanLog || adInfo == null || adInfo.Revenue <= 0d) return;
            var parameters = new Dictionary<string, object>
            {
                { "fb_ad_network", adInfo.NetworkName ?? string.Empty },
                { "fb_ad_placement", placement ?? string.Empty },
                { "fb_ad_format", adFormat ?? string.Empty },
                { "currency", "USD" }
            };
            FB.LogAppEvent("AdImpression", (float)adInfo.Revenue, parameters);
        }
#endif
    }
}
