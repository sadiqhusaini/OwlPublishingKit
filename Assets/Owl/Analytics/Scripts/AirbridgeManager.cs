using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Owl.Utility;

namespace Owl.Analytics.MMP
{
    public class AirbridgeManager : MonoBehaviour
    {

#if USE_AIRBRIDGE

#if USE_MAXADS
        public static void OnAdRevenuePaidEvent(MaxSdkBase.AdInfo adInfo)
        {
            if (!ConsentManager.CanCollectAttribution) return;
            if (adInfo != null) {
                AirbridgeEvent @event = new AirbridgeEvent("airbridge.adImpression");

                var appLovin = new Dictionary<string, object>();
                appLovin["revenue"] = adInfo.Revenue;
                appLovin["country_code"] = MaxSdk.GetSdkConfiguration().CountryCode;
                appLovin["network_name"] = adInfo.NetworkName;
                appLovin["network_placement"] = adInfo.NetworkPlacement;
                appLovin["adunit_identifier"] = adInfo.AdUnitIdentifier;
                appLovin["creative_identifier"] = adInfo.CreativeIdentifier;
                appLovin["placement"] = adInfo.Placement;

                var adPartners = new Dictionary<string, object>();
                adPartners["appLovin"] = appLovin;

                @event.SetAction(adInfo.NetworkName);
                @event.SetLabel(adInfo.NetworkPlacement);
                @event.SetValue(adInfo.Revenue);
                @event.AddSemanticAttribute("adPartners", adPartners);
                // AppLovin MAX has a default currency of USD
                @event.AddSemanticAttribute("currency", "USD");

                AirbridgeUnity.TrackEvent(@event);
            }
        }
#endif


#if USE_ADMOB
        public static void HandleAdPaidEvent(string placement, string adFormat, string adUnitId, GoogleMobileAds.Api.AdValue adValue,GoogleMobileAds.Api.ResponseInfo responseInfo)
        {
            if (!ConsentManager.CanCollectAttribution) return;
            var adNetworkAdapter = responseInfo.GetMediationAdapterClassName();
            var @event = new AirbridgeEvent("airbridge.adImpression");
            var admob = new Dictionary<string, object>();
            admob["placement"] = placement;
            admob["value_micros"] = adValue.Value;
            admob["currency_code"] = adValue.CurrencyCode;
            admob["precision"] = adValue.Precision;
            admob["ad_unit_id"] = adUnitId;
            admob["ad_Format"] = adFormat;
            admob["ad_network_adapter"] = adNetworkAdapter;
            var adPartners = new Dictionary<string, object>();
            adPartners["admob"] = admob;
            @event.SetAction(adUnitId);
            @event.SetLabel(adNetworkAdapter);
            @event.SetValue(adValue.Value / 1000000.0);
            @event.AddSemanticAttribute("adPartners", adPartners);
            @event.AddSemanticAttribute("currency", adValue.CurrencyCode);
            AirbridgeUnity.TrackEvent(@event);
        }
#endif

        public static void TrackProgressionEvent(string action,string label,int value,Dictionary<string, object> customParameters=null){
            if (!ConsentManager.CanCollectAttribution) return;
            var @event = new AirbridgeEvent("Progression");
            @event.SetAction(action);
            @event.SetLabel(label);
            @event.SetValue(value);
            if (customParameters!=null)
            {
                @event.AddCustomAttribute("custom_Parameters",customParameters);
            }

            AirbridgeUnity.TrackEvent(@event);
        }

        public static void TrackDesignEvent(string label,Dictionary<string, object> customParameters=null){
            if (!ConsentManager.CanCollectAttribution) return;
            var @event = new AirbridgeEvent("Design");
            @event.SetLabel(label);
            if (customParameters!=null)
            {
                @event.AddCustomAttribute("custom_Parameters",customParameters);
            }
            AirbridgeUnity.TrackEvent(@event);
        }
        
        public static void TrackCustomEvent(string categoryKey,string action,string label,Dictionary<string, object> customParameters=null){
            if (!ConsentManager.CanCollectAttribution) return;
            var @event = new AirbridgeEvent(categoryKey);
            @event.SetAction(action);
            @event.SetLabel(label);
            if (customParameters!=null)
            {
                @event.AddCustomAttribute("custom_Parameters",customParameters);
            }
            AirbridgeUnity.TrackEvent(@event);
        }
#endif


    }
}