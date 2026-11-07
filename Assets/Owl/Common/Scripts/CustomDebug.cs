using UnityEngine;

namespace Owl
{
    public static class CustomDebug
    {
        private static OwlSettings _owlSettings;

        private static void InitializeSettings()
        {
            if (_owlSettings == null)
                _owlSettings = Resources.Load<OwlSettings>("OwlSettings");
        }

        private static string Prefix => _owlSettings != null ? _owlSettings.SDKName + ": " : "Owl: ";

        // Routine diagnostics are opt-in. Warnings and errors remain visible even
        // before OwlManager starts or when the settings asset is missing.
        public static void Log(object message)
        {
            InitializeSettings();
            if (_owlSettings != null && _owlSettings.verbose)
                Debug.Log(Prefix + message);
        }

        public static void Log(object message, Object context)
        {
            InitializeSettings();
            if (_owlSettings != null && _owlSettings.verbose)
                Debug.Log(Prefix + message, context);
        }

        public static void LogError(object message)
        {
            InitializeSettings();
            Debug.LogError(Prefix + message);
        }

        public static void LogError(object message, Object context)
        {
            InitializeSettings();
            Debug.LogError(Prefix + message, context);
        }

        public static void LogWarning(object message)
        {
            InitializeSettings();
            Debug.LogWarning(Prefix + message);
        }

        public static void LogFormat(string format, params object[] args)
        {
            InitializeSettings();
            if (_owlSettings != null && _owlSettings.verbose)
                Debug.LogFormat(Prefix + format, args);
        }
    }
}
