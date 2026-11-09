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

        [MenuItem("Owl/Show Installed Assets", false, 2)]
        private static void ShowInstalledAssets()
        {
            var folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>("Assets/Owl");
            if (folder == null)
            {
                Debug.LogWarning("Owl assets are not installed in Assets/Owl yet. Reimport the package or run Owl > Install or Update Assets.");
                return;
            }

            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
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

            Dictionary<string, string> guidRemaps;
            try
            {
                guidRemaps = BuildGuidRemaps(source, destination, sourceMeta, destinationMeta);
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError($"Owl update was cancelled: {exception.Message}");
                return;
            }
            CopyDirectory(source, destination, overwrite: destinationExists, guidRemaps: guidRemaps);

            if (File.Exists(sourceMeta) && !File.Exists(destinationMeta))
                CopyFile(sourceMeta, destinationMeta, overwrite: false, guidRemaps);

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log($"Owl Publishing Kit assets installed to Assets/{DestinationFolderName}.");
            var installedFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>($"Assets/{DestinationFolderName}");
            if (installedFolder != null)
            {
                Selection.activeObject = installedFolder;
                EditorGUIUtility.PingObject(installedFolder);
            }
        }

        private static readonly Regex MetaGuidPattern = new Regex(@"(?m)^guid:\s*([a-fA-F0-9]{32})\s*$");
        private static readonly Regex GuidTokenPattern = new Regex(@"(?<![a-fA-F0-9])[a-fA-F0-9]{32}(?![a-fA-F0-9])");

        private static Dictionary<string, string> BuildGuidRemaps(string source, string destination, string sourceMeta, string destinationMeta)
        {
            var remaps = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var assigned = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var metaPaths = new List<string>(Directory.GetFiles(source, "*.meta", SearchOption.AllDirectories));
            if (File.Exists(sourceMeta)) metaPaths.Add(sourceMeta);

            foreach (var metaPath in metaPaths)
            {
                var originalGuid = ReadMetaGuid(metaPath);
                if (originalGuid == null) continue;
                var targetMeta = metaPath == sourceMeta
                    ? destinationMeta
                    : Path.Combine(destination, metaPath.Substring(source.Length + 1));
                var installedGuid = File.Exists(targetMeta) ? ReadMetaGuid(targetMeta) : null;
                if (File.Exists(targetMeta) && installedGuid == null)
                    throw new InvalidOperationException($"Cannot read the GUID in {targetMeta}. No files were changed.");

                var targetGuid = installedGuid ?? originalGuid;
                if (installedGuid == null && !string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(originalGuid)))
                    targetGuid = Guid.NewGuid().ToString("N");

                if (assigned.TryGetValue(originalGuid, out var previous) &&
                    !string.Equals(previous, targetGuid, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"The package GUID {originalGuid} maps to multiple installed assets. No files were changed.");
                assigned[originalGuid] = targetGuid;
                if (!string.Equals(originalGuid, targetGuid, StringComparison.OrdinalIgnoreCase))
                    remaps[originalGuid] = targetGuid;
            }
            return remaps;
        }

        private static string ReadMetaGuid(string path)
        {
            var match = MetaGuidPattern.Match(File.ReadAllText(path));
            return match.Success ? match.Groups[1].Value : null;
        }

        private static void CopyDirectory(string source, string destination, bool overwrite, Dictionary<string, string> guidRemaps)
        {
            Directory.CreateDirectory(destination);

            foreach (var file in Directory.GetFiles(source))
            {
                var target = Path.Combine(destination, Path.GetFileName(file));
                // Keep installed metadata: scenes and other assets refer to its GUID.
                if (file.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) && File.Exists(target))
                    continue;
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
            text = GuidTokenPattern.Replace(text, match =>
                guidRemaps.TryGetValue(match.Value, out var replacement) ? replacement : match.Value);
            File.WriteAllText(destination, text);
        }

        private static bool CanContainUnityReferences(string path)
        {
            var extension = Path.GetExtension(path);
            return extension == ".meta" || extension == ".prefab" || extension == ".unity" ||
                   extension == ".asset" || extension == ".controller" || extension == ".anim" ||
                   extension == ".overrideController" || extension == ".mask" || extension == ".mat" ||
                   extension == ".playable" || extension == ".preset" || extension == ".asmdef" ||
                   extension == ".asmref";
        }
    }
}
