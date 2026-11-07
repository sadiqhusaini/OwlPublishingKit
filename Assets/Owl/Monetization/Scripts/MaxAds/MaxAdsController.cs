using System;
using UnityEngine;

namespace Owl.Monetization
{
#if USE_MAXADS
    public class MaxAdsController : MonoBehaviour
    {
        private static string ClassName => $"<color=#004EFF>MaxAds</color>";

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            MaxSdkCallbacks.OnSdkInitializedEvent += OnInitialized;
            MaxSdk.InitializeSdk();

        }
        
        private void OnInitialized(MaxSdkBase.SdkConfiguration configuration)
        {
            MaxSdkCallbacks.OnSdkInitializedEvent -= OnInitialized;
            Log("Initializing Completed");
            AdManager.AdsInitialized();
        }

        private void OnDestroy() => MaxSdkCallbacks.OnSdkInitializedEvent -= OnInitialized;

        public static void Log(string value)
        {
            CustomDebug.Log($"{ClassName} {value}");
        }
        public static void LogWarning(string value)
        {
            CustomDebug.LogWarning($"{ClassName} {value}");
        }

        public static void LogError(string value)
        {
            CustomDebug.LogError($"{ClassName} {value}");
        }
    }
    
  
#endif

}