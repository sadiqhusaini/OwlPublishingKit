using System;
using UnityEngine;
using  System.Collections;

namespace Owl.Monetization
{
    public class MRECAdController : AdType
    {
        public BannerPosition position;

#if USE_ADMOB
        private GoogleAdsMRECController _googleAdsMrecController;
#elif USE_MAXADS
        private MaxAdsMRECController _maxAdsMrecController;
#endif

        private OwlSettings _owlSettings;
        private Coroutine _loadAgainCol;
        private const int MaxLoadRetries = 5;
        private int _loadRetries;
        private bool _visible = true;
        
        private new void OnEnable()
        {
            base.OnEnable();
            if (_owlSettings != null)
            {
                _loadRetries = 0;
                SetReference();
                LoadAd();
            }
        }

        protected override void CheckAdLoad(bool result)
        {
            if (result && !adLoadedStatus && _loadAgainCol == null && _owlSettings != null)
            {
                _loadRetries = 0;
                LoadAd();
            }
        }

        public void SetPosition(BannerPosition pos)
        {
            position = pos;
            Initialize();

        }
     
        public override void Initialize()
        {
            SetTestIds();
            SetReference();
            _loadRetries = 0;
            LoadAd();

        }
        private void SetReference()
        {
            _owlSettings = OwlManager.OwlSettings;

#if USE_ADMOB
            if(_googleAdsMrecController==null)
                _googleAdsMrecController = gameObject.AddComponent<GoogleAdsMRECController>();
#elif USE_MAXADS
            if(_maxAdsMrecController==null)
                _maxAdsMrecController = gameObject.AddComponent<MaxAdsMRECController>();  
#endif
        }
        
        private void SetTestIds()
        {
#if USE_ADMOB
            base.adIdAndroid = "ca-app-pub-3940256099942544/6300978111";
            base.adIdIOS = "ca-app-pub-3940256099942544/2934735716";
#elif USE_MAXADS
            base.adIdAndroid = "d7864b782ba4f51f";
            base.adIdIOS = "dc99d5c9e6a8f1d4";
#endif
            
#if UNITY_ANDROID
            ADUnitId = base.adIdAndroid;
#elif UNITY_IPHONE
            ADUnitId = base.adIdIOS;
#endif
        }
        private void SetIds()
        {
            var adUnits = _owlSettings.GetMRECID();
            base.SetAdUnitIds(adUnits.adUnitAndroid,adUnits.adUnitIOS);
            
#if UNITY_ANDROID
            ADUnitId = base.adIdAndroid;
#elif UNITY_IPHONE
            ADUnitId = base.adIdIOS;
#endif
        }
        private void LoadAd()
        {
            if (!isActiveAndEnabled) return;
            SetIds();
            if (!HasValidAdUnit()) return;
            ReportAdLoad("MREC", false);
            AdManager.EnableMRECBg(position);

#if USE_ADMOB
            var googlePos = _owlSettings.ConvertGoogleAdPositions(position);
            _googleAdsMrecController.LoadAd(ADUnitId,googlePos, () =>
            {
                ReportAdLoad("MREC", true);
                _loadRetries = 0;
                if (!_visible) HideAd();
            }, () =>
            {
                LoadAgain();
            });

#elif USE_MAXADS
            _maxAdsMrecController.LoadAd(ADUnitId, _owlSettings.ConvertMaxAdViewPositions(position), () =>
            {
                ReportAdLoad("MREC", true);
                _loadRetries = 0;
                if (!_visible) HideAd();
                if (_visible) ShowAd();
            }, () =>
            {
                  LoadAgain();
            });
#endif
        }

        private void LoadAgain()
        {
            ReportAdLoad("MREC", false);
            if (!isActiveAndEnabled || _loadAgainCol != null)
                return;
            if (_loadRetries >= MaxLoadRetries)
            {
                CustomDebug.LogWarning("[Owl Ads] MREC retry limit reached.");
                return;
            }

            float delay = Mathf.Min(30f, 2f * Mathf.Pow(2f, _loadRetries++));
            _loadAgainCol = StartCoroutine(LoadAgainDelay(delay));
        }

        private IEnumerator LoadAgainDelay(float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            // Clear before loading so a synchronous failure can schedule another retry.
            _loadAgainCol = null;
            LoadAd();
        }

        public void ShowAd()
        {
            _visible = true;
#if USE_ADMOB
            _googleAdsMrecController?.ShowAd();
            var googlePos = _owlSettings.ConvertGoogleAdPositions(position);
         
#elif USE_MAXADS
            _maxAdsMrecController?.ShowAd();
#endif
            AdManager.EnableMRECBg(position);
        }
        public void HideAd()
        {
            _visible = false;
#if USE_ADMOB
            _googleAdsMrecController?.HideAd();
#elif USE_MAXADS
            _maxAdsMrecController?.HideAd();
#endif
        }
        private void DestroyAd()
        {
            ReportAdLoad("MREC", false);

#if USE_ADMOB
            _googleAdsMrecController?.DestroyAd();
#elif USE_MAXADS
            _maxAdsMrecController?.DestroyAd();
#endif

            if (_loadAgainCol!=null)
            {
                StopCoroutine(_loadAgainCol);
                _loadAgainCol = null;
            }

            

        }
        private void OnDestroy()
        {
            DestroyAd();
        }
        private new void OnDisable()
        {
            base.OnDisable();
            DestroyAd();
        }
    }
}
