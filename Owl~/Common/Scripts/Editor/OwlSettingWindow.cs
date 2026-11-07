using System;
using Owl.Monetization;
using UnityEditor;
using UnityEngine;

namespace Owl.Common
{
    public class OwlSettingWindow : EditorWindow
    {

        #region PrivateFields

        private AnalyticsSDK _analyticsSdk;
        private bool _useFirebaseEventProvider;
        private bool _useGameAnalyticsEventProvider;
        private bool _useFirebaseAnalytics;
        private bool _useFirebaseCrashlytics;
        private bool _useFirebaseRemoteConfig;
        private bool _useFirebaseMessaging;
        private bool _useFacebookEvents;
        private bool _requestPushPermissionOnStart;
        private bool _remoteCompleteAds;
        private bool _remoteAppOpen;
        private bool _remoteBanner;
        private bool _remoteInterstitial;
        private bool _remoteRewarded;
        private bool _remoteRewardedInterstitial;
        private bool _remoteMREC;
        private bool _remoteTimedAds;
        private bool _showAppOpenOptions;
        private bool _showBannerOptions;
        private bool _showInterstitialOptions;
        private bool _showTimerOptions;

        private Mediation _mediation;


        private MMPSDK _mmpsdk;
        private bool _useAirbridgeProvider;
        private bool _useSolarEngineProvider;

        private Vector2 _scrollPosition = Vector2.zero;
        private int _selectedSection;
        private bool _saveQueued;
        private OwlSettings _owlSettings;
        private AdSettings _adSettings;
        private static readonly Color AccentColor = new Color(0.18f, 0.78f, 0.72f);
#if USE_GAMEANALYTICS
        private GameAnalyticsSDK.Setup.Settings _gameAnalyticsSettings;
#endif


        private ScriptableObject _target;
        private SerializedObject _so;



        #endregion

        #region PublicFields

        public string[] noAdsIds;
        public GoogleAds googleAds;
        public MaxAds maxAds;

        #endregion

        #region Mono-Editors
        private void OnEnable()
        {
            LoadOwlSettings();
            if (_owlSettings == null) return;

            _analyticsSdk = _owlSettings.analyticsSdk;
            _useFirebaseEventProvider = _analyticsSdk == AnalyticsSDK.Firebase ||
                                       _analyticsSdk == AnalyticsSDK.FirebaseGameAnalytics;
            _useGameAnalyticsEventProvider = _analyticsSdk == AnalyticsSDK.GameAnalytics ||
                                             _analyticsSdk == AnalyticsSDK.FirebaseGameAnalytics;
            _useFirebaseAnalytics = _owlSettings.useFirebaseAnalytics;
            _useFirebaseCrashlytics = _owlSettings.useFirebaseCrashlytics;
            _useFirebaseRemoteConfig = _owlSettings.useFirebaseRemoteConfig;
            _useFirebaseMessaging = _owlSettings.useFirebaseMessaging;
            _useFacebookEvents = _owlSettings.useFacebookEvents;
            _requestPushPermissionOnStart = _owlSettings.requestPushPermissionOnStart;
            _remoteCompleteAds=_owlSettings.remoteCompleteAds;
            _remoteAppOpen=_owlSettings.remoteAppOpen;
            _remoteBanner = _owlSettings.remoteBanner;
            _remoteInterstitial = _owlSettings.remoteInterstitial;
            _remoteRewarded = _owlSettings.remoteRewarded;
            _remoteRewardedInterstitial = _owlSettings.remoteRewardedInterstitial;
            _remoteMREC = _owlSettings.remoteMREC;
            _remoteTimedAds = _owlSettings.remoteTimedAds;
            _mmpsdk = _owlSettings.mmpsdk;
            _useAirbridgeProvider = _mmpsdk == MMPSDK.Airbridge || _mmpsdk == MMPSDK.AirbridgeSolarEngine;
            _useSolarEngineProvider = _mmpsdk == MMPSDK.SolarEngine || _mmpsdk == MMPSDK.AirbridgeSolarEngine;
            noAdsIds= _owlSettings.noAdsIds;

            _mediation = _owlSettings.mediation;
            googleAds = _owlSettings.adSettings.googleAds;
            maxAds = _owlSettings.adSettings.maxAds;
            _target = this;
            _so = new SerializedObject(_target);
        }

        private void LoadOwlSettings()
        {
            _owlSettings = Resources.Load<OwlSettings>("OwlSettings");
            if (_owlSettings == null) return;
            if (_owlSettings.adSettings == null)
            {
                _adSettings = Resources.Load<AdSettings>("AdSettings");
                CreateAdSettingsAsset();
                _owlSettings.adSettings = _adSettings;
            }

#if USE_GAMEANALYTICS
            if (_gameAnalyticsSettings == null)
            {
                _gameAnalyticsSettings = Resources.Load<GameAnalyticsSDK.Setup.Settings>("GameAnalytics/Settings");
                _owlSettings.gameAnalyticsSettings = _gameAnalyticsSettings;
            }
#endif


        }

        private void CreateAdSettingsAsset()
        {
            if(_adSettings == null)
            {
                //If the settings asset doesn't exist, then create it. We require a resources folder
                if(!System.IO.Directory.Exists(Application.dataPath + "/Resources"))
                {
                    System.IO.Directory.CreateDirectory(Application.dataPath + "/Resources");
                }
                if(!System.IO.Directory.Exists(Application.dataPath + "/Resources/Owl"))
                {
                    System.IO.Directory.CreateDirectory(Application.dataPath + "/Resources/Owl");
                    Debug.LogWarning("Owl: Resources/Owl folder is required to store settings. it was created ");
                }

                const string path = "Assets/Resources/Owl/AdSettings.asset";

                if(System.IO.File.Exists(path))
                {
                    AssetDatabase.DeleteAsset(path);
                    AssetDatabase.Refresh();
                }

                var asset = ScriptableObject.CreateInstance<AdSettings>();
                AssetDatabase.CreateAsset(asset, path);
                AssetDatabase.Refresh();

                AssetDatabase.SaveAssets();
                Selection.activeObject = asset;

                //save reference
                _adSettings =asset;
            }
        }

        [Obsolete("Obsolete")]
        private void OnGUI()
        {
            if (_owlSettings == null || _so == null)
            {
                EditorGUILayout.HelpBox("Owl settings could not be loaded from Resources.", MessageType.Error);
                return;
            }

            DrawHeader();
            _so.Update();
            DrawNavigation();
            using (var scroll = new EditorGUILayout.ScrollViewScope(_scrollPosition))
            {
                _scrollPosition = scroll.scrollPosition;
                GUILayout.Space(5);
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    using (new EditorGUILayout.VerticalScope(GUILayout.MaxWidth(900), GUILayout.ExpandWidth(true)))
                    {
                        switch (_selectedSection)
                        {
                            case 0: DrawDebug(); break;
                            case 1: DrawAnalytics(); break;
                            case 2: DrawMonetizations(); break;
                        }
                    }
                    GUILayout.FlexibleSpace();
                }
                GUILayout.Space(5);
            }
            _so.ApplyModifiedProperties();
            DrawSaveSettings();
        }

        private void OnValidate()
        {
        }
        #endregion

        #region Drawers

        private void DrawHeader()
        {
            var rect = GUILayoutUtility.GetRect(0, 100, GUILayout.ExpandWidth(true));
            var background = EditorGUIUtility.isProSkin
                ? new Color(0.12f, 0.17f, 0.20f)
                : new Color(0.88f, 0.95f, 0.94f);
            EditorGUI.DrawRect(rect, background);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 3, rect.width, 3), AccentColor);

            var icon = _owlSettings.IconTexture;
            var textX = rect.x + 20;
            if (icon != null)
            {
                GUI.DrawTexture(new Rect(rect.x + 22, rect.y + 24, 52, 52), icon, ScaleMode.ScaleToFit);
                textX = rect.x + 96;
            }

            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 21 };
            var subtitleStyle = new GUIStyle(EditorStyles.label) { fontSize = 11 };
            GUI.Label(new Rect(textX, rect.y + 19, rect.width - (textX - rect.x) - 12, 30), "Owl Publishing Kit", titleStyle);
            GUI.Label(new Rect(textX, rect.y + 51, rect.width - (textX - rect.x) - 12, 22), "Project settings and SDK integrations", subtitleStyle);
        }

        private void DrawNavigation()
        {
            var background = EditorGUIUtility.isProSkin
                ? new Color(0.17f, 0.19f, 0.21f)
                : new Color(0.92f, 0.94f, 0.94f);
            var rect = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, background);
            var labels = new[] { "01  Development", "02  Analytics & LiveOps", "03  Monetization" };
            var width = Mathf.Min(210, (rect.width - 32) / 3);
            for (var i = 0; i < labels.Length; i++)
            {
                var buttonRect = new Rect(rect.x + 16 + i * width, rect.y + 8, width - 4, 34);
                var active = _selectedSection == i;
                if (active)
                    EditorGUI.DrawRect(buttonRect, AccentColor);
                var style = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = 11
                };
                if (active)
                    style.normal.textColor = new Color(0.06f, 0.13f, 0.14f);
                if (GUI.Button(buttonRect, labels[i], style))
                {
                    _selectedSection = i;
                    _scrollPosition = Vector2.zero;
                    GUI.FocusControl(null);
                    GUIUtility.ExitGUI();
                }
            }
        }

        private static void DrawSectionHeader(string title, string description)
        {
            var rect = GUILayoutUtility.GetRect(0, 46, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(new Rect(rect.x + 5, rect.y + 6, 3, 28), AccentColor);
            GUI.Label(new Rect(rect.x + 18, rect.y + 2, rect.width - 25, 20), title,
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 15 });
            GUI.Label(new Rect(rect.x + 18, rect.y + 23, rect.width - 25, 15), description,
                EditorStyles.miniLabel);
        }

        private static void DrawSubsectionTitle(string title)
        {
            GUILayout.Space(2);
            EditorGUILayout.LabelField(title, new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 });
            GUILayout.Space(0);
        }

        private static bool DrawToggleTile(string title, bool value)
        {
            var previousColor = GUI.backgroundColor;
            GUI.backgroundColor = value ? AccentColor : Color.white;
            var style = new GUIStyle(EditorStyles.miniButton) { fixedHeight = 32, fontStyle = FontStyle.Bold };
            var result = GUILayout.Toggle(value, title, style, GUILayout.MinWidth(125), GUILayout.ExpandWidth(true));
            GUI.backgroundColor = previousColor;
            return result;
        }

        private static bool DrawFeatureToggle(string title, bool value, string tooltip = null)
        {
            var rect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
            GUI.Label(new Rect(rect.x, rect.y, rect.width - 76, rect.height), new GUIContent(title, tooltip),
                EditorStyles.boldLabel);
            return DrawCompactSwitch(new Rect(rect.xMax - 64, rect.y + 3, 64, 22), value);
        }

        private static bool DrawCompactSwitch(Rect rect, bool value)
        {
            var previousColor = GUI.backgroundColor;
            try
            {
                GUI.backgroundColor = value ? AccentColor : Color.white;
                return GUI.Toggle(rect, value, value ? "On" : "Off",
                    new GUIStyle(EditorStyles.miniButton)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 11,
                        fontStyle = FontStyle.Bold
                    });
            }
            finally
            {
                GUI.backgroundColor = previousColor;
            }
        }

        private static EditorGUILayout.VerticalScope BeginPlacementCard(bool enabled)
        {
            var scope = new EditorGUILayout.VerticalScope(new GUIStyle
            {
                padding = new RectOffset(10, 10, 6, 6),
                margin = new RectOffset(0, 0, 0, 5)
            });
            if (Event.current.type == EventType.Repaint)
            {
                EditorGUI.DrawRect(scope.rect, EditorGUIUtility.isProSkin
                    ? new Color(0.235f, 0.245f, 0.255f)
                    : new Color(0.91f, 0.93f, 0.94f));
                if (enabled)
                    EditorGUI.DrawRect(new Rect(scope.rect.x, scope.rect.y, 2, scope.rect.height), AccentColor);
            }
            return scope;
        }

        private static EditorGUILayout.VerticalScope BeginPlacementList()
        {
            return new EditorGUILayout.VerticalScope(new GUIStyle
            {
                padding = new RectOffset(8, 8, 2, 2),
                margin = new RectOffset(0, 0, 0, 1)
            });
        }

        private static bool DrawPlacementToggle(string title, string description, bool value)
        {
            var rect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
            var textWidth = Mathf.Max(0, rect.width - 80);
            GUI.Label(new Rect(rect.x, rect.y + 4, 150, 20), new GUIContent(title, description),
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 });
            GUI.Label(new Rect(rect.x + 154, rect.y + 4, Mathf.Max(0, textWidth - 154), 20),
                description, EditorStyles.miniLabel);
            return DrawCompactSwitch(new Rect(rect.xMax - 64, rect.y + 3, 64, 22), value);
        }

        private static bool DrawManagedAdPlacement(string title, string description, bool value,
            ref bool initializeOnStart)
        {
            var row = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
            const float switchWidth = 64f;
            const float modeWidth = 112f;
            var toggle = new Rect(row.xMax - switchWidth, row.y + 4, switchWidth, 22);
            var mode = new Rect(toggle.x - modeWidth - 8, row.y + 4, modeWidth, 22);
            var titleWidth = Mathf.Min(190f, row.width * 0.22f);
            GUI.Label(new Rect(row.x, row.y + 5, titleWidth, 20), new GUIContent(title, description),
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 });
            GUI.Label(new Rect(row.x + titleWidth + 8, row.y + 5,
                    Mathf.Max(0, mode.x - row.x - titleWidth - 16), 20), description, EditorStyles.miniLabel);
            EditorGUI.BeginDisabledGroup(!value);
            initializeOnStart = EditorGUI.Popup(mode, initializeOnStart ? 0 : 1,
                new[] { "On Start", "Manual" }) == 0;
            EditorGUI.EndDisabledGroup();
            return DrawCompactSwitch(toggle, value);
        }

        private static void DrawPlacementDivider()
        {
            GUILayout.Space(1);
            var rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, EditorGUIUtility.isProSkin
                ? new Color(1, 1, 1, 0.09f) : new Color(0, 0, 0, 0.12f));
            GUILayout.Space(1);
        }

        private static void BeginPlacementOptions()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);
            EditorGUILayout.BeginVertical();
        }

        private static void EndPlacementOptions()
        {
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        private static bool DrawOptionsDisclosure(string label, bool expanded)
        {
            var rect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
            var button = new Rect(rect.x + 16, rect.y + 1, 142, 20);
            var caption = expanded ? "Hide settings  ▴" : "More settings  ▾";
            if (GUI.Button(button, caption, EditorStyles.miniButton))
                expanded = !expanded;
            return expanded;
        }

        private static float DrawSecondsSetting(string label, string tooltip, float value,
            bool showSlider = false, float minimum = 0, float maximum = 360, bool clampValue = true)
        {
            var row = GUILayoutUtility.GetRect(0, 26, GUILayout.ExpandWidth(true));
            var labelWidth = Mathf.Min(190, row.width * 0.24f);
            GUI.Label(new Rect(row.x, row.y + 3, labelWidth, 20), new GUIContent(label, tooltip));
            var field = new Rect(row.xMax - 94, row.y + 3, 94, 20);
            if (showSlider)
            {
                var slider = new Rect(row.x + labelWidth, row.y + 4,
                    Mathf.Max(0, field.x - row.x - labelWidth - 12), 18);
                if (slider.width > 30)
                    value = GUI.HorizontalSlider(slider, value, minimum, maximum);
            }
            value = EditorGUI.FloatField(new Rect(field.x, field.y, 64, field.height), value);
            GUI.Label(new Rect(field.x + 68, field.y, 26, field.height), "sec", EditorStyles.miniLabel);
            return showSlider && clampValue ? Mathf.Clamp(value, minimum, maximum) : value;
        }

        private static bool DrawSettingCheckbox(string label, string tooltip, bool value)
        {
            var row = GUILayoutUtility.GetRect(0, 23, GUILayout.ExpandWidth(true));
            GUI.Label(new Rect(row.x, row.y + 2, row.width - 30, 19), new GUIContent(label, tooltip));
            return EditorGUI.ToggleLeft(new Rect(row.xMax - 26, row.y + 1, 26, 20), GUIContent.none, value);
        }

        private void DrawDebug()
        {
            using (new EditorGUILayout.VerticalScope(CompactSectionStyle()))
            {
                DrawSectionHeader("Development", "Control startup diagnostics and test behavior.");
                DrawSubsectionTitle("Startup & diagnostics");
                using (new EditorGUILayout.HorizontalScope())
                {
                    _owlSettings.autoSDKInit = DrawToggleTile(_owlSettings.autoSDKInit ? "Automatic startup" : "Custom startup", _owlSettings.autoSDKInit);
                    _owlSettings.screenSDKInfo = DrawToggleTile("Status overlay", _owlSettings.screenSDKInfo);
                }
                using (new EditorGUILayout.HorizontalScope())
                {
                    _owlSettings.verbose = DrawToggleTile("Verbose logs", _owlSettings.verbose);
                    _owlSettings.testIds = DrawToggleTile("Test ad IDs", _owlSettings.testIds);
                }

                GUILayout.Space(5);
                DrawSubsectionTitle("Popups & loading panels");
                using (BeginPlacementCard(false))
                {
                    _owlSettings.showNoInternetUi = DrawFeatureCardToggle("No Internet popup",
                        "Show the offline message when no connection is available.", _owlSettings.showNoInternetUi);
                    DrawPlacementDivider();
                    _owlSettings.showAdComingUi = DrawFeatureCardToggle("Ad is coming panel",
                        "Show the countdown before a full-screen ad.", _owlSettings.showAdComingUi);
                    if (_owlSettings.showAdComingUi)
                        _owlSettings.adComingWaitStep = DrawSecondsSetting("Countdown step",
                            "Time between countdown number updates.", _owlSettings.adComingWaitStep, true, 0.1f, 2f);
                    DrawPlacementDivider();
                    _owlSettings.showIapLoadingUi = DrawFeatureCardToggle("IAP loading panel",
                        "Show while a purchase or restore is processing.", _owlSettings.showIapLoadingUi);
                }

                GUILayout.Space(2);
                DrawSubsectionTitle("Ad backgrounds");
                using (BeginPlacementCard(false))
                {
                    _owlSettings.showBannerBgLayer = DrawFeatureCardToggle("Banner background",
                        "Show a backing layer behind banner ads.", _owlSettings.showBannerBgLayer);
                    DrawPlacementDivider();
                    _owlSettings.showMrecBgLayer = DrawFeatureCardToggle("MREC background",
                        "Show a backing layer behind MREC ads.", _owlSettings.showMrecBgLayer);
                }
                GUILayout.Space(2);
            }
        }

        private static bool DrawFeatureCardToggle(string title, string description, bool value)
        {
            using (BeginPlacementCard(value))
                return DrawPlacementToggle(title, description, value);
        }

        // ReSharper disable Unity.PerformanceAnalysis
        [Obsolete("Obsolete")]
        private void DrawAnalytics()
        {
            using (new EditorGUILayout.VerticalScope(CompactSectionStyle()))
            {
                DrawSectionHeader("Analytics", "Choose your attribution and event providers.");
                DrawMmpProviders();
                GUILayout.Space(2);
                DrawAnalyticsProviders();
                DrawFacebookSettings();
                GUILayout.Space(0);
            }
        }

        private static GUIStyle CompactSectionStyle()
        {
            return new GUIStyle(EditorStyles.helpBox)
            {
                padding = new RectOffset(3, 3, 2, 2)
            };
        }

        private void DrawFacebookSettings()
        {
            GUILayout.Space(2);
            using (BeginPlacementCard(_useFacebookEvents))
            {
                _useFacebookEvents = DrawPlacementToggle("Meta (Facebook) events",
                    "App activity, purchases, level completions, and ad revenue", _useFacebookEvents);
                if (_useFacebookEvents)
                {
                    DrawPlacementDivider();
                    var sdkDetected = HasFacebookSdk();
                    EditorGUILayout.LabelField(sdkDetected
                        ? "SDK detected. Events wait for Owl consent."
                        : "USE_FACEBOOK is enabled. Import the Meta Unity SDK to activate events.",
                        EditorStyles.wordWrappedMiniLabel);
                    if (sdkDetected)
                    {
                        using (new EditorGUILayout.HorizontalScope())
                        {
                            if (GUILayout.Button(new GUIContent("Open Facebook settings",
                                    "Select the Meta Unity SDK settings asset in the Inspector."),
                                GUILayout.Width(200), GUILayout.Height(28)))
                                OpenFacebookSettings();
                        }
                    }
                }
            }
        }

        private static void OpenFacebookSettings()
        {
            foreach (var guid in AssetDatabase.FindAssets("FacebookSettings"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith("/FacebookSettings.asset", StringComparison.OrdinalIgnoreCase))
                    continue;

                var settings = AssetDatabase.LoadMainAssetAtPath(path);
                if (settings == null)
                    continue;

                Selection.activeObject = settings;
                EditorGUIUtility.PingObject(settings);
                EditorApplication.ExecuteMenuItem("Window/General/Inspector");
                return;
            }

            EditorUtility.DisplayDialog("Facebook settings",
                "The Meta Unity SDK is detected, but its FacebookSettings asset could not be found. Reimport the SDK and try again.",
                "OK");
        }

        private static bool HasFacebookSdk()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (assembly.GetType("Facebook.Unity.FB", false) != null)
                    return true;
            return false;
        }
        private void DrawMmpProviders()
        {
            DrawSubsectionTitle("Attribution");
            DrawAirbridge();
            DrawSolarEngine();
            _owlSettings.useAirbridge = _useAirbridgeProvider;
            _owlSettings.useSolarEngine = _useSolarEngineProvider;
            _owlSettings.useMMP = _useAirbridgeProvider || _useSolarEngineProvider;
            _mmpsdk = _useAirbridgeProvider
                ? (_useSolarEngineProvider ? MMPSDK.AirbridgeSolarEngine : MMPSDK.Airbridge)
                : (_useSolarEngineProvider ? MMPSDK.SolarEngine : MMPSDK.NoMMP);
        }

        private void DrawAirbridge()
        {
            using (BeginPlacementCard(_useAirbridgeProvider))
                _useAirbridgeProvider = DrawPlacementToggle("Airbridge",
                    "Attribution and campaign measurement", _useAirbridgeProvider);
        }

        private void DrawSolarEngine()
        {
            using (BeginPlacementCard(_useSolarEngineProvider))
            {
                _useSolarEngineProvider = DrawPlacementToggle("SolarEngine",
                    "Attribution and ad revenue measurement", _useSolarEngineProvider);
                if (!_useSolarEngineProvider) return;
                DrawPlacementDivider();

                var row = GUILayoutUtility.GetRect(0, 30, GUILayout.ExpandWidth(true));
                var labelWidth = Mathf.Min(145, row.width * 0.35f);
                GUI.Label(new Rect(row.x, row.y + 5, labelWidth, 20),
                    new GUIContent("App key", "The app key from your SolarEngine project."));
                _owlSettings.solarEngineAppKey = EditorGUI.TextField(
                    new Rect(row.x + labelWidth, row.y + 5, Mathf.Max(0, row.width - labelWidth), 20),
                    _owlSettings.solarEngineAppKey);

                _owlSettings.solarEngineDebugMode = DrawFeatureToggle("Debug mode", _owlSettings.solarEngineDebugMode);
            }
        }

        [Obsolete("Obsolete")]
        private void DrawAnalyticsProviders()
        {
            DrawSubsectionTitle("Event analytics");
            DrawFirebase();
            DrawGameAnalytics();
        }
        private void DrawFirebase()
        {
            using (BeginPlacementCard(_useFirebaseEventProvider))
            {
                _useFirebaseEventProvider = DrawPlacementToggle("Firebase",
                    "Analytics events, crash reports, and remote controls", _useFirebaseEventProvider);
                if (!_useFirebaseEventProvider) return;
                DrawPlacementDivider();
                _useFirebaseAnalytics = DrawFeatureToggle("Analytics events", _useFirebaseAnalytics);
                _useFirebaseCrashlytics = DrawFeatureToggle("Crash reporting", _useFirebaseCrashlytics);
                _useFirebaseRemoteConfig = DrawFeatureToggle("Remote Config", _useFirebaseRemoteConfig);
                DrawMessagingSettings();
                if (_useFirebaseRemoteConfig)
                {
                    DrawPlacementDivider();
                    EditorGUILayout.LabelField("Remote ad controls", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField("Enable remote control per format. Local Off always takes priority.", EditorStyles.wordWrappedMiniLabel);
                    GUILayout.Space(2);
                    _remoteCompleteAds = DrawRemoteAdControl("All ads", Constants.REMOTECOMPLETEADS, _remoteCompleteAds);
                    _remoteAppOpen = DrawRemoteAdControl("App open ads", Constants.REMOTEAPPOPEN, _remoteAppOpen);
                    _remoteBanner = DrawRemoteAdControl("Banner ads", Constants.REMOTEBANNER, _remoteBanner);
                    _remoteInterstitial = DrawRemoteAdControl("Interstitial ads", Constants.REMOTEINTERSTITIAL, _remoteInterstitial);
                    _remoteRewarded = DrawRemoteAdControl("Rewarded ads", Constants.REMOTEREWARDED, _remoteRewarded);
                    _remoteRewardedInterstitial = DrawRemoteAdControl("Rewarded interstitial ads", Constants.REMOTEREWARDEDINTERSTITIAL, _remoteRewardedInterstitial);
                    _remoteMREC = DrawRemoteAdControl("MREC ads", Constants.REMOTEMREC, _remoteMREC);
                    _remoteTimedAds = DrawRemoteAdControl("Timed ads", Constants.REMOTETIMEDADS, _remoteTimedAds);
                    GUILayout.Space(4);
                    EditorGUILayout.LabelField("Firebase keys are Boolean: true enables, false disables. Missing keys use local settings.",
                        EditorStyles.wordWrappedMiniLabel);
                }
            }
        }

        private void DrawMessagingSettings()
        {
            GUILayout.Space(2);
            var style = new GUIStyle
            {
                padding = new RectOffset(6, 6, 3, 3),
                margin = new RectOffset(0, 0, 0, 1)
            };
            using (var panel = new EditorGUILayout.VerticalScope(style))
            {
                if (Event.current.type == EventType.Repaint)
                    EditorGUI.DrawRect(panel.rect, EditorGUIUtility.isProSkin
                        ? new Color(0, 0, 0, 0.12f)
                        : new Color(0, 0, 0, 0.04f));

                _useFirebaseMessaging = DrawPlacementToggle("Push notifications",
                    "Receive messages through Firebase Cloud Messaging", _useFirebaseMessaging);
                if (_useFirebaseMessaging)
                {
                    DrawPlacementDivider();
                    _requestPushPermissionOnStart = EditorGUILayout.ToggleLeft(
                        new GUIContent("Request permission on startup",
                            "Leave off to request permission later from your app."),
                        _requestPushPermissionOnStart);
                    EditorGUILayout.LabelField(_requestPushPermissionOnStart
                        ? "Ask for notification permission when Firebase starts."
                        : "Ask later at a suitable moment in your app.", EditorStyles.wordWrappedMiniLabel);
                    GUILayout.Space(3);

                    var sdkDetected = HasFirebaseMessaging();
                    EditorGUILayout.LabelField(sdkDetected
                        ? "Messaging SDK detected"
                        : "Messaging SDK not installed", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(sdkDetected
                        ? "Complete platform setup, then save settings to enable messaging."
                        : "Import FirebaseMessaging.unitypackage, then save settings.",
                        EditorStyles.wordWrappedMiniLabel);
                    GUILayout.Space(2);
                    if (GUILayout.Button("Open setup guide", GUILayout.Width(130), GUILayout.Height(24)))
                        Application.OpenURL("https://firebase.google.com/docs/cloud-messaging/unity/client");
                }
            }
        }

        private static bool DrawRemoteAdControl(string title, string key, bool enabled)
        {
            var row = GUILayoutUtility.GetRect(0, 39, GUILayout.ExpandWidth(true));
            var textWidth = Mathf.Max(0, row.width - 80);
            GUI.Label(new Rect(row.x, row.y + 1, textWidth, 18), title, EditorStyles.label);
            EditorGUI.SelectableLabel(new Rect(row.x, row.y + 19, textWidth, 16), key, EditorStyles.miniLabel);
            return DrawCompactSwitch(new Rect(row.xMax - 64, row.y + 8, 64, 22), enabled);
        }
        [Obsolete("Obsolete")]
        private void DrawGameAnalytics()
        {
            using (BeginPlacementCard(_useGameAnalyticsEventProvider))
            {
                _useGameAnalyticsEventProvider = DrawPlacementToggle("GameAnalytics",
                    "Platform events and progression analytics", _useGameAnalyticsEventProvider);
                if (!_useGameAnalyticsEventProvider) return;
                DrawPlacementDivider();
                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button(new GUIContent("Open GameAnalytics settings", "Open the GameAnalytics settings asset in the Inspector."),
                            GUILayout.Width(200), GUILayout.Height(28)))
                        OpenGameAnalyticsSettings();
                }

            }
        }
        private static void OpenGameAnalyticsSettings()
        {
#if USE_GAMEANALYTICS
            var settings = GameAnalyticsSDK.GameAnalytics.SettingsGA;
#else
            var settings = Resources.Load<ScriptableObject>("GameAnalytics/Settings");
#endif
            if (settings == null)
            {
                EditorUtility.DisplayDialog("GameAnalytics settings",
                    "Install the GameAnalytics SDK from Owl > SDKs > Install > GameAnalytics, then open its settings.", "OK");
                return;
            }

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
            EditorApplication.ExecuteMenuItem("Window/General/Inspector");
        }

        [Obsolete("Obsolete")]
        private void DrawMonetizations()
        {
            using (new EditorGUILayout.VerticalScope(CompactSectionStyle()))
            {
                DrawSectionHeader("Monetization", "Configure purchases, ad networks, and placements.");

                DrawIAP();
                DrawAdProviders();
                GUILayout.Space(0);
            }


        }
        [Obsolete("Obsolete")]
        private void DrawIAP()
        {
            using (BeginPlacementCard(_owlSettings.useIap))
            {
                _owlSettings.useIap = DrawPlacementToggle("In-app purchases", "Store products and remove-ads purchases", _owlSettings.useIap);
                if (_owlSettings.useIap)
                {
                    DrawPlacementDivider();
                    DrawRemoveAdsProducts();
                }
            }

        }
        private void DrawRemoveAdsProducts()
        {
            var products = _so.FindProperty("noAdsIds");
            EditorGUILayout.LabelField("Remove ads products", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Purchasing any of these products removes ads. Use IDs from your store catalog.",
                EditorStyles.wordWrappedMiniLabel);
            GUILayout.Space(3);

            var removeIndex = -1;
            for (var i = 0; i < products.arraySize; i++)
            {
                var row = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
                GUI.Label(new Rect(row.x, row.y + 4, 24, 20), (i + 1).ToString(), EditorStyles.centeredGreyMiniLabel);
                var field = new Rect(row.x + 28, row.y + 4, Mathf.Max(0, row.width - 108), 20);
                var product = products.GetArrayElementAtIndex(i);
                var controlName = "OwlRemoveAdsProduct" + i;
                GUI.SetNextControlName(controlName);
                product.stringValue = EditorGUI.TextField(field, product.stringValue);
                if (string.IsNullOrEmpty(product.stringValue) && GUI.GetNameOfFocusedControl() != controlName)
                    GUI.Label(new Rect(field.x + 5, field.y, Mathf.Max(0, field.width - 10), field.height),
                        "Product ID", EditorStyles.centeredGreyMiniLabel);
                if (GUI.Button(new Rect(row.xMax - 72, row.y + 4, 72, 20),
                        new GUIContent("Remove", "Remove this product from the list."), EditorStyles.miniButton))
                    removeIndex = i;
            }

            GUILayout.Space(4);
            var add = GUILayout.Button("+ Add product", EditorStyles.miniButton, GUILayout.Width(120), GUILayout.Height(24));
            if (removeIndex >= 0)
                products.DeleteArrayElementAtIndex(removeIndex);
            else if (add)
            {
                products.InsertArrayElementAtIndex(products.arraySize);
                products.GetArrayElementAtIndex(products.arraySize - 1).stringValue = string.Empty;
            }
            else
                return;

            _so.ApplyModifiedProperties();
            GUI.FocusControl(null);
            // Rebuild layout after changing the number of fields.
            GUIUtility.ExitGUI();
        }

        private void DrawInterstitialDelay()
        {
            using (BeginPlacementList())
            {
                _owlSettings.useInterstitialAd = DrawManagedAdPlacement("Interstitial ads",
                    "Full-screen ads between gameplay moments", _owlSettings.useInterstitialAd,
                    ref _owlSettings.interstitialOnStart);
                if (_owlSettings.useInterstitialAd)
                {
                    _showInterstitialOptions = DrawOptionsDisclosure("More settings", _showInterstitialOptions);
                    if (_showInterstitialOptions)
                    {
                        BeginPlacementOptions();
                        _owlSettings.interstitialInterval = DrawSecondsSetting("Minimum interval",
                            "Minimum time between interstitial ads.", _owlSettings.interstitialInterval, true, 5, 120);
                        _owlSettings.resetIntervalOnRewarded = DrawSettingCheckbox("Reset after rewarded ads",
                            "Restart the interstitial interval after a rewarded ad closes.", _owlSettings.resetIntervalOnRewarded);
                        DrawTimerAdDetail();
                        EndPlacementOptions();
                    }
                }
            }
        }
        private void DrawAdditionalAdPlacements()
        {
            using (BeginPlacementList())
                _owlSettings.useRewardedAd = DrawManagedAdPlacement("Rewarded ads",
                    "Player-initiated ads with rewards", _owlSettings.useRewardedAd,
                    ref _owlSettings.rewardedOnStart);
            DrawPlacementDivider();
            using (BeginPlacementList())
            {
                if (_mediation == Mediation.Max)
                {
                    using (new EditorGUI.DisabledScope(true))
                        DrawPlacementToggle("Rewarded interstitial ads",
                            "Currently supported by AdMob only.", _owlSettings.useRewardedInterstitialAd);
                    EditorGUILayout.HelpBox(
                        "Rewarded Interstitial is not implemented for MAX. Its saved setting is retained, but it will not initialize or show.",
                        MessageType.Info);
                }
                else
                {
                    _owlSettings.useRewardedInterstitialAd = DrawManagedAdPlacement("Rewarded interstitial ads",
                        "Full-screen ads that grant a reward", _owlSettings.useRewardedInterstitialAd,
                        ref _owlSettings.rewardedInterstitialOnStart);
                }
            }
            DrawPlacementDivider();
            using (BeginPlacementList())
                _owlSettings.useMREC = DrawPlacementToggle("MREC ads", "Medium rectangle placements within your game", _owlSettings.useMREC);
        }
        private void DrawAppOpen()
        {
            using (BeginPlacementList())
            {
                _owlSettings.useAppOpenAd = DrawPlacementToggle("App open ads", "When players open or return to the app", _owlSettings.useAppOpenAd);
                if (_owlSettings.useAppOpenAd)
                {
                    _showAppOpenOptions = DrawOptionsDisclosure("More settings", _showAppOpenOptions);
                    if (_showAppOpenOptions)
                    {
                        BeginPlacementOptions();
                        _owlSettings.appOpenStartDelay = DrawSecondsSetting("Start delay",
                            "Delay before the first app-open ad attempt.", _owlSettings.appOpenStartDelay, true, 0, 15);
                        EndPlacementOptions();
                    }
                }
            }

        }
        private void DrawBanner()
        {
            using (BeginPlacementList())
            {
                _owlSettings.useBanner = DrawPlacementToggle("Banner ads", "Persistent ads at the edge of the screen", _owlSettings.useBanner);
                if (_owlSettings.useBanner)
                {
                    _showBannerOptions = DrawOptionsDisclosure("More settings", _showBannerOptions);
                    if (_showBannerOptions)
                    {
                        BeginPlacementOptions();
                        var row = GUILayoutUtility.GetRect(0, 24, GUILayout.ExpandWidth(true));
                        GUI.Label(new Rect(row.x, row.y + 3, 110, 20), "Position", EditorStyles.label);
                        _owlSettings.bannerPosition = (BannerPosition)EditorGUI.EnumPopup(
                            new Rect(row.x + 116, row.y + 2, Mathf.Max(80, row.width - 116), 20),
                            _owlSettings.bannerPosition);
                        _owlSettings.bannerOnStart = DrawSettingCheckbox("Show on startup",
                            "Show the banner as soon as the SDK initializes.", _owlSettings.bannerOnStart);
                        EndPlacementOptions();
                    }
                }
            }

        }
        private void DrawTimerAdDetail()
        {
            GUILayout.Space(2);
            using (BeginPlacementList())
            {
                _owlSettings.enableTimerAd = DrawFeatureToggle("Timed ads", _owlSettings.enableTimerAd);
                if (_owlSettings.enableTimerAd)
                {
                    _showTimerOptions = DrawOptionsDisclosure("More settings", _showTimerOptions);
                    if (_showTimerOptions)
                    {
                        BeginPlacementOptions();
                        _owlSettings.timerAdInterval = DrawSecondsSetting("Repeat interval",
                            "Time between automatic interstitial ad attempts.", _owlSettings.timerAdInterval, true, 0, 360, clampValue: false);
                        _owlSettings.autoStartTimerAd = DrawSettingCheckbox("Start automatically",
                            "Begin the timer when the SDK initializes. Otherwise, start it from your game.",
                            _owlSettings.autoStartTimerAd);
                        EndPlacementOptions();
                    }
                }
            }
        }
        // ReSharper disable Unity.PerformanceAnalysis
        [Obsolete("Obsolete")]
        private void DrawAdProviders()
        {
            DrawSubsectionTitle("Advertising");
            DrawProviderTabs();

            var useGoogleAds = false;
            var useMaxAds = false;
            switch (_mediation)
            {
                case Mediation.NoAds:
                    useGoogleAds =useMaxAds = false;
                    break;
                case Mediation.GoogleAds:
                    useGoogleAds = true;
                    useMaxAds = false;
                    break;
                case Mediation.Max:
                    useGoogleAds = false;
                    useMaxAds = true;
                    break;
                default:
                    useGoogleAds =useMaxAds = false;
                    break;
            }

            if (useGoogleAds || useMaxAds)
            {
                GUILayout.Space(4);
                DrawSubsectionTitle("Ad placements");
                DrawAppOpen();
                DrawBanner();
                DrawInterstitialDelay();
                DrawAdditionalAdPlacements();
                DrawSubsectionTitle("Ad unit configuration");
                if (useGoogleAds)
                    DrawGoogleAds();
                else
                    DrawMaxAds();
            }

        }
        private static T DrawSelectionTabs<T>(T current, T[] values, string[] labels) where T : struct, Enum
        {
            // Wrap at narrow widths so long provider names remain readable.
            var rect = GUILayoutUtility.GetRect(0f, EditorGUIUtility.currentViewWidth < 700f && values.Length > 3 ? 72f : 38f,
                GUILayout.ExpandWidth(true));
            int columns = rect.height > 38f ? 2 : values.Length;
            var background = EditorGUIUtility.isProSkin
                ? new Color(0.17f, 0.19f, 0.21f)
                : new Color(0.91f, 0.93f, 0.93f);
            EditorGUI.DrawRect(rect, background);
            float width = (rect.width - 8f) / columns;
            for (int i = 0; i < values.Length; i++)
            {
                var tab = new Rect(rect.x + 4f + (i % columns) * width,
                    rect.y + 3f + (i / columns) * 34f, width - 4f, 30f);
                bool selected = System.Collections.Generic.EqualityComparer<T>.Default.Equals(current, values[i]);
                if (selected) EditorGUI.DrawRect(tab, AccentColor);
                var style = new GUIStyle(EditorStyles.label)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    fontSize = 12,
                    wordWrap = true
                };
                if (selected) style.normal.textColor = new Color(0.06f, 0.13f, 0.14f);
                if (GUI.Button(tab, new GUIContent(labels[i], "Select " + labels[i]), style) && !selected)
                {
                    current = values[i];
                    GUI.FocusControl(null);
                }
            }
            GUILayout.Space(4f);
            return current;
        }

        private void DrawProviderTabs()
        {
            _mediation = DrawSelectionTabs(_mediation,
                new[] { Mediation.NoAds, Mediation.GoogleAds, Mediation.Max },
                new[] { "No Ads", "AdMob", "MAX" });
            GUILayout.Space(4f);
            if (_mediation == Mediation.NoAds)
                EditorGUILayout.HelpBox("Advertising is disabled. Your AdMob and MAX settings are kept for later use.", MessageType.Info);
            else
                EditorGUILayout.LabelField(_mediation == Mediation.GoogleAds
                    ? "Configure AdMob placements and ad units below."
                    : "Configure AppLovin MAX placements and ad units below.", EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawGoogleAds()
        {
            var property = _so.FindProperty("googleAds");
            EditorGUILayout.PropertyField(property, true);
        }
        private void DrawMaxAds()
        {
            var property = _so.FindProperty("maxAds");
            EditorGUILayout.PropertyField(property, true);
        }
        [Obsolete("Obsolete")]
        private void DrawSaveSettings()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Space(10);
                    EditorGUILayout.LabelField("Owl SDK  " + _owlSettings.SDKVersion.VERSION, EditorStyles.miniLabel);
                    GUILayout.FlexibleSpace();
                    var previousColor = GUI.backgroundColor;
                    GUI.backgroundColor = AccentColor;
                    if (GUILayout.Button("Save settings", new GUIStyle(EditorStyles.miniButton) { fixedHeight = 34, fontStyle = FontStyle.Bold }, GUILayout.Width(180)))
                        QueueSaveSettings();
                    GUI.backgroundColor = previousColor;
                    GUILayout.Space(8);
                }
            }
        }
        private void QueueSaveSettings()
        {
            if (_saveQueued) return;
            _saveQueued = true;
            // Asset writes and define changes can refresh/reload the editor.
            // Run them only after Unity has finished the current GUI event.
            EditorApplication.delayCall += SaveSettingsAfterGUI;
        }

        [Obsolete("Obsolete")]
        private void SaveSettingsAfterGUI()
        {
            _saveQueued = false;
            if (this == null || _owlSettings == null) return;
            SaveSettings();
        }

        private void OnDisable()
        {
            EditorApplication.delayCall -= SaveSettingsAfterGUI;
            _saveQueued = false;
            _so?.Dispose();
            _so = null;
        }

        private bool DrawButton(string label,Color color,float width)
        {
            var style = new GUIStyle(EditorStyles.miniButtonMid)
            {
                normal = {textColor = color},
                onHover = {textColor = color},
                hover = {textColor = color},
                fixedWidth = width
            };
            return GUILayout.Button(label,style);
        }


        #endregion

        [Obsolete("Obsolete")]
        private void SaveSettings()
        {
            LoadOwlSettings();

            _analyticsSdk = _useFirebaseEventProvider
                ? (_useGameAnalyticsEventProvider ? AnalyticsSDK.FirebaseGameAnalytics : AnalyticsSDK.Firebase)
                : (_useGameAnalyticsEventProvider ? AnalyticsSDK.GameAnalytics : AnalyticsSDK.WithoutData);
            _owlSettings.analyticsSdk = _analyticsSdk;

            _owlSettings.mmpsdk = _useAirbridgeProvider
                ? (_useSolarEngineProvider ? MMPSDK.AirbridgeSolarEngine : MMPSDK.Airbridge)
                : (_useSolarEngineProvider ? MMPSDK.SolarEngine : MMPSDK.NoMMP);

            _owlSettings.useFirebaseAnalytics =  _useFirebaseAnalytics;
            _owlSettings.useFirebaseCrashlytics = _useFirebaseCrashlytics;
            _owlSettings.useFirebaseRemoteConfig =_useFirebaseRemoteConfig;
            _owlSettings.useFirebaseMessaging = _useFirebaseMessaging;
            _owlSettings.useFacebookEvents = _useFacebookEvents;
            _owlSettings.requestPushPermissionOnStart = _requestPushPermissionOnStart;
            _owlSettings.remoteCompleteAds =  _remoteCompleteAds && _useFirebaseRemoteConfig;
            _owlSettings.remoteAppOpen =  _remoteAppOpen && _useFirebaseRemoteConfig;
            _owlSettings.remoteBanner = _remoteBanner && _useFirebaseRemoteConfig;
            _owlSettings.remoteInterstitial = _remoteInterstitial && _useFirebaseRemoteConfig;
            _owlSettings.remoteRewarded = _remoteRewarded && _useFirebaseRemoteConfig;
            _owlSettings.remoteRewardedInterstitial = _remoteRewardedInterstitial && _useFirebaseRemoteConfig;
            _owlSettings.remoteMREC = _remoteMREC && _useFirebaseRemoteConfig;
            _owlSettings.remoteTimedAds = _remoteTimedAds && _useFirebaseRemoteConfig;

            _owlSettings.mediation = _mediation;

            _owlSettings.noAdsIds = noAdsIds;

            switch (_mediation)
            {
                case Mediation.GoogleAds:
                    _owlSettings.adSettings.googleAds=googleAds;
                    GoogleMobileAdsSettingsAccessor.SetAndroidAppId(googleAds.appIdAndroid);
                    GoogleMobileAdsSettingsAccessor.SetIOSAppId(googleAds.appIdIOS);
                    break;
                case Mediation.Max:
                    _owlSettings.adSettings.maxAds=maxAds;
                    break;
            }
            SetSymbolsDirectives();
            SaveAsset();
        }

        private void SaveAsset()
        {
            if (_owlSettings)
                EditorUtility.SetDirty(_owlSettings);

            if (_owlSettings.adSettings)
                EditorUtility.SetDirty(_owlSettings.adSettings);

            AssetDatabase.SaveAssets();
        }

        private static bool HasFirebaseMessaging()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (assembly.GetType("Firebase.Messaging.FirebaseMessaging", false) != null)
                    return true;
            return false;
        }

        private static bool HasType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                if (assembly.GetType(fullName, false) != null)
                    return true;
            return false;
        }

        // ReSharper disable Unity.PerformanceAnalysis
        [Obsolete("Obsolete")]
        private void SetSymbolsDirectives()
        {
            // --- Mediation defines ---
            bool useGoogleAds = _mediation == Mediation.GoogleAds &&
                                HasType("GoogleMobileAds.Api.MobileAds");
            bool useMaxAds = _mediation == Mediation.Max && HasType("MaxSdkBase");

            bool useFirebase = _useFirebaseEventProvider;
            bool useGameAnalytics = _useGameAnalyticsEventProvider;
            bool firebaseInstalled = HasType("Firebase.FirebaseApp");
            bool firebaseAnalyticsInstalled = HasType("Firebase.Analytics.FirebaseAnalytics");
            bool firebaseCrashlyticsInstalled = HasType("Firebase.Crashlytics.Crashlytics");
            bool firebaseRemoteConfigInstalled = HasType("Firebase.RemoteConfig.FirebaseRemoteConfig");
            bool gameAnalyticsInstalled = HasType("GameAnalyticsSDK.GameAnalytics");
            bool facebookInstalled = HasFacebookSdk();
            bool airbridgeInstalled = HasType("AirbridgeUnity");
            bool solarEngineInstalled = HasType("SolarEngine.Analytics");
            bool iapInstalled = HasType("UnityEngine.Purchasing.StoreController");

            DefineSymbolUtility.ScriptingDefineSymbol(Constants.USE_ADMOB, useGoogleAds);
            DefineSymbolUtility.ScriptingDefineSymbol(Constants.USE_MAXADS, useMaxAds);
            DefineSymbolUtility.ScriptingDefineSymbol(Constants.USE_MMP, _owlSettings.useMMP);
            DefineSymbolUtility.ScriptingDefineSymbol(Constants.USE_AIRBRIDGE, _owlSettings.useMMP && _owlSettings.useAirbridge && airbridgeInstalled);
            DefineSymbolUtility.ScriptingDefineSymbol(Constants.USE_SOLARENGINE, _owlSettings.useMMP && _owlSettings.useSolarEngine && solarEngineInstalled);

            // --- Analytics defines ---

            DefineSymbolUtility.ScriptingDefineSymbol(Constants.USE_FIREBASE, useFirebase && firebaseInstalled);
            DefineSymbolUtility.ScriptingDefineSymbol(Constants.USE_FIREBASEANALYTICS, useFirebase && _useFirebaseAnalytics && firebaseAnalyticsInstalled);
            DefineSymbolUtility.ScriptingDefineSymbol(Constants.USE_FIREBASECRASHLYTICS, useFirebase && _useFirebaseCrashlytics && firebaseCrashlyticsInstalled);
            DefineSymbolUtility.ScriptingDefineSymbol(Constants.USE_FIREBASEREMOTECONFIG, useFirebase && _useFirebaseRemoteConfig && firebaseRemoteConfigInstalled);
            DefineSymbolUtility.ScriptingDefineSymbol(Constants.USE_FIREBASEMESSAGING, useFirebase && _useFirebaseMessaging && HasFirebaseMessaging());
            DefineSymbolUtility.ScriptingDefineSymbol(Constants.USE_GAMEANALYTICS, useGameAnalytics && gameAnalyticsInstalled);
            DefineSymbolUtility.ScriptingDefineSymbol(Constants.USE_FACEBOOK, _useFacebookEvents && facebookInstalled);
            DefineSymbolUtility.ScriptingDefineSymbol(Constants.USE_FACEBOOK_SDK,
                _useFacebookEvents && facebookInstalled);

            // --- IAP define ---
            DefineSymbolUtility.ScriptingDefineSymbol(Constants.USE_IAP, _owlSettings.useIap && iapInstalled);
        }


    }

}
