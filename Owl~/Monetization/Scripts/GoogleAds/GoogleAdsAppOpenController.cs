using System;

using UnityEngine;
namespace Owl.Monetization
{
#if USE_ADMOB
    public class GoogleAdsAppOpenController : GoogleFullscreenAdOwner
    {
        private GoogleMobileAds.Api.AppOpenAd _appOpenAd;

        private Action _completeAction;
        private Action _adCompletionAction;
        private Action _onLoaded;
        private Action _loadAgain;

        private Action _adOpened;
        private Action _adClosed;
        private string _adUnitId;


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
                GooglesAdsController.Log($"AppOpen({adFloor}): Request");
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (!IsCurrentLoad(generation)) { complete(); return; }
                    try
                    {
                        var request = new GoogleMobileAds.Api.AdRequest();
                        GoogleMobileAds.Api.AppOpenAd.Load(adUnitId, request, (ad, error) =>
                        {
                            complete();
                            if (!IsCurrentLoad(generation)) { DestroyAppOpenAd(ad); return; }
                            if (error != null || ad == null)
                            {
                                DestroyAppOpenAd(ad);
                                GooglesAdsController.LogWarning($"AppOpen({adFloor}): Load failed: {error?.ToString() ?? "SDK returned no ad"}");
                                InvokeSafely(loadAgain);
                                OwlManager.AppOpenResult(false);
                                return;
                            }
                            _appOpenAd = ad;
                            RegisterEventHandlers(ad);
                            InvokeSafely(onLoaded);
                            OwlManager.AppOpenResult(true);
                            GooglesAdsController.Log($"AppOpen({adFloor}): Loaded");
                        });
                    }
                    catch (Exception exception)
                    {
                        complete();
                        GooglesAdsController.LogWarning("AppOpen: request failed: " + exception.Message);
                        InvokeSafely(loadAgain);
                    }
                });
            });
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _completeAction = _adCompletionAction = _onLoaded = _loadAgain = _adOpened = _adClosed = null;
        }

        protected override void DestroyAd()
        {
            DestroyAppOpenAd(_appOpenAd);
            _appOpenAd = null;
        }

        private static void DestroyAppOpenAd(GoogleMobileAds.Api.AppOpenAd ad)
        {
            if (ad == null) return;
            if (CanDestroySimulatedAd()) ad.Destroy();
        }

        public void ShowAd(string placement,Action onComplete,Action adCompletion,Action adOpened,Action adClosed)
        {
            if (_appOpenAd == null || !_appOpenAd.CanShowAd() || !BeginPresentation()) { InvokeSafely(adCompletion); InvokeSafely(adClosed); return; }
            this._placement = placement;
            this._completeAction = onComplete;
            this._adCompletionAction = adCompletion;
            this._adOpened = adOpened;
            this._adClosed = adClosed;
            try { _appOpenAd?.Show(); }
            catch (Exception exception) { EndPresentation(); GooglesAdsController.LogWarning("Ad show failed: " + exception.Message); InvokeSafely(adCompletion); InvokeSafely(adClosed); }
        }


        private void RegisterEventHandlers(GoogleMobileAds.Api.AppOpenAd ad)
        {
            ad.OnAdPaid += (GoogleMobileAds.Api.AdValue adValue) =>
            {
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (!IsOwnerAlive || !ReferenceEquals(_appOpenAd, ad)) return;
                    Analytics.MMP.MMPEventTracker.HandleAdPaidEvent(_placement,"AppOpen",_adUnitId,adValue,ad.GetResponseInfo());
                });

            };
            ad.OnAdClicked += () =>
            {
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (!IsOwnerAlive || !ReferenceEquals(_appOpenAd, ad)) return;
                    Analytics.MMP.MMPEventTracker.HandleAdClickEvent(_placement,"AppOpen",_adUnitId,ad.GetResponseInfo());

                });
            };
            ad.OnAdFullScreenContentOpened += () =>
            {
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (!IsOwnerAlive || !ReferenceEquals(_appOpenAd, ad)) return;
                    OwlManager.OwlStatusCanvas?.UpdateStatusInfo("Shown AppOpen: "+_placement);
                    GooglesAdsController.Log($"AppOpen({_adFloor}): Opened");
                    InvokeSafely(_adOpened);
                    _adOpened = null;
                });

            };
            ad.OnAdFullScreenContentClosed += () =>
            {
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (!IsOwnerAlive || !ReferenceEquals(_appOpenAd, ad)) return;
                    if (!EndPresentation()) return;
                    OwlManager.OwlStatusCanvas?.UpdateStatusInfo("Closed AppOpen: "+_placement);
                    GooglesAdsController.Log($"AppOpen({_adFloor}): Closed");

                    InvokeSafely(_adCompletionAction);
                    InvokeSafely(_adClosed);
                    InvokeSafely(_completeAction);
                    _completeAction = null;
                    _adClosed = null;
                    InvokeSafely(_loadAgain);
                });
            };
            ad.OnAdFullScreenContentFailed += (GoogleMobileAds.Api.AdError error) =>
            {
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (!IsOwnerAlive || !ReferenceEquals(_appOpenAd, ad)) return;
                    if (!EndPresentation()) return;
                    GooglesAdsController.LogError($"AppOpen({_adFloor}): Display failed: {error}");

                    InvokeSafely(_adCompletionAction);
                    var closed = _adClosed;
                    _adClosed = null;
                    _completeAction = null;
                    InvokeSafely(closed);
                    InvokeSafely(_loadAgain);
                });
            };
        }



        public bool IsAppOpenAvailable()
        {
            if (_appOpenAd != null)
            {
                return _appOpenAd.CanShowAd();

            }
            else
            {
                return false;
            }
        }

    }

#endif

}
