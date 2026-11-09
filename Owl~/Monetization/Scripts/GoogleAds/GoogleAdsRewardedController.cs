using System;
using UnityEngine;

namespace Owl.Monetization
{
#if USE_ADMOB
    public class GoogleAdsRewardedController : GoogleFullscreenAdOwner
    {

        private GoogleMobileAds.Api.RewardedAd _rewardedAd;

        private Action _onLoaded;
        private Action _loadAgain;

        private Action _adOpened;
        private Action _adClosed;

        private string _adUnitId;
        private bool _rewardGranted = false;
        private AdFloor _adFloor;
        private string _placement="unnamed";



        public void LoadAd(string adUnitId, AdFloor adFloor, Action onLoaded, Action loadAgain)
        {
            QueueLoad((generation, complete) =>
            {
                _onLoaded = onLoaded;
                _adUnitId = adUnitId;
                _adFloor = adFloor;
                _loadAgain = loadAgain;
                DestroyAd();
                GooglesAdsController.Log($"Rewarded({adFloor}): Request");
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (!IsCurrentLoad(generation)) { complete(); return; }
                    try
                    {
                        var request = new GoogleMobileAds.Api.AdRequest();
                        GoogleMobileAds.Api.RewardedAd.Load(adUnitId, request, (ad, error) =>
                        {
                            complete();
                            if (!IsCurrentLoad(generation)) { ad?.Destroy(); return; }
                            if (error != null || ad == null)
                            {
                                ad?.Destroy();
                                GooglesAdsController.LogWarning($"Rewarded({adFloor}): Load failed: {error?.ToString() ?? "SDK returned no ad"}");
                                InvokeSafely(loadAgain);
                                return;
                            }
                            _rewardedAd = ad;
                            RegisterEventHandlers(ad);
                            InvokeSafely(onLoaded);
                            GooglesAdsController.Log($"Rewarded({adFloor}): Loaded");
                        });
                    }
                    catch (Exception exception)
                    {
                        complete();
                        GooglesAdsController.LogWarning("Rewarded: request failed: " + exception.Message);
                        InvokeSafely(loadAgain);
                    }
                });
            });
        }

        public void ShowAd(string placement, Action adOpened, Action adClosed)
        {
            if (_rewardedAd == null || !_rewardedAd.CanShowAd() || !BeginPresentation()) { InvokeSafely(adClosed); AdManager.RewardGiven(false, placement); return; }
            _placement = placement;
            _rewardGranted = false;
            this._adOpened = adOpened;
            this._adClosed = adClosed;
            var presentingAd = _rewardedAd;
            try
            {
                presentingAd.Show((GoogleMobileAds.Api.Reward reward) =>
                {
                    if (IsOwnerAlive && ReferenceEquals(_rewardedAd, presentingAd)) _rewardGranted = true;
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
            _onLoaded = _loadAgain = _adOpened = _adClosed = null;
        }

        protected override void DestroyAd()
        {
            if (_rewardedAd != null)
            {
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }
        }

        private void RegisterEventHandlers(GoogleMobileAds.Api.RewardedAd ad)
        {
            // var adNetworkAdapter = ad.MediationAdapterClassName();

            ad.OnAdPaid += (GoogleMobileAds.Api.AdValue adValue) =>
            {
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (!IsOwnerAlive || !ReferenceEquals(_rewardedAd, ad)) return;
                    Analytics.MMP.MMPEventTracker.HandleAdPaidEvent(_placement,"Rewarded",_adUnitId,adValue,ad.GetResponseInfo());
                });
            };
            ad.OnAdClicked += () =>
            {
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (!IsOwnerAlive || !ReferenceEquals(_rewardedAd, ad)) return;
                    Analytics.MMP.MMPEventTracker.HandleAdClickEvent(_placement,"Rewarded",_adUnitId,ad.GetResponseInfo());

                });

            };
            ad.OnAdFullScreenContentOpened += () =>
            {
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (!IsOwnerAlive || !ReferenceEquals(_rewardedAd, ad)) return;
                    GooglesAdsController.Log($"Rewarded({_adFloor}): Opened");

                    InvokeSafely(_adOpened);
                    _adOpened = null;
                });

            };
            ad.OnAdFullScreenContentClosed += () =>
            {
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (!IsOwnerAlive || !ReferenceEquals(_rewardedAd, ad)) return;
                    if (!EndPresentation()) return;
                    GooglesAdsController.Log($"Rewarded({_adFloor}): Closed");

                    InvokeSafely(_adClosed);
                    AdManager.RewardGiven(_rewardGranted,_placement);
                    _adClosed = null;
                    InvokeSafely(_loadAgain);
                    _rewardGranted = false;
                });

            };
            ad.OnAdFullScreenContentFailed += (GoogleMobileAds.Api.AdError error) =>
            {
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (!IsOwnerAlive || !ReferenceEquals(_rewardedAd, ad)) return;
                    if (!EndPresentation()) return;
                    GooglesAdsController.LogError($"Rewarded({_adFloor}): Display failed: {error}");

                    InvokeSafely(_adClosed);
                    AdManager.RewardGiven(false,_placement);
                    _adClosed = null;
                    InvokeSafely(_loadAgain);
                });

            };
        }

        public bool IsRewardedAvailable()
        {
            return _rewardedAd != null && _rewardedAd.CanShowAd();
        }
    }
#endif
}
