#if UNITY_EDITOR && UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace Owl.Utility
{
    public class BuildPostProcessorInfo
    {
        [PostProcessBuild(100)]
        public static void OnPostBuildProcessInfo(BuildTarget target, string pathXcode)
        {
            if (target != BuildTarget.iOS) return;
#if USE_ATT || USE_FIREBASEANALYTICS
            var infoPlistPath = Path.Combine(pathXcode, "Info.plist");
            var document = new PlistDocument();
            document.ReadFromString(File.ReadAllText(infoPlistPath));
#if USE_ATT
            document.root.SetString("NSUserTrackingUsageDescription",
                Resources.Load<Owl.OwlSettings>("OwlSettings").nativePopupText);
#endif
#if USE_FIREBASEANALYTICS
            // Runtime consent may enable collection after dependencies and the user's choice resolve.
            document.root.SetBoolean("FIREBASE_ANALYTICS_COLLECTION_ENABLED", false);
#endif
            File.WriteAllText(infoPlistPath, document.WriteToString());
#endif
        }
    }
}
#endif
