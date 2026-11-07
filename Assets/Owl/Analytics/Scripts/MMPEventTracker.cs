using System;
using System.Collections.Generic;
using UnityEngine;
using Owl.Utility;

namespace Owl.Analytics.MMP
{
    public class MMPEventTracker : MonoBehaviour
    {
        private static MMPEventTracker _instance;

        private void Awake()
        {
            _instance = this;
        }

#if USE_SOLARENGINE
            private static SolarEngineManager _solarEngineManager;
#endif
        public void Initialize()
        {
            if (!ConsentManager.CanCollectAttribution) return;
#if USE_SOLARENGINE
            _solarEngineManager??= _instance.gameObject.AddComponent<SolarEngineManager>();
            SolarEngineManager.Init(OwlManager.OwlSettings.solarEngineAppKey,OwlManager.OwlSettings.solarEngineDebugMode);
#endif
        }

#if USE_MAXADS
        public static void OnAdRevenuePaidEvent(MaxSdkBase.AdInfo adInfo)
        {
            if (!ConsentManager.CanCollectAttribution) return;
#if USE_FACEBOOK && USE_FACEBOOK_SDK
            FacebookEvents.MaxImpression(adInfo?.AdUnitIdentifier, adInfo?.AdFormat, null, adInfo);
#endif
#if USE_SOLARENGINE
            SolarEngineManager.TrackAdImpressionMax(adInfo);
#endif

        }
        
        public static void OnAdClickEvent(MaxSdkBase.AdInfo adInfo)
        {
            if (!ConsentManager.CanCollectAttribution) return;
#if USE_SOLARENGINE
            SolarEngineManager.TrackAdClick(adInfo);
#endif

        }

#endif


#if USE_ADMOB
        public static void HandleAdPaidEvent(string placement, string adFormat, string adUnitId, GoogleMobileAds.Api.AdValue adValue,GoogleMobileAds.Api.ResponseInfo responseInfo)
        {
            if (!ConsentManager.CanCollectAttribution) return;
#if USE_FACEBOOK && USE_FACEBOOK_SDK
            FacebookEvents.AdMobImpression(adValue, adFormat, adUnitId, placement);
#endif
#if USE_SOLARENGINE
            SolarEngineManager.TrackAdImpressionAdmob(placement, adFormat, adUnitId, adValue,responseInfo);
#endif
        }
        
        public static void HandleAdClickEvent(string placement, string adFormat, string adUnitId,GoogleMobileAds.Api.ResponseInfo responseInfo)
        {
            if (!ConsentManager.CanCollectAttribution) return;
#if USE_SOLARENGINE
            SolarEngineManager.TrackAdClick(placement, adFormat, adUnitId,responseInfo);
#endif
        }
#endif

        public static void TrackProgressionEvent(string action, string label, int value,
            Dictionary<string, object> customParameters = null)
        {
            if (!ConsentManager.CanCollectAttribution) return;
#if USE_AIRBRIDGE
            AirbridgeManager.TrackProgressionEvent(action, label, value, customParameters);
#endif
        }

        public static void TrackDesignEvent(string label, Dictionary<string, object> customParameters = null)
        {
            if (!ConsentManager.CanCollectAttribution) return;
#if USE_AIRBRIDGE
            AirbridgeManager.TrackDesignEvent(label, customParameters);
#endif
        }

        public static void TrackCustomEvent(string categoryKey, string action, string label,
            Dictionary<string, object> customParameters = null)
        {
            if (!ConsentManager.CanCollectAttribution) return;
                      
#if USE_AIRBRIDGE
            AirbridgeManager.TrackCustomEvent(categoryKey,action,label,customParameters);
#endif
        }
    }

}
