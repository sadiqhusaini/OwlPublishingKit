using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Owl.Common
{
    public static class GoogleMobileAdsSettingsAccessor
    {
        private const string ResourceName = "GoogleMobileAdsSettings";

        // Load the internal ScriptableObject from Resources
        private static object LoadInstance()
        {
            ScriptableObject obj = Resources.Load(ResourceName) as ScriptableObject;

            if (obj == null)
            {
                CustomDebug.LogError("GoogleMobileAdsSettings.asset NOT FOUND in Resources.");
                return null;
            }

            return obj;
        }

        // Get property value using reflection
        private static T GetPropertyValue<T>(string propertyName)
        {
            var settings = LoadInstance();
            if (settings == null) return default;

            var type = settings.GetType();
            var prop = type.GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (prop == null)
            {
                CustomDebug.LogError("Property not found: " + propertyName);
                return default;
            }

            return (T)prop.GetValue(settings);
        }

        // Set property value using reflection
        private static void SetPropertyValue<T>(string propertyName, T value)
        {
            var settings = LoadInstance();
            if (settings == null) return;

            var type = settings.GetType();
            var prop = type.GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (prop == null)
            {
                CustomDebug.LogError("Property not found: " + propertyName);
                return;
            }

            prop.SetValue(settings, value);

            // Save changes
            EditorUtility.SetDirty((UnityEngine.Object)settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        // Public GET API
        public static string AndroidAppId =>
            GetPropertyValue<string>("GoogleMobileAdsAndroidAppId");

        public static string IOSAppId =>
            GetPropertyValue<string>("GoogleMobileAdsIOSAppId");

        // Public SET API
        public static void SetAndroidAppId(string value) =>
            SetPropertyValue("GoogleMobileAdsAndroidAppId", value);

        public static void SetIOSAppId(string value) =>
            SetPropertyValue("GoogleMobileAdsIOSAppId", value);
    }
}