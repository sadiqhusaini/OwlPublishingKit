using System;
using UnityEngine;

namespace Owl.Monetization
{
    
    public class MaxAdsInterstitialController: MonoBehaviour
    {
#if USE_MAXADS

        private string _adUnitId;
        private bool _loadPending;
        private bool _presenting;
        private bool _disposed;
    
        private Action _onLoaded;
        private Action _onAdCompletion;
        private Action _loadAgain;

        private Action _adOpened;
        private Action _adClosed;

        private string _placement="unnamed";
        public void LoadAd(string adUnitId,Action onLoaded,Action loadAgain)
        {
            if (_disposed || _loadPending || _presenting) return;
            _loadPending = true;
            MaxAdsController.Log($"Interstitial: Request");

            _adUnitId = adUnitId;
            _onLoaded = onLoaded;
            _loadAgain = loadAgain;
            SubscribeMaxCallBacks();
            try { MaxSdk.LoadInterstitial(adUnitId); }
            catch (Exception exception)
            {
                _loadPending = false;
                UnsubscribeMaxCallBacks();
                MaxAdsController.LogWarning("Interstitial request failed: " + exception.Message);
                InvokeSafely(_loadAgain);
            }
        }

        private void SubscribeMaxCallBacks()
        {
            UnsubscribeMaxCallBacks();
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayedEvent;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialClickedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHiddenEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialAdFailedToDisplayEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnAdRevenuePaidEvent;
        }

        private void UnsubscribeMaxCallBacks()
        {
            MaxSdkCallbacks.Interstitial.OnAdLoadedEvent -= OnInterstitialLoadedEvent;
            MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent -= OnInterstitialLoadFailedEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent -= OnInterstitialDisplayedEvent;
            MaxSdkCallbacks.Interstitial.OnAdClickedEvent -= OnInterstitialClickedEvent;
            MaxSdkCallbacks.Interstitial.OnAdHiddenEvent -= OnInterstitialHiddenEvent;
            MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent -= OnInterstitialAdFailedToDisplayEvent;
            MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent -= OnAdRevenuePaidEvent;

        }
        
        private void OnDestroy()
        {
            _disposed = true;
            _loadPending = _presenting = false;
            UnsubscribeMaxCallBacks();
            _onLoaded = _onAdCompletion = _loadAgain = _adOpened = _adClosed = null;
        }

        private static void InvokeSafely(Action action)
        {
            try { action?.Invoke(); }
            catch (Exception exception) { MaxAdsController.LogWarning("Interstitial listener failed: " + exception.Message); }
        }

        public void ShowAd(string placement, Action onAdCompletion, Action adOpened, Action adClosed)
        {

            if (_disposed || _presenting) return;
            _presenting = true;
            this._placement = placement;
            this._adOpened = adOpened;
            this._adClosed = adClosed;
            this._onAdCompletion = onAdCompletion;
            Analytics.AnalyticsManager.TrackInterstitialAd(ALAdAction.Show,placement);
            MaxSdk.ShowInterstitial(_adUnitId);

        }

        public void ShowAd(string placement, Action<bool> rewardAction, Action onAdCompletion, Action adOpened, Action adClosed)
        {
            ShowAd(placement, onAdCompletion, adOpened, adClosed);
        }

        public bool IsInterstitialAvailable()
        {
            return MaxSdk.IsInterstitialReady(_adUnitId);
        }
        private void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (_disposed || adUnitId != _adUnitId) return;
            _loadPending = false;
            MaxAdsController.Log($"Interstitial: Loaded");

            InvokeSafely(_onLoaded);
        }

        private void OnInterstitialLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            if (_disposed || adUnitId != _adUnitId) return;
            if (!_loadPending) return;
            _loadPending = false;
            MaxAdsController.LogWarning($"Interstitial: Load failed: {errorInfo}");

          
            UnsubscribeMaxCallBacks();
            InvokeSafely(_loadAgain);

        }

        private void OnInterstitialDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (_disposed || adUnitId != _adUnitId) return;
            MaxAdsController.Log($"Interstitial: Displayed");

            InvokeSafely(_adOpened);
            _adOpened = null;
        }

        private void OnInterstitialAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            if (_disposed || adUnitId != _adUnitId) return;
            if (!_presenting) return;
            _presenting = false;
            MaxAdsController.LogError($"Interstitial: Display failed: {errorInfo}");

            var completion = _onAdCompletion;
            _onAdCompletion = null;
            InvokeSafely(completion);
            InvokeSafely(_adClosed);
            AdManager.CompleteInterstitial(false, _placement);
            _adClosed = null;
            UnsubscribeMaxCallBacks();
            InvokeSafely(_loadAgain);
        }

        private void OnInterstitialClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (_disposed || adUnitId != _adUnitId) return;
            Analytics.FirebaseController.TrackAdClick(adInfo);

        }

        private void OnInterstitialHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (_disposed || adUnitId != _adUnitId) return;
            if (!_presenting) return;
            _presenting = false;
            MaxAdsController.Log($"Interstitial: Closed");


            var completion = _onAdCompletion;
            _onAdCompletion = null;
            InvokeSafely(completion);
            InvokeSafely(_adClosed);
            AdManager.CompleteInterstitial(true, _placement);
            _adClosed = null;
            UnsubscribeMaxCallBacks();
            InvokeSafely(_loadAgain);
        }
        
        private void OnAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (_disposed || adUnitId != _adUnitId) return;
            Analytics.FirebaseController.TrackAdImpression(adInfo);
        }
#endif

    }
}