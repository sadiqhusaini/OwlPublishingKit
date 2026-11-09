using System;
using UnityEngine;

namespace Owl.Monetization
{
    public class GoogleAdsRewardedInterstitialController :
#if USE_ADMOB
        GoogleFullscreenAdOwner
#else
        MonoBehaviour
#endif
    {
#if USE_ADMOB

        private GoogleMobileAds.Api.RewardedInterstitialAd _rewardedInterstitialAd;

        private Action _onLoaded;
        private Action _loadAgain;
        private Action _adCompletionAction;


        private Action _adOpened;
        private Action _adClosed;

        private string _adUnitId;
        private bool _rewardGranted = false;
        private AdFloor _adFloor;
        private string _placement;


        public void LoadAd(string adUnitId, AdFloor adFloor, Action onLoaded, Action loadAgain)
        {
            QueueLoad((generation, complete) =>
            {
                _onLoaded = onLoaded;
                _adUnitId = adUnitId;
                _adFloor = adFloor;
                _loadAgain = loadAgain;
                DestroyAd();
                GooglesAdsController.Log($"RewardedInterstitial({adFloor}): Request");
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (!IsCurrentLoad(generation)) { complete(); return; }
                    try
                    {
                        var request = new GoogleMobileAds.Api.AdRequest();
                        GoogleMobileAds.Api.RewardedInterstitialAd.Load(adUnitId, request, (ad, error) =>
                        {
                            complete();
                            if (!IsCurrentLoad(generation)) { ad?.Destroy(); return; }
                            if (error != null || ad == null)
                            {
                                ad?.Destroy();
                                GooglesAdsController.LogWarning($"RewardedInterstitial({adFloor}): Load failed: {error?.ToString() ?? "SDK returned no ad"}");
                                InvokeSafely(loadAgain);
                                return;
                            }
                            _rewardedInterstitialAd = ad;
                            RegisterEventHandlers(ad);
                            InvokeSafely(onLoaded);
                            GooglesAdsController.Log($"RewardedInterstitial({adFloor}): Loaded");
                        });
                    }
                    catch (Exception exception)
                    {
                        complete();
                        GooglesAdsController.LogWarning("RewardedInterstitial: request failed: " + exception.Message);
                        InvokeSafely(loadAgain);
                    }
                });
            });
        }

        public void ShowAd(string placement,Action adCompleted, Action adOpened, Action adClosed)
        {
            if (_rewardedInterstitialAd == null || !_rewardedInterstitialAd.CanShowAd() || !BeginPresentation()) { InvokeSafely(adClosed); AdManager.RewardGiven(false, placement); return; }
            this._placement = placement;
            _rewardGranted = false;
            this._adOpened = adOpened;
            this._adClosed = adClosed;
            this._adCompletionAction = adCompleted;
            var presentingAd = _rewardedInterstitialAd;
            try
            {
                presentingAd.Show((GoogleMobileAds.Api.Reward reward) =>
                {
                    if (IsOwnerAlive && ReferenceEquals(_rewardedInterstitialAd, presentingAd)) _rewardGranted = true;
                });
            }
            catch (Exception exception)
            {
                EndPresentation();
                GooglesAdsController.LogWarning("Ad show failed: " + exception.Message);
                InvokeSafely(adClosed); AdManager.RewardGiven(false, placement);
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _onLoaded = _loadAgain = _adCompletionAction = _adOpened = _adClosed = null;
        }

        protected override void DestroyAd()
        {
            if (_rewardedInterstitialAd != null)
            {
                _rewardedInterstitialAd.Destroy();
                _rewardedInterstitialAd = null;
            }
        }

        private void RegisterEventHandlers(GoogleMobileAds.Api.RewardedInterstitialAd ad)
        {
            // Raised when the ad is estimated to have earned money.
            ad.OnAdPaid += (GoogleMobileAds.Api.AdValue adValue) =>
            {
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (!IsOwnerAlive || !ReferenceEquals(_rewardedInterstitialAd, ad)) return;

                    Analytics.MMP.MMPEventTracker.HandleAdPaidEvent(_placement,"RewardedInterstitial",_adUnitId,adValue,ad.GetResponseInfo());

                });

            };
            // Raised when an impression is recorded for an ad.
            // Raised when a click is recorded for an ad.
            ad.OnAdClicked += () =>
            {
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (!IsOwnerAlive || !ReferenceEquals(_rewardedInterstitialAd, ad)) return;
                    Analytics.MMP.MMPEventTracker.HandleAdClickEvent(_placement,"RewardedInterstitial",_adUnitId,ad.GetResponseInfo());

                });

            };
            // Raised when an ad opened full screen content.
            ad.OnAdFullScreenContentOpened += () =>
            {
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (!IsOwnerAlive || !ReferenceEquals(_rewardedInterstitialAd, ad)) return;
                    GooglesAdsController.Log($"RewardedInterstitial({_adFloor}): Opened");
                    InvokeSafely(_adOpened);
                    _adOpened = null;
                });

            };
            // Raised when the ad closed full screen content.
            ad.OnAdFullScreenContentClosed += () =>
            {
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (!IsOwnerAlive || !ReferenceEquals(_rewardedInterstitialAd, ad)) return;
                    if (!EndPresentation()) return;
                    GooglesAdsController.Log($"RewardedInterstitial({_adFloor}): Closed");
                    InvokeSafely(_adClosed);
                    _adClosed = null;

                    AdManager.RewardGiven(_rewardGranted,_placement);

                    InvokeSafely(_loadAgain);
                });

            };
            // Raised when the ad failed to open full screen content.
            ad.OnAdFullScreenContentFailed += (GoogleMobileAds.Api.AdError error) =>
            {
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (!IsOwnerAlive || !ReferenceEquals(_rewardedInterstitialAd, ad)) return;
                    if (!EndPresentation()) return;
                    GooglesAdsController.LogError($"RewardedInterstitial({_adFloor}): Display failed: {error}");
                    InvokeSafely(_adClosed);
                    _adClosed = null;

                    AdManager.RewardGiven(false,_placement);

                    InvokeSafely(_loadAgain);
                });
            };
        }

        public bool IsRewardedInterstitialAvailable()
        {
            if (_rewardedInterstitialAd != null)
            {
                return _rewardedInterstitialAd.CanShowAd();
            }
            else
            {
                return false;
            }
        }
#endif

    }
}
