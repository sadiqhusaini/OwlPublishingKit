using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Owl.Monetization
{
    public class FlooringRewardedAdController : MonoBehaviour
    {
        public List<RewardedAdController> rewardedAdControllers = new List<RewardedAdController>();

        private RewardedAdController _rewardedAdController;

        public void AddRewardedAdController(RewardedAdController rewardedAdController)
        {
            rewardedAdControllers.Add(rewardedAdController);
        }

        public void ShowRewardedVideo(string placement, FlooringInterstitialAdController flooringInterstitialAdController, 
            FlooringRewardedInterstitialAdController flooringRewardedInterstitialAdController)
        {

#if USE_ADMOB
            if (!AdManager.IsRewardedEnabled)
            {
                AdManager.RewardGiven(false, placement);
                return;
            }
            if (IsRewardedAvailable())
            {
                Analytics.AnalyticsManager.TrackRewardedVideoAd(ALAdAction.Show, placement);
                _rewardedAdController.ShowAd(placement);
            }
            else if (AdManager.IsRewardedInterstitialEnabled && flooringRewardedInterstitialAdController != null && flooringRewardedInterstitialAdController.IsRewardedInterstitialAvailable())
            {
                Analytics.AnalyticsManager.TrackRewardedInterstitialAd(ALAdAction.Show, placement);
                flooringRewardedInterstitialAdController.ShowRewardedInterstitialShadow(placement+"_Shadow");
            }
            else if (AdManager.IsInterstitialEnabled && flooringInterstitialAdController != null && flooringInterstitialAdController.IsInterstitialAvailable())
            {
                Analytics.AnalyticsManager.TrackInterstitialAd(ALAdAction.Show, placement);
                flooringInterstitialAdController.ShowInterstitialShadow(placement+"_Shadow");
            }
            else
            {
               AdManager.RewardGiven(false,placement);
            }
#endif


        }

        public bool IsRewardedAvailable()
        {
            foreach (var t in rewardedAdControllers.Where(t => t.IsRewardedAvailable()))
            {
                _rewardedAdController = t;
                return true;
            }

            return false;
        }
    }
}