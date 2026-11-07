using System.Collections.Generic;
using System.Text;
using Owl;
using Owl.Analytics;
using Owl.Monetization;
using Owl.Notifications;
using Owl.Utility;
using UnityEngine;
using UnityEngine.UI;

public class OwlStatusScript : MonoBehaviour
{
    [SerializeField] private Text _statusText;

    private const float RefreshInterval = 0.5f;
    private const int MaxRecentEvents = 16;
    private static readonly Dictionary<string, FlowStatus> FlowStatuses = new Dictionary<string, FlowStatus>();
    private static readonly Queue<string> RecentEvents = new Queue<string>();

    private Image _panelBackground;
    private RectTransform _viewport;
    private Text _fontSizeLabel;
    private Scrollbar _verticalScrollbar;
    private float _nextRefresh;
    private string _lastRendered;

    private const int MinFontSize = 12;
    private const int MaxFontSize = 24;
    private const int DefaultFontSize = 16;

    private struct FlowStatus
    {
        public string State;
        public string Detail;

        public FlowStatus(string state, string detail)
        {
            State = state;
            Detail = detail;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetFlow()
    {
        FlowStatuses.Clear();
        RecentEvents.Clear();
    }

    private void Awake()
    {
        CreateDebugPanel();
        ConfigureText();
        RefreshDebugText(true);
    }

    private void Update()
    {
        if (Time.unscaledTime < _nextRefresh) return;
        _nextRefresh = Time.unscaledTime + RefreshInterval;
        RefreshDebugText(false);
    }

    public void ShowStatusInfo()
    {
        if (_panelBackground) _panelBackground.gameObject.SetActive(true);
        _statusText?.gameObject.SetActive(true);
        RefreshDebugText(true);
    }

    public void HideStatusInfo()
    {
        if (_panelBackground) _panelBackground.gameObject.SetActive(false);
        _statusText?.gameObject.SetActive(false);
    }

    public void UpdateStatusInfo(string statusInfo)
    {
        var message = string.IsNullOrWhiteSpace(statusInfo) ? "Status updated" : statusInfo;
        RecordEvent(message);
        RefreshDebugText(true);
    }

    public static void SetFlowStatus(string service, string state, string detail = null)
    {
        if (string.IsNullOrEmpty(service)) return;
        var next = new FlowStatus(state ?? "Unknown", detail ?? string.Empty);
        if (!FlowStatuses.TryGetValue(service, out var previous) || previous.State != next.State || previous.Detail != next.Detail)
            RecordEvent($"{service}: {next.State}{(string.IsNullOrEmpty(next.Detail) ? string.Empty : " — " + next.Detail)}");
        FlowStatuses[service] = next;
        OwlManager.OwlStatusCanvas?.RefreshDebugText(true);
    }

    private static void RecordEvent(string message)
    {
        RecentEvents.Enqueue($"{Time.realtimeSinceStartup,6:0.0}s  {message}");
        while (RecentEvents.Count > MaxRecentEvents) RecentEvents.Dequeue();
    }

    private void CreateDebugPanel()
    {
        var backgroundObject = new GameObject("OwlFlowDebugBackground", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        backgroundObject.transform.SetParent(transform, false);
        backgroundObject.transform.SetAsFirstSibling();

        var backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = new Vector2(0, 1);
        backgroundRect.anchorMax = new Vector2(0, 1);
        backgroundRect.pivot = new Vector2(0, 1);
        backgroundRect.anchoredPosition = new Vector2(12, -12);
        backgroundRect.sizeDelta = new Vector2(620, 700);

        _panelBackground = backgroundObject.GetComponent<Image>();
        _panelBackground.color = new Color(0.025f, 0.045f, 0.065f, 0.9f);
        _panelBackground.raycastTarget = false;

        var viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D), typeof(Image));
        viewportObject.transform.SetParent(backgroundObject.transform, false);
        _viewport = viewportObject.GetComponent<RectTransform>();
        _viewport.anchorMin = Vector2.zero;
        _viewport.anchorMax = Vector2.one;
        _viewport.offsetMin = new Vector2(14, 58);
        _viewport.offsetMax = new Vector2(-30, -14);
        var viewportImage = viewportObject.GetComponent<Image>();
        viewportImage.color = new Color(1, 1, 1, 0);
        viewportImage.raycastTarget = true;

        var scroll = backgroundObject.GetComponent<ScrollRect>();
        scroll.viewport = _viewport;
        scroll.content = _statusText ? _statusText.rectTransform : null;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 24;
        scroll.inertia = true;
        CreateVerticalScrollbar(backgroundObject.transform);
        scroll.verticalScrollbar = _verticalScrollbar;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        scroll.verticalScrollbarSpacing = -3;

        CreateFontSizeControl(backgroundObject.transform);
    }

    private void ConfigureText()
    {
        if (!_statusText) return;

        var rect = _statusText.rectTransform;
        rect.SetParent(_viewport, false);
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(0.5f, 1);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(0, 1200);

        _statusText.alignment = TextAnchor.UpperLeft;
        _statusText.fontSize = DefaultFontSize;
        _statusText.resizeTextForBestFit = false;
        _statusText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _statusText.verticalOverflow = VerticalWrapMode.Overflow;
        _statusText.lineSpacing = 1.15f;
        _statusText.color = Color.white;
        _statusText.raycastTarget = false;
        _statusText.supportRichText = true;
    }

    private void CreateFontSizeControl(Transform parent)
    {
        var barObject = new GameObject("TextSizeControl", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        barObject.transform.SetParent(parent, false);
        var barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0, 0);
        barRect.anchorMax = new Vector2(1, 0);
        barRect.pivot = new Vector2(0.5f, 0);
        barRect.offsetMin = new Vector2(8, 8);
        barRect.offsetMax = new Vector2(-8, 48);
        var barImage = barObject.GetComponent<Image>();
        barImage.color = new Color(0.08f, 0.12f, 0.15f, 0.98f);
        barImage.raycastTarget = false;

        _fontSizeLabel = CreateControlText("TextSizeLabel", barObject.transform, "Text size: 16", 14);
        var labelRect = _fontSizeLabel.rectTransform;
        labelRect.anchorMin = new Vector2(0, 0);
        labelRect.anchorMax = new Vector2(0, 1);
        labelRect.pivot = new Vector2(0, 0.5f);
        labelRect.anchoredPosition = new Vector2(8, 0);
        labelRect.sizeDelta = new Vector2(112, 0);

        var sliderObject = new GameObject("TextSizeSlider", typeof(RectTransform), typeof(Slider));
        sliderObject.transform.SetParent(barObject.transform, false);
        var sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = new Vector2(0, 0.5f);
        sliderRect.anchorMax = new Vector2(1, 0.5f);
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.offsetMin = new Vector2(128, -10);
        sliderRect.offsetMax = new Vector2(-14, 10);

        var track = CreateControlImage("Track", sliderObject.transform,
            new Color(0.28f, 0.36f, 0.4f, 1f));
        var trackRect = track.rectTransform;
        trackRect.anchorMin = new Vector2(0, 0.5f);
        trackRect.anchorMax = new Vector2(1, 0.5f);
        trackRect.pivot = new Vector2(0.5f, 0.5f);
        trackRect.sizeDelta = new Vector2(0, 6);

        var handle = CreateControlImage("Handle", sliderObject.transform,
            new Color(0.25f, 0.82f, 0.75f, 1f));
        var handleRect = handle.rectTransform;
        handleRect.anchorMin = new Vector2(0, 0.5f);
        handleRect.anchorMax = new Vector2(0, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(22, 22);
        handle.raycastTarget = true;

        var fontSizeSlider = sliderObject.GetComponent<Slider>();
        fontSizeSlider.minValue = MinFontSize;
        fontSizeSlider.maxValue = MaxFontSize;
        fontSizeSlider.wholeNumbers = true;
        fontSizeSlider.direction = Slider.Direction.LeftToRight;
        fontSizeSlider.handleRect = handleRect;
        fontSizeSlider.targetGraphic = handle;
        fontSizeSlider.value = DefaultFontSize;
        fontSizeSlider.onValueChanged.AddListener(OnFontSizeChanged);
    }

    private void CreateVerticalScrollbar(Transform parent)
    {
        var scrollbarObject = new GameObject("FlowScrollbar", typeof(RectTransform), typeof(CanvasRenderer),
            typeof(Image), typeof(Scrollbar));
        scrollbarObject.transform.SetParent(parent, false);
        var scrollbarRect = scrollbarObject.GetComponent<RectTransform>();
        scrollbarRect.anchorMin = new Vector2(1, 0);
        scrollbarRect.anchorMax = new Vector2(1, 1);
        scrollbarRect.pivot = new Vector2(1, 0.5f);
        scrollbarRect.offsetMin = new Vector2(-24, 58);
        scrollbarRect.offsetMax = new Vector2(-8, -14);

        var background = scrollbarObject.GetComponent<Image>();
        background.color = new Color(0.14f, 0.2f, 0.23f, 1f);
        background.raycastTarget = true;

        var slidingArea = new GameObject("SlidingArea", typeof(RectTransform));
        slidingArea.transform.SetParent(scrollbarObject.transform, false);
        var slidingRect = slidingArea.GetComponent<RectTransform>();
        slidingRect.anchorMin = Vector2.zero;
        slidingRect.anchorMax = Vector2.one;
        slidingRect.offsetMin = new Vector2(2, 2);
        slidingRect.offsetMax = new Vector2(-2, -2);

        var handle = CreateControlImage("Handle", slidingArea.transform,
            new Color(0.25f, 0.82f, 0.75f, 1f));
        var handleRect = handle.rectTransform;
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = Vector2.one;
        handleRect.offsetMin = Vector2.zero;
        handleRect.offsetMax = Vector2.zero;
        handle.raycastTarget = true;

        _verticalScrollbar = scrollbarObject.GetComponent<Scrollbar>();
        _verticalScrollbar.direction = Scrollbar.Direction.BottomToTop;
        _verticalScrollbar.handleRect = handleRect;
        _verticalScrollbar.targetGraphic = handle;
        _verticalScrollbar.size = 0.25f;
    }

    private Text CreateControlText(string objectName, Transform parent, string value, int size)
    {
        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textObject.transform.SetParent(parent, false);
        var text = textObject.GetComponent<Text>();
        text.font = _statusText ? _statusText.font : Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = size;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.text = value;
        text.raycastTarget = false;
        return text;
    }

    private static Image CreateControlImage(string objectName, Transform parent, Color color)
    {
        var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        var image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void OnFontSizeChanged(float value)
    {
        if (_statusText) _statusText.fontSize = Mathf.RoundToInt(value);
        if (_fontSizeLabel) _fontSizeLabel.text = $"Text size: {Mathf.RoundToInt(value)}";
        _lastRendered = null;
        RefreshDebugText(true);
    }

    private void RefreshDebugText(bool force)
    {
        if (!_statusText) return;
        var report = BuildReport();
        if (!force && report == _lastRendered) return;
        _lastRendered = report;
        _statusText.text = report;
        var textRect = _statusText.rectTransform;
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,
            Mathf.Max(_viewport.rect.height, _statusText.preferredHeight + 12));
    }

    private string BuildReport()
    {
        var settings = OwlManager.OwlSettings;
        var builder = new StringBuilder(2048);
        builder.AppendLine("<b>OWL SDK  /  FLOW DEBUG</b>");

        var startup = OwlBootstrap.State.ToString();
        AppendRow(builder, "Startup", startup,
            $"step={OwlBootstrap.CurrentStep}  elapsed={OwlBootstrap.StartupElapsedSeconds:0.0}s  " +
            (OwlBootstrap.StartupDegraded ? "degraded fallback active" : (OwlManager.OwlInitialized ? "gameplay released" : "waiting")));

        AppendSection(builder, "SERVICES");
        AppendRow(builder, "Runtime", Application.platform.ToString(),
            $"Unity={Application.unityVersion}  app={Application.version}  device={SystemInfo.deviceModel}");
        AppendDynamicRow(builder, "Internet", "Internet");
        AppendRow(builder, "Network check", InternetManager.IsInternetAvailable ? "Online" : "Offline",
            $"consecutive failures={InternetManager.ConsecutiveFailures}");
        AppendDynamicRow(builder, "IAP", "IAP");
        AppendRow(builder, "IAP store", IAPManager.StoreDiagnosticState,
            $"products={IAPManager.FetchedProductCount}  purchaseInProgress={IAPManager.IAPInProgress}");
        AppendRow(builder, "IAP error", string.IsNullOrEmpty(IAPManager.StoreDiagnosticError) ? "None" : "Error",
            IAPManager.StoreDiagnosticError ?? string.Empty);
        AppendDynamicRow(builder, "Firebase", "Firebase");
        AppendRow(builder, "Firebase core", FirebaseController.CoreDiagnosticState,
            $"ready={FirebaseController.IsReady}  products={FirebaseController.IsProductsInitialized}");
        var firebaseProductsEnabled = settings && (settings.useFirebaseAnalytics || settings.useFirebaseCrashlytics ||
            settings.useFirebaseRemoteConfig || settings.useFirebaseMessaging);
        AppendFeature(builder, "Firebase products", firebaseProductsEnabled, FirebaseAvailable,
            FirebaseController.IsProductsInitialized ? "Initialized" : "Waiting for consent/core");
        AppendRow(builder, "Firebase dependency", FirebaseController.CoreDiagnosticState,
            $"coreReady={FirebaseController.IsReady}  coreInitialized={FirebaseController.IsInitialized}");
        AppendDynamicRow(builder, "Consent", "Consent");
        AppendRow(builder, "Consent gates", ConsentManager.IsResolved ? "Resolved" : "Waiting",
            $"ads={ConsentManager.CanRequestAds}  analytics={ConsentManager.CanCollectAnalytics}  " +
            $"attribution={ConsentManager.CanCollectAttribution}  tracking={ConsentManager.TrackingAuthorized}");
        AppendDynamicRow(builder, "Analytics", "Analytics");
        AppendDynamicRow(builder, "Attribution", "Attribution");
        AppendFeature(builder, "Facebook SDK", settings && settings.useFacebookEvents, FacebookEnabled,
            Owl.Analytics.FacebookController.IsInitialized ? "Initialized" :
            FlowStatuses.TryGetValue("Facebook", out var facebookStatus) ? facebookStatus.State : "Waiting for consent");
        AppendDynamicRow(builder, "Ads SDK", "Ads SDK");

        AppendSection(builder, "FIREBASE PRODUCTS");
        if (settings)
        {
            AppendFeature(builder, "Firebase Analytics", settings.useFirebaseAnalytics, FirebaseAnalyticsAvailable,
                FirebaseController.IsAnalyticsConfigured
                    ? $"Configured; collection={ConsentManager.CanCollectAnalytics}" : "Waiting for consent/core");
            AppendFeature(builder, "Crashlytics", settings.useFirebaseCrashlytics, FirebaseCrashlyticsAvailable,
                FirebaseController.IsCrashlyticsInitialized ? "Initialized" : "Waiting for core");
            AppendFeature(builder, "Remote Config", settings.useFirebaseRemoteConfig, FirebaseRemoteConfigAvailable,
                FirebaseController.RemoteConfigDiagnosticState);
            AppendFeature(builder, "Push Messaging", settings.useFirebaseMessaging, FirebaseMessagingAvailable,
                FirebaseMessagingService.IsInitialized
                    ? FirebaseMessagingService.HasRegistrationToken ? $"Token received ({FirebaseMessagingService.TokenSuffix})" : "Initialized; no token yet"
                    : "Waiting for Firebase core");
            AppendRow(builder, "FCM last error", string.IsNullOrEmpty(FirebaseMessagingService.LastError) ? "None" : "Error",
                FirebaseMessagingService.LastError ?? string.Empty);
            AppendRow(builder, "FCM permission mode", settings.requestPushPermissionOnStart ? "Prompt at startup" : "Request manually",
                $"serviceReady={FirebaseMessagingService.IsInitialized}  token={FirebaseMessagingService.HasRegistrationToken}");
            AppendSetting(builder, "Push permission on start", settings.useFirebaseMessaging && settings.requestPushPermissionOnStart);
        }

        AppendSection(builder, "ANALYTICS & ATTRIBUTION");
        if (settings)
        {
            var gameAnalyticsConfigured = settings.analyticsSdk == AnalyticsSDK.GameAnalytics ||
                                          settings.analyticsSdk == AnalyticsSDK.FirebaseGameAnalytics;
            AppendFeature(builder, "GameAnalytics", gameAnalyticsConfigured, GameAnalyticsAvailable,
                AnalyticsManager.IsGameAnalyticsInitialized ? "Initialized" : "Waiting for consent/startup");
            AppendFeature(builder, "Meta events", settings.useFacebookEvents, FacebookEnabled,
                Owl.Analytics.FacebookController.IsInitialized ? "Ready" :
                    settings.useFacebookEvents && ConsentManager.CanCollectAnalytics && ConsentManager.CanCollectAttribution
                        ? "Initializing" : "Waiting for analytics and attribution consent");
            AppendFeature(builder, "Airbridge", settings.useMMP && settings.useAirbridge, AirbridgeAvailable, "attribution provider");
            AppendFeature(builder, "SolarEngine", settings.useMMP && settings.useSolarEngine, SolarEngineAvailable, "attribution provider");
            AppendRow(builder, "Analytics selection", settings.analyticsSdk.ToString(),
                $"consent={ConsentManager.CanCollectAnalytics}  ready={AnalyticsManager.IsInitialized}");
            AppendRow(builder, "GameAnalytics events", AnalyticsManager.IsGameAnalyticsInitialized ? "Ready" : "Not initialized",
                $"selected={gameAnalyticsConfigured}  consent={ConsentManager.CanCollectAnalytics}");
            AppendRow(builder, "Attribution selection", settings.mmpsdk.ToString(),
                $"consent={ConsentManager.CanCollectAttribution}");
        }

        AppendSection(builder, "AD PLACEMENTS  (enabled / setup / loaded)");
        if (settings)
        {
            AppendPlacementRow(builder, "App Open", "App Open", AdManager.IsAppOpenEnabled,
                AdManager.IsAppOpenPlacementInitialized, AdManager.IsAppOpenAvailable(), true, "On start");
            AppendPlacementRow(builder, "Banner", "Banner", AdManager.IsBannerEnabled,
                AdManager.IsBannerPlacementInitialized, AdManager.IsBannerLoaded,
                settings.bannerOnStart, settings.bannerOnStart ? "On start" : "ShowBanner call");
            AppendPlacementRow(builder, "Interstitial", "Interstitial", AdManager.IsInterstitialEnabled,
                AdManager.IsInterstitialPlacementInitialized, AdManager.IsInterstitialAvailable(),
                settings.interstitialOnStart, settings.interstitialOnStart ? "On start" : "Manual initializer");
            AppendPlacementRow(builder, "Rewarded Int.", "Rewarded Interstitial", AdManager.IsRewardedInterstitialEnabled,
                AdManager.IsRewardedInterstitialPlacementInitialized, AdManager.IsRewardedInterstitialAvailable(),
                settings.rewardedInterstitialOnStart, AdManager.IsRewardedInterstitialSupported
                    ? settings.rewardedInterstitialOnStart ? "On start" : "Manual initializer" : "Unsupported by provider");
            AppendPlacementRow(builder, "Rewarded", "Rewarded", AdManager.IsRewardedEnabled,
                AdManager.IsRewardedPlacementInitialized, AdManager.IsRewardedVideoAvailable(),
                settings.rewardedOnStart, settings.rewardedOnStart ? "On start" : "Manual initializer");
            AppendPlacementRow(builder, "MREC", "MREC", AdManager.IsMRECEnabled,
                AdManager.IsMRECPlacementInitialized, AdManager.IsMRECLoaded, false, "On demand");
            AppendPlacementRow(builder, "Timer Ads", "Timer Ads", AdManager.IsTimerAdAvailable,
                AdManager.IsTimerAdInitialized, AdManager.IsInterstitialAvailable(),
                settings.autoStartTimerAd, $"Interval={settings.timerAdInterval:0.#}s");
        }

        AppendSection(builder, "AD NETWORK");
        AppendRow(builder, "Selected mediation", settings ? settings.mediation.ToString() : "Unknown",
            $"sdkReady={AdManager.IsAdNetworkInitialized}  placementsReady={AdManager.IsInitialized}  " +
            $"consent={ConsentManager.CanRequestAds}  removeAds={OwlManager.HasRemoveAdsEntitlement}");
        if (settings && settings.adSettings)
            AppendRow(builder, "Configured ad units", GetAdUnitCounts(settings), "IDs hidden");

        AppendSection(builder, "OWL UI & OPTIONS");
        if (settings)
        {
            AppendSetting(builder, "Automatic startup", settings.autoSDKInit);
            AppendSetting(builder, "Status overlay", settings.screenSDKInfo);
            AppendSetting(builder, "Verbose logs", settings.verbose);
            AppendSetting(builder, "Test ad IDs", settings.testIds);
            AppendSetting(builder, "No Internet popup", settings.showNoInternetUi);
            AppendSetting(builder, "Ad is coming panel", settings.showAdComingUi);
            AppendSetting(builder, "Banner background", settings.showBannerBgLayer);
            AppendSetting(builder, "MREC background", settings.showMrecBgLayer);
            AppendSetting(builder, "IAP loading panel", settings.showIapLoadingUi);
            AppendSetting(builder, "IAP", settings.useIap);
            AppendRow(builder, "IAP loading canvas", IAPManager.IsInitialized || IAPManager.IAPInProgress ? "Active" : "Idle",
                $"visible={IAPManager.IAPInProgress}  prefab configured={settings.OwlComponents != null && settings.OwlComponents.IapLoadingCanvas != null}");
            AppendRow(builder, "Store", IAPManager.StoreDiagnosticState,
                $"initialized={IAPManager.IsInitialized}  products={IAPManager.FetchedProductCount}  purchase={IAPManager.IAPInProgress}");
            AppendRow(builder, "Ad mediation", settings.mediation.ToString(), "selected in Owl Settings");
            AppendSetting(builder, "Remove Ads entitlement", OwlManager.HasRemoveAdsEntitlement);
            AppendSetting(builder, "Remote all ads", settings.remoteCompleteAds);
            AppendSetting(builder, "Remote App Open", settings.remoteAppOpen);
            AppendSetting(builder, "Remote Banner", settings.remoteBanner);
            AppendSetting(builder, "Remote Interstitial", settings.remoteInterstitial);
            AppendSetting(builder, "Remote Rewarded", settings.remoteRewarded);
            AppendSetting(builder, "Remote Rewarded Interstitial", settings.remoteRewardedInterstitial);
            AppendSetting(builder, "Remote MREC", settings.remoteMREC);
            AppendSetting(builder, "Remote timed ads", settings.remoteTimedAds);
            AppendRow(builder, "App Open delay", $"{settings.appOpenStartDelay:0.##}s", "startup delay");
            AppendRow(builder, "Interstitial interval", $"{settings.interstitialInterval:0.##}s", "minimum show interval");
            AppendRow(builder, "Timer interval", $"{settings.timerAdInterval:0.##}s", $"autoStart={settings.autoStartTimerAd}");
            AppendSetting(builder, "Reset interval after reward", settings.resetIntervalOnRewarded);
        }

        AppendSection(builder, "LIVE COMPONENTS  (active / load)");
        AppendComponents(builder);

        AppendSection(builder, "RECENT EVENTS");
        if (RecentEvents.Count == 0) builder.AppendLine("  Waiting for startup events");
        else
            foreach (var item in RecentEvents)
                builder.Append("  ").AppendLine(Escape(item));

        return builder.ToString();
    }

    private static void AppendDynamicRow(StringBuilder builder, string label, string key)
    {
        if (FlowStatuses.TryGetValue(key, out var status))
            AppendRow(builder, label, status.State, status.Detail);
        else
            AppendRow(builder, label, "Waiting", string.Empty);
    }

    private static void AppendPlacementRow(StringBuilder builder, string label, string key, bool enabled,
        bool initialized, bool available, bool onStart, string mode)
    {
        if (!enabled)
        {
            AppendRow(builder, label, "Disabled", $"setup={initialized}  available={available}");
            return;
        }

        var state = FlowStatuses.TryGetValue(key, out var status)
            ? status.State
            : initialized ? available ? "Loaded" : "Loading" : onStart ? "Waiting" : "Manual";
        var detail = FlowStatuses.TryGetValue(key, out status) && !string.IsNullOrEmpty(status.Detail)
            ? status.Detail + "  " : string.Empty;
        AppendRow(builder, label, state,
            $"{detail}enabled={enabled}  initialized={initialized}  available={available}  mode={mode}");
    }

    private static void AppendFeature(StringBuilder builder, string label, bool configured, bool available, string detail)
    {
        var state = !available ? "Not installed" : !configured ? "Disabled" : detail;
        AppendRow(builder, label, state, configured ? "enabled in Owl Settings" : "off in Owl Settings");
    }

    private static void AppendSetting(StringBuilder builder, string label, bool enabled)
    {
        AppendRow(builder, label, enabled ? "On" : "Off", string.Empty);
    }

    private static string GetAdUnitCounts(OwlSettings settings)
    {
        var adSettings = settings.adSettings;
        if (settings.mediation == Mediation.GoogleAds && adSettings.googleAds != null)
        {
            var ads = adSettings.googleAds;
            return $"AppOpen={Length(ads.appOpenAdUnits)} Banner={Length(ads.bannerAdUnits)} " +
                   $"Inter={Length(ads.interstitialAdUnits)} Rewarded={Length(ads.rewardedAdUnits)} " +
                   $"RewardedInter={Length(ads.rewardedInterstitialAdUnits)} MREC={Length(ads.mRecAdUnits)}";
        }

        if (settings.mediation == Mediation.Max && adSettings.maxAds != null)
        {
            var ads = adSettings.maxAds;
            return $"AppOpen={Length(ads.appOpenAdUnits)} Banner={Length(ads.bannerAdUnits)} " +
                   $"Inter={Length(ads.interstitialAdUnits)} Rewarded={Length(ads.rewardedAdUnits)} " +
                   $"MREC={Length(ads.mRecAdUnits)}";
        }

        return "No ad provider configured";
    }

    private static int Length<T>(T[] values) => values != null ? values.Length : 0;

#if USE_FACEBOOK && USE_FACEBOOK_SDK
    private const bool FacebookEnabled = true;
#else
    private const bool FacebookEnabled = false;
#endif
#if USE_FIREBASE
    private const bool FirebaseAvailable = true;
#else
    private const bool FirebaseAvailable = false;
#endif

#if USE_FIREBASEANALYTICS
    private const bool FirebaseAnalyticsAvailable = true;
#else
    private const bool FirebaseAnalyticsAvailable = false;
#endif
#if USE_FIREBASECRASHLYTICS
    private const bool FirebaseCrashlyticsAvailable = true;
#else
    private const bool FirebaseCrashlyticsAvailable = false;
#endif
#if USE_FIREBASEREMOTECONFIG
    private const bool FirebaseRemoteConfigAvailable = true;
#else
    private const bool FirebaseRemoteConfigAvailable = false;
#endif
#if USE_FIREBASEMESSAGING
    private const bool FirebaseMessagingAvailable = true;
#else
    private const bool FirebaseMessagingAvailable = false;
#endif
#if USE_GAMEANALYTICS
    private const bool GameAnalyticsAvailable = true;
#else
    private const bool GameAnalyticsAvailable = false;
#endif
#if USE_AIRBRIDGE
    private const bool AirbridgeAvailable = true;
#else
    private const bool AirbridgeAvailable = false;
#endif
#if USE_SOLARENGINE
    private const bool SolarEngineAvailable = true;
#else
    private const bool SolarEngineAvailable = false;
#endif

    private static void AppendSection(StringBuilder builder, string title)
    {
        builder.Append("\n<b>").Append(title).AppendLine("</b>");
    }

    private static void AppendRow(StringBuilder builder, string label, string state, string detail)
    {
        var color = StateColor(state);
        builder.Append("<b>").Append(Escape(label)).Append("</b>  <color=")
            .Append(color).Append("><b>").Append(Escape(state)).Append("</b></color>");
        if (!string.IsNullOrEmpty(detail)) builder.Append("  ").Append(Escape(detail));
        builder.AppendLine();
    }

    private static string StateColor(string state)
    {
        if (state.IndexOf("fail", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            state.IndexOf("timeout", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            state.IndexOf("unsupported", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            state.IndexOf("offline", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return "#FF786E";
        if (state.IndexOf("ready", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            state.IndexOf("loaded", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            state.IndexOf("online", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            state.IndexOf("resolved", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            state.IndexOf("active", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return "#42D9C5";
        if (state.IndexOf("disabled", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            state.IndexOf("skipped", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            state.IndexOf("manual", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return "#B8C4CC";
        return "#FFD166";
    }

    private static void AppendComponents(StringBuilder builder)
    {
        var components = OwlManager.GetDebugComponents();
        if (components == null || components.Length == 0)
        {
            builder.AppendLine("  No Owl runtime components found");
            return;
        }

        var shown = 0;
        foreach (var component in components)
        {
            if (!component) continue;

            var active = component.isActiveAndEnabled;
            var state = active ? "Active" : "Inactive";
            var detail = component is AdType adType
                ? (adType.IsAdLoaded ? "loaded" : "not loaded")
                : component.gameObject.name;
            AppendRow(builder, component.GetType().Name, state, detail);
            shown++;
        }
    }

    private static string Escape(string value)
    {
        return (value ?? string.Empty).Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
    }
}
