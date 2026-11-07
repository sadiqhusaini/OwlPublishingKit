using System;
using UnityEngine;

namespace Owl.Monetization
{
#if USE_ADMOB
    public class GooglesAdsController : MonoBehaviour
    {
        private static string ClassName => $"<color=#A4FF00>GoogleAds</color>";

        [Obsolete("Obsolete")]
        private void Start(){
            
            Initialize(); 
        }
        
        [Obsolete("Obsolete")]
        private void Initialize()
        {
            Log("Initializing Request");

            GoogleMobileAds.Api.MobileAds.RaiseAdEventsOnUnityMainThread = true;
            GoogleMobileAds.Api.MobileAds.Initialize((GoogleMobileAds.Api.InitializationStatus initStatus) =>
            {
                if (!this) return;
                var map = initStatus.getAdapterStatusMap();
                foreach (var keyValuePair in map)
                {
                    var className = keyValuePair.Key;
                    var status = keyValuePair.Value;
                    switch (status.InitializationState)
                    {
                        case GoogleMobileAds.Api.AdapterState.NotReady:
                            Log($"Adapter: {className} not ready.");
                            break;
                        case GoogleMobileAds.Api.AdapterState.Ready:
                            Log($"Adapter: {className} is initialized.");
                            break;
                    }
                }
                OnInitializationComplete();
            });
        }

        private void OnInitializationComplete()
        {
            GoogleMobileAds.Common.MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (!this) return;
                Log("Initializing Completed");
                AdManager.AdsInitialized();
            });

        }
        
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