using System;
using UnityEngine;

namespace Owl.Monetization
{
    public class MaxAdsMRECController: MonoBehaviour
    {
#if USE_MAXADS
        
        private MaxSdkBase.AdViewPosition  _position;

        private Action _onLoaded;
        private Action _loadAgain;
        private string _adUnitId;
        
        public void LoadAd(string adUnitId,MaxSdkBase.AdViewPosition  bannerPosition, Action onLoaded,Action loadAgain)
        {
            DestroyAd();
            if (string.IsNullOrWhiteSpace(adUnitId)) return;
            _loadAgain = loadAgain;
            _onLoaded = onLoaded;
            _adUnitId = adUnitId;
            _position = bannerPosition;
            MaxAdsController.Log($"MREC: Request");

            SubscribeMaxCallBacks();
            MaxSdk.CreateMRec(_adUnitId, _position);
        }  
        
        public void DestroyAd()
        {

            UnsubscribeMaxCallBacks();
            var previousId = _adUnitId;
            _adUnitId = null;
            _onLoaded = null;
            _loadAgain = null;
            if (!string.IsNullOrEmpty(previousId))
                MaxSdk.DestroyMRec(previousId);
        }

        private void OnDisable() => DestroyAd();
        private void OnDestroy() => DestroyAd();
        
        public void ShowAd()
        {

            if (string.IsNullOrEmpty(_adUnitId)) return;
            MaxSdk.ShowMRec(_adUnitId);
            Analytics.AnalyticsManager.TrackBannerAd(ALAdAction.Show,"MREC_"+_position);
        }

        public void HideAd()
        {

            if (string.IsNullOrEmpty(_adUnitId)) return;
            MaxSdk.HideMRec(_adUnitId);
        }
        
      
        private void SubscribeMaxCallBacks()
        {
            UnsubscribeMaxCallBacks();
            MaxSdkCallbacks.MRec.OnAdLoadedEvent      += OnMRecAdLoadedEvent;
            MaxSdkCallbacks.MRec.OnAdLoadFailedEvent  += OnMRecAdLoadFailedEvent;
            MaxSdkCallbacks.MRec.OnAdClickedEvent     += OnMRecAdClickedEvent;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent += OnMRecAdRevenuePaidEvent;
            MaxSdkCallbacks.MRec.OnAdExpandedEvent    += OnMRecAdExpandedEvent;
            MaxSdkCallbacks.MRec.OnAdCollapsedEvent   += OnMRecAdCollapsedEvent;
        }
        
        private void UnsubscribeMaxCallBacks()
        {
            MaxSdkCallbacks.MRec.OnAdLoadedEvent      -= OnMRecAdLoadedEvent;
            MaxSdkCallbacks.MRec.OnAdLoadFailedEvent  -= OnMRecAdLoadFailedEvent;
            MaxSdkCallbacks.MRec.OnAdClickedEvent     -= OnMRecAdClickedEvent;
            MaxSdkCallbacks.MRec.OnAdRevenuePaidEvent -= OnMRecAdRevenuePaidEvent;
            MaxSdkCallbacks.MRec.OnAdExpandedEvent    -= OnMRecAdExpandedEvent;
            MaxSdkCallbacks.MRec.OnAdCollapsedEvent   -= OnMRecAdCollapsedEvent;
        }
        
        private void OnMRecAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            _onLoaded?.Invoke();
            MaxAdsController.Log($"MREC: Loaded");
        }

        private void OnMRecAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo error)
        {
            if (adUnitId != _adUnitId) return;
            MaxAdsController.LogWarning($"MREC: Load failed: {error}");

            _loadAgain?.Invoke();
        }

        private void OnMRecAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            Analytics.FirebaseController.TrackAdClick(adInfo);
        }

        private void OnMRecAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            Analytics.FirebaseController.TrackAdImpression(adInfo);
        }

        private void OnMRecAdExpandedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;

        }

        private void OnMRecAdCollapsedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;

            UnsubscribeMaxCallBacks();
            _loadAgain?.Invoke();

        }  
#endif
   
    }
}