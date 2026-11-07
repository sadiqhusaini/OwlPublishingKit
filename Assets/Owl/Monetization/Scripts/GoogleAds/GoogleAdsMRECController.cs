using System;
using UnityEngine;

namespace Owl.Monetization
{
    public class GoogleAdsMRECController : MonoBehaviour
    {
        public string placement="unnamed";

#if USE_ADMOB
        private GoogleMobileAds.Api.AdPosition _position;
        private GoogleMobileAds.Api.BannerView _bannerView;

        private Action _onLoaded;
        private Action _loadAgain;
        private string _adUnitId;

        private AdFloor _adFloor = AdFloor.All;

        
        public void LoadAd(string adUnitId,GoogleMobileAds.Api.AdPosition  bannerPosition, Action onLoaded,Action loadAgain)
        {
            _loadAgain = loadAgain;
            _onLoaded = onLoaded;
            _adUnitId = adUnitId;
            _position = bannerPosition;
            
            if (_bannerView != null)
            {
                DestroyAd();
            }

            GooglesAdsController.Log($"MREC({_adFloor}): Request");


            GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                _bannerView = new GoogleMobileAds.Api.BannerView(_adUnitId, GoogleMobileAds.Api.AdSize.MediumRectangle, _position);
                ListenToAdEvents();
                var adRequest = new GoogleMobileAds.Api.AdRequest();
                _bannerView?.LoadAd(adRequest);
            });
        }  
        
        
        public void DestroyAd()
        {
            if (_bannerView != null)
            {
                UnListenToAdEvents();
                _bannerView?.Destroy();
                _bannerView = null;
            }
        }
        
        public void ShowAd()
        {
            _bannerView?.Show();
            Analytics.AnalyticsManager.TrackBannerAd(ALAdAction.Show, "MREC_"+_position.ToString());

        }

        public void HideAd()
        {
            _bannerView?.Hide();
        }
        
        private void ListenToAdEvents()
        {

            _bannerView.OnBannerAdLoaded += OnBannerAdLoadedCallBack;
            _bannerView.OnBannerAdLoadFailed += OnBannerAdLoadFailed;
            _bannerView.OnAdPaid += OnAdPaid;
            _bannerView.OnAdClicked += OnAdClicked;
            _bannerView.OnAdFullScreenContentOpened += OnAdFullScreenContentOpened;
            _bannerView.OnAdFullScreenContentClosed += OnAdFullScreenContentClosed;
        }
                
        private void UnListenToAdEvents()
        {


            _bannerView.OnBannerAdLoaded -= OnBannerAdLoadedCallBack;
            _bannerView.OnBannerAdLoadFailed -= OnBannerAdLoadFailed;
            _bannerView.OnAdPaid -= OnAdPaid;
            _bannerView.OnAdClicked -= OnAdClicked;
            _bannerView.OnAdFullScreenContentOpened -= OnAdFullScreenContentOpened;
            _bannerView.OnAdFullScreenContentClosed -= OnAdFullScreenContentClosed;
        }

        private void OnBannerAdLoadedCallBack()
        {
            GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                GooglesAdsController.Log($"MREC({_adFloor}): Loaded");
                _onLoaded?.Invoke();
            });

        }

        private void OnBannerAdLoadFailed(GoogleMobileAds.Api.LoadAdError error)
        {
            GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                GooglesAdsController.LogWarning($"MREC({_adFloor}): Load failed: {error}");

                _loadAgain?.Invoke();
            });

        }

        private void OnAdPaid(GoogleMobileAds.Api.AdValue adValue)
        {
            GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Analytics.MMP.MMPEventTracker.HandleAdPaidEvent(placement,"MREC",_adUnitId,adValue,_bannerView.GetResponseInfo());
            });

        }

        
        private void OnAdClicked()
        {
            GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Analytics.MMP.MMPEventTracker.HandleAdClickEvent(placement,"MREC",_adUnitId,_bannerView.GetResponseInfo());  

            });


        }
        
        private void OnAdFullScreenContentOpened()
        {
            GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                GooglesAdsController.Log($"MREC({_adFloor}): Opened");
            });

        }
        
        private void OnAdFullScreenContentClosed()
        {
            GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                GooglesAdsController.Log($"MREC({_adFloor}): Closed");
                _loadAgain?.Invoke();
            });
        }

#endif
    }
}