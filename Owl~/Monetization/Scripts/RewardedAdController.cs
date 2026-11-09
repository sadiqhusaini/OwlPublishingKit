using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;


namespace Owl.Monetization
{
    public class RewardedAdController : AdType
    {
        private OwlSettings _owlSettings;
        private Coroutine _loadDelayCol;
        private int floorIndex;
        private AdFloor adFloor;


#if USE_ADMOB
        private GoogleAdsRewardedController _googleAdsRewardedController;
#endif

#if USE_MAXADS
        private MaxAdsRewardedController _maxAdsRewardedController;
#endif


        private void SetReference()
        {
            _owlSettings = OwlManager.OwlSettings;

#if USE_ADMOB
            _googleAdsRewardedController = gameObject.AddComponent<GoogleAdsRewardedController>();
#elif USE_MAXADS
            _maxAdsRewardedController = gameObject.AddComponent<MaxAdsRewardedController>();
#endif
        }
        private void OnEnable()
        {
            base.OnEnable();
        }

        protected override void CheckAdLoad(bool result)
        {
            if (adLoadedStatus)
                return;
            
            if (result)
                LoadAd();
        }

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
            base.adIdAndroid = "ca-app-pub-3940256099942544/5224354917";
            base.adIdIOS = "ca-app-pub-3940256099942544/1712485313";
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
        
        private void SetIds(int index=0)
        {
            var adUnits = _owlSettings.GetRewardedID(index);
            base.SetAdUnitIds(adUnits.adUnitAndroid,adUnits.adUnitIOS);

#if UNITY_ANDROID
            ADUnitId = base.adIdAndroid;
#elif UNITY_IPHONE
            ADUnitId = base.adIdIOS;
#endif
            
        }

        private void LoadAd()
        {
#if USE_ADMOB
            LoadAd(floorIndex);
            return;
#else
            SetIds();
            if (!HasValidAdUnit()) return;
#if  USE_MAXADS
            _maxAdsRewardedController.LoadAd(ADUnitId, () =>
            {
                ReportAdLoad("Rewarded", true);
            }, () => Invoke(nameof(LoadAd), 2));
#endif

#endif
        }

        private void LoadAd(int index)
        {
            SetIds(index);
            if (!HasValidAdUnit()) return;

#if USE_ADMOB
            _googleAdsRewardedController.LoadAd(ADUnitId,adFloor, ()=>
            {
                ReportAdLoad("Rewarded", true);
            }, () =>
            {
                LoadAgain();
            });
#endif
        }

        public void ShowAd(string placement)
        {
            ReportAdLoad("Rewarded", false);
            AdManager.OnAdOpen();
            AdStarting();

#if USE_ADMOB
            _googleAdsRewardedController.ShowAd(placement,()=>{ AdOpened(false,true); }, () =>
            {
                AdClosed(false,true);
            });
#elif USE_MAXADS
            _maxAdsRewardedController.ShowAd(placement,()=>{ AdOpened(false,true); },()=>{ AdClosed(false,true);});
#endif
        }

        public bool IsRewardedAvailable()
        {
#if USE_ADMOB
            return  _googleAdsRewardedController.IsRewardedAvailable();
#elif USE_MAXADS
            return _maxAdsRewardedController.IsRewardedAvailable();
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
