using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Owl.Monetization
{
    public class FlooringInterstitialAdController : MonoBehaviour
    {
        public List<InterstitialAdController> interstitialAdControllers= new List<InterstitialAdController>();

        private InterstitialAdController _interstitialAdController;
        
        public void AddInterstitialAdController(InterstitialAdController interstitialAdController)
        {
            interstitialAdControllers.Add(interstitialAdController);
        }
        
        public void ShowInterstitial(string placement)
        {
            if(IsInterstitialAvailable())
            {
                Analytics.AnalyticsManager.TrackInterstitialAd(ALAdAction.Show,placement);
                _interstitialAdController.ShowAd(placement);
            }
            else
            {
                AdManager.InterstitialClosed("NotAvailable Interstitial: "+placement);
            }
        }
        
        public void ShowInterstitialShadow(string placement)
        {
            _interstitialAdController.ShowAd(placement);
        }
        public bool IsInterstitialAvailable()
        {
            foreach (var t in interstitialAdControllers.Where(t => t.IsInterstitialAvailable()))
            {
                _interstitialAdController = t;
                return true;
            }

            return false;
        }
    }
}