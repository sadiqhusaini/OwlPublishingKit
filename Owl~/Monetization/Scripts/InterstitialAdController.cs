using System;
using System.Collections;
using UnityEngine;


namespace Owl.Monetization
{
    public class InterstitialAdController : AdType
    {
        private OwlSettings _owlSettings;
        private Coroutine _loadDelayCol;

#if USE_ADMOB
        private GoogleAdsInterstitialController _googleAdsInterstitialController;
#endif

#if USE_MAXADS
        private MaxAdsInterstitialController _maxAdsInterstitialController;
#endif

        private int floorIndex;
        private AdFloor adFloor;
        private float flooringDelay = 60f;
        private int _retryAttempt;

        protected new void OnEnable()
        {
            base.OnEnable();
            if (_owlSettings != null) CheckAdLoad(Utility.InternetManager.IsInternetAvailable);
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


        private void SetReference()
        {
            _owlSettings = OwlManager.OwlSettings;
#if USE_ADMOB
            _googleAdsInterstitialController = gameObject.AddComponent<GoogleAdsInterstitialController>();
#elif USE_MAXADS
            _maxAdsInterstitialController = gameObject.AddComponent<MaxAdsInterstitialController>();
#endif
        }

        protected override void CheckAdLoad(bool result)
        {
            if (adLoadedStatus)
                return;

            if (result && _loadDelayCol == null)
                LoadAd();
        }


        private void SetTestIds()
        {
#if USE_ADMOB
            base.adIdAndroid = "ca-app-pub-3940256099942544/1033173712";
            base.adIdIOS = "ca-app-pub-3940256099942544/4411468910";
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
            var adUnits = _owlSettings.GetInterstitialID(index);
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
#if USE_MAXADS
            _maxAdsInterstitialController.LoadAd(ADUnitId, () =>
            {
                _retryAttempt = 0;
                ReportAdLoad("Interstitial", true);
            }, ScheduleMaxRetry);
#endif

#endif
        }

#if USE_MAXADS
        private void ScheduleMaxRetry()
        {
            ReportAdLoad("Interstitial", false);
            if (_loadDelayCol != null || !isActiveAndEnabled) return;
            var delay = System.Math.Min(60f, (float)System.Math.Pow(2, System.Math.Min(_retryAttempt++, 6)));
            _loadDelayCol = StartCoroutine(RetryMax(delay));
        }

        private IEnumerator RetryMax(float delay)
        {
            yield return new WaitForSecondsRealtime(System.Math.Min(60f, delay + UnityEngine.Random.Range(0f, 1f)));
            _loadDelayCol = null;
            if (Utility.InternetManager.IsInternetAvailable && Utility.ConsentManager.CanRequestAds &&
                !AdManager.IsRemoveAdsPurchased()) LoadAd();
        }
#endif

        protected new void OnDisable()
        {
            base.OnDisable();
            if (_loadDelayCol != null) StopCoroutine(_loadDelayCol);
            _loadDelayCol = null;
        }

        private void LoadAd(int index)
        {
            SetIds(index);
            if (!HasValidAdUnit()) return;
#if USE_ADMOB
            _googleAdsInterstitialController.LoadAd(ADUnitId,adFloor, ()=>
            {
                ReportAdLoad("Interstitial", true);
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

            if (_loadDelayCol == null)
            {
                _loadDelayCol = StartCoroutine(LoadDelay(delay));
            }
        }

        private IEnumerator LoadDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            _loadDelayCol = null;
            if (Utility.InternetManager.IsInternetAvailable && Utility.ConsentManager.CanRequestAds) LoadAd(floorIndex);
        }



        public void ShowAd(string placement)
        {
            ReportAdLoad("Interstitial", false);
            AdManager.OnAdOpen();
            AdStarting();
#if USE_ADMOB
            _googleAdsInterstitialController.ShowAd(placement,() => { ReportAdUnloaded("Interstitial");},()=>{ AdOpened(true,false); },()=>{ AdClosed(true,false);});
#elif USE_MAXADS
            _maxAdsInterstitialController.ShowAd( placement,() => { ReportAdUnloaded("Interstitial"); },()=>{ AdOpened(true,false); },()=>{ AdClosed(true,false);});
#endif
        }




        public bool IsInterstitialAvailable()
        {
#if USE_ADMOB
            return _googleAdsInterstitialController.IsInterstitialAvailable();
#elif USE_MAXADS
            return  _maxAdsInterstitialController.IsInterstitialAvailable();
#endif
            return false;
        }
    }


}
