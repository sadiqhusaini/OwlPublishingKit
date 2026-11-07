using System;
using System.Collections;
using UnityEngine;

namespace Owl.Monetization
{
    public class RewardedInterstitialAdController : AdType
    {
        private OwlSettings _owlSettings;
        private Coroutine _loadDelayCol;
        private int floorIndex;
        private AdFloor adFloor;
        
#if USE_ADMOB
        private GoogleAdsRewardedInterstitialController _googleAdsRewardedInterstitialController;
#elif USE_MAXADS
    
#endif
        private void OnEnable()
        {
            base.OnEnable();
        }

        protected override void CheckAdLoad(bool result)
        {
            if (adLoadedStatus)
            {
                return;
            }

            if (result)
                LoadAd(floorIndex);

        }
        
        public override void Initialize(int index,AdFloor af)
        {
            SetTestIds();
            SetReference();
            floorIndex = index;
            adFloor = af;

            var o = this.gameObject;
            o.name = adFloor + o.name;
            LoadAd(index);
        }
        
        private void SetTestIds()
        {
#if USE_ADMOB
            base.adIdAndroid = "ca-app-pub-3940256099942544/5354046379";
            base.adIdIOS = "ca-app-pub-3940256099942544/6978759866";
#elif USE_MAXADS
            base.adIdAndroid = "58a05c175168386c";
            base.adIdIOS = "05905d181ec86d94";
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
            _googleAdsRewardedInterstitialController = gameObject.AddComponent<GoogleAdsRewardedInterstitialController>();
#elif USE_MAXADS
        
#endif

        }
        
        private void SetIds(int index=0)
        {
            var adUnits = _owlSettings.GetRewardedInterstitialID(index);
            base.SetAdUnitIds(adUnits.adUnitAndroid,adUnits.adUnitIOS);

#if UNITY_ANDROID
            ADUnitId = base.adIdAndroid;
#elif UNITY_IPHONE
            ADUnitId = base.adIdIOS;
#endif
            
        }
        
        private void LoadAd(int index)
        {
            SetIds(index);
            if (!HasValidAdUnit()) return;

#if USE_ADMOB
        _googleAdsRewardedInterstitialController.LoadAd(ADUnitId,adFloor, ()=>
            {
                ReportAdLoad("Rewarded Interstitial", true);
            }, () =>
            {
                LoadAgain();
            });
#elif USE_MAXADS
    
#endif
            
        }
        
        public void ShowAd(string placement)
        {
            AdManager.OnAdOpen();
#if USE_ADMOB
            _googleAdsRewardedInterstitialController.ShowAd(placement, () => { ReportAdLoad("Rewarded Interstitial", false);},()=>{ AdOpened(false,true); },()=>{ AdClosed(false,true);});
#elif USE_MAXADS
    
#endif
        }
        
        public bool IsRewardedInterstitialAvailable()
        {
#if USE_ADMOB
            return  _googleAdsRewardedInterstitialController.IsRewardedInterstitialAvailable();
#elif USE_MAXADS
    
#endif
            return false;
        }
        
        private void LoadAgain()
        {
            float delay = 2;
            switch (adFloor)
            {
                case AdFloor.High:
                case AdFloor.Medium:
                    delay = 120;
                    break;

                case AdFloor.All:
                    delay = 1;
                    break; ;

            }

            if (_loadDelayCol == null)
            {
                _loadDelayCol = StartCoroutine(LoadDelay(delay));
            }
        }

        private IEnumerator LoadDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            LoadAd(floorIndex);
            _loadDelayCol = null;
        }
    }
    
}
