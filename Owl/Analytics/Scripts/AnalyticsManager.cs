using System;
using UnityEngine;
using Owl.Utility;

namespace Owl.Analytics
{
    
    public class AnalyticsManager : MonoBehaviour
    {
        private FirebaseController _firebaseController;
        public static event Action<bool> OnAnalyticsInitialized;

        private bool _firebaseReady;
        private bool _gameAnalyticsReady;
        private bool _completed;
        private bool _initializing;
        private bool _collectionInitialized;
        private static bool _gameAnalyticsInitialized;
        /// <summary>Analytics startup branches resolved; use ConsentManager for collection permission.</summary>
        public static bool IsInitialized { get; private set; }
        public static bool IsGameAnalyticsInitialized => _gameAnalyticsInitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsInitialized = _gameAnalyticsInitialized = false;
            OnAnalyticsInitialized = null;
        }
        
        private void Awake()
        {
            supported = supportedEvents;
        }
        
        
        private void OnEnable()
        {
            ConsentManager.OnCollectionConsentChanged += ApplyCollectionConsent;
            if (_collectionInitialized) ApplyCollectionConsent();
#if USE_FIREBASE
            FirebaseController.OnFirebaseInitialize += OnFirebaseInitialized;
#if USE_FIREBASEREMOTECONFIG
            FirebaseController.OnRemoteDataRetrieved += OnRemoteConfigReady;
#endif
#endif
        }
        
        private void OnDisable()
        {
            ConsentManager.OnCollectionConsentChanged -= ApplyCollectionConsent;
#if USE_FIREBASE
            FirebaseController.OnFirebaseInitialize -= OnFirebaseInitialized;

#if USE_FIREBASEREMOTECONFIG
            FirebaseController.OnRemoteDataRetrieved -= OnRemoteConfigReady;
#endif
            
#endif
        }

       


        [SerializeField]private bool supportedEvents=true;
        private static bool supported;
        
      
        
    

    

        
        public void Initialize()
        {
            if (_completed)
                return;

            if (_initializing)
                return;
            
            _initializing = true;
            _completed = false;

#if USE_FIREBASE
            _firebaseReady = false;
#else
    _firebaseReady = true;
#endif

#if USE_GAMEANALYTICS
            _gameAnalyticsReady = false;
#else
            _gameAnalyticsReady = true;
#endif

#if USE_FIREBASE
            _firebaseController ??= GetComponent<FirebaseController>();
            if (_firebaseController == null)
                _firebaseController = gameObject.AddComponent<FirebaseController>();
            _firebaseController.Initialize();
#endif

            _collectionInitialized = true;
            ApplyCollectionConsent();
            _gameAnalyticsReady = true;

            CompleteInitializationIfReady();
        }
        private void ApplyCollectionConsent()
        {
            if (!_collectionInitialized) return;
#if USE_GAMEANALYTICS
            var enabled = ConsentManager.CanCollectAnalytics;
            try
            {
                // Disable with no event caching when consent is withdrawn.
                GameAnalyticsSDK.GameAnalytics.SetEnabledEventSubmission(enabled, false);
                if (enabled && !_gameAnalyticsInitialized)
                {
                    if (!FindObjectOfType<GameAnalyticsSDK.GameAnalytics>())
                    {
                        var ga = new GameObject("GameAnalytics");
                        DontDestroyOnLoad(ga);
                        ga.AddComponent<GameAnalyticsSDK.Events.GA_SpecialEvents>();
                        ga.AddComponent<GameAnalyticsSDK.GameAnalytics>();
                    }
                    GameAnalyticsSDK.GameAnalytics.Initialize();
                    _gameAnalyticsInitialized = true;
                }
            }
            catch (Exception exception)
            {
                CustomDebug.LogWarning("GameAnalytics initialization/consent failed: " + exception.GetBaseException().Message);
            }
#endif
        }

#if USE_FIREBASE    
        private void OnFirebaseInitialized(Firebase.DependencyStatus status)
        {
            if (status != Firebase.DependencyStatus.Available)
            {
                OwlManager.OwlStatusCanvas?.UpdateStatusInfo($"Firebase unavailable: {status}");

                _firebaseReady = true;
                CompleteInitializationIfReady();
                return;
            }

            _firebaseController?.InitializeProducts();

#if !USE_FIREBASEREMOTECONFIG
           _firebaseReady = true;
            CompleteInitializationIfReady();
#endif
        }
#endif       
        private void OnRemoteConfigReady(RemoteDataStatus status)
        {
            if (status == RemoteDataStatus.LoadedAndReady ||
                status == RemoteDataStatus.Unsuccessful ||
                status == RemoteDataStatus.RetrievalNotFinished)
            {
                _firebaseReady = true;
                CompleteInitializationIfReady();
            }
        }
        
       
        
        
        private void CompleteInitializationIfReady()
        {
            if (_completed)
                return;

            if (!_firebaseReady)
                return;

            if (!_gameAnalyticsReady)
                return;

            IsInitialized = true;
            _completed = true;
            _initializing = false;
            
            OwlManager.OwlStatusCanvas?.UpdateStatusInfo("Analytics Initialized");
            OnAnalyticsInitialized?.Invoke(true);
        }
      
        public static void TrackLevelStart(int levelNo)
        {
            if (!ConsentManager.CanCollectAnalytics || !IsInitialized) return;

#if USE_GAMEANALYTICS
            if (supported)
            {
                GameAnalyticsSDK.GameAnalytics.NewProgressionEvent(GameAnalyticsSDK.GAProgressionStatus.Start, "LevelStarted"+ levelNo);
            }
            else
            {
                GameAnalyticsSDK.GameAnalytics.NewProgressionEvent(GameAnalyticsSDK.GAProgressionStatus.Start, "LevelStarted", levelNo.ToString());
            }
#endif
#if USE_FIREBASEANALYTICS
            try
            {
                if (supported)
                {
                    Firebase.Analytics.FirebaseAnalytics.LogEvent("LevelStarted"+levelNo);
                }
                else
                {
                    Firebase.Analytics.FirebaseAnalytics.LogEvent( Firebase.Analytics.FirebaseAnalytics.EventLevelStart,"LevelStarted",levelNo);
                }
            }
            catch
            {
                // ignored
            }
#endif
            TrackLevelStart(levelNo,null);

        }
        public static void TrackLevelStart(int levelNo,System.Collections.Generic.Dictionary<string, object> customParameters)
        {
            if (!ConsentManager.CanCollectAnalytics || !IsInitialized) return;

#if USE_MMP && USE_AIRBRIDGE
            if (customParameters!=null)
            {
                customParameters["level_no"] = levelNo;
            }
            MMP.MMPEventTracker.TrackProgressionEvent("Start","LevelStarted",levelNo,customParameters);
#endif
            
        }
        public static void TrackLevelComplete(int levelNo)
        {
            if (!ConsentManager.CanCollectAnalytics || !IsInitialized) return;

#if USE_GAMEANALYTICS
            if (supported)
            {
                GameAnalyticsSDK.GameAnalytics.NewProgressionEvent(GameAnalyticsSDK.GAProgressionStatus.Complete, "LevelCompleted"+ levelNo);
            }
            else
            {
                GameAnalyticsSDK.GameAnalytics.NewProgressionEvent(GameAnalyticsSDK.GAProgressionStatus.Complete, "LevelCompleted", levelNo.ToString());
            }
#endif
#if USE_FIREBASEANALYTICS
            try
            {
                if (supported)
                {
                    Firebase.Analytics.FirebaseAnalytics.LogEvent("LevelCompleted"+levelNo);
                }
                else
                {
                    Firebase.Analytics.FirebaseAnalytics.LogEvent( Firebase.Analytics.FirebaseAnalytics.EventLevelEnd,"LevelCompleted",levelNo);
                }
            }
            catch
            {
                // ignored
            }
#endif
            TrackLevelComplete(levelNo,null);
        }
        public static void TrackLevelComplete(int levelNo,System.Collections.Generic.Dictionary<string, object> customParameters)
        {
            if (!ConsentManager.CanCollectAnalytics || !IsInitialized) return;
#if USE_FACEBOOK
            FacebookEvents.LevelCompleted(levelNo);
#endif

#if USE_MMP && USE_AIRBRIDGE
            if (customParameters!=null)
            {
                customParameters["level_no"] = levelNo;
            }

            MMP.MMPEventTracker.TrackProgressionEvent("Complete","LevelCompleted",levelNo,customParameters);
#endif
        }
        public static void TrackLevelFail(int levelNo)
        {
            if (!ConsentManager.CanCollectAnalytics || !IsInitialized) return;
#if USE_GAMEANALYTICS
            if (supported)
            {
                GameAnalyticsSDK.GameAnalytics.NewProgressionEvent(GameAnalyticsSDK.GAProgressionStatus.Fail, "LevelFailed"+ levelNo);
            }
            else
            {
                GameAnalyticsSDK.GameAnalytics.NewProgressionEvent(GameAnalyticsSDK.GAProgressionStatus.Fail, "LevelFailed", levelNo.ToString());
            }
#endif
#if USE_FIREBASEANALYTICS
            try
            {
                if (supported)
                {
                    Firebase.Analytics.FirebaseAnalytics.LogEvent("LevelFailed" + levelNo);
                }
                else
                {
                    Firebase.Analytics.FirebaseAnalytics.LogEvent(Firebase.Analytics.FirebaseAnalytics.EventLevelEnd,"LevelFailed", levelNo);
                }
            }
            catch
            {
                // ignored
            }
#endif
            TrackLevelFail(levelNo,null);
        }
        public static void TrackLevelFail(int levelNo,System.Collections.Generic.Dictionary<string, object> customParameters)
        {
            if (!ConsentManager.CanCollectAnalytics || !IsInitialized) return;
#if USE_MMP && USE_AIRBRIDGE
            if (customParameters!=null)
            {
                customParameters["level_no"] = levelNo;
            }
            MMP.MMPEventTracker.TrackProgressionEvent("Complete","LevelFailed",levelNo,customParameters);
#endif
        }
        public static void TrackDesignEvent(string designEvent = "[category]:[sub_category]:[outcome]")
        {
            if (!ConsentManager.CanCollectAnalytics || !IsInitialized) return;


#if USE_GAMEANALYTICS
            GameAnalyticsSDK.GameAnalytics.NewDesignEvent(designEvent);
#endif
#if USE_FIREBASEANALYTICS
            try
            {
                Firebase.Analytics.FirebaseAnalytics.LogEvent(designEvent);
            }
            catch
            {
                // ignored
            }
#endif
            
            TrackDesignEvent(designEvent,null);
        }
        public static void TrackDesignEvent(string designEvent="[category]:[sub_category]:[outcome]",float value=0)
        {
            if (!ConsentManager.CanCollectAnalytics || !IsInitialized) return;

#if USE_GAMEANALYTICS
            GameAnalyticsSDK.GameAnalytics.NewDesignEvent (designEvent,value);
#endif
#if USE_FIREBASEANALYTICS
            try
            {

                Firebase.Analytics.FirebaseAnalytics.LogEvent("Design",designEvent,value);
            }
            catch
            {
                // ignored
            }
#endif

            var customParameters = new System.Collections.Generic.Dictionary<string, object>
            {
                [designEvent + "_value"] = value
            };
            TrackDesignEvent(designEvent,customParameters);

        }
        public static void TrackDesignEvent(string label,System.Collections.Generic.Dictionary<string, object> customParameters)
        {
            if (!ConsentManager.CanCollectAnalytics || !IsInitialized) return;
#if USE_MMP && USE_AIRBRIDGE
            MMP.MMPEventTracker.TrackDesignEvent(label,customParameters);
#endif
        }
        public static void TrackCustomEvent(string eventName, string parameterName, float value = 0)
        {
            if (!ConsentManager.CanCollectAnalytics || !IsInitialized) return;

#if USE_FIREBASEANALYTICS
            try
            {
                Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName, parameterName, value);
            }
            catch
            {
                // ignored
            }
#endif

        }
        public static void TrackBannerAd(ALAdAction alAdAction,string placement="Top")
        {
            if (!ConsentManager.CanCollectAnalytics || !IsInitialized) return;
            

            var adSdkName = "Owl";

#if USE_GAMEANALYTICS
            var adType = GameAnalyticsSDK.GAAdType.Banner;
            var adAction = GameAnalyticsSDK.GAAdAction.Show;
            adSdkName = OwlManager.OwlSettings?.mediation.ToString();

            adAction = alAdAction switch
            {
                ALAdAction.Show => GameAnalyticsSDK.GAAdAction.Show,
                ALAdAction.Undefined => GameAnalyticsSDK.GAAdAction.Undefined,
                ALAdAction.Clicked => GameAnalyticsSDK.GAAdAction.Clicked,
                ALAdAction.FailedShow => GameAnalyticsSDK.GAAdAction.FailedShow,
                ALAdAction.RewardReceived => GameAnalyticsSDK.GAAdAction.RewardReceived,
                ALAdAction.Request => GameAnalyticsSDK.GAAdAction.Request,
                ALAdAction.Loaded => GameAnalyticsSDK.GAAdAction.Loaded,
                _ => throw new ArgumentOutOfRangeException(nameof(alAdAction), alAdAction, null)
            };
            GameAnalyticsSDK.GameAnalytics.NewAdEvent(adAction, adType,adSdkName, placement);
#endif
#if USE_FIREBASEANALYTICS
            try
            {
                Firebase.Analytics.FirebaseAnalytics.LogEvent("Banner",alAdAction.ToString(),placement);
            }
            catch
            {
                // ignored
            }
#endif
        }
        public static void TrackInterstitialAd(ALAdAction alAdAction,string placement="TestInterstitial")
        {
            if (!ConsentManager.CanCollectAnalytics || !IsInitialized) return;


#if USE_GAMEANALYTICS
            var adSdkName = "Owl";
            var adType = GameAnalyticsSDK.GAAdType.Interstitial;
            var adAction = GameAnalyticsSDK.GAAdAction.Show;
            adSdkName = OwlManager.OwlSettings?.mediation.ToString();
            
            adAction = alAdAction switch
            {
                ALAdAction.Show => GameAnalyticsSDK.GAAdAction.Show,
                ALAdAction.Undefined => GameAnalyticsSDK.GAAdAction.Undefined,
                ALAdAction.Clicked => GameAnalyticsSDK.GAAdAction.Clicked,
                ALAdAction.FailedShow => GameAnalyticsSDK.GAAdAction.FailedShow,
                ALAdAction.RewardReceived => GameAnalyticsSDK.GAAdAction.RewardReceived,
                ALAdAction.Request => GameAnalyticsSDK.GAAdAction.Request,
                ALAdAction.Loaded => GameAnalyticsSDK.GAAdAction.Loaded,
                _ => throw new ArgumentOutOfRangeException(nameof(alAdAction), alAdAction, null)
            };

            GameAnalyticsSDK.GameAnalytics.NewAdEvent(adAction, adType,adSdkName, placement);
#endif
            
#if USE_FIREBASEANALYTICS
            try
            {
                Firebase.Analytics.FirebaseAnalytics.LogEvent("Interstitial",alAdAction.ToString(),placement);
            }
            catch
            {
                // ignored
            }
#endif

        }
        public static void TrackRewardedInterstitialAd(ALAdAction alAdAction,string placement="TestRInter")
        {
            if (!ConsentManager.CanCollectAnalytics || !IsInitialized) return;


#if USE_GAMEANALYTICS
            var adSdkName = "Owl";
            var adType = GameAnalyticsSDK.GAAdType.RewardedVideo;
            var adAction = GameAnalyticsSDK.GAAdAction.Show;
            adSdkName = OwlManager.OwlSettings?.mediation.ToString();
            adAction = alAdAction switch
            {
                ALAdAction.Show => GameAnalyticsSDK.GAAdAction.Show,
                ALAdAction.Undefined => GameAnalyticsSDK.GAAdAction.Undefined,
                ALAdAction.Clicked => GameAnalyticsSDK.GAAdAction.Clicked,
                ALAdAction.FailedShow => GameAnalyticsSDK.GAAdAction.FailedShow,
                ALAdAction.RewardReceived => GameAnalyticsSDK.GAAdAction.RewardReceived,
                ALAdAction.Request => GameAnalyticsSDK.GAAdAction.Request,
                ALAdAction.Loaded => GameAnalyticsSDK.GAAdAction.Loaded,
                _ => throw new ArgumentOutOfRangeException(nameof(alAdAction), alAdAction, null)
            };

            GameAnalyticsSDK.GameAnalytics.NewAdEvent(adAction, adType,adSdkName, placement);
#endif
#if USE_FIREBASEANALYTICS
            try
            {
                Firebase.Analytics.FirebaseAnalytics.LogEvent("RewardedVideo", alAdAction.ToString(), placement);
            }
            catch
            {
                // ignored
            }
#endif
        }
        public static void TrackRewardedVideoAd(ALAdAction alAdAction,string placement="TestRV")
        {
            if (!ConsentManager.CanCollectAnalytics || !IsInitialized) return;


#if USE_GAMEANALYTICS
            var adSdkName = "Owl";
            var adType = GameAnalyticsSDK.GAAdType.RewardedVideo;
            var adAction = GameAnalyticsSDK.GAAdAction.Show;
            adSdkName = OwlManager.OwlSettings?.mediation.ToString();

            adAction = alAdAction switch
            {
                ALAdAction.Show => GameAnalyticsSDK.GAAdAction.Show,
                ALAdAction.Undefined => GameAnalyticsSDK.GAAdAction.Undefined,
                ALAdAction.Clicked => GameAnalyticsSDK.GAAdAction.Clicked,
                ALAdAction.FailedShow => GameAnalyticsSDK.GAAdAction.FailedShow,
                ALAdAction.RewardReceived => GameAnalyticsSDK.GAAdAction.RewardReceived,
                ALAdAction.Request => GameAnalyticsSDK.GAAdAction.Request,
                ALAdAction.Loaded => GameAnalyticsSDK.GAAdAction.Loaded,
                _ => throw new ArgumentOutOfRangeException(nameof(alAdAction), alAdAction, null)
            };

            GameAnalyticsSDK.GameAnalytics.NewAdEvent(adAction, adType,adSdkName, placement);
#endif
#if USE_FIREBASEANALYTICS
            try
            {
                Firebase.Analytics.FirebaseAnalytics.LogEvent("RewardedVideo", alAdAction.ToString(), placement);
            }
            catch
            {
                // ignored
            }
#endif
        }
        public static void TrackAppOpenAd(ALAdAction alAdAction,string placement="OnStart")
        {
            if (!ConsentManager.CanCollectAnalytics || !IsInitialized) return;
            
#if USE_GAMEANALYTICS
            var adSdkName = "Owl";
            var adType = GameAnalyticsSDK.GAAdType.AppOpen;
            var adAction = GameAnalyticsSDK.GAAdAction.Show;
            adSdkName = OwlManager.OwlSettings?.mediation.ToString();

            adAction = alAdAction switch
            {
                ALAdAction.Show => GameAnalyticsSDK.GAAdAction.Show,
                ALAdAction.Undefined => GameAnalyticsSDK.GAAdAction.Undefined,
                ALAdAction.Clicked => GameAnalyticsSDK.GAAdAction.Clicked,
                ALAdAction.FailedShow => GameAnalyticsSDK.GAAdAction.FailedShow,
                ALAdAction.RewardReceived => GameAnalyticsSDK.GAAdAction.RewardReceived,
                ALAdAction.Request => GameAnalyticsSDK.GAAdAction.Request,
                ALAdAction.Loaded => GameAnalyticsSDK.GAAdAction.Loaded,
                _ => throw new ArgumentOutOfRangeException(nameof(alAdAction), alAdAction, null)
            };

            GameAnalyticsSDK.GameAnalytics.NewAdEvent(adAction, adType,adSdkName, placement);
#endif
#if USE_FIREBASEANALYTICS
            try
            {
                Firebase.Analytics.FirebaseAnalytics.LogEvent("AppOpen", alAdAction.ToString(), placement);
            }
            catch
            {
                // ignored
            }
#endif
        }
    }
}
