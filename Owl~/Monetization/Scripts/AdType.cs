using System;
using Owl.Utility;
using UnityEngine;

namespace Owl.Monetization
{
    public class AdType : MonoBehaviour
    {
        [SerializeField]  protected bool adLoadedStatus;
        public bool IsAdLoaded => adLoadedStatus;
        protected void ReportAdLoad(string placement, bool loaded)
        {
            adLoadedStatus = loaded;
            OwlStatusScript.SetFlowStatus(placement, loaded ? "Loaded" : "Loading");
        }
        protected void ReportAdUnloaded(string placement) => ReportAdLoad(placement, false);
        public string adIdAndroid = "";
        public string adIdIOS = "";
        
        [NonSerialized] protected string ADUnitId;
        
        protected void OnEnable()
        {
            InternetManager.OnOnlineCheckComplete += CheckAdLoad;
        }
        protected void OnDisable()
        {
            InternetManager.OnOnlineCheckComplete -= CheckAdLoad;
        }

        protected virtual void CheckAdLoad(bool result)
        {
            
        }

#if UNITY_IOS || UNITY_EDITOR
        private static bool _presentationPaused;
        private static bool _presentationStateCaptured;
        private static float _previousTimeScale;
        private static float _previousVolume;
#endif

        protected static void AdStarting()
        {
#if UNITY_IOS || UNITY_EDITOR
            // The Google Mobile Ads Editor mock pauses Unity before it raises the
            // fullscreen-opened event, so capture the gameplay state before Show().
            if (_presentationPaused) return;

            _previousTimeScale = Time.timeScale;
            _previousVolume = AudioListener.volume;
            _presentationStateCaptured = true;
#endif
        }

        protected static void AdOpened(bool isInter,bool isRv)
        {
#if UNITY_IOS || UNITY_EDITOR
            if (!_presentationStateCaptured)
            {
                _previousTimeScale = Time.timeScale;
                _previousVolume = AudioListener.volume;
            }
            _presentationStateCaptured = false;
            _presentationPaused = true;
            Time.timeScale = 0;
            AudioListener.volume = 0; 
#endif
     
        }

        protected static void AdClosed(bool isInter,bool isRv)
        {
#if UNITY_IOS || UNITY_EDITOR
            if (_presentationPaused || _presentationStateCaptured)
            {
                Time.timeScale = _previousTimeScale;
                AudioListener.volume = _previousVolume;
                _presentationPaused = false;
                _presentationStateCaptured = false;
            }
#endif
          
            AdManager.OnAdClose(isInter,isRv);
        }

        public virtual void Initialize()
        {
            
        }
        
        public virtual void Initialize(int index,AdFloor adFloor)
        {
            
        }

        protected bool HasValidAdUnit()
        {
            if (!string.IsNullOrWhiteSpace(ADUnitId))
                return true;

            adLoadedStatus = false;
            CustomDebug.LogWarning($"[Owl Ads] Skipping {GetType().Name}: no ad unit configured for this platform.");
            return false;
        }

        protected void SetAdUnitIds(string android, string ios)
        {
            // Only explicit test mode may retain the IDs assigned by SetTestIds.
            if (OwlManager.OwlSettings.testIds)
                return;

            adIdAndroid = string.IsNullOrWhiteSpace(android) ? "" : android.Trim();
            adIdIOS = string.IsNullOrWhiteSpace(ios) ? "" : ios.Trim();
        }
    }
}
