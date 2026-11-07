using System.IO;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace OwlPublishingKit
{
    [InitializeOnLoad]
    internal static class OwlPackageInstaller
    {
        private const string PackageName = "com.sadiqhusaini.owl-publishing-kit";
        private const string SourceFolderName = "Owl~";
        private const string DestinationFolderName = "Owl";

        static OwlPackageInstaller()
        {
            EditorApplication.delayCall += RemoveStaleProviderDefines;
            EditorApplication.delayCall += InstallIfMissing;
        }

        [MenuItem("Owl/Install or Update Assets", false, 1)]
        private static void InstallOrUpdateFromMenu()
        {
            Install(promptBeforeOverwrite: true);
        }

        private static void InstallIfMissing()
        {
            Install(promptBeforeOverwrite: false);
        }

        private static void RemoveStaleProviderDefines()
        {
            var unavailable = new Dictionary<string, bool>
            {
                ["USE_ADMOB"] = HasProvider("GoogleMobileAds.Api.MobileAds", "com.google.ads.mobile"),
                ["USE_MAXADS"] = HasProvider("MaxSdkBase", "com.applovin.mediation.ads"),
                ["USE_FIREBASE"] = HasProvider("Firebase.FirebaseApp"),
                ["USE_FIREBASEANALYTICS"] = HasProvider("Firebase.Analytics.FirebaseAnalytics"),
                ["USE_FIREBASECRASHLYTICS"] = HasProvider("Firebase.Crashlytics.Crashlytics"),
                ["USE_FIREBASEREMOTECONFIG"] = HasProvider("Firebase.RemoteConfig.FirebaseRemoteConfig"),
                ["USE_FIREBASEMESSAGING"] = HasProvider("Firebase.Messaging.FirebaseMessaging"),
                ["USE_GAMEANALYTICS"] = HasProvider("GameAnalyticsSDK.GameAnalytics"),
                ["USE_FACEBOOK"] = HasProvider("Facebook.Unity.FB"),
                ["USE_FACEBOOK_SDK"] = HasProvider("Facebook.Unity.FB"),
                ["USE_AIRBRIDGE"] = HasProvider("AirbridgeUnity", "Airbridge.AirbridgeUnity"),
                ["USE_SOLARENGINE"] = HasProvider("SolarEngine.Analytics"),
                ["USE_IAP"] = HasProvider("UnityEngine.Purchasing.StoreController", "com.unity.purchasing")
            };

            foreach (var group in new[] { BuildTargetGroup.Android, BuildTargetGroup.iOS })
            {
                var symbols = new List<string>(PlayerSettings.GetScriptingDefineSymbolsForGroup(group).Split(';'));
                var changed = false;
                foreach (var provider in unavailable)
                {
                    if (provider.Value || !symbols.Remove(provider.Key))
                        continue;
                    changed = true;
                    Debug.LogWarning($"Owl removed {provider.Key} because its provider SDK is not installed.");
                }

                if (changed)
                    PlayerSettings.SetScriptingDefineSymbolsForGroup(group, string.Join(";", symbols));
            }
        }

        private static bool HasProvider(string typeName, string packageName = null)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetType(typeName, false) != null)
                    return true;
            }

            if (string.IsNullOrEmpty(packageName))
                return false;

            try
            {
                return UnityEditor.PackageManager.PackageInfo.FindForPackageName(packageName) != null;
            }
            catch
            {
                return false;
            }
        }

        private static void Install(bool promptBeforeOverwrite)
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForPackageName(PackageName);
            if (package == null)
                return;

            var source = Path.Combine(package.resolvedPath, SourceFolderName);
            var destination = Path.Combine(Application.dataPath, DestinationFolderName);
            var destinationMeta = destination + ".meta";
            var sourceMeta = source + ".meta";

            if (!Directory.Exists(source))
            {
                Debug.LogError($"Owl Publishing Kit source folder was not found: {source}");
                return;
            }

            var destinationExists = Directory.Exists(destination);
            if (destinationExists && !promptBeforeOverwrite)
                return;

            if (destinationExists && !EditorUtility.DisplayDialog(
                    "Update Owl Publishing Kit",
                    "This will overwrite files in Assets/Owl with the package version. Back up any changes you made inside that folder first.",
                    "Update Assets",
                    "Cancel"))
                return;

            var guidRemaps = destinationExists
                ? new Dictionary<string, string>()
                : FindGuidConflicts(source, sourceMeta);
            CopyDirectory(source, destination, overwrite: destinationExists, guidRemaps: guidRemaps);

            if (File.Exists(sourceMeta) && !File.Exists(destinationMeta))
                CopyFile(sourceMeta, destinationMeta, overwrite: false, guidRemaps);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"Owl Publishing Kit assets installed to Assets/{DestinationFolderName}.");
        }

        private static Dictionary<string, string> FindGuidConflicts(string source, string sourceMeta)
        {
            var remaps = new Dictionary<string, string>();
            var metaPaths = new List<string>(Directory.GetFiles(source, "*.meta", SearchOption.AllDirectories));
            if (File.Exists(sourceMeta))
                metaPaths.Add(sourceMeta);

            foreach (var metaPath in metaPaths)
            {
                var match = Regex.Match(File.ReadAllText(metaPath), @"(?m)^guid:\s*([a-fA-F0-9]{32})\s*$");
                if (!match.Success)
                    continue;

                var originalGuid = match.Groups[1].Value;
                if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(originalGuid)) || remaps.ContainsKey(originalGuid))
                    continue;

                remaps.Add(originalGuid, Guid.NewGuid().ToString("N"));
                Debug.LogWarning($"Owl found an existing asset with GUID {originalGuid}; it will assign a new GUID to the installed copy and update Owl asset references.");
            }

            return remaps;
        }

        private static void CopyDirectory(string source, string destination, bool overwrite, Dictionary<string, string> guidRemaps)
        {
            Directory.CreateDirectory(destination);

            foreach (var file in Directory.GetFiles(source))
            {
                var target = Path.Combine(destination, Path.GetFileName(file));
                if (overwrite || !File.Exists(target))
                    CopyFile(file, target, overwrite, guidRemaps);
            }

            foreach (var directory in Directory.GetDirectories(source))
            {
                var target = Path.Combine(destination, Path.GetFileName(directory));
                CopyDirectory(directory, target, overwrite, guidRemaps);
            }
        }

        private static void CopyFile(string source, string destination, bool overwrite, Dictionary<string, string> guidRemaps)
        {
            if (guidRemaps.Count == 0 || !CanContainUnityReferences(source))
            {
                File.Copy(source, destination, overwrite);
                return;
            }

            var text = File.ReadAllText(source);
            foreach (var remap in guidRemaps)
                text = text.Replace(remap.Key, remap.Value);
            File.WriteAllText(destination, text);
        }

        private static bool CanContainUnityReferences(string path)
        {
            var extension = Path.GetExtension(path);
            return extension == ".meta" || extension == ".prefab" || extension == ".unity" ||
                   extension == ".asset" || extension == ".controller" || extension == ".anim" ||
                   extension == ".overrideController" || extension == ".mask" || extension == ".mat" ||
                   extension == ".playable" || extension == ".preset";
        }
    }
}
