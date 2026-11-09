using System;
using UnityEngine;
using UnityEngine.PlayerLoop;


namespace Owl.Monetization
{
    public class GoogleAdsBannerController : MonoBehaviour
    {
#if USE_ADMOB
        
        public GoogleMobileAds.Api.AdPosition position;
        private GoogleMobileAds.Api.BannerView _bannerView;
        private int _generation;
        
        private Action<bool> _rewardAction;
     
        private Action _onLoaded;
        private Action _loadAgain;


        private string _adUnitId;

        private bool _bannerShown=false;
        private string _placement="unnamed";

        
        public void LoadAd(string adUnitId,GoogleMobileAds.Api.AdPosition bannerPosition, Action onLoaded,Action loadAgain)
        {
            _onLoaded = onLoaded;
            _loadAgain = loadAgain;
            _adUnitId = adUnitId;
            position = bannerPosition;
            _placement = position.ToString();
            
         

            if (_bannerView != null)
            {
                DestroyAd();
            }
            
            var generation = ++_generation;
            GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (!this || generation != _generation) return;
                _bannerView = new GoogleMobileAds.Api.BannerView(_adUnitId, GoogleMobileAds.Api.AdSize.Banner, position);
                ListenToAdEvents();
                GooglesAdsController.Log($"Banner: Request");
                var adRequest = new GoogleMobileAds.Api.AdRequest();

                _bannerView?.LoadAd(adRequest);
            });
        }  
        
       
    
        private void OnDestroy() => DestroyAd();

        public void DestroyAd()
        {
            ++_generation;
            _bannerShown = false;
            if (_bannerView != null)
            {
                UnListenToAdEvents();
                _bannerView.Destroy();
                _bannerView = null;
            }
        }
        
        public void ShowAd()
        {
            if (_bannerView != null)
            {
                if (!_bannerShown)
                {
                    _bannerShown = true;
                    _bannerView.Show();
                    Analytics.AnalyticsManager.TrackBannerAd(ALAdAction.Show, position.ToString());
                }
            }
        }

        public void HideAd()
        {
            if (_bannerView != null)
            {

                _bannerShown = false;
                _bannerView.Hide();
            }
        }
        
        public void ChangeBannerPosition(GoogleMobileAds.Api.AdPosition _pos)
        {
            _placement = _pos.ToString();
            _bannerView?.SetPosition(_pos);

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
            var view = _bannerView;
            var generation = _generation;
            GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (!this || view == null || generation != _generation || !ReferenceEquals(view, _bannerView)) return;
                GooglesAdsController.Log($"Banner: Loaded");
                _bannerShown = true;
                _onLoaded?.Invoke();
            });
        }

        private void OnBannerAdLoadFailed(GoogleMobileAds.Api.LoadAdError error)
        {
            var view = _bannerView;
            var generation = _generation;
            GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (!this || view == null || generation != _generation || !ReferenceEquals(view, _bannerView)) return;
                _bannerShown = false;
                GooglesAdsController.LogWarning($"Banner: Load failed: {error}");
                _loadAgain?.Invoke();
            });
        }

        private void OnAdPaid(GoogleMobileAds.Api.AdValue adValue)
        {
            var view = _bannerView;
            var generation = _generation;
            GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (!this || view == null || generation != _generation || !ReferenceEquals(view, _bannerView)) return;
                Analytics.MMP.MMPEventTracker.HandleAdPaidEvent(_placement,"Banner",_adUnitId,adValue,_bannerView.GetResponseInfo());
            });
        }

        
        private void OnAdClicked()
        {
            var view = _bannerView;
            var generation = _generation;
            GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (!this || view == null || generation != _generation || !ReferenceEquals(view, _bannerView)) return;
                Analytics.MMP.MMPEventTracker.HandleAdClickEvent(_placement,"Banner",_adUnitId,_bannerView.GetResponseInfo());  

            });
        }
        
        private void OnAdFullScreenContentOpened()
        {
            var view = _bannerView;
            var generation = _generation;
            GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (!this || view == null || generation != _generation || !ReferenceEquals(view, _bannerView)) return;
                GooglesAdsController.Log($"Banner: Opened");


            });
        }
        
        private void OnAdFullScreenContentClosed()
        {
            var view = _bannerView;
            var generation = _generation;
            GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (!this || view == null || generation != _generation || !ReferenceEquals(view, _bannerView)) return;
                GooglesAdsController.Log($"Banner: Closed");

                _bannerShown = false;
            });
        }

#endif

    }
}