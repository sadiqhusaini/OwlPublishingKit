using System;
using System.Collections.Generic;
using UnityEngine;
using Owl.Utility;

namespace Owl.Analytics.MMP
{
    public class SolarEngineManager : MonoBehaviour
    {
#if USE_SOLARENGINE


        [Obsolete("Obsolete")]
        private void OnEnable()
        {
#if USE_IAP
            Monetization.IAPManager.OnIAPProcessPurchase += OnIAPPurchaseComplete;
            Monetization.IAPManager.OnIAPPurchaseFailed += OnIAPPurchaseFailed;
            Monetization.IAPManager.OnIAPRestoreSuccessFul += OnIAPRestoreSuccessFul;
#endif
        }

      
#if USE_IAP
        [Obsolete("Obsolete")]
        private void OnIAPPurchaseComplete(UnityEngine.Purchasing.PendingOrder order)
        {
            if (order == null || order.CartOrdered == null)
                return;

            foreach (var item in order.CartOrdered.Items())
            {
                if (item?.Product != null)
                    TrackPurchaseSuccess(item.Product, order.Info.TransactionID);
            }
        }

        private void OnIAPPurchaseFailed(UnityEngine.Purchasing.FailedOrder order)
        {
            if (order == null || order.CartOrdered == null)
                return;

            foreach (var item in order.CartOrdered.Items())
            {
                if (item?.Product != null)
                    TrackPurchaseFailed(item.Product, order.Info.TransactionID);
            }
        }
        
        private void OnIAPRestoreSuccessFul()
        {
        
        }

#endif
        [Obsolete("Obsolete")]
        private void OnDisable()
        {
#if USE_IAP
            Monetization.IAPManager.OnIAPProcessPurchase -= OnIAPPurchaseComplete;
            Monetization.IAPManager.OnIAPPurchaseFailed -= OnIAPPurchaseFailed;
            Monetization.IAPManager.OnIAPRestoreSuccessFul -= OnIAPRestoreSuccessFul;

#endif
        }

        public static void Init(string appKey, bool isDebugMode)
        {
            if (!ConsentManager.CanCollectAttribution) return;
            var seConfig = new SolarEngine.SEConfig
            {
                logEnabled = isDebugMode,
                isDebugModel = isDebugMode
            };

            var callback = new SolarEngine.Analytics.SEAttributionCallback(AttributionCallback);
            seConfig.attributionCallback = callback;

            SolarEngine.Analytics.SESDKInitCompletedCallback initCallback = InitSuccessCallback;
            seConfig.initCompletedCallback = initCallback;
      
            SolarEngine.Analytics.preInitSeSdk(appKey);
            SolarEngine.Analytics.initSeSdk(appKey, seConfig);
        }
   
        private static void InitSuccessCallback(int code)
        {
            // 0	Initialization success
            // 101	Initialization failed, as pre-initialization is not called
            // 102	Initialization failed, due to illegal appkey
            // 103	Initialization failed, due to null context (Android only)
            // 104	Initialization failed, due to missing distinct_id (Android only)
            if (code == 0)
                CustomDebug.Log("SolarEngine initialized.");
            else
                CustomDebug.LogError($"SolarEngine initialization failed: code {code}.");
        }


        private static void AttributionCallback(int code, Dictionary<string, object> attribution)
        {
            if (code != 0)
                CustomDebug.LogWarning($"SolarEngine attribution failed: code {code}.");
        }
        // 1 = Rewarded Video
        // 2 = Splash (Open Screen)
        // 3 = Interstitial
        // 4 = Fullscreen Video
        // 5 = Banner
        // 6 = Native (Info Flow)
        // 7 = Short Video Native
        // 8 = Large Banner
        // 9 = Video Sticker
        // 0 = Other
#if USE_ADMOB
   public static void TrackAdImpressionAdmob
      (string placement, string adFormat, string adUnitId, GoogleMobileAds.Api.AdValue adValue,GoogleMobileAds.Api.ResponseInfo responseInfo)
   {
            if (!ConsentManager.CanCollectAttribution) return;
      var ad_type=0;
      switch (adFormat)
      {
         case "AppOpen":
            ad_type = 2;
            break;
         case "Banner":
            ad_type = 5;
            break;
         case "MREC":
            ad_type = 8;
            break;
         case "Interstitial":
            ad_type = 3;
            break;
         case "Rewarded":
            ad_type = 1;
            break;
         case "RewardedInterstitial":
            ad_type = 1;
            break;
         
      }
      
      double valueMicros = adValue.Value;
      string currencyCode = adValue.CurrencyCode;
      
      var impressionAttributes = new SolarEngine.ImpressionAttributes
      {
         ad_platform = responseInfo.GetMediationAdapterClassName(),
         mediation_platform = "admob",
         ad_appid = Application.platform == RuntimePlatform.Android? Owl.OwlManager.OwlSettings.adSettings.googleAds.appIdAndroid :Owl.OwlManager.OwlSettings.adSettings.googleAds.appIdIOS,
         ad_id = adUnitId,
         ad_type = ad_type,
         ad_ecpm = valueMicros/1000,
         currency_type = currencyCode,
         is_rendered = true,
         customProperties = new Dictionary<string, object>
         {
            { "placement", placement},
         },
      };
      SolarEngine.Analytics.trackAdImpression(impressionAttributes);

   }
   
   public static void TrackAdClick(string placement, string adFormat, string adUnitId,GoogleMobileAds.Api.ResponseInfo responseInfo)
   {
            if (!ConsentManager.CanCollectAttribution) return;
       
       var ad_type=0;
       switch (adFormat)
       {
           case "AppOpen":
               ad_type = 2;
               break;
           case "Banner":
               ad_type = 5;
               break;
           case "MREC":
               ad_type = 8;
               break;
           case "Interstitial":
               ad_type = 3;
               break;
           case "Rewarded":
               ad_type = 1;
               break;
           case "RewardedInterstitial":
               ad_type = 1;
               break;
         
       } 

       var adClickAttributes = new SolarEngine.AdClickAttributes
       {
           ad_platform = responseInfo.GetMediationAdapterClassName(),
           mediation_platform = "admob",
           ad_id = adUnitId,
           ad_type = ad_type,
           customProperties = new Dictionary<string, object>
           {
               { "placement", placement},
           },
       };
       SolarEngine.Analytics.trackAdClick(adClickAttributes);

   }
   
#endif
#if USE_MAXADS
        public static void TrackAdImpressionMax(MaxSdkBase.AdInfo ad)
        {
            if (!ConsentManager.CanCollectAttribution) return;
            if (ad != null)
            {
                var adType = ad.AdFormat switch
                {
                    "APP_OPEN" => 2,
                    "BANNER" => 5,
                    "MREC" => 8,
                    "INTER" => 3,
                    "REWARDED" => 1,
                    _ => 0
                };

                var impressionAttributes = new SolarEngine.ImpressionAttributes
                {
                    ad_platform = ad.NetworkName.ToLower(), // "applovin", "admob", etc.
                    mediation_platform = "max",
                    ad_appid = Owl.OwlManager.OwlSettings.adSettings.maxAds.sdkID,
                    ad_id = ad.AdUnitIdentifier,
                    ad_type = adType,
                    ad_ecpm = ad.Revenue * 1000, // Revenue is per impression
                    currency_type = "USD",
                    is_rendered = true,
                    customProperties = new Dictionary<string, object>
                    {
                        { "placement", ad.Placement },
                    },
                };
                SolarEngine.Analytics.trackAdImpression(impressionAttributes);
            }
        }
        
        public static void TrackAdClick(MaxSdkBase.AdInfo ad)
        {
            if (!ConsentManager.CanCollectAttribution) return;
            if (ad == null)
                return;
            
            var adType = ad.AdFormat switch
            {
                "APP_OPEN" => 2,
                "BANNER" => 5,
                "MREC" => 8,
                "INTER" => 3,
                "REWARDED" => 1,
                _ => 0
            };
            

            var adClickAttributes = new SolarEngine.AdClickAttributes
            {
                ad_platform = ad.NetworkName.ToLower(),
                mediation_platform = "max",
                ad_id = ad.AdUnitIdentifier,
                ad_type = adType,
                customProperties = new Dictionary<string, object>()
            };
            SolarEngine.Analytics.trackAdClick(adClickAttributes);

        }
#endif

        // public void TrackAdClick()
        // {
        //
        //     var adClickAttributesd = new SolarEngine.AdClickAttributes();
        //     adClickAttributesd.ad_platform = "izz";
        //     adClickAttributesd.mediation_platform = "gromore_test";
        //     adClickAttributesd.ad_id = "product_id_test";
        //     adClickAttributesd.ad_type = 1;
        //    
        //     adClickAttributesd.customProperties = new Dictionary<string, object>();
        //     SolarEngine.Analytics.trackAdClick(adClickAttributesd);
        //
        // }
        
#if USE_IAP
        private void TrackPurchaseSuccess(UnityEngine.Purchasing.Product purchasedProduct, string transactionId)
        {
            if (!ConsentManager.CanCollectAttribution || purchasedProduct?.metadata == null) return;
            var purchaseData = new SolarEngine.ProductsAttributes
            {
                product_name = purchasedProduct.definition.id,
                product_id = purchasedProduct.definition.id,
                product_num = 1,
                currency_type = purchasedProduct.metadata.isoCurrencyCode,
                order_id = transactionId,
                paystatus = SolarEngine.PayStatus.Success, // 1 = success
                pay_type = "UnityIAP",
                pay_amount = (double)purchasedProduct.metadata.localizedPrice
            };

            SolarEngine.Analytics.trackPurchase(purchaseData);
        }

        private void TrackPurchaseFailed(UnityEngine.Purchasing.Product product, string transactionId)
        {
            if (!ConsentManager.CanCollectAttribution || product?.metadata == null) return;
            var purchaseData = new SolarEngine.ProductsAttributes
            {
                product_name = product.definition.id,
                product_id = product.definition.id,
                product_num = 1,
                currency_type = product.metadata.isoCurrencyCode,
                order_id = transactionId ?? "N/A",
                paystatus = SolarEngine.PayStatus.Fail, // 2 = fail
                fail_reason = "unknown",
                pay_type = "UnityIAP",
                pay_amount = (double)product.metadata.localizedPrice
            };
            SolarEngine.Analytics.trackPurchase(purchaseData);
        }
        
        
#endif    
#endif
    }

}
