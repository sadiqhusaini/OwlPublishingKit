
using UnityEngine;

public class Test : MonoBehaviour
{
    public void ShowB()
    {
        Owl.Monetization.AdManager.ShowBanner();

    }

    public void HideB()
    {
        Owl.Monetization.AdManager.HideBanner();

    }

    public void ShowIn()
    {
        Owl.Monetization.AdManager.ShowInterstitial("Interstitial", () =>
            Owl.CustomDebug.Log("Demo interstitial completed."));
    }

    public void ShowReIn()
    {
        Owl.Monetization.AdManager.ShowRewardedInterstitial("RewardedInterstitial", given =>
            Owl.CustomDebug.Log($"Demo rewarded interstitial completed. Reward granted: {given}"));
    }

    public void ShowRe()
    {
        Owl.Monetization.AdManager.ShowRewardedVideo("Rewarded", given =>
            Owl.CustomDebug.Log($"Demo rewarded ad completed. Reward granted: {given}"));
    }

    public void StartT()
    {
        Owl.Monetization.AdManager.StartTimerAd();
    }
    
    public void StopT()
    {
        Owl.Monetization.AdManager.StopTimerAd();
    }
   
    public void ResetT()
    {
        Owl.Monetization.AdManager.StartTimerAd();
    }

    private static int _levelNo=1;

    public void TestTrackLevelStart()
    {
        Owl.Analytics.AnalyticsManager.TrackLevelStart(_levelNo);
    }
        
    public void TestTrackLevelComplete()
    {
        Owl.Analytics.AnalyticsManager.TrackLevelComplete(_levelNo);
        _levelNo++;
    }
        
    public void TestTrackLevelFail()
    {
        Owl.Analytics.AnalyticsManager.TrackLevelFail(_levelNo);
    }

    public void TestTrackDesign()
    {
        Owl.Analytics.AnalyticsManager.TrackDesignEvent("TestDesign:"+_levelNo);
    }
}
