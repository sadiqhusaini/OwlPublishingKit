using System;

using UnityEngine;
#if USE_ADMOB
using GoogleMobileAds.Api;
#endif

namespace Owl.Monetization
{
    public class GoogleAdsInterstitialController :
#if USE_ADMOB
        GoogleFullscreenAdOwner
#else
        MonoBehaviour
#endif
    {
#if USE_ADMOB
        private InterstitialAd _interstitialAd;
        private Action _adCompletionAction;
        private Action _onLoaded;
        private Action _loadAgain;

        private Action _adOpened;
        private Action _adClosed;

        private string _adUnitId;

        private AdFloor _adFloor;
        private string _placement="unnamed";


        public void LoadAd(string adUnitId, Action onLoaded)
        {
            LoadAd(adUnitId, AdFloor.All, onLoaded, null);
        }

        public void LoadAd(string adUnitId, AdFloor adFloor, Action onLoaded, Action loadAgain)
        {
            QueueLoad((generation, complete) =>
            {
                _onLoaded = onLoaded;
                _adUnitId = adUnitId;
                _adFloor = adFloor;
                _loadAgain = loadAgain;
                DestroyAd();
                GooglesAdsController.Log($"Interstitial({adFloor}): Request");
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (!IsCurrentLoad(generation)) { complete(); return; }
                    try
                    {
                        var request = new GoogleMobileAds.Api.AdRequest();
                        GoogleMobileAds.Api.InterstitialAd.Load(adUnitId, request, (ad, error) =>
                        {
                            complete();
                            if (!IsCurrentLoad(generation)) { ad?.Destroy(); return; }
                            if (error != null || ad == null)
                            {
                                ad?.Destroy();
                                GooglesAdsController.LogWarning($"Interstitial({adFloor}): Load failed: {error?.ToString() ?? "SDK returned no ad"}");
                                InvokeSafely(loadAgain);
                                return;
                            }
                            _interstitialAd = ad;
                            RegisterEventHandlers(ad);
                            InvokeSafely(onLoaded);
                            GooglesAdsController.Log($"Interstitial({adFloor}): Loaded");
                        });
                    }
                    catch (Exception exception)
                    {
                        complete();
                        GooglesAdsController.LogWarning("Interstitial: request failed: " + exception.Message);
                        InvokeSafely(loadAgain);
                    }
                });
            });
        }

        public void ShowAd(string placement,Action adCompletion,Action adOpened,Action adClosed)
        {
            if (_interstitialAd == null || !_interstitialAd.CanShowAd() || !BeginPresentation()) return;
            this._placement = placement;
            this._adCompletionAction = adCompletion;
            this._adOpened = adOpened;
            this._adClosed = adClosed;
            _interstitialAd?.Show();
        }


        protected override void OnDestroy()
        {
            base.OnDestroy();
            _adCompletionAction = _onLoaded = _loadAgain = _adOpened = _adClosed = null;
        }

        protected override void DestroyAd()
        {
            if (_interstitialAd != null)
            {
                _interstitialAd.Destroy();
                _interstitialAd = null;
            }
        }

        public bool IsInterstitialAvailable()
        {
            if (_interstitialAd != null)
            {
                return _interstitialAd.CanShowAd();

            }
            else
            {
                return false;
            }
        }

        private void RegisterEventHandlers(InterstitialAd ad)
        {
            // Raised when the ad is estimated to have earned money.
            ad.OnAdPaid += (AdValue adValue) =>
            {
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() => {
                    if (!IsOwnerAlive || !ReferenceEquals(_interstitialAd, ad)) return;
                    Analytics.MMP.MMPEventTracker.HandleAdPaidEvent(_placement,"Interstitial",_adUnitId,adValue,ad.GetResponseInfo());
                });

            };
            // Raised when an impression is recorded for an ad.
            // Raised when a click is recorded for an ad.
            ad.OnAdClicked += () =>
            {
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() => {
                    if (!IsOwnerAlive || !ReferenceEquals(_interstitialAd, ad)) return;
                    Analytics.MMP.MMPEventTracker.HandleAdClickEvent(_placement,"Interstitial",_adUnitId,ad.GetResponseInfo());

                });
            };
            // Raised when an ad opened full screen content.
            ad.OnAdFullScreenContentOpened += () =>
            {
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() => {
                    if (!IsOwnerAlive || !ReferenceEquals(_interstitialAd, ad)) return;
                    OwlManager.OwlStatusCanvas?.UpdateStatusInfo("Shown Interstitial: "+_placement);
                    GooglesAdsController.Log($"Interstitial({_adFloor}): Opened");
                    InvokeSafely(_adOpened);
                    _adOpened = null;
                });

            };
            // Raised when the ad closed full screen content.
            ad.OnAdFullScreenContentClosed += () =>
            {
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() => {
                    if (!IsOwnerAlive || !ReferenceEquals(_interstitialAd, ad)) return;
                    if (!EndPresentation()) return;
                    GooglesAdsController.Log($"Interstitial({_adFloor}): Closed");

                    InvokeSafely(_adCompletionAction);
                    InvokeSafely(_adClosed);
                    _adClosed = null;

                    AdManager.CompleteInterstitial(true, _placement);
                    InvokeSafely(_loadAgain);
                });

            };
            // Raised when the ad failed to open full screen content.
            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (!IsOwnerAlive || !ReferenceEquals(_interstitialAd, ad)) return;
                    if (!EndPresentation()) return;
                    GooglesAdsController.LogError($"Interstitial({_adFloor}): Display failed: {error}");

                    InvokeSafely(_adCompletionAction);
                    InvokeSafely(_adClosed);
                    _adClosed = null;
                    AdManager.CompleteInterstitial(false, _placement);
                    InvokeSafely(_loadAgain);
                });
            };
        }
#endif
    }
}
