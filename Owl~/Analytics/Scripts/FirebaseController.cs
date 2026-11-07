using System;
#if USE_FIREBASE
using System.Threading.Tasks;
using Firebase.Extensions;
#endif
using UnityEngine;
using Owl.Utility;

#if USE_FIREBASEREMOTECONFIG
using Firebase.Extensions;
using Firebase.RemoteConfig;
#endif

#if USE_FIREBASECRASHLYTICS
using Firebase.Crashlytics;
#endif

namespace Owl.Analytics{
    
    
    public class FirebaseController : MonoBehaviour
    {
        private static FirebaseController _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _instance = null;
            IsReady = false;
            OnCoreInitialized = null;
            OnRemoteDataRetrieved = null;
#if USE_FIREBASE
            DependencyStatus = Firebase.DependencyStatus.UnavailableOther;
            OnFirebaseInitialize = null;
#if USE_FIREBASEREMOTECONFIG
            RemoteDataStatus = RemoteDataStatus.Unsuccessful;
#endif
#endif
        }

        private void Awake()
        {
            _instance = this;
        }

        public static bool IsReady { get; private set; }
        public static bool IsInitialized => _instance != null && _instance._initialized;
        public static bool IsProductsInitialized => _instance != null && _instance._productsInitialized;
        public static bool IsAnalyticsConfigured => _instance != null && _instance._analyticsConfigured;
        public static bool IsCrashlyticsInitialized => _instance != null && _instance._crashlyticsInitialized;
        public static string CoreDiagnosticState
        {
            get
            {
#if USE_FIREBASE
                return _instance == null ? "Waiting" : _instance._initializing ? "Checking dependencies" :
                    !_instance._initialized ? "Not started" : DependencyStatus.ToString();
#else
                return "Not installed";
#endif
            }
        }
        public static string RemoteConfigDiagnosticState
        {
            get
            {
#if USE_FIREBASEREMOTECONFIG
                return RemoteDataStatus.ToString();
#else
                return "Not installed";
#endif
            }
        }
        public static bool IsRemoteConfigReady
        {
            get
            {
#if USE_FIREBASEREMOTECONFIG
                return RemoteDataStatus == RemoteDataStatus.LoadedAndReady;
#else
                return false;
#endif
            }
        }
        public static event Action<bool> OnCoreInitialized;
        private bool _analyticsConfigured;
        private bool _crashlyticsInitialized;
        private bool _productsInitialized;

        private void OnEnable() => ConsentManager.OnCollectionConsentChanged += ApplyAnalyticsConsent;
        private void OnDisable() => ConsentManager.OnCollectionConsentChanged -= ApplyAnalyticsConsent;

        private void ApplyAnalyticsConsent()
        {
#if USE_FIREBASEANALYTICS
            if (!_analyticsConfigured) return;
            try { Firebase.Analytics.FirebaseAnalytics.SetAnalyticsCollectionEnabled(ConsentManager.CanCollectAnalytics); }
            catch (Exception exception) { CustomDebug.LogWarning("Firebase analytics consent failed: " + exception.GetBaseException().Message); }
#endif
        }
#if USE_FIREBASE
        [SerializeField] private Firebase.DependencyStatus _dependencyStatus;
        private static Firebase.DependencyStatus DependencyStatus = Firebase.DependencyStatus.UnavailableOther;
        
#if USE_FIREBASEREMOTECONFIG
        [SerializeField] private RemoteDataStatus _remoteDataStatus;
        private static RemoteDataStatus RemoteDataStatus = RemoteDataStatus.Unsuccessful;
#endif
#endif
        
#if USE_FIREBASE
        public static event Action<Firebase.DependencyStatus> OnFirebaseInitialize;
#endif
        public static event Action<RemoteDataStatus> OnRemoteDataRetrieved;
        
        private bool _initializing;
        private bool _initialized;
        
        public void Initialize()
        {
            if (_initialized)
            {
#if USE_FIREBASE
                OnFirebaseInitialize?.Invoke(DependencyStatus);
#endif
                return;
            }

            if (_initializing)
                return;

            _initializing = true;
            
            
#if USE_FIREBASE
            try
            {
                Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
                {
                    var status = Firebase.DependencyStatus.UnavailableOther;
                    if (task.IsCanceled || task.IsFaulted)
                    {
                        CustomDebug.LogError("Firebase dependency check failed: " + task.Exception);
                    }
                    else
                    {
                        status = task.Result;
                        if (status == Firebase.DependencyStatus.Available)
                        {
                            try
                            {
                                _ = Firebase.FirebaseApp.DefaultInstance;
                            }
                            catch (Exception e)
                            {
                                CustomDebug.LogError("Firebase app creation failed: " + e.GetBaseException());
                                status = Firebase.DependencyStatus.UnavailableOther;
                            }
                        }
                    }

                    if (this == null) return;
                    IsReady = status == Firebase.DependencyStatus.Available;
                    _dependencyStatus = DependencyStatus = status;
                    OwlManager.OwlStatusCanvas?.UpdateStatusInfo($"Firebase Initializing result: {DependencyStatus}");
                    if (status == Firebase.DependencyStatus.Available)
                        InitializeMessaging();
                    _initialized = true;
                    _initializing = false;
                    OnFirebaseInitialize?.Invoke(DependencyStatus);
                    OnCoreInitialized?.Invoke(IsReady);
                });
            }
            catch (Exception e)
            {
                _initializing = false;
                CustomDebug.LogError("Firebase initialization failed: " + e.GetBaseException());
                IsReady = false;
                DependencyStatus = Firebase.DependencyStatus.UnavailableOther;
                _dependencyStatus = DependencyStatus;
                _initialized = true;
                OnFirebaseInitialize?.Invoke(Firebase.DependencyStatus.UnavailableOther);
                OnCoreInitialized?.Invoke(false);
            }
#endif
        }

        /// <summary>Starts consent-gated Firebase products after the core dependency check.</summary>
        public void InitializeProducts()
        {
#if USE_FIREBASE
            if (!_initialized || !IsReady || _productsInitialized) return;
            _productsInitialized = true;
            OwlManager.OwlStatusCanvas?.UpdateStatusInfo($"InitializeFirebaseProducts");
            InitAnalytics();
            InitCrashlytics();
            InitRemoteConfig();
#endif
        }

        private void InitializeMessaging()
        {
#if USE_FIREBASEMESSAGING
            var settings = OwlManager.OwlSettings;
            if (settings == null || !settings.useFirebaseMessaging) return;
            var messaging = GetComponent<Owl.Notifications.FirebaseMessagingService>();
            if (messaging == null) messaging = gameObject.AddComponent<Owl.Notifications.FirebaseMessagingService>();
            messaging.Initialize(settings.requestPushPermissionOnStart);
#endif
        }

        #region FirebaseAnalytics

        private void InitAnalytics()
        {
#if USE_FIREBASEANALYTICS
            OwlManager.OwlStatusCanvas?.UpdateStatusInfo("Firebase Product(Analytics): Initializing");

            try
            {
                _analyticsConfigured = true;
                ApplyAnalyticsConsent();
                if (!ConsentManager.CanCollectAnalytics) return;
                Firebase.Analytics.FirebaseAnalytics.SetUserProperty(
                    Firebase.Analytics.FirebaseAnalytics.UserPropertySignUpMethod, "Google");
                Firebase.Analytics.FirebaseAnalytics.SetUserProperty(
                    Firebase.Analytics.FirebaseAnalytics.ParameterCampaign, "Google");
                Firebase.Analytics.FirebaseAnalytics.SetSessionTimeoutDuration(
                    new TimeSpan(0, 30, 0));
            }
            catch (Exception e)
            {
                Log("Firebase Analytics initialization failed: " + e.GetBaseException());
            }
#endif
        }

#if USE_MAXADS
        public static void TrackAdImpression(MaxSdkBase.AdInfo impressionData)
        {
#if USE_FIREBASE
#if USE_FIREBASEANALYTICS
            if (ConsentManager.CanCollectAnalytics)
            {
            var revenue = impressionData.Revenue;
            var impressionParameters = new[] {
                new Firebase.Analytics.Parameter("ad_platform", "AppLovin"),
                new Firebase.Analytics.Parameter("ad_source", impressionData.NetworkName),
                new Firebase.Analytics.Parameter("ad_unit_name", impressionData.AdUnitIdentifier),
                new Firebase.Analytics.Parameter("ad_format", impressionData.AdFormat),
                new Firebase.Analytics.Parameter("value", revenue),
                new Firebase.Analytics.Parameter("currency", "USD"), // All AppLovin revenue is sent in USD
            };
            Firebase.Analytics.FirebaseAnalytics.LogEvent("ad_impression", impressionParameters);
            }
#endif
#endif
            MMP.MMPEventTracker.OnAdRevenuePaidEvent(impressionData);
        }
        
        public static void TrackAdClick(MaxSdkBase.AdInfo impressionData)
        {
            MMP.MMPEventTracker.OnAdClickEvent(impressionData);
        }
        
#endif
        #endregion

        #region FirebaseRemoteConfig

        private static void InitRemoteConfig()
        {
            
#if USE_FIREBASEREMOTECONFIG
            try
            {
              _instance.FetchDataAsync();
            }
            catch (Exception e)
            {
                Log("Remote Config initialization failed: " + e.GetBaseException());
            }
#endif
        }
        
        
#if USE_FIREBASE && USE_FIREBASEREMOTECONFIG
        private System.Threading.Tasks.Task FetchDataAsync()
        {
            try
            {
                OwlManager.OwlStatusCanvas?.UpdateStatusInfo($"Firebase Product(RemoteConfig): Initializing");
                
                CustomDebug.Log("Remote Config: Fetching values.");
                var fetchTask = FirebaseRemoteConfig.DefaultInstance.FetchAsync(TimeSpan.Zero);
                return fetchTask.ContinueWithOnMainThread(FetchComplete);
            }
            catch (Exception e)
            {
                CustomDebug.LogError($"Remote Config fetch failed: {e}");
                _remoteDataStatus = RemoteDataStatus=RemoteDataStatus.Unsuccessful;
                OnRemoteDataRetrieved?.Invoke(_remoteDataStatus);
                throw;
            }
        } 

        private void FetchComplete(System.Threading.Tasks.Task fetchTask)
        {
            try
            {
                if (!fetchTask.IsCompleted)
                {
                    _remoteDataStatus = RemoteDataStatus=RemoteDataStatus.RetrievalNotFinished;
                    OnRemoteDataRetrieved?.Invoke(_remoteDataStatus);
                    CustomDebug.LogWarning("Remote Config fetch has not completed; using defaults.");
                    return;
                }

                var remoteConfig = FirebaseRemoteConfig.DefaultInstance;
                var info = remoteConfig.Info;
                if (info.LastFetchStatus != LastFetchStatus.Success)
                {
                    _remoteDataStatus = RemoteDataStatus=RemoteDataStatus.Unsuccessful;
                    OnRemoteDataRetrieved?.Invoke(_remoteDataStatus);
                    
                    CustomDebug.LogWarning($"Remote Config fetch failed ({info.LastFetchStatus}); using defaults.");
                    return;
                }
                // Fetch successful. Parameter values must be activated to use.
                remoteConfig.ActivateAsync()
                    .ContinueWithOnMainThread(
                        task =>
                        {
                            if (task.IsFaulted || task.IsCanceled)
                            {
                                _remoteDataStatus = RemoteDataStatus = RemoteDataStatus.Unsuccessful;
                                CustomDebug.LogWarning($"Remote Config activation did not complete: {task.Exception}");
                                OnRemoteDataRetrieved?.Invoke(_remoteDataStatus);
                                return;
                            }
                            _remoteDataStatus = RemoteDataStatus=RemoteDataStatus.LoadedAndReady;
                            OnRemoteDataRetrieved?.Invoke(_remoteDataStatus);
                            CustomDebug.Log($"Remote data loaded and ready for use. Last fetch time {info.FetchTime}.");
                        });
            }
            catch (Exception e)
            {
                CustomDebug.LogError($"Remote Config activation failed: {e}");
                _remoteDataStatus = RemoteDataStatus=RemoteDataStatus.Unsuccessful;
                OnRemoteDataRetrieved?.Invoke(_remoteDataStatus);
            } 
        }
        
        public static bool GetBoolean(string variable)
        {
            return GetBoolean(variable, false);
        }

        public static bool GetBoolean(string variable, bool defaultValue)
        {
            if (DependencyStatus != Firebase.DependencyStatus.Available ||
                RemoteDataStatus != RemoteDataStatus.LoadedAndReady)
                return defaultValue;

            try
            {
                var value = FirebaseRemoteConfig.DefaultInstance.GetValue(variable);
                // Firebase's static false is also returned for a key that does not exist.
                return value.Source == ValueSource.StaticValue ? defaultValue : value.BooleanValue;
            }
            catch (Exception e)
            {
                CustomDebug.LogWarning($"Remote Config Boolean '{variable}' is unavailable: {e.Message}");
                return defaultValue;
            }
        }

        public static int GetInt(string variable,int defaultValue)
        {
            if (DependencyStatus == Firebase.DependencyStatus.Available && RemoteDataStatus==RemoteDataStatus.LoadedAndReady)
            {
                var numberValue = FirebaseRemoteConfig.DefaultInstance.GetValue(variable).DoubleValue;
                return (int)numberValue;
            }

            return defaultValue;
        }
        
        public static string GetString(string variable)
        {
            if (DependencyStatus == Firebase.DependencyStatus.Available && RemoteDataStatus==RemoteDataStatus.LoadedAndReady)
            {
                var value = FirebaseRemoteConfig.DefaultInstance.GetValue(variable).StringValue;
                return value;
            }
            return "";
        }
        
        public static System.Collections.Generic.List<int> GetLevelSequence(string variable="levelSequence")
        {
            var levelsList = new System.Collections.Generic.List<int>();
            var LevelsString = "";
            
            LevelsString =GetString(variable);
            if (!string.IsNullOrEmpty(LevelsString))
            {
                var excludedLevelsArrayStr = LevelsString.Split(',');
                foreach (var excludedLevelStr in excludedLevelsArrayStr)
                {
                    if (int.TryParse(excludedLevelStr.Trim(), out var excludedLevel))
                    {
                        levelsList.Add(excludedLevel);
                    }
                }
            }
            return levelsList;
        }
#endif

        #endregion
        
        #region FirebaseCrashlytics

        private static void InitCrashlytics()
        {
#if USE_FIREBASECRASHLYTICS
            OwlManager.OwlStatusCanvas?.UpdateStatusInfo($"Firebase Product(Crashlytics): Initializing");
            try
            {
                Crashlytics.ReportUncaughtExceptionsAsFatal = true;
                Firebase.FirebaseApp.LogLevel = Firebase.LogLevel.Debug;
                if (_instance != null) _instance._crashlyticsInitialized = true;
            }
            catch (Exception e)
            {
                if (_instance != null) _instance._crashlyticsInitialized = false;
                Log("Crashlytics initialization failed: " + e.GetBaseException());
            }
           
#endif
         
        }


        private static void Log(string log)
        {
            CustomDebug.LogError(log);
#if USE_FIREBASECRASHLYTICS
            try
            {
                Crashlytics.Log(log);
            }
            catch (Exception e)
            {
                CustomDebug.LogError("Crashlytics log submission failed: " + e.GetBaseException());
            }
         
#endif
            
         
        }
        
        public static void LogException(System.Exception exception)
        {
#if USE_FIREBASECRASHLYTICS
            
            try
            {
                Crashlytics.LogException(exception);

            }
            catch (Exception e)
            {
                CustomDebug.LogError("Crashlytics exception submission failed: " + e.GetBaseException());
            }

#endif
         
        }
        
        
        #endregion
        
        public static string GetStatus()
        {
#if USE_FIREBASE
            return DependencyStatus.ToString();
#endif
            return string.Empty;
        }
    }
    

}
