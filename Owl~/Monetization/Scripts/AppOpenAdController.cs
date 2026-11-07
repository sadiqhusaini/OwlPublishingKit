using System;
using System.Collections;
using Owl.Analytics;
using UnityEngine;

namespace Owl.Monetization
{
    public class AppOpenAdController : AdType
    {
        private OwlSettings _owlSettings;
#if USE_ADMOB
        private GoogleAdsAppOpenController _googleAdsAppOpenController;
#endif
        private MaxAdsAppOpenController _maxAdsAppOpenController;

        private Coroutine _loadDelayCol;
        private float flooringDelay = 60f;
        private int floorIndex;
        private AdFloor adFloor;

            
        public override void Initialize()
        {
            SetTestIds();
            SetReference();
            LoadAd();
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
            base.adIdAndroid = "ca-app-pub-3940256099942544/9257395921";
            base.adIdIOS = "ca-app-pub-3940256099942544/5575463023";
#elif USE_MAXADS
            base.adIdAndroid = "Test_Android_ID";
            base.adIdIOS = "Test_IOS_ID";
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
            _googleAdsAppOpenController = gameObject.AddComponent<GoogleAdsAppOpenController>();
#elif USE_MAXADS
            _maxAdsAppOpenController = gameObject.AddComponent<MaxAdsAppOpenController>();  
#endif
        }
        
        
        protected override void CheckAdLoad(bool result)
        {
            if (adLoadedStatus)
                return;

            if (result)
                LoadAd();
        }
        
        
        private void OnDestroy()
        {
            DestroyAd();
        }
        
        private void SetIds(int index=0)
        {
            var adUnits = _owlSettings.GetAppOpenID(index);
            base.SetAdUnitIds(adUnits.adUnitAndroid,adUnits.adUnitIOS);

#if UNITY_ANDROID
            ADUnitId = base.adIdAndroid;
#elif UNITY_IPHONE
            ADUnitId = base.adIdIOS;
#endif
            
        }

        [Obsolete]
        private void LoadAd()
        {
#if USE_ADMOB
            LoadAd(floorIndex);
            return;
#else
            if (AdManager.IsRemoveAdsPurchased())
                return;

            SetIds();
            if (!HasValidAdUnit()) return;
            
#if  USE_MAXADS
            _maxAdsAppOpenController.LoadAd(ADUnitId, ()=>
            {
                ReportAdLoad("App Open", true);
            }, () =>
            {
                Invoke(nameof(LoadAd),5); 
            });
         
#endif
#endif
        }
        
        private void LoadAd(int index)
        {
            if (AdManager.IsRemoveAdsPurchased())
                return;
            
            SetIds(index);
            if (!HasValidAdUnit()) return;
#if USE_ADMOB
            _googleAdsAppOpenController.LoadAd(ADUnitId,adFloor, ()=>
            {
                ReportAdLoad("App Open", true);
            }, () =>
            {
                LoadAgain();
            });
#endif
        }
        
        private void LoadAgain()
        {
            float delay = 2;
            switch (adFloor)
            {
                case AdFloor.High:
                case AdFloor.Medium:
                    delay = flooringDelay;
                    break;

                case AdFloor.All:
                    delay = 1;
                    break; ;

            }

            _loadDelayCol ??= StartCoroutine(LoadDelay(delay));
        }
        
        private IEnumerator LoadDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            LoadAd(floorIndex);
            _loadDelayCol = null;
        }
        
        public void ShowAd(string placement)
        {
            if (AdManager.IsRemoveAdsPurchased())
                return;

            AdManager.OnAdOpen();

#if USE_ADMOB
            _googleAdsAppOpenController.ShowAd(placement,() => { }, () =>
            {
                ReportAdLoad("App Open", false);
            }, () =>
            {
                AnalyticsManager.TrackAppOpenAd(ALAdAction.Show, placement);
                AdOpened(false,false);
            },()=>
            {
                AdClosed(false,false);
            });

#elif USE_MAXADS
            _maxAdsAppOpenController.ShowAd(() => { }, () => 
            {
                ReportAdLoad("App Open", false);
            }, () =>
            {
                AnalyticsManager.TrackAppOpenAd(ALAdAction.Show, placement);
                AdOpened(false,false);

            },()=>
            {
                AdClosed(false,false);
            });
#endif
        }

        private void DestroyAd()
        {
            ReportAdLoad("App Open", false);
        }
        
        public bool IsAppOpenAvailable()
        {
#if USE_ADMOB
            return _googleAdsAppOpenController.IsAppOpenAvailable();
#elif USE_MAXADS
            return  _maxAdsAppOpenController.IsAppOpenAvailable();
#endif
            return false;
        }
    }
}
