using System;
using UnityEngine;

namespace Owl.Monetization
{
    
    public class MaxAdsRewardedController: MonoBehaviour
    {
#if USE_MAXADS
        private Action _onLoaded;
        private Action _loadAgain;

        private Action _adOpened;
        private Action _adClosed;

        private string _adUnitId;
        private bool _rewardGranted = false;
        private string _placement="unnamed";

        public void LoadAd(string adUnitId, Action onLoaded, Action loadAgain)
        {
            MaxAdsController.Log($"Rewarded: Request");

            _onLoaded = onLoaded;
            _loadAgain = loadAgain;
            _adUnitId = adUnitId;
            SubscribeMaxCallBacks();
            MaxSdk.LoadRewardedAd(_adUnitId);
        }


        private void SubscribeMaxCallBacks()
        {
            UnsubscribeMaxCallBacks();
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdLoadFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdHiddenEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;
        }

        private void UnsubscribeMaxCallBacks()
        {
            MaxSdkCallbacks.Rewarded.OnAdLoadedEvent -= OnRewardedAdLoadedEvent;
            MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent -= OnRewardedAdLoadFailedEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent -= OnRewardedAdDisplayedEvent;
            MaxSdkCallbacks.Rewarded.OnAdClickedEvent -= OnRewardedAdClickedEvent;
            MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent -= OnRewardedAdRevenuePaidEvent;
            MaxSdkCallbacks.Rewarded.OnAdHiddenEvent -= OnRewardedAdHiddenEvent;
            MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent -= OnRewardedAdFailedToDisplayEvent;
            MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent -= OnRewardedAdReceivedRewardEvent;
        }


        private void OnDestroy()
        {
            UnsubscribeMaxCallBacks();
            _onLoaded = null;
            _loadAgain = null;
            _adOpened = null;
            _adClosed = null;
        }

        public void ShowAd(string placement, Action adOpened, Action adClosed)
        {
            this._placement = placement;
            this._adOpened = adOpened;
            this._adClosed = adClosed;
            _rewardGranted = false;
            Analytics.AnalyticsManager.TrackRewardedVideoAd(ALAdAction.Show,placement);
            MaxSdk.ShowRewardedAd(_adUnitId);
        }

        public bool IsRewardedAvailable()
        {
            return MaxSdk.IsRewardedAdReady(_adUnitId);
        }

        private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            MaxAdsController.Log($"Rewarded: Loaded");

            _onLoaded.Invoke();
        }

        private void OnRewardedAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
        {
            if (adUnitId != _adUnitId) return;
            MaxAdsController.LogWarning($"Rewarded: Load failed: {errorInfo}");

            UnsubscribeMaxCallBacks();
            _loadAgain?.Invoke();
        }

        private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            MaxAdsController.Log($"Rewarded: Displayed");

            _adOpened?.Invoke();
            _adOpened = null;
        }

        private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            MaxAdsController.LogError($"Rewarded: Display failed: {errorInfo}");

            _adClosed?.Invoke();
            AdManager.RewardGiven(false,_placement);
            _adClosed = null;
            UnsubscribeMaxCallBacks();
            _loadAgain?.Invoke();
        }

        private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            Analytics.FirebaseController.TrackAdClick(adInfo);
        }

        private void OnRewardedAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            MaxAdsController.Log($"Rewarded: Closed");
            _adClosed?.Invoke();
            AdManager.RewardGiven(_rewardGranted,_placement);
            _adClosed = null;
            UnsubscribeMaxCallBacks();
            _loadAgain?.Invoke();
        }

        private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            MaxAdsController.Log($"Rewarded: ReceivedReward");

            _rewardGranted = true;
        }

        private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
        {
            if (adUnitId != _adUnitId) return;
            Analytics.FirebaseController.TrackAdImpression(adInfo);
        }
#endif

    }
}