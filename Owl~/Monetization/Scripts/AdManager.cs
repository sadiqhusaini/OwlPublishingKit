using System;
using System.Collections;
using Owl.Analytics;
using UnityEngine;

namespace Owl.Monetization
{
    public class AdManager : MonoBehaviour
    {
        #region Public Fields
        public static event Action<bool> OnAdsInitialized;
        public static bool IsAdManager => OwlManager.IsOwlManager;
        public static bool IsAppOpenEnabled => OwlSettings.useAppOpenAd && _remoteCompleteAds && _remoteAppOpen;
        public static bool IsBannerEnabled => OwlSettings.useBanner && _remoteCompleteAds && _remoteBanner;
        public static bool IsInterstitialEnabled => OwlSettings.useInterstitialAd && _remoteCompleteAds && _remoteInterstitial;
        public static bool IsRewardedEnabled => OwlSettings.useRewardedAd && _remoteCompleteAds && _remoteRewarded;
        public static bool IsRewardedInterstitialEnabled => OwlSettings.useRewardedInterstitialAd && _remoteCompleteAds && _remoteRewardedInterstitial;
        public static bool IsMRECEnabled => OwlSettings.useMREC && _remoteCompleteAds && _remoteMREC;
        public static bool IsTimerAdAvailable => OwlSettings.enableTimerAd && IsInterstitialEnabled && _remoteTimedAds;
        public static bool IsAdNetworkInitialized => _adSdkReady;
        public static bool IsAppOpenPlacementInitialized => _appOpenAdController || _flooringAppOpenAdController;
        public static bool IsBannerPlacementInitialized => _bannerAdController;
        public static bool IsBannerLoaded => _bannerAdController && _bannerAdController.IsAdLoaded;
        public static bool IsMRECPlacementInitialized => _mRecAdController;
        public static bool IsMRECLoaded => _mRecAdController && _mRecAdController.IsAdLoaded;
        public static bool IsInterstitialPlacementInitialized => _interstitialInitialized;
        public static bool IsRewardedInterstitialPlacementInitialized => _rewardedInterstitialInitialized;
        public static bool IsRewardedPlacementInitialized => _rewardedInitialized;
        public static bool IsTimerAdInitialized => _interstitialTimerInitialized;
#if USE_ADMOB
        public static bool IsRewardedInterstitialSupported => true;
#else
        public static bool IsRewardedInterstitialSupported => false;
#endif
        public static BannerPosition BannerPosition;
        #endregion

        #region Private Fields
        private static AdManager _instance;

        private static AppOpenAdController _appOpenAdController;
        private static BannerAdController _bannerAdController;
        private static MRECAdController _mRecAdController;
        private static InterstitialAdController _interstitialAdController;
        private static RewardedInterstitialAdController _rewardedInterstitialAdController;
        private static RewardedAdController _rewardedAdController;


        private static OwlSettings OwlSettings => OwlManager.OwlSettings;
        private static AdSettings _adSettings;
        private static AdComingLayerScript _adComingLayerScript;
        private static BannerBgLayerScript _bannerBgLayerScript;
        private static MRECBgLayerScript _mrecBgLayerScript;


        private static FlooringAppOpenAdController _flooringAppOpenAdController;
        private static FlooringInterstitialAdController _flooringInterstitialAdController;
        private static FlooringRewardedInterstitialAdController _flooringRewardedInterstitialAdController;
        private static FlooringRewardedAdController _flooringRewardedAdController;

        private static bool _startAppOpenCall = false;
        private static bool _appOpenPending;
        private bool _firstAppOpenScheduled;
        private bool _applicationPaused;
        private bool _wasPaused;
        private float _pauseStarted;
        public const float MinimumAppOpenBackgroundSeconds = 30f;
        private static bool _remoteCompleteAds = true;
        private static bool _remoteAppOpen = true;
        private static bool _remoteBanner = true;
        private static bool _remoteInterstitial = true;
        private static bool _remoteRewarded = true;
        private static bool _remoteRewardedInterstitial = true;
        private static bool _remoteMREC = true;
        private static bool _remoteTimedAds = true;
        private static bool _adBreak = false;
        private static bool _appOpenAdBreak = false;


        private static Coroutine _adBreakCol;
        private static Coroutine _appOpenAdBreakCol;
        private static Coroutine _sdkInitializedCol;

        private static bool _initializing;
        private static bool _adSdkReady;
        private static bool _interstitialInitialized;
        private static bool _rewardedInterstitialInitialized;
        private static bool _rewardedInitialized;
        private static bool _interstitialTimerInitialized;
        /// <summary>Ad SDK and placement setup completed. Individual ad availability is separate.</summary>
        public static bool IsInitialized => _initialized;
        private static bool _initialized;


        #endregion

        #region Delegates
        public static Action OnAdOpenedEvent;
        public static Action OnAdClosedEvent;
        public static Action<bool> OnAdBreakLayerDisplayEvent;
        private static Action<bool> _rewardResults;
        private static Action _interstitialClosed;

        #endregion

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            _initialized = _initializing = _adSdkReady = _startAppOpenCall = _appOpenPending = false;
            _interstitialInitialized = _rewardedInterstitialInitialized = _rewardedInitialized = false;
            _interstitialTimerInitialized = false;
            _adBreak = _appOpenAdBreak = false;
            _remoteCompleteAds = _remoteAppOpen = _remoteBanner = _remoteInterstitial = true;
            _remoteRewarded = _remoteRewardedInterstitial = _remoteMREC = _remoteTimedAds = true;
            _appOpenAdController = null;
            _bannerAdController = null;
            _mRecAdController = null;
            _interstitialAdController = null;
            _rewardedInterstitialAdController = null;
            _rewardedAdController = null;
            _flooringAppOpenAdController = null;
            _flooringInterstitialAdController = null;
            _flooringRewardedInterstitialAdController = null;
            _flooringRewardedAdController = null;
            _adSettings = null;
            _adComingLayerScript = null;
            _bannerBgLayerScript = null;
            _mrecBgLayerScript = null;
            _adBreakCol = _appOpenAdBreakCol = _sdkInitializedCol = null;
            OnAdsInitialized = null;
            OnAdOpenedEvent = OnAdClosedEvent = _interstitialClosed = null;
            OnAdBreakLayerDisplayEvent = _rewardResults = null;
            BannerPosition = default;
        }

        private void Awake()
        {
            if (_instance == null)
                _instance = this;
        }

        private void OnEnable()
        {
            OwlManager.OnAppOpenInitializedResult += OnAppOpenInitializedResult;
        }

        private void OnAppOpenInitializedResult()
        {
            if (_startAppOpenCall || _firstAppOpenScheduled || !IsAppOpenAvailable())
                return;
            _firstAppOpenScheduled = true;

            StartCoroutine(OnAppOpenInitializedResultDelay());
            IEnumerator OnAppOpenInitializedResultDelay()
            {
                OwlManager.OwlStatusCanvas?.UpdateStatusInfo("WaitingForAppOpen");
                yield return new WaitForSecondsRealtime(OwlSettings.appOpenStartDelay);
                _startAppOpenCall = true;
                ShowAppOpen("First_Time");
            }
        }

        private void OnDisable()
        {
            OwlManager.OnAppOpenInitializedResult -= OnAppOpenInitializedResult;
        }


        private void OnApplicationPause(bool paused)
        {
            if (_instance != this) return;
            _applicationPaused = paused;
            if (paused)
            {
                if (!_wasPaused) _pauseStarted = Time.realtimeSinceStartup;
                _wasPaused = true;
                return;
            }
            if (!_wasPaused) return;
            _wasPaused = false;
            if (Time.realtimeSinceStartup - _pauseStarted >= MinimumAppOpenBackgroundSeconds)
                ShowAppOpen("App_State_Changed");
        }

        private void OnDestroy()
        {
            if (_instance != this) return;
            StopAllCoroutines();
            try
            {
                CancelAppOpen();
                InterstitialClosed("Owl destroyed");
                RewardGiven(false, "Owl destroyed");
            }
            finally { ResetStatics(); }
        }

        #region AdInitializers


        public static void InitializeRemoteVariables()
        {
            // Reset on every initialization/refresh so opting out restores local control.
            _remoteCompleteAds = true;
            _remoteAppOpen = true;
            _remoteBanner = true;
            _remoteInterstitial = true;
            _remoteRewarded = true;
            _remoteRewardedInterstitial = true;
            _remoteMREC = true;
            _remoteTimedAds = true;
#if USE_FIREBASE && USE_FIREBASEREMOTECONFIG
            _remoteCompleteAds = !OwlSettings.remoteCompleteAds || FirebaseController.GetBoolean(Constants.REMOTECOMPLETEADS, true);
            _remoteAppOpen = !OwlSettings.remoteAppOpen || FirebaseController.GetBoolean(Constants.REMOTEAPPOPEN, true);
            _remoteBanner = !OwlSettings.remoteBanner || FirebaseController.GetBoolean(Constants.REMOTEBANNER, true);
            _remoteInterstitial = !OwlSettings.remoteInterstitial || FirebaseController.GetBoolean(Constants.REMOTEINTERSTITIAL, true);
            _remoteRewarded = !OwlSettings.remoteRewarded || FirebaseController.GetBoolean(Constants.REMOTEREWARDED, true);
            _remoteRewardedInterstitial = !OwlSettings.remoteRewardedInterstitial || FirebaseController.GetBoolean(Constants.REMOTEREWARDEDINTERSTITIAL, true);
            _remoteMREC = !OwlSettings.remoteMREC || FirebaseController.GetBoolean(Constants.REMOTEMREC, true);
            _remoteTimedAds = !OwlSettings.remoteTimedAds || FirebaseController.GetBoolean(Constants.REMOTETIMEDADS, true);
#endif
        }
        public static void InitializeAd()
        {
            if (_initialized)
                return;

            if (IsRemoveAdsPurchased())
            {
                _initialized = true;
                _initializing = false;
                OnAdsInitialized?.Invoke(true);
                return;
            }

            if (!Utility.ConsentManager.CanRequestAds)
                return;

            if (_initializing)
                return;

            _initializing = true;
            OwlManager.OwlStatusCanvas?.UpdateStatusInfo("Initializing Ads");

            InitializeRemoteVariables();
            _adSettings = OwlSettings.adSettings;
            Owl.OwlSettings.ResetAdUnitRotation();

            BannerPosition = OwlSettings.GetBannerPosition();

            _adComingLayerScript ??= OwlSettings.showAdComingUi
                ? OwlSettings.OwlComponents.SpawnAdComingLayerCanvas(OwlSettings.adComingWaitStep)
                : null;
            _bannerBgLayerScript ??= OwlSettings.showBannerBgLayer
                ? OwlSettings.OwlComponents.SpawnBannerBgLayerCanvas()
                : null;
            _mrecBgLayerScript ??= OwlSettings.showMrecBgLayer
                ? OwlSettings.OwlComponents.SpawnMrecBgLayerCanvas()
                : null;

#if USE_ADMOB
            _instance.gameObject.AddComponent<GooglesAdsController>();
#elif USE_MAXADS
            _instance.gameObject.AddComponent<MaxAdsController>();
#else
            _initialized = true;
            _initializing = false;
            OnAdsInitialized?.Invoke(true);
#endif
        }
        public static void AdsInitialized()
        {
            _adSdkReady = true;
            _sdkInitializedCol ??= _instance.StartCoroutine(AdsInitializedDelay());
        }
        private static IEnumerator AdsInitializedDelay()
        {
            OwlManager.OwlStatusCanvas?.UpdateStatusInfo("Ads Initialized");
            OwlStatusScript.SetFlowStatus("Ads SDK", "Ready", "provider initialized; configuring placements");
            yield return new WaitForSecondsRealtime(0.1f);
            InitializeAppOpen();
            yield return new WaitForSecondsRealtime(0.1f);
            InitializeBanner(OwlSettings.bannerOnStart);
            if (!IsBannerEnabled)
                OwlStatusScript.SetFlowStatus("Banner", "Disabled", "disabled by settings or remote config");
            else if (!OwlSettings.bannerOnStart)
                OwlStatusScript.SetFlowStatus("Banner", "Manual", "call ShowBanner to initialize and load");
            yield return new WaitForSecondsRealtime(0.1f);
            if (OwlSettings.interstitialOnStart)
                InitializeInterstitialInternal();
            else
                OwlStatusScript.SetFlowStatus("Interstitial", IsInterstitialEnabled ? "Manual" : "Disabled", "public InitializeInterstitial available");
            yield return new WaitForSecondsRealtime(0.1f);
            if (OwlSettings.rewardedInterstitialOnStart)
                InitializeRewardedInterstitialInternal();
            else
                OwlStatusScript.SetFlowStatus("Rewarded Interstitial", IsRewardedInterstitialEnabled ? "Manual" : "Disabled", "public InitializeRewardedInterstitial available");
            yield return new WaitForSecondsRealtime(0.1f);
            if (OwlSettings.rewardedOnStart)
                InitializeRewardedInternal();
            else
                OwlStatusScript.SetFlowStatus("Rewarded", IsRewardedEnabled ? "Manual" : "Disabled", "public InitializeRewarded available");
            yield return new WaitForSecondsRealtime(0.1f);
            InitializeInterstitialTimer();
            OwlStatusScript.SetFlowStatus("Timer Ads", IsTimerAdAvailable ?
                    (_interstitialTimerInitialized ? "Ready" : "Waiting") : "Disabled",
                IsTimerAdAvailable ? "requires interstitial setup" : "disabled by settings or remote config");
            _initialized = true;
            _initializing = false;
            OwlStatusScript.SetFlowStatus("Ads SDK", "Ready", "ad provider and startup setup complete");

            OnAdsInitialized?.Invoke(true);

            _sdkInitializedCol = null;
        }
        private static void InitializeAppOpen()
        {
            if(!IsAppOpenEnabled)
            {
                OwlStatusScript.SetFlowStatus("App Open", "Disabled", "disabled by settings or remote config");
                return;
            }

            OwlStatusScript.SetFlowStatus("App Open", "Initializing");


            OwlManager.OwlStatusCanvas?.UpdateStatusInfo("InitializeAppOpen");

#if USE_ADMOB
            var floor=new GameObject("FlooringAppOpenAdController");
            floor.transform.parent = _instance.transform;
            _flooringAppOpenAdController = floor.AddComponent<FlooringAppOpenAdController>();

            for (var i = 0; i < (OwlSettings.UsesGoogleFlooring ? _adSettings.googleAds.appOpenAdUnits.Length : 1); i++)
            {
                var af = OwlSettings.UsesGoogleFlooring ? OwlSettings.GetAppOpenAdFloor(i) : AdFloor.All;
                var ad = new GameObject(" AppOpenAdController-" + i);
                ad.transform.parent = _flooringAppOpenAdController.transform;

                _appOpenAdController = ad.AddComponent<AppOpenAdController>();
                _flooringAppOpenAdController.AddAppOpenAdController(_appOpenAdController);
                _appOpenAdController.Initialize(i,af);
            }

            if (OwlSettings.UsesGoogleFlooring && _adSettings.googleAds.appOpenAdUnits.Length == 0)
                OwlStatusScript.SetFlowStatus("App Open", "Failed", "no app open ad units configured");

            return;
#endif
            var appOpen=new GameObject("AppOpenAdController");
            appOpen.transform.parent = _instance.transform;
            _appOpenAdController = appOpen.AddComponent<AppOpenAdController>();
            _appOpenAdController.Initialize();
        }
        private static void InitializeBanner(bool showOnStart)
        {
            if (!Utility.ConsentManager.CanRequestAds)
            {
                OwlStatusScript.SetFlowStatus("Banner", "Deferred", "waiting for ad consent");
                return;
            }
            if(!IsBannerEnabled)
            {
                OwlStatusScript.SetFlowStatus("Banner", "Disabled", "disabled by settings or remote config");
                return;
            }

            if (!showOnStart)
            {
                OwlStatusScript.SetFlowStatus("Banner", "Manual", "call ShowBanner to initialize and load");
                return;
            }

            if (_bannerAdController)
                return;
#if USE_ADMOB || USE_MAXADS
            OwlStatusScript.SetFlowStatus("Banner", "Initializing");
            OwlManager.OwlStatusCanvas?.UpdateStatusInfo("InitializeBanner");
            var banner=new GameObject("BannerAdController");
            banner.transform.parent = _instance.transform;
            _bannerAdController = banner.AddComponent<BannerAdController>();
            _bannerAdController.Initialize();
#endif
        }
        private static void InitializeMREC(BannerPosition position)
        {
            if (!IsMRECEnabled)
            {
                OwlStatusScript.SetFlowStatus("MREC", "Disabled", "disabled by settings or remote config");
                return;
            }
            if (!_initialized || !Utility.ConsentManager.CanRequestAds)
            {
                OwlStatusScript.SetFlowStatus("MREC", "Deferred", "waiting for ad SDK and consent");
                return;
            }

            if (_mRecAdController)
                HideMREC();
#if USE_ADMOB || USE_MAXADS
            OwlStatusScript.SetFlowStatus("MREC", "Initializing");
            OwlManager.OwlStatusCanvas?.UpdateStatusInfo("InitializeMRec");
            var mrec=new GameObject("MRECAdController");
            mrec.transform.parent = _instance.transform;
            _mRecAdController = mrec.AddComponent<MRECAdController>();
            _mRecAdController.SetPosition(position);
#endif
        }
        /// <summary>Initialize and load the interstitial placement after the ad SDK is ready.</summary>
        public static void InitializeInterstitial()
        {
            if (!CanInitializePlacement("Interstitial")) return;
            InitializeInterstitialInternal();
            InitializeInterstitialTimer();
        }

        private static void InitializeInterstitialInternal()
        {
            if (_interstitialInitialized) return;
            if (!IsInterstitialEnabled)
            {
                OwlStatusScript.SetFlowStatus("Interstitial", "Disabled", "disabled by settings or remote config");
                return;
            }
            _interstitialInitialized = true;
#if USE_ADMOB || USE_MAXADS
            OwlStatusScript.SetFlowStatus("Interstitial", "Initializing");
#else
            OwlStatusScript.SetFlowStatus("Interstitial", "Unsupported", "no ad provider define is active");
#endif


            OwlManager.OwlStatusCanvas?.UpdateStatusInfo("InitializeInterstitial");

#if USE_ADMOB
            var floor=new GameObject("FlooringInterstitialAdController");
            floor.transform.parent = _instance.transform;
            _flooringInterstitialAdController = floor.AddComponent<FlooringInterstitialAdController>();

            for (var i = 0; i < (OwlSettings.UsesGoogleFlooring ? _adSettings.googleAds.interstitialAdUnits.Length : 1); i++)
            {
                var af = OwlSettings.UsesGoogleFlooring ? OwlSettings.GetInterstitialAdFloor(i) : AdFloor.All;
                var ad = new GameObject(i + "InterstitialAdController-" + af);
                ad.transform.parent = _flooringInterstitialAdController.transform;
                _interstitialAdController = ad.AddComponent<InterstitialAdController>();
                _flooringInterstitialAdController.AddInterstitialAdController(_interstitialAdController);
                _interstitialAdController.Initialize(i,af);
            }

            return;
#endif
            var inter=new GameObject("InterstitialAdController");
            inter.transform.parent = _instance.transform;
            _interstitialAdController = inter.AddComponent<InterstitialAdController>();
            _interstitialAdController.Initialize();
        }
        /// <summary>Initialize and load rewarded interstitial after the ad SDK is ready (AdMob builds).</summary>
        public static void InitializeRewardedInterstitial()
        {
            if (!CanInitializePlacement("Rewarded Interstitial")) return;
            InitializeRewardedInterstitialInternal();
        }

        private static void InitializeRewardedInterstitialInternal()
        {
            if (_rewardedInterstitialInitialized) return;
            if (!IsRewardedInterstitialEnabled)
            {
                OwlStatusScript.SetFlowStatus("Rewarded Interstitial", "Disabled", "disabled by settings or remote config");
                return;
            }
#if USE_ADMOB
            OwlStatusScript.SetFlowStatus("Rewarded Interstitial", "Initializing");
#else
            OwlStatusScript.SetFlowStatus("Rewarded Interstitial", "Unsupported", "active provider does not support this format");
#endif
            OwlManager.OwlStatusCanvas?.UpdateStatusInfo("InitializeRewardedInterstitial");

#if USE_ADMOB
            _rewardedInterstitialInitialized = true;
            var floor=new GameObject("FlooringRewardedInterstitialAdController");
            floor.transform.parent = _instance.transform;
            _flooringRewardedInterstitialAdController = floor.AddComponent<FlooringRewardedInterstitialAdController>();

            for (var i = 0; i < (OwlSettings.UsesGoogleFlooring ? _adSettings.googleAds.rewardedInterstitialAdUnits.Length : 1); i++)
            {
                var af = OwlSettings.UsesGoogleFlooring ? OwlSettings.GetRewardedInterstitialAdFloor(i) : AdFloor.All;
                var ad = new GameObject(" RewardedInterstitialAdController-" + i);
                ad.transform.parent = _flooringRewardedInterstitialAdController.transform;

                _rewardedInterstitialAdController = ad.AddComponent<RewardedInterstitialAdController>();
                _flooringRewardedInterstitialAdController.AddRewardedInterstitialAdController(_rewardedInterstitialAdController);
                _rewardedInterstitialAdController.Initialize(i,af);
            }
#else
            _rewardedInterstitialInitialized = true;
            CustomDebug.LogWarning("Rewarded Interstitial is not implemented for the active ad provider. Use AdMob to initialize this placement.");
#endif
        }
        /// <summary>Initialize and load rewarded ads after the ad SDK is ready.</summary>
        public static void InitializeRewarded()
        {
            if (!CanInitializePlacement("Rewarded")) return;
            InitializeRewardedInternal();
        }

        private static void InitializeRewardedInternal()
        {
            if (_rewardedInitialized) return;
            if (!IsRewardedEnabled)
            {
                OwlStatusScript.SetFlowStatus("Rewarded", "Disabled", "disabled by settings or remote config");
                return;
            }
            _rewardedInitialized = true;
#if USE_ADMOB || USE_MAXADS
            OwlStatusScript.SetFlowStatus("Rewarded", "Initializing");
#else
            OwlStatusScript.SetFlowStatus("Rewarded", "Unsupported", "no ad provider define is active");
#endif
            OwlManager.OwlStatusCanvas?.UpdateStatusInfo("InitializeRewarded");

#if USE_ADMOB
            var floor=new GameObject("FlooringRewardedAdController");
            floor.transform.parent = _instance.transform;
            _flooringRewardedAdController = floor.AddComponent<FlooringRewardedAdController>();

            for (var i = 0; i < (OwlSettings.UsesGoogleFlooring ? _adSettings.googleAds.rewardedAdUnits.Length : 1); i++)
            {

                var af = OwlSettings.UsesGoogleFlooring ? OwlSettings.GetRewardedAdFloor(i) : AdFloor.All;
                var ad = new GameObject(" RewardedAdController-" + i);
                ad.transform.parent = _flooringRewardedAdController.transform;


                _rewardedAdController = ad.AddComponent<RewardedAdController>();
                _flooringRewardedAdController.AddRewardedAdController(_rewardedAdController);
                _rewardedAdController.Initialize(i,af);
            }
            return;
#endif
            var rewarded=new GameObject("RewardedAdController");
            rewarded.transform.parent = _instance.transform;
            _rewardedAdController = rewarded.AddComponent<RewardedAdController>();
            _rewardedAdController.Initialize();
        }

        private static bool CanInitializePlacement(string placement)
        {
            if (!_adSdkReady || !_initialized)
            {
                CustomDebug.LogWarning($"Cannot initialize {placement} ads before AdManager.OnAdsInitialized. Call the public initializer after that event.");
                return false;
            }

            if (!Utility.ConsentManager.CanRequestAds || IsRemoveAdsPurchased())
            {
                CustomDebug.Log($"Skipping {placement} initialization because ads are not currently eligible.");
                return false;
            }

            return true;
        }

        private static void InitializeInterstitialTimer()
        {
            if (_interstitialTimerInitialized) return;
            if (!IsTimerAdAvailable)
            {
                OwlStatusScript.SetFlowStatus("Timer Ads", "Disabled", "disabled by settings or remote config");
                return;
            }
            if (!_interstitialInitialized)
            {
                OwlStatusScript.SetFlowStatus("Timer Ads", "Deferred", "waiting for interstitial initialization");
                return;
            }
            OwlStatusScript.SetFlowStatus("Timer Ads", "Initializing");
            OwlManager.OwlStatusCanvas?.UpdateStatusInfo("InitializeInterstitialTimer");
            _instance.gameObject.AddComponent<InterstitialTimerAdController>();
            InterstitialTimerAdController.Initialize(OwlSettings.autoStartTimerAd,  OwlSettings.timerAdInterval);
            _interstitialTimerInitialized = true;
        }
        #endregion

        #region AdBackgrounds
        public static void ChangeBannerPosition(BannerPosition position)
        {
            if (_bannerAdController==null)
            {
                return;
            }

            BannerPosition = position;
            _bannerAdController.ChangeBannerPosition(BannerPosition);
            EnableBannerBg();
        }
        public static void EnableBannerBg()
        {
            if (_bannerBgLayerScript==null)
                return;
            _bannerBgLayerScript.EnableBannerBg(BannerPosition);
        }
        public static void HideBannerBg()
        {
            if (_bannerBgLayerScript==null)
                return;
            _bannerBgLayerScript.DisableBannerBg();
        }
        public static void EnableMRECBg(BannerPosition _pos)
        {
            if (_mrecBgLayerScript==null)
                return;
            _mrecBgLayerScript.EnableMRECBg(_pos);
        }

        private static void HideMrecBg()
        {
            _mrecBgLayerScript?.DisableMRECBg();
        }
        #endregion

        private static void ShowAdBreakLayer(bool available, Action showAd)
        {
            if (_adComingLayerScript != null)
                _adComingLayerScript.ShowAdBreakLayer(available, showAd);
            else
                showAd?.Invoke();
        }

        #region AdCalls
        public static void ShowAppOpen(string placement)
        {
            if (_instance == null || _instance._applicationPaused) return;
            if (_appOpenPending || _interstitialClosed != null || _rewardResults != null)
                return;
            if (!Utility.InternetManager.IsInternetAvailable)
                return;
            if (IsRemoveAdsPurchased())
                return;
            if (IAPManager.IAPInProgress)
                return;
            if(_appOpenAdBreak)
                return;
            if(!_startAppOpenCall)
                return;
            if(!IsAppOpenEnabled)
                return;

            OwlManager.OwlStatusCanvas?.UpdateStatusInfo("ReadyToShow AppOpen: "+placement);

#if USE_ADMOB
            if (!_flooringAppOpenAdController || !_flooringAppOpenAdController.IsAppOpenAvailable())
                return;
#else
            if (!_appOpenAdController || !_appOpenAdController.IsAppOpenAvailable())
                return;
#endif
            // Reserve fullscreen ownership before the countdown can yield.
            _appOpenPending = true;
            ShowAdBreakLayer(true, () =>
            {
                if (_instance == null || _instance._applicationPaused || !Utility.InternetManager.IsInternetAvailable || IsRemoveAdsPurchased() ||
                    !Utility.ConsentManager.CanRequestAds || IAPManager.IAPInProgress || !IsAppOpenEnabled)
                {
                    CancelAppOpen();
                    return;
                }
#if USE_ADMOB
                if (_flooringAppOpenAdController && _flooringAppOpenAdController.IsAppOpenAvailable())
                    _flooringAppOpenAdController.ShowAppOpen(placement);
#else
                if (_appOpenAdController && _appOpenAdController.IsAppOpenAvailable())
                    _appOpenAdController.ShowAd(placement);
#endif
                else
                    CancelAppOpen();
            });
        }

        private static void CancelAppOpen()
        {
            _appOpenPending = false;
            _adComingLayerScript?.DisableAdLayer();
        }

        public static void ShowBanner()
        {
            if (IsRemoveBannerAdsPurchased())
                return;
            if (!IsAdManager)
                return;
            if (!Utility.InternetManager.IsInternetAvailable)
                return;
            if (!OwlManager.OwlInitialized)
                return;
            if(!IsBannerEnabled)
                return;
            if (_bannerAdController == null && !OwlSettings.bannerOnStart)
                OwlStatusScript.SetFlowStatus("Banner", "Initializing", "requested through ShowBanner");
            if (!OwlSettings.bannerOnStart && _bannerAdController==null)
            {
                InitializeBanner(true);
                return;
            }
            if (_bannerAdController==null)
                return;

            _bannerAdController.gameObject.SetActive(true);
            _bannerAdController.ShowAd();
        }
        private static void ShowBannerPartial()
        {
            if (IsRemoveBannerAdsPurchased())
                return;

            if (!Utility.InternetManager.IsInternetAvailable)
                return;

            if(!IsBannerEnabled)
                return;

            if (_bannerAdController==null)
                return;

            if (_bannerAdController.gameObject.activeInHierarchy)
                _bannerAdController.ShowAd();
        }
        public static void HideBanner()
        {
            if (_bannerAdController==null)
                return;

            _bannerAdController.HideAd(true);
            _bannerAdController.gameObject.SetActive(false);
        }
        private static void HideBannerPartial()
        {
            if (_bannerAdController==null)
                return;

            if (_bannerAdController.gameObject.activeInHierarchy)
            {
                _bannerAdController.HideAd(false);
            }
        }
        public static void ShowMREC(BannerPosition position)
        {
            if (!IsMRECEnabled) return;
            if (IsRemoveBannerAdsPurchased())
                return;

            if (!IsAdManager)
                return;


            if (!Utility.InternetManager.IsInternetAvailable)
                return;


            InitializeMREC(position);


        }
        private static void ShowPartialMREC()
        {
            if (IsRemoveBannerAdsPurchased())
                return;

            if (!Utility.InternetManager.IsInternetAvailable)
                return;

            if(!IsMRECEnabled)
                return;

            if (_mRecAdController==null)
                return;

            _mRecAdController.ShowAd();
        }
        public static void HideMREC()
        {
            HideMrecBg();

            if (_mRecAdController)
            {
                Destroy(_mRecAdController.gameObject);
            }
        }
        private static void HideMrecPartial()
        {
            if (_mRecAdController==null)
                return;

            _mRecAdController.HideAd();
        }
        public static void ShowInterstitial()
        {
            ShowInterstitial("DirectInterstitial", () => { });
        }
        public static void ShowInterstitial(string placement, Action onComplete=null)
        {
            if (!IsInterstitialEnabled)
            {
                onComplete?.Invoke();
                return;
            }

            if (_appOpenPending || _interstitialClosed != null || _rewardResults != null)
            {
                onComplete?.Invoke();
                return;
            }
            _interstitialClosed = onComplete ?? (() => { });

            if (!Utility.InternetManager.IsInternetAvailable || !IsAdManager || IsRemoveAdsPurchased() || _adBreak)
            {
                InterstitialClosed("NotAvailable Interstitial: "+placement);
                return;
            }


            OwlManager.OwlStatusCanvas?.UpdateStatusInfo("ReadyToShow Interstitial: "+placement);
#if USE_ADMOB
            if (_flooringInterstitialAdController)
                ShowAdBreakLayer(_flooringInterstitialAdController.IsInterstitialAvailable(),() =>
                {
                    if (IsInterstitialEnabled)
                        _flooringInterstitialAdController?.ShowInterstitial(placement);
                    else
                        InterstitialClosed("Disabled Interstitial: " + placement);
                });
            else
                InterstitialClosed("NotAvailable Interstitial: " + placement);
            return;
#endif
            if (_interstitialAdController)
                ShowAdBreakLayer(_interstitialAdController.IsInterstitialAvailable(),() =>
                {
                    if (IsInterstitialEnabled && _interstitialAdController.IsInterstitialAvailable())
                        _interstitialAdController.ShowAd(placement);
                    else
                        InterstitialClosed("NotAvailable Interstitial: " + placement);
                });
            else
                InterstitialClosed("NotAvailable Interstitial: " + placement);
        }
        public static void ShowRewardedInterstitial()
        {
            ShowRewardedInterstitial("DirectRewardedInterstitial", b => { });
        }
        public static void ShowRewardedInterstitial(string placement, Action<bool> rewardedAction=null)
        {
            if (!IsRewardedInterstitialEnabled)
            {
                rewardedAction?.Invoke(false);
                return;
            }


            if (_appOpenPending || _rewardResults != null || _interstitialClosed != null)
            {
                rewardedAction?.Invoke(false);
                return;
            }
            _rewardResults = rewardedAction ?? (_ => { });

            if (!IsAdManager || !Utility.InternetManager.IsInternetAvailable)
            {
                RewardGiven(false,placement);
                return;
            }


#if USE_ADMOB
            OwlManager.OwlStatusCanvas?.UpdateStatusInfo("ReadyToShow RewardedInterstitial: "+placement);
            if (_flooringRewardedInterstitialAdController)
                ShowAdBreakLayer(_flooringRewardedInterstitialAdController.IsRewardedInterstitialAvailable(),() =>
                {
                    if (IsRewardedInterstitialEnabled)
                        _flooringRewardedInterstitialAdController?.ShowRewardedInterstitial(placement);
                    else
                        RewardGiven(false, placement);
                });
            else
                RewardGiven(false, placement);
#else
            RewardGiven(false, placement);
#endif
        }
        public static void ShowRewardedVideo()
        {
            ShowRewardedVideo("DirectRewardedAd", b => { });
        }
        public static void ShowRewardedVideo(string placement, Action<bool> rewardedAction=null)
        {
            if (!IsRewardedEnabled)
            {
                rewardedAction?.Invoke(false);
                return;
            }

            if (_appOpenPending || _rewardResults != null || _interstitialClosed != null)
            {
                rewardedAction?.Invoke(false);
                return;
            }
            _rewardResults = rewardedAction ?? (_ => { });

            if (!IsAdManager || !Utility.InternetManager.IsInternetAvailable)
            {
                RewardGiven(false,placement);
                return;
            }

            OwlManager.OwlStatusCanvas?.UpdateStatusInfo("ReadyToShow RewardedVideo: "+placement);

#if USE_ADMOB
            if (_flooringRewardedAdController)
                ShowAdBreakLayer(_flooringRewardedAdController.IsRewardedAvailable(),() =>
                {
                    if (IsRewardedEnabled)
                        _flooringRewardedAdController?.ShowRewardedVideo(placement,_flooringInterstitialAdController,_flooringRewardedInterstitialAdController);
                    else
                        RewardGiven(false, placement);
                });
            else
                RewardGiven(false, placement);
            return;
#endif

            if (_rewardedAdController)
            {
                if (_rewardedAdController.IsRewardedAvailable())
                {
                    ShowAdBreakLayer(_rewardedAdController.IsRewardedAvailable(),() =>
                    {
                        if (IsRewardedEnabled && _rewardedAdController.IsRewardedAvailable())
                            _rewardedAdController.ShowAd(placement);
                        else
                            RewardGiven(false, placement);
                    });
                }
                else if (IsInterstitialEnabled && _interstitialAdController != null && _interstitialAdController.IsInterstitialAvailable())
                {
                    ShowAdBreakLayer(_interstitialAdController.IsInterstitialAvailable(),() =>
                    {
                        if (IsRewardedEnabled && IsInterstitialEnabled && _interstitialAdController.IsInterstitialAvailable())
                            _interstitialAdController.ShowAd(placement);
                        else
                            RewardGiven(false, placement);
                    });
                }
                else
                {
                    RewardGiven(false,placement);
                }
            }
            else
                RewardGiven(false, placement);
        }
        public static bool IsInterstitialAvailable()
        {
            if (!IsInterstitialEnabled) return false;
#if USE_ADMOB
            if(_flooringInterstitialAdController)
                return  _flooringInterstitialAdController.IsInterstitialAvailable();
            return false;
#endif

            if (_interstitialAdController)
                return _interstitialAdController.IsInterstitialAvailable();

            return false;
        }
        public static bool IsAppOpenAvailable()
        {
            if (!IsAppOpenEnabled) return false;
#if USE_ADMOB
            return _flooringAppOpenAdController && _flooringAppOpenAdController.IsAppOpenAvailable();
#else
            return _appOpenAdController && _appOpenAdController.IsAppOpenAvailable();
#endif
        }

        public static bool IsRewardedInterstitialAvailable()
        {
            if (!IsRewardedInterstitialEnabled) return false;
#if USE_ADMOB
            if(_flooringRewardedInterstitialAdController)
                return  _flooringRewardedInterstitialAdController.IsRewardedInterstitialAvailable();

            return false;
#endif
            return false;
        }
        public static bool IsRewardedVideoAvailable()
        {
            if (!IsRewardedEnabled) return false;
#if USE_ADMOB
            if(_flooringRewardedAdController)
                return  _flooringRewardedAdController.IsRewardedAvailable();

            return false;
#endif
            if (_rewardedAdController)
                return _rewardedAdController.IsRewardedAvailable();

            return false;
        }
        public static void CompleteInterstitial(bool success, string placement)
        {
            // Detach both request callbacks before invoking game code that may request another ad.
            var closed = _interstitialClosed;
            var reward = _rewardResults;
            _interstitialClosed = null;
            _rewardResults = null;
            try { closed?.Invoke(); }
            catch (Exception exception) { CustomDebug.LogWarning("Interstitial listener failed: " + exception.Message); }
            try { reward?.Invoke(success); }
            catch (Exception exception) { CustomDebug.LogWarning("Reward listener failed: " + exception.Message); }
        }

        public static void InterstitialClosed(string status)
        {
            try
            {
                OwlManager.OwlStatusCanvas?.UpdateStatusInfo(status);

                if (_interstitialClosed != null)
                {
                    var callback = _interstitialClosed;
                    _interstitialClosed = null;
                    callback.Invoke();
                }
            }
            catch (Exception e)
            {
                FirebaseController.LogException(e);
            }

        }
        public static void RewardGiven(bool given,string placement)
        {
            try
            {
                if (_rewardResults != null)
                {
                    OwlManager.OwlStatusCanvas?.UpdateStatusInfo($"RewardGiven: {placement}  status:{given}");
                    var callback = _rewardResults;
                    _rewardResults = null;
                    callback.Invoke(given);
                }
            }
            catch (Exception e)
            {
                FirebaseController.LogException(e);
            }

        }
        #endregion

        #region TimerAdMethods
        public static void StartTimerAd()
        {
            if (!IsAdManager)
                return;

            if (IsTimerAdAvailable)
                InterstitialTimerAdController.StartTimerAd();
        }
        public static void StopTimerAd()
        {
            if (!IsAdManager)
                return;

            if (OwlSettings.enableTimerAd)
                InterstitialTimerAdController.StopTimerAd();
        }
        public static void ResetTimerAd()
        {
            if (!IsAdManager)
                return;

            if (OwlSettings.enableTimerAd)
                InterstitialTimerAdController.ResetTimerAd();
        }

        #endregion

        #region IAP
        public static void PurchaseRemoveAds()
        {
            PlayerPrefs.SetInt(Constants.REMOVE_ADS, 1);
            if (!IsAdManager)
                return;

            PurchaseRemoverBannerAds();
            HideMrecBg();

            if (_mRecAdController)
            {
                Destroy(_mRecAdController.gameObject);
            }
        }

        public static void PurchaseRemoverBannerAds()
        {
            if (!IsAdManager) return;
            PlayerPrefs.SetInt(Constants.REMOVE_BANNER_ADS, 1);

            if (_bannerAdController)
                _bannerAdController.HideAd(isBgHide:true);
        }

        public static bool IsRemoveAdsPurchased()
        {
            return PlayerPrefs.GetInt(Constants.REMOVE_ADS, 0) == 1 ? true : false;
        }

        public static bool IsRemoveBannerAdsPurchased()
        {
            return PlayerPrefs.GetInt(Constants.REMOVE_BANNER_ADS, 0) == 1 ? true : false;
        }
        #endregion

        #region CallBacks
        public static void OnAdOpen()
        {

            HideBannerPartial();
            HideMrecPartial();


            StartAppOpenAdBreak();
            OnAdOpenedEvent?.Invoke();
        }
        public static void OnAdClose(bool isInter,bool isRv)
        {
            _appOpenPending = false;
            _adComingLayerScript?.DisableAdLayer();

            ShowBannerPartial();
            ShowPartialMREC();
            StartAppOpenAdBreak();

            if (isRv && OwlSettings.resetIntervalOnRewarded)
            {
                StartAdBreak();
            }
            else if (isInter)
            {
                StartAdBreak();
            }
            OnAdClosedEvent?.Invoke();
        }
        #endregion

        #region OtherMethods
        private static void StartAdBreak()
        {
            if (_adBreakCol!=null)
            {
                _instance.StopCoroutine(_adBreakCol);
            }
            _adBreakCol= _instance.StartCoroutine(StartAdBreakDelay());
        }
        private static  IEnumerator StartAdBreakDelay()
        {
            _adBreak = true;
            yield return new WaitForSecondsRealtime(OwlSettings.interstitialInterval);
            _adBreak = false;
            _adBreakCol = null;
        }
        private static void StartAppOpenAdBreak()
        {
            if (_appOpenAdBreakCol!=null)
            {
                _instance.StopCoroutine(_appOpenAdBreakCol);
            }
            _appOpenAdBreakCol= _instance.StartCoroutine(StartAppOpenAdBreakDelay());
        }
        private static  IEnumerator StartAppOpenAdBreakDelay()
        {
            _appOpenAdBreak = true;
            yield return new WaitForSecondsRealtime(5);
            _appOpenAdBreak = false;
            _appOpenAdBreakCol = null;
        }

        #endregion
    }

}