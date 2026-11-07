using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Owl.Monetization
{
    public class AdSettings : ScriptableObject
    {
        public GoogleAds googleAds = new GoogleAds();
        public MaxAds maxAds = new MaxAds();
    }

    [Serializable]
    public class GoogleAds : ISerializationCallbackReceiver
    {
        public GoogleAdUnitMode mode = GoogleAdUnitMode.Single;
        public AdUnit bannerAdUnit = new AdUnit();
        public AdUnit mRecAdUnit = new AdUnit();
        public AdUnit interstitialAdUnit = new AdUnit();
        public AdUnit rewardedInterstitialAdUnit = new AdUnit();
        public AdUnit rewardedAdUnit = new AdUnit();
        public AdUnit appOpenAdUnit = new AdUnit();
        [Space(20)] public string appIdAndroid;
        public string appIdIOS;

        [Space(20)] public AdUnit[] bannerAdUnits = { new AdUnit() };
        [Space(20), FormerlySerializedAs("mRECAdUnits")] public AdUnit[] mRecAdUnits = { new AdUnit() };
        [Space(20)] public AdUnit[] interstitialAdUnits = { new AdUnit() };
        [Space(20)] public AdUnit[] rewardedInterstitialAdUnits = { new AdUnit() };
        [Space(20)] public AdUnit[] rewardedAdUnits = { new AdUnit() };
        [Space(20)] public AdUnit[] appOpenAdUnits = { new AdUnit() };

        [SerializeField, HideInInspector, FormerlySerializedAs("singleAdUnits")]
        private LegacySingleAdUnitIds _legacySingleIds;

        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize()
        {
            if (_legacySingleIds == null) return;
            bannerAdUnit = LegacySingleAdUnitIds.Merge(bannerAdUnit, _legacySingleIds.bannerAdUnit);
            mRecAdUnit = LegacySingleAdUnitIds.Merge(mRecAdUnit, _legacySingleIds.mRecAdUnit);
            interstitialAdUnit = LegacySingleAdUnitIds.Merge(interstitialAdUnit, _legacySingleIds.interstitialAdUnit);
            rewardedInterstitialAdUnit = LegacySingleAdUnitIds.Merge(rewardedInterstitialAdUnit, _legacySingleIds.rewardedInterstitialAdUnit);
            rewardedAdUnit = LegacySingleAdUnitIds.Merge(rewardedAdUnit, _legacySingleIds.rewardedAdUnit);
            appOpenAdUnit = LegacySingleAdUnitIds.Merge(appOpenAdUnit, _legacySingleIds.appOpenAdUnit);
            _legacySingleIds = null;
        }
    }

    [Serializable]
    public class MaxAds : ISerializationCallbackReceiver
    {
        public MaxAdUnitMode mode = MaxAdUnitMode.Single;
        public AdUnit bannerAdUnit = new AdUnit();
        public AdUnit mRecAdUnit = new AdUnit();
        public AdUnit interstitialAdUnit = new AdUnit();
        public AdUnit rewardedAdUnit = new AdUnit();
        public AdUnit appOpenAdUnit = new AdUnit();
        public string sdkID = "L30WFP_IaD4NhWvoBHOpEpdPdVk82buQ6fl0h4eCkNDEqQbz9paDSZGnFVrh7WokbNxc9MNN7su45ARgVxUigH";

        [Space(20)] public AdUnit[] bannerAdUnits = { new AdUnit() };
        [Space(20), FormerlySerializedAs("mRECAdUnits")] public AdUnit[] mRecAdUnits = { new AdUnit() };
        [Space(20)] public AdUnit[] interstitialAdUnits = { new AdUnit() };
        [Space(20)] public AdUnit[] rewardedAdUnits = { new AdUnit() };
        [Space(20)] public AdUnit[] appOpenAdUnits = { new AdUnit() };

        [SerializeField, HideInInspector, FormerlySerializedAs("singleAdUnits")]
        private LegacySingleAdUnitIds _legacySingleIds;

        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize()
        {
            if (_legacySingleIds == null) return;
            bannerAdUnit = LegacySingleAdUnitIds.Merge(bannerAdUnit, _legacySingleIds.bannerAdUnit);
            mRecAdUnit = LegacySingleAdUnitIds.Merge(mRecAdUnit, _legacySingleIds.mRecAdUnit);
            interstitialAdUnit = LegacySingleAdUnitIds.Merge(interstitialAdUnit, _legacySingleIds.interstitialAdUnit);
            rewardedAdUnit = LegacySingleAdUnitIds.Merge(rewardedAdUnit, _legacySingleIds.rewardedAdUnit);
            appOpenAdUnit = LegacySingleAdUnitIds.Merge(appOpenAdUnit, _legacySingleIds.appOpenAdUnit);
            _legacySingleIds = null;
        }
    }

    // Explicit values preserve modes stored in existing Unity assets.
    public enum GoogleAdUnitMode { Single = 1, Multiple = 2, Flooring = 0 }
    public enum MaxAdUnitMode { Single = 1, Multiple = 0 }

    [Serializable]
    internal class LegacySingleAdUnitIds
    {
        [FormerlySerializedAs("banner")] public AdUnit bannerAdUnit = new AdUnit();
        [FormerlySerializedAs("mrec")] public AdUnit mRecAdUnit = new AdUnit();
        [FormerlySerializedAs("interstitial")] public AdUnit interstitialAdUnit = new AdUnit();
        [FormerlySerializedAs("rewardedInterstitial")] public AdUnit rewardedInterstitialAdUnit = new AdUnit();
        [FormerlySerializedAs("rewarded")] public AdUnit rewardedAdUnit = new AdUnit();
        [FormerlySerializedAs("appOpen")] public AdUnit appOpenAdUnit = new AdUnit();

        internal static AdUnit Merge(AdUnit current, AdUnit legacy)
        {
            current ??= new AdUnit();
            if (legacy == null) return current;
            if (string.IsNullOrEmpty(current.adUnitAndroid)) current.adUnitAndroid = legacy.adUnitAndroid;
            if (string.IsNullOrEmpty(current.adUnitIOS)) current.adUnitIOS = legacy.adUnitIOS;
            return current;
        }

    }

    /// <summary>One platform ID pair, with an optional Google flooring priority.</summary>
    [Serializable]
    public class AdUnit : ISerializationCallbackReceiver
    {
        public string adUnitAndroid;
        public string adUnitIOS;
        public AdFloor adFloor = AdFloor.All;

        // Read the old Google wrapper without retaining a separate flooring model.
        [SerializeField, HideInInspector, FormerlySerializedAs("adUnits")]
        private LegacyAdUnitIds _legacyIds;

        [Serializable]
        private class LegacyAdUnitIds
        {
            public string adUnitAndroid = "";
            public string adUnitIOS = "";
        }

        public AdUnit(string android = "", string ios = "", AdFloor floor = AdFloor.All)
        {
            adUnitAndroid = android;
            adUnitIOS = ios;
            adFloor = floor;
        }

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize()
        {
            if (_legacyIds == null) return;
            if (string.IsNullOrEmpty(adUnitAndroid)) adUnitAndroid = _legacyIds.adUnitAndroid ?? "";
            if (string.IsNullOrEmpty(adUnitIOS)) adUnitIOS = _legacyIds.adUnitIOS ?? "";
            _legacyIds = null;
        }
    }

    public enum AdFloor { High = 0, Medium = 1, All = 2 }
}
