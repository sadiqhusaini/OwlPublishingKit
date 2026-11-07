using Owl.Analytics;
using Owl.Analytics.MMP;
using Owl.Monetization;
using Owl.Utility;
using UnityEngine;

namespace Owl
{
    public class OwlBootstrap : MonoBehaviour
    {
        private static OwlBootstrap _instance;

        private InternetManager _internetManager;
        private ConsentManager _consentManager;
        private IAPManager _iapManager;
        private FirebaseController _firebaseController;
        private MMPEventTracker _mmpEventTracker;
        private AnalyticsManager _analyticsManager;
        private AdManager _adManager;

        public static OwlSDKState State { get; private set; }

        public static event System.Action<OwlSDKState> OnStateChanged;

        private bool _started;
        private bool _servicesStarted;
        private bool _consentFinished;
        private bool _analyticsFinished;
        private bool _adsFinished;
        private bool _ready;
        private bool _mmpStarted;
        private bool _facebookStarted;
        private bool _iapFinished;
        private Coroutine _iapWatchdog;
        private Coroutine _watchdog;
        private Coroutine _gameplayWatchdog;
        private float _gameplayDeadline;
        public const float GameplayStartupTimeout = 10f;
        private int _stage;

        // Deadlines release gameplay; late SDK completion may still enable optional services.
        public const float InternetStartupTimeout = 10f;
        public const float ConsentStartupTimeout = 25f;
        public const float AnalyticsStartupTimeout = 15f;
        public const float AdsStartupTimeout = 15f;
        public const float IapStartupTimeout = 10f;
        public const float FirebaseCoreStartupTimeout = 10f;
        public static bool StartupDegraded { get; private set; }
        public static float StartupElapsedSeconds => _instance != null && _instance._started
            ? Mathf.Max(0f, Time.realtimeSinceStartup - _instance._startupStartedAt)
            : 0f;
        public static string CurrentStep => State.ToString();
        private float _startupStartedAt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            State = default;
            StartupDegraded = false;
            OnStateChanged = null;
        }

        private void Awake() => _instance = this;

        private void OnEnable()
        {
            InternetManager.OnOnlineCheckComplete += OnInternetFinished;
            ConsentManager.onConsentResolved += OnConsentFinished;
            ConsentManager.OnCollectionConsentChanged += TryInitializeMmp;
            ConsentManager.OnCollectionConsentChanged += TryInitializeFacebook;
            AnalyticsManager.OnAnalyticsInitialized += OnAnalyticsFinished;
            AdManager.OnAdsInitialized += OnAdsFinished;
            IAPManager.OnIAPInitialized += OnIapInitialized;
            FirebaseController.OnCoreInitialized += OnFirebaseCoreInitialized;
            if (_started && !_ready) ArmGameplayDeadline();
            if (_started && !_iapFinished)
            {
                if (IAPManager.IsInitialized) OnIapInitialized(true);
                else ArmIapTimeout();
            }
            if (_started && _consentFinished) TryInitializeFacebook();
            if (_started && !_adsFinished)
            {
                if (!_servicesStarted)
                {
                    if (InternetManager.IsInternetAvailable) BeginFirebaseCore();
                    else if (!_ready) ArmTimeout(InternetStartupTimeout, ContinueOffline);
                }
                else if (!_consentFinished)
                {
                    if (ConsentManager.IsResolved) OnConsentFinished(ConsentManager.TrackingAuthorized);
                    else ArmTimeout(ConsentStartupTimeout, ConsentTimedOut);
                }
                else if (!_analyticsFinished)
                {
                    if (AnalyticsManager.IsInitialized) OnAnalyticsFinished(true);
                    else ArmTimeout(AnalyticsStartupTimeout, AnalyticsTimedOut);
                }
                else if (AdManager.IsInitialized) OnAdsFinished(true);
                else ArmTimeout(AdsStartupTimeout, AdsTimedOut);
            }
        }

        private void OnDisable()
        {
            InternetManager.OnOnlineCheckComplete -= OnInternetFinished;
            ConsentManager.onConsentResolved -= OnConsentFinished;
            ConsentManager.OnCollectionConsentChanged -= TryInitializeMmp;
            ConsentManager.OnCollectionConsentChanged -= TryInitializeFacebook;
            AnalyticsManager.OnAnalyticsInitialized -= OnAnalyticsFinished;
            AdManager.OnAdsInitialized -= OnAdsFinished;
            IAPManager.OnIAPInitialized -= OnIapInitialized;
            FirebaseController.OnCoreInitialized -= OnFirebaseCoreInitialized;
            CancelTimeout();
            CancelIapTimeout();
            if (_gameplayWatchdog != null) StopCoroutine(_gameplayWatchdog);
            _gameplayWatchdog = null;
        }

        /// <summary>Idempotent startup. Ready means gameplay can proceed, including with fallback services.</summary>
        public void Initialize()
        {
            if (_started) return;
            _started = true;
            _startupStartedAt = Time.realtimeSinceStartup;
            _gameplayDeadline = Time.realtimeSinceStartup + GameplayStartupTimeout;
            ArmGameplayDeadline();
            StartupDegraded = false;
            SetState(OwlSDKState.Booting);
            OwlStatusScript.SetFlowStatus("Startup", "Booting", $"t={StartupElapsedSeconds:0.0}s");
            _iapFinished = false;
            OwlStatusScript.SetFlowStatus("IAP", "Initializing");
            OwlStatusScript.SetFlowStatus("Internet", "Initializing");
            OwlStatusScript.SetFlowStatus("Firebase", "Waiting", "after internet check");
            OwlStatusScript.SetFlowStatus("Consent", "Waiting");
            OwlStatusScript.SetFlowStatus("Analytics", "Waiting");
            OwlStatusScript.SetFlowStatus("Attribution", "Waiting");
            OwlStatusScript.SetFlowStatus("Facebook", "Waiting");
            OwlStatusScript.SetFlowStatus("Ads SDK", "Waiting");
            ArmIapTimeout();
            TryRun(InitializeIapManager, "IAP", null);
            SetState(OwlSDKState.CheckingInternet);
            ArmTimeout(InternetStartupTimeout, ContinueOffline);
            TryRun(InitializeInternetAvailability, "Internet", ContinueOffline);
        }

        private void OnInternetFinished(bool hasInternet)
        {
            OwlStatusScript.SetFlowStatus("Internet", hasInternet ? "Online" : "Offline");
            if (!_started || _servicesStarted || !hasInternet) return;
            BeginFirebaseCore();
        }

        private void BeginFirebaseCore()
        {
            if (_servicesStarted) return;
#if USE_FIREBASE
            if (FirebaseController.IsInitialized)
            {
                OwlStatusScript.SetFlowStatus("Firebase", "Ready", "core already initialized");
                StartConsent();
                return;
            }
            OwlStatusScript.SetFlowStatus("Firebase", "Initializing", "dependency check");
            ArmTimeout(FirebaseCoreStartupTimeout, FirebaseCoreTimedOut);
            TryRun(() =>
            {
                _firebaseController ??= GetComponent<FirebaseController>();
                if (_firebaseController == null)
                    _firebaseController = gameObject.AddComponent<FirebaseController>();
                _firebaseController.Initialize();
            }, "Firebase core", FirebaseCoreTimedOut);
#else
            OwlStatusScript.SetFlowStatus("Firebase", "Skipped", "Firebase define is off");
            StartConsent();
#endif
        }

        private void OnFirebaseCoreInitialized(bool available)
        {
#if USE_FIREBASE
            OwlStatusScript.SetFlowStatus("Firebase", available ? "Ready" : "Failed",
                available ? "core available" : "local defaults will be used");
#else
            OwlStatusScript.SetFlowStatus("Firebase", "Skipped", "Firebase define is off");
#endif
            if (_servicesStarted) return;
            CancelTimeout();
            if (!available)
                StartupDegraded = true;
            StartConsent();
        }

        private void FirebaseCoreTimedOut()
        {
            if (_servicesStarted) return;
            StartupDegraded = true;
            OwlStatusScript.SetFlowStatus("Firebase", "Timeout", "continuing with local defaults");
            CustomDebug.LogWarning("Owl startup: Firebase core deadline reached; continuing to consent with local defaults.");
            StartConsent();
        }

        private void ContinueOffline()
        {
            if (_servicesStarted) return;
            StartupDegraded = true;
            OwlStatusScript.SetFlowStatus("Consent", "Deferred", "waiting for internet");
            OwlStatusScript.SetFlowStatus("Firebase", "Deferred", "waiting for internet");
            OwlStatusScript.SetFlowStatus("Internet", "Offline", "startup continues without network");
            _internetManager?.ContinueOffline();
            CustomDebug.LogWarning("Owl startup: continuing offline; services will start when connectivity returns.");
            FinishInitialization();
        }

        private void StartConsent()
        {
            if (_servicesStarted) return;
            _servicesStarted = true;
            SetStartupState(OwlSDKState.RequestingConsent);
            OwlStatusScript.SetFlowStatus("Consent", "Initializing", "privacy and ad eligibility");
            ArmTimeout(ConsentStartupTimeout, ConsentTimedOut);
            TryRun(() =>
            {
                _consentManager = GetComponent<ConsentManager>() ?? gameObject.AddComponent<ConsentManager>();
                _consentManager.Initialize();
            }, "Consent", ConsentTimedOut);
        }

        private void ConsentTimedOut()
        {
            if (_consentFinished) return;
            StartupDegraded = true;
            OwlStatusScript.SetFlowStatus("Consent", "Timeout", "ads and tracking remain disabled");
            _consentManager?.ResolveWithoutConsent();
            OnConsentFinished(false);
        }

        private void OnConsentFinished(bool trackingGranted)
        {
            if (!_started || !_servicesStarted || _consentFinished) return;
            _consentFinished = true;
            CancelTimeout();
            OwlStatusScript.SetFlowStatus("Consent", "Resolved",
                $"ads={ConsentManager.CanRequestAds}  analytics={ConsentManager.CanCollectAnalytics}  attribution={ConsentManager.CanCollectAttribution}");
            if (!ConsentManager.CanCollectAttribution)
                OwlStatusScript.SetFlowStatus("Attribution", "Skipped", "attribution consent unavailable");
            else if (OwlManager.OwlSettings != null && OwlManager.OwlSettings.mmpsdk == MMPSDK.NoMMP)
                OwlStatusScript.SetFlowStatus("Attribution", "Skipped", "no attribution provider selected");
            TryInitializeFacebook();
            TryInitializeMmp();
            ArmTimeout(AnalyticsStartupTimeout, AnalyticsTimedOut);
            TryRun(InitializeAnalytics, "Analytics", AnalyticsTimedOut);
        }

        private void TryInitializeMmp()
        {
            if (!_consentFinished || _mmpStarted || !ConsentManager.CanCollectAttribution) return;
#if USE_MMP
            _mmpStarted = true;
            OwlStatusScript.SetFlowStatus("Attribution", "Initializing",
                OwlManager.OwlSettings != null ? OwlManager.OwlSettings.mmpsdk.ToString() : "provider setting unavailable");
            TryRun(InitializeMmpTracker, "Attribution", null);
#else
            OwlStatusScript.SetFlowStatus("Attribution", "Skipped", "MMP support is not installed");
#endif
        }

        private void TryInitializeFacebook()
        {
#if USE_FACEBOOK && USE_FACEBOOK_SDK
            if (!_consentFinished || _facebookStarted) return;
            if (!ConsentManager.CanCollectAnalytics || !ConsentManager.CanCollectAttribution)
            {
                OwlStatusScript.SetFlowStatus("Facebook", "Skipped", "analytics and attribution consent required");
                return;
            }
            _facebookStarted = true;
            OwlStatusScript.SetFlowStatus("Facebook", "Initializing");
            TryRun(() =>
            {
                var facebook = GetComponent<Owl.Analytics.FacebookController>() ??
                               gameObject.AddComponent<Owl.Analytics.FacebookController>();
                facebook.Initialize();
            }, "Facebook", null);
#else
            OwlStatusScript.SetFlowStatus("Facebook", "Skipped", "Facebook SDK define is off");
#endif
        }

        private void AnalyticsTimedOut()
        {
            if (_analyticsFinished) return;
            StartupDegraded = true;
            OwlStatusScript.SetFlowStatus("Analytics", "Timeout", "continuing with cached or local controls");
            CustomDebug.LogWarning("Owl startup: analytics deadline reached; continuing with cached/local ad controls.");
            OnAnalyticsFinished(false);
        }

        private void OnAnalyticsFinished(bool success)
        {
            if (!_consentFinished) return;
#if USE_FIREBASE || USE_GAMEANALYTICS
            OwlStatusScript.SetFlowStatus("Analytics", success ? "Ready" : "Degraded",
                OwlManager.OwlSettings != null ? OwlManager.OwlSettings.analyticsSdk.ToString() : string.Empty);
#else
            OwlStatusScript.SetFlowStatus("Analytics", "Skipped", "analytics provider defines are off");
#endif
            if (_analyticsFinished) return;
            _analyticsFinished = true;
            StartupDegraded |= !success;
            CancelTimeout();
            TryStartAds();
        }

        private void OnIapInitialized(bool success)
        {
#if USE_IAP
            OwlStatusScript.SetFlowStatus("IAP", success || IAPManager.IsInitialized ? "Ready" : "Failed",
                success || IAPManager.IsInitialized ? "store ownership resolved" : "ownership check failed");
#else
            OwlStatusScript.SetFlowStatus("IAP", "Skipped", "IAP define is off");
#endif
            if (_iapFinished) return;
            _iapFinished = true;
            CancelIapTimeout();
#if USE_IAP
            if (!success) StartupDegraded = true;
#endif
            TryStartAds();
        }

        private void IapTimedOut()
        {
            if (_iapFinished) return;
            _iapFinished = true;
            StartupDegraded = true;
            OwlStatusScript.SetFlowStatus("IAP", "Timeout", "ads skipped until ownership is known");
            _iapWatchdog = null;
            CustomDebug.LogWarning("Owl startup: IAP ownership check timed out; skipping ads for this session.");
            TryStartAds();
        }

        private void TryStartAds()
        {
            if (!_analyticsFinished || _adsFinished || !_iapFinished) return;
            if (!ConsentManager.CanRequestAds || AdManager.IsRemoveAdsPurchased() || IapOwnershipUnknown())
            {
                var reason = !ConsentManager.CanRequestAds ? "consent does not allow ad requests" :
                    AdManager.IsRemoveAdsPurchased() ? "Remove Ads entitlement is active" : "IAP ownership is unknown";
                OwlStatusScript.SetFlowStatus("Ads SDK", "Skipped", reason);
                _adsFinished = true;
                FinishInitialization();
                return;
            }
            SetStartupState(OwlSDKState.InitializingAds);
            ArmTimeout(AdsStartupTimeout, AdsTimedOut);
            TryRun(InitializeAdManager, "Ads", AdsTimedOut);
        }

        private static bool IapOwnershipUnknown()
        {
#if USE_IAP
            return !IAPManager.IsInitialized;
#else
            return false;
#endif
        }

        private void AdsTimedOut()
        {
            if (_adsFinished) return;
            StartupDegraded = true;
            OwlStatusScript.SetFlowStatus("Ads SDK", "Timeout", "gameplay continues without waiting for ads");
            CustomDebug.LogWarning("Owl startup: ad SDK deadline reached; continuing without waiting for ads.");
            OnAdsFinished(false);
        }

        private void OnAdsFinished(bool success)
        {
            if (!_analyticsFinished) return;
#if USE_ADMOB || USE_MAXADS
            OwlStatusScript.SetFlowStatus("Ads SDK", success ? "Ready" : "Degraded",
                success ? "provider and startup placements configured" : "startup deadline reached");
#else
            OwlStatusScript.SetFlowStatus("Ads SDK", "Skipped", "no ad provider define is active");
#endif
            if (_adsFinished) return;
            _adsFinished = true;
            StartupDegraded |= !success;
            FinishInitialization();
        }

        private void FinishInitialization()
        {
            CancelTimeout();
            if (_ready) return;
            _ready = true;
            if (_gameplayWatchdog != null) StopCoroutine(_gameplayWatchdog);
            _gameplayWatchdog = null;
            SetState(OwlSDKState.Ready);
            OwlManager.SDKInitialized();
        }

        private void ArmGameplayDeadline()
        {
            if (_gameplayWatchdog != null) return;
            _gameplayWatchdog = StartCoroutine(ReleaseGameplay());
        }

        private System.Collections.IEnumerator ReleaseGameplay()
        {
            yield return new WaitForSecondsRealtime(System.Math.Max(0f, _gameplayDeadline - Time.realtimeSinceStartup));
            _gameplayWatchdog = null;
            if (_ready) yield break;
            StartupDegraded = true;
            _ready = true;
            _internetManager?.ContinueOffline();
            SetState(OwlSDKState.Ready);
            OwlManager.SDKInitialized();
            // Keep the current stage watchdog alive so optional services can recover.
        }

        private void SetStartupState(OwlSDKState state)
        {
            // Background recovery must not revoke gameplay readiness.
            if (!_ready) SetState(state);
        }

        private void ArmTimeout(float seconds, System.Action fallback)
        {
            CancelTimeout();
            var stage = _stage;
            _watchdog = StartCoroutine(WaitForDeadline());
            System.Collections.IEnumerator WaitForDeadline()
            {
                yield return new WaitForSecondsRealtime(seconds);
                if (stage != _stage) yield break;
                _watchdog = null;
                fallback();
            }
        }

        private void CancelTimeout()
        {
            _stage++;
            if (_watchdog != null) StopCoroutine(_watchdog);
            _watchdog = null;
        }

        private static void TryRun(System.Action action, string service, System.Action fallback)
        {
            try { action(); }
            catch (System.Exception exception)
            {
                StartupDegraded = true;
                CustomDebug.LogWarning($"Owl startup: {service} failed: {exception.GetBaseException().Message}");
                fallback?.Invoke();
            }
        }

        #region Managers

        private void InitializeInternetAvailability()
        {
            _internetManager ??= GetComponent<InternetManager>();
            if (_internetManager == null)
                _internetManager = gameObject.AddComponent<InternetManager>();
            _internetManager.Initialize();
        }

        private void InitializeIapManager()
        {
#if USE_IAP
            OwlStatusScript.SetFlowStatus("IAP", "Initializing", "store connection and ownership");
            _iapManager ??= GetComponent<IAPManager>();
            if (_iapManager == null)
                _iapManager = gameObject.AddComponent<IAPManager>();
            IAPManager.Initialize(transform);
#else
            OwlStatusScript.SetFlowStatus("IAP", "Skipped", "IAP define is off");
            OnIapInitialized(false);
#endif
        }

        private void ArmIapTimeout()
        {
            CancelIapTimeout();
            _iapWatchdog = StartCoroutine(WaitForIapDeadline());
            System.Collections.IEnumerator WaitForIapDeadline()
            {
                yield return new WaitForSecondsRealtime(IapStartupTimeout);
                IapTimedOut();
            }
        }

        private void CancelIapTimeout()
        {
            if (_iapWatchdog != null) StopCoroutine(_iapWatchdog);
            _iapWatchdog = null;
        }

        private void InitializeMmpTracker()
        {
#if USE_MMP
 _mmpEventTracker ??= GetComponent<MMPEventTracker>();
            if (_mmpEventTracker == null)
                _mmpEventTracker = gameObject.AddComponent<MMPEventTracker>();
            _mmpEventTracker.Initialize();
#endif
        }

        private void InitializeAnalytics()
        {
#if USE_FIREBASE || USE_GAMEANALYTICS
            OwlStatusScript.SetFlowStatus("Analytics", "Initializing",
                OwlManager.OwlSettings != null ? OwlManager.OwlSettings.analyticsSdk.ToString() : string.Empty);
            _analyticsManager ??= GetComponent<AnalyticsManager>();
            if (_analyticsManager == null)
                _analyticsManager = gameObject.AddComponent<AnalyticsManager>();
            _analyticsManager.Initialize();
#else
            OnAnalyticsFinished(true);
#endif
        }

        private void InitializeAdManager()
        {
#if USE_ADMOB || USE_MAXADS
            OwlStatusScript.SetFlowStatus("Ads SDK", "Initializing",
                OwlManager.OwlSettings != null ? OwlManager.OwlSettings.mediation.ToString() : string.Empty);
            _adManager ??= GetComponent<AdManager>();
            if (_adManager == null)
                _adManager = gameObject.AddComponent<AdManager>();
            AdManager.InitializeAd();
#else
             OnAdsFinished(true);
#endif
        }

        #endregion

        #region State

        private void SetState(OwlSDKState state)
        {
            if (State == state)
                return;

            State = state;
            OwlStatusScript.SetFlowStatus("Startup", state.ToString(), StartupDegraded ? "degraded fallback active" : string.Empty);
            var handlers = OnStateChanged;
            if (handlers == null) return;
            foreach (System.Action<OwlSDKState> handler in handlers.GetInvocationList())
            {
                try { handler(state); }
                catch (System.Exception exception) { CustomDebug.LogWarning("Owl state listener failed: " + exception.Message); }
            }
        }

        #endregion
    }
}
