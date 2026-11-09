using System;
using UnityEngine;

namespace Owl.Monetization
{
    public class MaxAdsBannerController: MonoBehaviour
    {
#if USE_MAXADS
        public MaxSdkBase.BannerPosition position;

        private Action<bool> _rewardAction;
        private Action _onLoaded;
        private Action _loadAgain;
        
        private string _adUnitId;
        
        public void LoadAd(string adUnitId,MaxSdkBase.BannerPosition bannerPosition, Action onLoaded,Action loadAgain)
        {
            _loadAgain = loadAgain;
            _onLoaded = onLoaded;
            if (!string.IsNullOrEmpty(_adUnitId))
                MaxSdk.DestroyBanner(_adUnitId);
            _adUnitId = adUnitId;
            position = bannerPosition;
            MaxAdsController.Log($"Banner: Request");

            SubscribeMaxCallBacks();
            MaxSdk.CreateBanner(_adUnitId, position);
            MaxSdk.StartBannerAutoRefresh(_adUnitId);
        }  
        
        public void DestroyAd()
        {

            UnsubscribeMaxCallBacks();
            var previousId = _adUnitId;
            _adUnitId = null;
            _onLoaded = _loadAgain = null;
            if (!string.IsNullOrEmpty(previousId)) MaxSdk.DestroyBanner(previousId);
        }

        private void OnDestroy() => DestroyAd();
        
        public void ShowAd()
        {
            Analytics.AnalyticsManager.TrackBannerAd(ALAdAction.Show,position.ToString());

            MaxSdk.ShowBanner(_adUnitId);
        }

        public void HideAd()
        {

            MaxSdk.HideBanner(_adUnitId);
        }
        
        public void ChangeBannerPosition(MaxSdkBase.BannerPosition _pos)
        {
            MaxSdk.UpdateBannerPosition(_adUnitId, _pos);
        }
        
        private void OnBannerAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            MaxAdsController.Log($"Banner: Loaded");

            _onLoaded?.Invoke();
        }

        private void OnBannerAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            if (adUnitId != _adUnitId) return;
            MaxAdsController.LogWarning($"Banner: Load failed: {errorInfo}");

            UnsubscribeMaxCallBacks();
            _loadAgain?.Invoke();
        }

        private void OnBannerAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;

            Analytics.FirebaseController.TrackAdClick(adInfo);

        }

        private void OnBannerAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            Analytics.FirebaseController.TrackAdImpression(adInfo);
        }

        private void OnBannerAdExpandedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;

        }

        private void OnBannerAdCollapsedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            UnsubscribeMaxCallBacks();
            _loadAgain?.Invoke();
        }
        
        private void SubscribeMaxCallBacks()
        {
            UnsubscribeMaxCallBacks();
            MaxSdkCallbacks.Banner.OnAdLoadedEvent      += OnBannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent  += OnBannerAdLoadFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent     += OnBannerAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent += OnBannerAdRevenuePaidEvent;
            MaxSdkCallbacks.Banner.OnAdExpandedEvent    += OnBannerAdExpandedEvent;
            MaxSdkCallbacks.Banner.OnAdCollapsedEvent   += OnBannerAdCollapsedEvent;
        }
        
        private void UnsubscribeMaxCallBacks()
        {
            MaxSdkCallbacks.Banner.OnAdLoadedEvent      -= OnBannerAdLoadedEvent;
            MaxSdkCallbacks.Banner.OnAdLoadFailedEvent  -= OnBannerAdLoadFailedEvent;
            MaxSdkCallbacks.Banner.OnAdClickedEvent     -= OnBannerAdClickedEvent;
            MaxSdkCallbacks.Banner.OnAdRevenuePaidEvent -= OnBannerAdRevenuePaidEvent;
            MaxSdkCallbacks.Banner.OnAdExpandedEvent    -= OnBannerAdExpandedEvent;
            MaxSdkCallbacks.Banner.OnAdCollapsedEvent   -= OnBannerAdCollapsedEvent;
        }
#endif
    }
}

