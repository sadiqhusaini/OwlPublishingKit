namespace Owl
{
    public static class Constants
    {
        public const string USE_FIREBASE = "USE_FIREBASE";
        public const string USE_FIREBASEREMOTECONFIG = "USE_FIREBASEREMOTECONFIG";
        public const string USE_FIREBASEMESSAGING = "USE_FIREBASEMESSAGING";
        public const string USE_FIREBASEANALYTICS = "USE_FIREBASEANALYTICS";
        public const string USE_FIREBASECRASHLYTICS = "USE_FIREBASECRASHLYTICS";
        public const string USE_ADMOB = "USE_ADMOB";
        public const string USE_MAXADS = "USE_MAXADS";
        public const string USE_IAP = "USE_IAP";
        public const string USE_GAMEANALYTICS = "USE_GAMEANALYTICS";
        public const string USE_FACEBOOK = "USE_FACEBOOK";
        public const string USE_FACEBOOK_SDK = "USE_FACEBOOK_SDK";
        
        
        public const string USE_MMP = "USE_MMP";
        public const string USE_AIRBRIDGE = "USE_AIRBRIDGE";
        public const string USE_SOLARENGINE = "USE_SOLARENGINE";
        
        public const string REMOVE_ADS = "REMOVE_ADS";
        public const string REMOVE_BANNER_ADS = "REMOVE_BANNER_ADS";
        
        public const string REMOTECOMPLETEADS = "RemoteCompleteAds";
        public const string REMOTEAPPOPEN = "RemoteAppOpen";
        public const string REMOTEBANNER = "RemoteBanner";
        public const string REMOTEINTERSTITIAL = "RemoteInterstitial";
        public const string REMOTEREWARDED = "RemoteRewarded";
        public const string REMOTEREWARDEDINTERSTITIAL = "RemoteRewardedInterstitial";
        public const string REMOTEMREC = "RemoteMREC";
        public const string REMOTETIMEDADS = "RemoteTimedAds";
        
    }

    public enum RemoteDataStatus
    {
        LoadedAndReady,RetrievalNotFinished,Unsuccessful
    }
    public enum Mediation
    {
        NoAds,GoogleAds,Max
    }

    public enum AnalyticsSDK
    {
        WithoutData,Firebase,GameAnalytics,FirebaseGameAnalytics
    }
    public enum MMPSDK
    {
        NoMMP,Airbridge,SolarEngine,AirbridgeSolarEngine
    }

    public enum Options
    {
        Yes,No
    }
}
