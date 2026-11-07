using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Owl.Monetization
{
    public class FlooringRewardedInterstitialAdController : MonoBehaviour
    {
        public List<RewardedInterstitialAdController> rewardedInterstitialAdControllers = new List<RewardedInterstitialAdController>();

        private RewardedInterstitialAdController _rewardedInterstitialAdController;

        public void AddRewardedInterstitialAdController(RewardedInterstitialAdController rewardedAdController)
        {
            rewardedInterstitialAdControllers.Add(rewardedAdController);
        }
        
        public void ShowRewardedInterstitial(string placement)
        {
            if (IsRewardedInterstitialAvailable())
            {
                Analytics.AnalyticsManager.TrackRewardedInterstitialAd(ALAdAction.Show, placement);
                _rewardedInterstitialAdController.ShowAd(placement);
            }
            else
            {
                AdManager.RewardGiven(false,placement);
                
            }
        }
        
        public void ShowRewardedInterstitialShadow(string placement)
        {
            _rewardedInterstitialAdController.ShowAd(placement);
        }
        public bool IsRewardedInterstitialAvailable()
        {
            foreach (var rt in rewardedInterstitialAdControllers.Where(t => t.IsRewardedInterstitialAvailable()))
            {
                _rewardedInterstitialAdController = rt;
                return true;
            }

            return false;
        }
    }
}