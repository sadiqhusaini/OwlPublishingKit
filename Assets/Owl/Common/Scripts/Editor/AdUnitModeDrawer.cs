using Owl.Monetization;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Owl.EditorTools
{
    // Shared by the SDK window and the AdSettings asset inspector.
    [CustomPropertyDrawer(typeof(GoogleAds))]
    [CustomPropertyDrawer(typeof(MaxAds))]
    public class AdUnitModeDrawer : PropertyDrawer
    {
        private static System.Collections.Generic.IEnumerable<SerializedProperty> Rows(SerializedProperty root)
        {
            bool google = root.type == nameof(GoogleAds);
            int mode = root.FindPropertyRelative("mode").intValue;
            bool single = google ? mode == (int)GoogleAdUnitMode.Single : mode == (int)MaxAdUnitMode.Single;
            var child = root.Copy();
            var end = root.GetEndProperty();
            if (!child.NextVisible(true)) yield break;
            do
            {
                if (SerializedProperty.EqualContents(child, end)) yield break;
                // Unity reports the element type for some serialized arrays too.
                // Skip only the direct single fields, never their array counterparts.
                if (!child.isArray && child.type == nameof(AdUnit)) continue;
                if (single && child.isArray && child.propertyType != SerializedPropertyType.String)
                {
                    string name = null;
                    switch (child.name)
                    {
                        case "bannerAdUnits": name = nameof(GoogleAds.bannerAdUnit); break;
                        case "mRecAdUnits": name = nameof(GoogleAds.mRecAdUnit); break;
                        case "interstitialAdUnits": name = nameof(GoogleAds.interstitialAdUnit); break;
                        case "rewardedInterstitialAdUnits": name = nameof(GoogleAds.rewardedInterstitialAdUnit); break;
                        case "rewardedAdUnits": name = nameof(GoogleAds.rewardedAdUnit); break;
                        case "appOpenAdUnits": name = nameof(GoogleAds.appOpenAdUnit); break;
                    }
                    if (name != null)
                        yield return root.FindPropertyRelative(name);
                }
                else yield return child.Copy();
            } while (child.NextVisible(false));
        }

        private static bool IsSingleRow(SerializedProperty property) =>
            !property.isArray && property.type == nameof(AdUnit) &&
            !property.propertyPath.Contains(".Array.data[");

        private readonly System.Collections.Generic.Dictionary<string, AdUnitGroup> _groups =
            new System.Collections.Generic.Dictionary<string, AdUnitGroup>();

        private AdUnitGroup Group(SerializedProperty property)
        {
            string key = property.serializedObject.targetObject.GetInstanceID() + ":" + property.propertyPath;
            if (!_groups.TryGetValue(key, out var group) || group.Owner != property.serializedObject)
            {
                group = new AdUnitGroup(property, IsSingleRow(property));
                _groups[key] = group;
            }
            return group;
        }

        private float RowHeight(SerializedProperty property)
        {
            return IsSingleRow(property) || property.isArray && property.propertyType != SerializedPropertyType.String
                ? Group(property).Height : EditorGUIUtility.singleLineHeight +
                (property.name == "mode" ? 46f : 0f);
        }

        private static string FormatLabel(string name)
        {
            switch (name)
            {
                case "bannerAdUnit": case "bannerAdUnits": return "Banner";
                case "mRecAdUnit": case "mRecAdUnits": return "MREC";
                case "interstitialAdUnit": case "interstitialAdUnits": return "Interstitial";
                case "rewardedInterstitialAdUnit": case "rewardedInterstitialAdUnits": return "Rewarded interstitial";
                case "rewardedAdUnit": case "rewardedAdUnits": return "Rewarded";
                case "appOpenAdUnit": case "appOpenAdUnits": return "App open";
                default: return ObjectNames.NicifyVariableName(name);
            }
        }

        private static string ModeDescription(SerializedProperty network)
        {
            int mode = network.FindPropertyRelative("mode").intValue;
            if (network.type == nameof(GoogleAds))
            {
                if (mode == (int)GoogleAdUnitMode.Flooring)
                    return "Fullscreen ads use the first available unit in list order. Banner and MREC rotate through their lists.";
                if (mode == (int)GoogleAdUnitMode.Multiple)
                    return "Each new load uses the next unit in its format's list, then starts again from the first.";
            }
            else if (mode == (int)MaxAdUnitMode.Multiple)
                return "Each new load uses the next unit in its format's list, then starts again from the first.";
            return "One ad unit per format. Enter the IDs for the platforms your game supports.";
        }

        // Shared format card; only the number of entries changes between modes.
        private sealed class AdUnitGroup
        {
            public SerializedObject Owner => _property.serializedObject;
            private readonly SerializedProperty _property;
            private readonly bool _single;
            private readonly ReorderableList _list;
            private bool _expanded;
            private const float HeaderHeight = 26f;
            private bool Expanded
            {
                get => _single ? _expanded : _property.isExpanded;
                set { if (_single) _expanded = value; else _property.isExpanded = value; }
            }

            public AdUnitGroup(SerializedProperty property, bool single)
            {
                _property = property.Copy();
                _single = single;
                _expanded = true;
                if (single) return;
                _list = new ReorderableList(property.serializedObject, _property, true, false, true, true);
                _list.elementHeightCallback = i => AdUnitsDrawer.FieldsHeight(Element(i), true) + 12f;
                _list.drawElementCallback = (rect, i, active, focused) =>
                {
                    rect.y += 6f;
                    rect.height -= 12f;
                    AdUnitsDrawer.DrawFields(rect, Element(i), "Unit " + (i + 1));
                };
                _list.onAddCallback = list =>
                {
                    int index = _property.arraySize++;
                    var entry = _property.GetArrayElementAtIndex(index);
                    entry.FindPropertyRelative("adUnitAndroid").stringValue = "";
                    entry.FindPropertyRelative("adUnitIOS").stringValue = "";
                    entry.FindPropertyRelative("adFloor").intValue = (int)AdFloor.All;
                    list.index = index;
                };
            }

            private SerializedProperty Element(int index) =>
                _single ? _property : _property.GetArrayElementAtIndex(index);

            private string Summary()
            {
                if (!_single) return _property.arraySize == 1 ? "1 unit" : _property.arraySize + " units";
                bool android = !string.IsNullOrWhiteSpace(_property.FindPropertyRelative("adUnitAndroid").stringValue);
                bool ios = !string.IsNullOrWhiteSpace(_property.FindPropertyRelative("adUnitIOS").stringValue);
                return android && ios ? "Android + iOS" : android ? "Android" : ios ? "iOS" : "No IDs";
            }

            public float Height => 8f + HeaderHeight + (Expanded
                ? (_single ? AdUnitsDrawer.FieldsHeight(_property, false) + 16f : _list.GetHeight() + 8f) : 0f);

            public void Draw(Rect position)
            {
                position.y += 8f;
                position.height -= 8f;
                position = EditorGUI.IndentedRect(position);
                using (new EditorGUI.PropertyScope(position, GUIContent.none, _property))
                {
                    GUI.Box(position, GUIContent.none, EditorStyles.helpBox);
                    var header = new Rect(position.x + 12f, position.y + 3f, position.width - 24f, HeaderHeight - 6f);
                    int indent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = 0;
                    try
                    {
                        var title = header;
                        title.width = Mathf.Max(60f, header.width - 125f);
                        Expanded = EditorGUI.Foldout(title, Expanded, FormatLabel(_property.name), true, EditorStyles.foldoutHeader);
                        var summary = new Rect(header.xMax - 120f, header.y, 120f, header.height);
                        GUI.Label(summary, Summary(), new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight });
                        if (Expanded)
                        {
                            var body = new Rect(position.x + 10f, position.y + HeaderHeight + 4f,
                                position.width - 20f, position.height - HeaderHeight - 8f);
                            if (_single) AdUnitsDrawer.DrawFields(body, _property, null);
                            else _list.DoList(body);
                        }
                    }
                    finally
                    {
                        EditorGUI.indentLevel = indent;
                    }
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = EditorGUIUtility.singleLineHeight;
            if (property.isExpanded)
                foreach (var child in Rows(property))
                    height += RowHeight(child) + EditorGUIUtility.standardVerticalSpacing;
            return height;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            using (new EditorGUI.PropertyScope(position, label, property))
            {
                var row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                property.isExpanded = EditorGUI.Foldout(row, property.isExpanded, label, true);
                if (property.isExpanded)
                {
                    int indent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel++;
                    try
                    {
                        foreach (var child in Rows(property))
                        {
                            row.y += row.height + EditorGUIUtility.standardVerticalSpacing;
                            row.height = RowHeight(child);
                            if (IsSingleRow(child) || child.isArray && child.propertyType != SerializedPropertyType.String) Group(child).Draw(row);
                            else
                            {
                                var field = new Rect(row.x, row.y, row.width, EditorGUIUtility.singleLineHeight);
                                if (child.name == "mode")
                                {
                                    EditorGUI.PropertyField(field, child, new GUIContent("Ad unit mode"));
                                    var hint = new Rect(row.x, field.yMax + 4f, row.width, 38f);
                                    EditorGUI.HelpBox(hint, ModeDescription(property), MessageType.Info);
                                }
                                else if (child.propertyType == SerializedPropertyType.String)
                                {
                                    string title = child.name == "appIdAndroid" ? "Android app ID" :
                                        child.name == "appIdIOS" ? "iOS app ID" : child.name == "sdkID" ? "SDK key" : child.displayName;
                                    child.stringValue = EditorGUI.TextField(field, title, child.stringValue);
                                }
                                else EditorGUI.PropertyField(field, child, true);
                            }
                        }
                    }
                    finally
                    {
                        EditorGUI.indentLevel = indent;
                    }
                }
            }
        }
    }

    [CustomPropertyDrawer(typeof(AdUnit))]
    public class AdUnitsDrawer : PropertyDrawer
    {
        private static bool ShowsFloor(SerializedProperty property)
        {
            if (property.type == nameof(AdUnit) && !property.propertyPath.Contains(".Array.data[")) return false;
            int dot = property.propertyPath.IndexOf('.');
            if (dot < 0) return false;
            var network = property.serializedObject.FindProperty(property.propertyPath.Substring(0, dot));
            return network != null && network.type == nameof(GoogleAds) &&
                network.FindPropertyRelative("mode").intValue == (int)GoogleAdUnitMode.Flooring;
        }

        internal static float FieldsHeight(SerializedProperty property, bool title)
        {
            int rows = 2 + (ShowsFloor(property) ? 1 : 0) + (title ? 1 : 0);
            return rows * EditorGUIUtility.singleLineHeight + (rows - 1) * 4f;
        }

        internal static void DrawFields(Rect position, SerializedProperty property, string title)
        {
            using (new EditorGUI.PropertyScope(position, GUIContent.none, property))
            {
                float oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = Mathf.Min(115f, position.width * 0.35f);
                try
                {
                    var row = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
                    if (title != null)
                    {
                        EditorGUI.LabelField(row, title, EditorStyles.boldLabel);
                        row.y += row.height + 4f;
                    }
                    if (ShowsFloor(property))
                    {
                        EditorGUI.PropertyField(row, property.FindPropertyRelative("adFloor"),
                            new GUIContent("Floor", "Priority follows list order; configure the price floor in AdMob."));
                        row.y += row.height + 4f;
                    }
                    EditorGUI.PropertyField(row, property.FindPropertyRelative("adUnitAndroid"), new GUIContent("Android ID"));
                    row.y += row.height + 4f;
                    EditorGUI.PropertyField(row, property.FindPropertyRelative("adUnitIOS"), new GUIContent("iOS ID"));
                }
                finally
                {
                    EditorGUIUtility.labelWidth = oldLabelWidth;
                }
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => FieldsHeight(property, true);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) =>
            DrawFields(position, property, label.text);
    }
}
