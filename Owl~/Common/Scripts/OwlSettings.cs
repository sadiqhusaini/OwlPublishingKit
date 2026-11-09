using Owl.Monetization;
using UnityEngine;
using UnityEngine.Serialization;

namespace Owl
{
    public class OwlSettings : ScriptableObject
    {
        public bool bannerAd;
        public bool interstitialAd;
        public bool rewardedAd;
        public bool rewardedInterstitialAd;
        public bool mRecAd;
        public bool appOpenAd;
        
        
        
        public  string SDKName = "Owl";
#if UNITY_EDITOR
        public Texture2D IconTexture;
#endif
        
        [Space]
        public Version SDKVersion;
        public Links SDKLinks;
        public Components OwlComponents;
        public AdSettings adSettings;
        
#if USE_GAMEANALYTICS
        public GameAnalyticsSDK.Setup.Settings gameAnalyticsSettings;
#endif
        
        
        [Space]
        [Header("Debug")]
        public bool autoSDKInit = true;
        public bool screenSDKInfo;
        public bool verbose;
        public bool testIds;

        
        [Space]
        [Header("Analytics")]
        public MMPSDK mmpsdk;
        public AnalyticsSDK analyticsSdk;
        [Space]
        public bool useMMP;
        public bool useAirbridge;
        public bool useSolarEngine;
        

        public bool useFirebaseAnalytics;
        public bool useFirebaseCrashlytics;
        public bool useFirebaseRemoteConfig;
        public bool useFirebaseMessaging;
        public bool requestPushPermissionOnStart = true;
        public bool useFacebookEvents;

                
        [Space]
        [Header("Monetization")]
        public bool useIap;
        public string[] noAdsIds;
        
        [Space]
        public Mediation mediation;
        public BannerPosition bannerPosition;
        [Space]
        public bool remoteCompleteAds;
        public bool remoteAppOpen;
        public bool remoteBanner;
        public bool remoteInterstitial;
        public bool remoteRewarded;
        public bool remoteRewardedInterstitial;
        public bool remoteMREC;
        public bool remoteTimedAds;
        public bool useAppOpenAd;
        public bool useBanner;
        public bool useInterstitialAd = true;
        public bool useRewardedAd = true;
        public bool useMREC = true;
        public bool useRewardedInterstitialAd = true;
        public bool interstitialOnStart = true;
        public bool rewardedInterstitialOnStart = true;
        public bool rewardedOnStart = true;
        public bool bannerOnStart;
        [Space]
        public bool enableTimerAd;
        public bool autoStartTimerAd;
        public bool resetIntervalOnRewarded=false;
        public bool showNoInternetUi=true;
        public bool showAdComingUi;
        [Range(0.1f, 2f)] public float adComingWaitStep = 0.5f;
        public bool showBannerBgLayer;
        public bool showMrecBgLayer;
        public bool showIapLoadingUi = true;
        public bool solarEngineDebugMode;

        [Range(0,15)]  public float appOpenStartDelay = 0;
        [Range(0,360)] public float interstitialInterval = 10f;
        [Range(0,360)] public float timerAdInterval = 30;
        
        [Space]
        public string nativePopupText;
        public string solarEngineAppKey;
        
        private static int _mrecdIdCounter = 0;
        private static int _bannerAdIdCounter = 0;
        private static int _interstitialAdIdCounter = 0;
        private static int _rewardedAdIdCounter = 0;
        private static int _rewardedInterstitialAdIdCounter = 0;
        private static int _appOpenAdIdCounter = 0;
        
       

        public BannerPosition GetBannerPosition()
        {
            return bannerPosition;
        }

        
#if USE_ADMOB
        
        private GoogleMobileAds.Api.AdPosition _adPosition;
        public GoogleMobileAds.Api.AdPosition ConvertGoogleAdPositions(BannerPosition banner)
        {
            switch (banner)
            {
                case BannerPosition.Top:
                    _adPosition = GoogleMobileAds.Api.AdPosition.Top;
                    break;
                case BannerPosition.Bottom:
                    _adPosition = GoogleMobileAds.Api.AdPosition.Bottom;
                    break;
                case BannerPosition.Center:
                case BannerPosition.CenterLeft:
                case BannerPosition.CenterRight:
                    _adPosition = GoogleMobileAds.Api.AdPosition.Center;
                    break;
                case BannerPosition.TopLeft:
                    _adPosition = GoogleMobileAds.Api.AdPosition.TopLeft;
                    break;
                case BannerPosition.TopRight:
                    _adPosition = GoogleMobileAds.Api.AdPosition.TopRight;
                    break;
                case BannerPosition.BottomLeft:
                    _adPosition = GoogleMobileAds.Api.AdPosition.BottomLeft;
                    break;
                case BannerPosition.BottomRight:
                    _adPosition = GoogleMobileAds.Api.AdPosition.BottomRight;
                    break;
            }
            return _adPosition; 
        }
        
#elif USE_MAXADS
    
        private MaxSdkBase.BannerPosition _adPosition;

        public MaxSdkBase.BannerPosition  ConvertMaxBannerAdPositions(BannerPosition banner)
        {
            switch (banner)
            {
                case BannerPosition.Top:
                    _adPosition = MaxSdkBase.BannerPosition.TopCenter;
                    break;
                case BannerPosition.Bottom:
                    _adPosition = MaxSdkBase.BannerPosition.BottomCenter;
                    break;
                case BannerPosition.Center:
                    _adPosition = MaxSdkBase.BannerPosition.Centered;
                    break;
                case BannerPosition.TopLeft:
                    _adPosition = MaxSdkBase.BannerPosition.TopLeft;
                    break;
                case BannerPosition.TopRight:
                    _adPosition = MaxSdkBase.BannerPosition.TopRight;
                    break;
                case BannerPosition.BottomLeft:
                    _adPosition = MaxSdkBase.BannerPosition.BottomLeft;
                    break;
                case BannerPosition.BottomRight:
                    _adPosition = MaxSdkBase.BannerPosition.BottomRight;
                    break;
                case BannerPosition.CenterLeft:
                    _adPosition = MaxSdkBase.BannerPosition.CenterLeft;
                    break;
                case BannerPosition.CenterRight:
                    _adPosition = MaxSdkBase.BannerPosition.CenterRight;
                    break;
            }
            return _adPosition; 
        }
        
        private MaxSdkBase.AdViewPosition _adViewPosition;
        public MaxSdkBase.AdViewPosition  ConvertMaxAdViewPositions(BannerPosition banner)
        {
            switch (banner)
            {
                case BannerPosition.Top:
                    _adViewPosition = MaxSdkBase.AdViewPosition.TopCenter;
                    break;
                case BannerPosition.Bottom:
                    _adViewPosition = MaxSdkBase.AdViewPosition.BottomCenter;
                    break;
                case BannerPosition.Center:
                    _adViewPosition = MaxSdkBase.AdViewPosition.Centered;
                    break;
                case BannerPosition.TopLeft:
                    _adViewPosition = MaxSdkBase.AdViewPosition.TopLeft;
                    break;
                case BannerPosition.TopRight:
                    _adViewPosition = MaxSdkBase.AdViewPosition.TopRight;
                    break;
                case BannerPosition.BottomLeft:
                    _adViewPosition = MaxSdkBase.AdViewPosition.BottomLeft;
                    break;
                case BannerPosition.BottomRight:
                    _adViewPosition = MaxSdkBase.AdViewPosition.BottomRight;
                    break;
                case BannerPosition.CenterLeft:
                    _adViewPosition = MaxSdkBase.AdViewPosition.CenterLeft;
                    break;
                case BannerPosition.CenterRight:
                    _adViewPosition = MaxSdkBase.AdViewPosition.CenterRight;
                    break;
            }
            return _adViewPosition; 
        }

#endif
       
        
        public static void ResetAdUnitRotation()
        {
            _bannerAdIdCounter = _mrecdIdCounter = _interstitialAdIdCounter = 0;
            _rewardedAdIdCounter = _rewardedInterstitialAdIdCounter = _appOpenAdIdCounter = 0;
        }

        public bool UsesGoogleFlooring => adSettings != null &&
            adSettings.googleAds.mode == GoogleAdUnitMode.Flooring;

        // A separate cursor per format advances only when a new ad is requested.
        private static AdUnit SelectUnits(AdUnit single, AdUnit[] multiple, bool useSingle, ref int cursor)
        {
            if (useSingle) return single ?? new AdUnit("", "");
            if (multiple == null || multiple.Length == 0)
                return new AdUnit("", "");
            for (int attempt = 0; attempt < multiple.Length; attempt++)
            {
                var units = multiple[cursor % multiple.Length];
                cursor = (cursor + 1) % multiple.Length;
                if (IsConfigured(units)) return units;
            }
            return new AdUnit("", "");
        }

        private static bool IsConfigured(AdUnit units)
        {
#if UNITY_ANDROID
            return !string.IsNullOrWhiteSpace(units?.adUnitAndroid);
#elif UNITY_IOS
            return !string.IsNullOrWhiteSpace(units?.adUnitIOS);
#else
            return !string.IsNullOrWhiteSpace(units?.adUnitAndroid) ||
                   !string.IsNullOrWhiteSpace(units?.adUnitIOS);
#endif
        }

        private AdUnit SelectGoogleUnits(AdUnit single, AdUnit[] units, ref int cursor, int index)
        {
            if (adSettings.googleAds.mode == GoogleAdUnitMode.Single)
                return single ?? new AdUnit("", "");
            if (units == null || units.Length == 0) return new AdUnit("", "");
            if (UsesGoogleFlooring && index >= 0)
                return index >= 0 && index < units.Length
                    ? units[index] ?? new AdUnit("", "") : new AdUnit("", "");
            return SelectUnits(single, units, false, ref cursor);
        }

        public AdUnit GetBannerIDs()
        {
            if (testIds) return new AdUnit("", "");
#if USE_ADMOB
            return SelectGoogleUnits(adSettings.googleAds.bannerAdUnit,
                adSettings.googleAds.bannerAdUnits, ref _bannerAdIdCounter, -1);
#elif USE_MAXADS
            return SelectUnits(adSettings.maxAds.bannerAdUnit,
                adSettings.maxAds.bannerAdUnits, adSettings.maxAds.mode == MaxAdUnitMode.Single, ref _bannerAdIdCounter);
#else
            return new AdUnit("", "");
#endif
        }

        public AdUnit GetMRECID(int index = 0)
        {
            if (testIds) return new AdUnit("", "");
#if USE_ADMOB
            return SelectGoogleUnits(adSettings.googleAds.mRecAdUnit,
                adSettings.googleAds.mRecAdUnits, ref _mrecdIdCounter, -1);
#elif USE_MAXADS
            return SelectUnits(adSettings.maxAds.mRecAdUnit,
                adSettings.maxAds.mRecAdUnits, adSettings.maxAds.mode == MaxAdUnitMode.Single, ref _mrecdIdCounter);
#else
            return new AdUnit("", "");
#endif
        }

        public AdUnit GetInterstitialID(int index = 0)
        {
            if (testIds) return new AdUnit("", "");
#if USE_ADMOB
            return SelectGoogleUnits(adSettings.googleAds.interstitialAdUnit,
                adSettings.googleAds.interstitialAdUnits, ref _interstitialAdIdCounter, index);
#elif USE_MAXADS
            return SelectUnits(adSettings.maxAds.interstitialAdUnit,
                adSettings.maxAds.interstitialAdUnits, adSettings.maxAds.mode == MaxAdUnitMode.Single, ref _interstitialAdIdCounter);
#else
            return new AdUnit("", "");
#endif
        }

        public AdUnit GetRewardedInterstitialID(int index = 0)
        {
            if (testIds) return new AdUnit("", "");
#if USE_ADMOB
            return SelectGoogleUnits(adSettings.googleAds.rewardedInterstitialAdUnit,
                adSettings.googleAds.rewardedInterstitialAdUnits, ref _rewardedInterstitialAdIdCounter, index);
#else
            return new AdUnit("", "");
#endif
        }

        public AdUnit GetRewardedID(int index = 0)
        {
            if (testIds) return new AdUnit("", "");
#if USE_ADMOB
            return SelectGoogleUnits(adSettings.googleAds.rewardedAdUnit,
                adSettings.googleAds.rewardedAdUnits, ref _rewardedAdIdCounter, index);
#elif USE_MAXADS
            return SelectUnits(adSettings.maxAds.rewardedAdUnit,
                adSettings.maxAds.rewardedAdUnits, adSettings.maxAds.mode == MaxAdUnitMode.Single, ref _rewardedAdIdCounter);
#else
            return new AdUnit("", "");
#endif
        }

        public AdUnit GetAppOpenID(int index = 0)
        {
            if (testIds) return new AdUnit("", "");
#if USE_ADMOB
            return SelectGoogleUnits(adSettings.googleAds.appOpenAdUnit,
                adSettings.googleAds.appOpenAdUnits, ref _appOpenAdIdCounter, index);
#elif USE_MAXADS
            return SelectUnits(adSettings.maxAds.appOpenAdUnit,
                adSettings.maxAds.appOpenAdUnits, adSettings.maxAds.mode == MaxAdUnitMode.Single, ref _appOpenAdIdCounter);
#else
            return new AdUnit("", "");
#endif
        }

        public AdFloor GetAppOpenAdFloor(int index)
        {
            return adSettings.googleAds.appOpenAdUnits[index].adFloor;
        }
        
        public AdFloor GetMRECAdFloor(int index)
        {
            return adSettings.googleAds.mRecAdUnits[index].adFloor;
        }

        public AdFloor GetInterstitialAdFloor(int index)
        {
            return adSettings.googleAds.interstitialAdUnits[index].adFloor;
        }
        
        public AdFloor GetRewardedInterstitialAdFloor(int index)
        {
            return adSettings.googleAds.rewardedInterstitialAdUnits[index].adFloor;
        }
        
        public AdFloor GetRewardedAdFloor(int index)
        {
            return adSettings.googleAds.rewardedAdUnits[index].adFloor;
        }
        

    }
    
   
}
