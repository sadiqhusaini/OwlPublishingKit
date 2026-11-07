using System;
using UnityEngine;

namespace Owl.Monetization
{
    public class BannerAdController : AdType
    {
#if USE_ADMOB
        private GoogleAdsBannerController _googleAdsBannerController;
#elif USE_MAXADS
        private MaxAdsBannerController _maxAdsBannerController;
#endif
        private OwlSettings _owlSettings;
        
        private void OnEnable()
        {
            base.OnEnable();
        }

        private void OnDisable()
        {
            base.OnDisable();
        }
        
        public override void Initialize()
        {
            SetTestIds();
            SetReference();
            LoadAd();
        }
        
        private void SetTestIds()
        {
#if USE_ADMOB
            base.adIdAndroid = "ca-app-pub-3940256099942544/6300978111";
            base.adIdIOS = "ca-app-pub-3940256099942544/2934735716";
#elif USE_MAXADS
            base.adIdAndroid = "8f9542bebc028fc1";
            base.adIdIOS = "9d38999a9bf276e5";
#endif
            
#if UNITY_ANDROID
            ADUnitId = base.adIdAndroid;
#elif UNITY_IPHONE
            ADUnitId = base.adIdIOS;
#endif
        }
        
        private void SetReference()
        {
            _owlSettings = OwlManager.OwlSettings;
            
#if USE_ADMOB
            _googleAdsBannerController = gameObject.AddComponent<GoogleAdsBannerController>();
#elif USE_MAXADS
            _maxAdsBannerController = gameObject.AddComponent<MaxAdsBannerController>();  
#endif
        }
        
        private void SetIds()
        {
            var adUnits = _owlSettings.GetBannerIDs();
            base.SetAdUnitIds(adUnits.adUnitAndroid,adUnits.adUnitIOS);
            
#if UNITY_ANDROID
            ADUnitId = base.adIdAndroid;
#elif UNITY_IPHONE
            ADUnitId = base.adIdIOS;
#endif
        }
        
        protected override void CheckAdLoad(bool result)
        {
            if (adLoadedStatus)
            {
                return;
            }

            if (result)
                LoadAd();
        }
        
        public void LoadAd()
        {
            if (AdManager.IsRemoveBannerAdsPurchased())
                return;
            
            SetIds();
            if (!HasValidAdUnit()) return;
            
            AdManager.EnableBannerBg();

#if USE_ADMOB
            var googlePos =_owlSettings.ConvertGoogleAdPositions(AdManager.BannerPosition);
            _googleAdsBannerController?.LoadAd(ADUnitId,googlePos, () =>
            {
                ReportAdLoad("Banner", true);
            },()=>
            {
                Invoke(nameof(LoadAd),2); 
            });
#elif USE_MAXADS
            var maxPos=_owlSettings.ConvertMaxBannerAdPositions(AdManager.BannerPosition);
            _maxAdsBannerController?.LoadAd(ADUnitId,maxPos, () =>
            {
                ReportAdLoad("Banner", true);
                ShowAd();
            },()=>
            {
                Invoke(nameof(LoadAd),2); 
            });
#endif
        }
        
        public void ShowAd()
        {
#if USE_ADMOB
            _googleAdsBannerController?.ShowAd();
#elif USE_MAXADS
            _maxAdsBannerController?.ShowAd();
#endif
            AdManager.EnableBannerBg();
        }

        public void HideAd(bool isBgHide=false)
        {
#if USE_ADMOB
            _googleAdsBannerController?.HideAd();
#elif USE_MAXADS
            _maxAdsBannerController?.HideAd();
#endif
            if (isBgHide)
                AdManager.HideBannerBg();
        }
        
        public void ChangeBannerPosition(BannerPosition _pos)
        {
#if USE_ADMOB
            var googlePos =_owlSettings.ConvertGoogleAdPositions(_pos);
            _googleAdsBannerController?.ChangeBannerPosition(googlePos);
#elif USE_MAXADS
            var maxPos=_owlSettings.ConvertMaxBannerAdPositions(_pos);
            _maxAdsBannerController?.ChangeBannerPosition(maxPos);
#endif
        }
        
        private void DestroyAd()
        {
            ReportAdLoad("Banner", false);
#if USE_ADMOB
            _googleAdsBannerController?.DestroyAd();
            _googleAdsBannerController = null;

#elif USE_MAXADS
            _maxAdsBannerController?.DestroyAd();
            _maxAdsBannerController = null;

#endif

        }
    }
}
