using System;
using UnityEngine;

namespace Owl.Monetization
{
    public class MaxAdsAppOpenController: MonoBehaviour
    {
   
#if USE_MAXADS
        private string _adUnitId;
        private Action _completeAction;
        private Action _onLoaded;
        private Action _onAdCompletion;
        private Action _loadAgain;

        private Action _adOpened;
        private Action _adClosed;

        private bool _rewardGranted = false;

        public void LoadAd(string adUnitId,Action onLoaded,Action loadAgain)
        {

            MaxAdsController.Log($"AppOpen: Request");

            _adUnitId = adUnitId;
            _onLoaded = onLoaded;
            _loadAgain = loadAgain;
            SubscribeMaxCallBacks();
            MaxSdk.LoadAppOpenAd(_adUnitId);

        }

        private void SubscribeMaxCallBacks()
        {
            UnsubscribeMaxCallBacks();
            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent += OnAppOpenLoadedEvent;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent += OnAppOpenFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent += OnAppOpenDisplayFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent += OnAppOpenDismissedEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent += OnAppOpenDisplayedEvent;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
            MaxSdkCallbacks.AppOpen.OnAdClickedEvent += OnAdClickedEvent;

        }

      

        private void UnsubscribeMaxCallBacks()
        {
            MaxSdkCallbacks.AppOpen.OnAdLoadedEvent -= OnAppOpenLoadedEvent;
            MaxSdkCallbacks.AppOpen.OnAdLoadFailedEvent -= OnAppOpenFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayFailedEvent -= OnAppOpenDisplayFailedEvent;
            MaxSdkCallbacks.AppOpen.OnAdHiddenEvent -= OnAppOpenDismissedEvent;
            MaxSdkCallbacks.AppOpen.OnAdDisplayedEvent -= OnAppOpenDisplayedEvent;
            MaxSdkCallbacks.AppOpen.OnAdRevenuePaidEvent -= OnAdRevenuePaidEvent;
            MaxSdkCallbacks.AppOpen.OnAdClickedEvent -= OnAdClickedEvent;
        }
        
        private void OnDestroy()
        {
            UnsubscribeMaxCallBacks();
            _completeAction = null;
            _onLoaded = null;
            _onAdCompletion = null;
            _loadAgain = null;
            _adOpened = null;
            _adClosed = null;
        }

        public void ShowAd(Action onComplete, Action onAdCompletion, Action adOpened, Action adClosed)
        {

            this._completeAction = onComplete;
            this._adOpened = adOpened;
            this._adClosed = adClosed;
            this._onAdCompletion = onAdCompletion;
            MaxSdk.ShowAppOpenAd(_adUnitId);
          
        }
        public bool IsAppOpenAvailable()
        {
            return MaxSdk.IsAppOpenAdReady(_adUnitId);
        }
        private void OnAppOpenLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            MaxAdsController.Log($"AppOpen: Loaded");
            _onLoaded?.Invoke();
            OwlManager.AppOpenResult(true);
        }
        private void OnAppOpenFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            if (adUnitId != _adUnitId) return;
            MaxAdsController.LogWarning($"AppOpen: Load failed: {errorInfo}");
            OwlManager.AppOpenResult(false);
            UnsubscribeMaxCallBacks();
            _loadAgain?.Invoke();
        }
        private void OnAppOpenDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            MaxAdsController.Log($"AppOpen: Displayed");

            _adOpened?.Invoke();
        }
        private void OnAppOpenDisplayFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            MaxAdsController.LogError($"AppOpen: Display failed: {errorInfo}");
            _onAdCompletion?.Invoke();
            var closed = _adClosed;
            _adClosed = null;
            _adOpened = null;
            _completeAction = null;
            UnsubscribeMaxCallBacks();
            closed?.Invoke();
            _loadAgain?.Invoke();
        }

        private void OnAppOpenDismissedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            MaxAdsController.Log($"AppOpen: Closed");

            _adClosed?.Invoke();
            UnsubscribeMaxCallBacks();
            _loadAgain?.Invoke();

        }
        private void OnAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            Analytics.FirebaseController.TrackAdImpression(adInfo);
        }
        
        private void OnAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            Analytics.FirebaseController.TrackAdClick(adInfo);
        }
#endif
    }
}