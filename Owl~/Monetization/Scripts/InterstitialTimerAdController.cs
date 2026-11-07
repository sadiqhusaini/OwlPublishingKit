using System;
using UnityEngine;

namespace Owl.Monetization
{
    public class InterstitialTimerAdController : MonoBehaviour
    {
        public  float timer; 
        private TimerAdCanvasScript _timerCanvas;
        
        private static float _delay = 30.0f;
        private static float _timer = 0.0f;
        
        private static bool _startTimer = false;
        private static bool _processStarted = false;
        private static bool _autoStart = false;
        private static bool _halt = false;
        private static bool _stopOnAdBreak = false;
        private void Awake()
        {
            ResetTimerAd();
        }

        private void Start()
        {
            Init(OwlManager.OwlSettings.OwlComponents.SpawnTimerAdCanvas(transform));
        }

        private void Init(TimerAdCanvasScript timerAdCanvasScript)
        {
            _timerCanvas = timerAdCanvasScript;
            _timerCanvas?.Init(ShowInterstitial,ShowRewardedVideo);
            _timerCanvas?.HideTimerCanvas();
        }

        private void OnEnable()
        {
            try
            {
                AdManager.OnAdOpenedEvent += OnAdOpened;
                AdManager.OnAdClosedEvent += OnAdClosed;
                AdManager.OnAdBreakLayerDisplayEvent += OnAdBreakLayerDisplayEvent;

            }
            catch (Exception e)
            {
                CustomDebug.LogError($"Timer ad event subscription failed: {e}");
            }
          
        }

        private void OnDisable()
        {
            try
            {
                AdManager.OnAdOpenedEvent -= OnAdOpened;
                AdManager.OnAdClosedEvent -= OnAdClosed;
                AdManager.OnAdBreakLayerDisplayEvent -= OnAdBreakLayerDisplayEvent;

            }
            catch (Exception e)
            {
                CustomDebug.LogError($"Timer ad event cleanup failed: {e}");
            }
        }

        private static void OnAdBreakLayerDisplayEvent(bool status)
        {
            _stopOnAdBreak = status;
        }
        
        
        private void LateUpdate()
        {

            if (!AdManager.IsTimerAdAvailable || AdManager.IsRemoveAdsPurchased())
                return;

            timer = _timer;
            
            if (!_startTimer)
                return; 
            
            
            if (_stopOnAdBreak)
                return; 
            
            if (_halt)
                return;

            if (_timer <= 5)
            {
                _timerCanvas?.UpdateTimerText(_timer);
                _timerCanvas?.ShowTimerCanvas();
            }
            else
            {
                _timerCanvas?.HideTimerCanvas();
            }

            if (_timer <= 0)
            {
                _halt = true;
                ShowInterstitial();
                return;
            }
            _timer -= Time.deltaTime;
        }
        
        public static void Initialize(bool autoStart,float interval)
        {
            _autoStart = autoStart;
            _delay = interval;
            
            ResetTimerAd();
            if (_autoStart)
                StartTimerAd();

        }
        public static void StartTimerAd()
        {
            _startTimer = true;
            _processStarted = true;
        }
        
        public static void StopTimerAd()
        {
            _startTimer = false;
            _processStarted = false;
        }

        private void ResetAll()
        {
            _timerCanvas?.HideTimerCanvas();
            ResetTimerAd();
        }

        public static void ResetTimerAd()
        {
            _timer = _delay;
        }

        private void OnAdOpened()
        {
            _startTimer = false;
        }
        
        private void OnAdClosed()
        {
            ResetTimerAd();
            
            if(AdManager.IsTimerAdAvailable)
                if(_processStarted)
                    _startTimer = true;

        }

        private void ShowRewardedVideo()
        {
            AdManager.ShowRewardedVideo("TimerAdCashReward_RV",(bool obj) =>
            {
                ResetTimerAd();
                // UiManager.instance.AddCash(100);// give 100 coins
                ResetAll();
                _halt = false;
                Time.timeScale = 1;
                
            });
        }

        private void ShowInterstitial()
        {
            AdManager.ShowInterstitial("TimerInterstitial",() =>
            {
                Time.timeScale = 1;
                ResetTimerAd();
                _halt = false;
                ResetAll();
            });
        }
    }
}