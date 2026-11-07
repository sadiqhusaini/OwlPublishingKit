using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Owl
{
    public enum ALAdType
    {
        Undefined = 0,
        Video = 1,
        RewardedVideo = 2,
        Playable = 3,
        Interstitial = 4,
        OfferWall = 5,
        Banner = 6,
        AppOpen = 7
    }
    
    public enum ALAdAction
    {
        Undefined = 0,
        Clicked = 1,
        Show = 2,
        FailedShow = 3,
        RewardReceived = 4,
        Request = 5,
        Loaded = 6
    }

    public enum BannerPosition
    {
        Top,
        Bottom,
        Center,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        CenterLeft,
        CenterRight
    }
    
    public enum OwlSDKState
    {
        None,
        Booting,
        CheckingInternet,
        RequestingConsent,
        InitializingFirebase,
        InitializingAds,
        Ready,
        Paused,
        Destroyed
    }
    
    public enum OwlInitializationResult
    {
        Success,
        Failed,
        Timeout,
        Skipped
    }

}