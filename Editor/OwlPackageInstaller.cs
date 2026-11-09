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
            EditorApplication.delayCall += RememberInstalledGuids;
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
                if (destinationExists) RememberInstalledGuids();
                guidRemaps = BuildGuidRemaps(source, destination, sourceMeta, destinationMeta);
                var saved = LoadGuidSnapshot();
                if (!File.Exists(destinationMeta) && saved.TryGetValue(".meta", out var rootMeta))
                    EnsureGuidAvailable(ReadGuidFromText(rootMeta), "Assets/Owl");
            }
            catch (InvalidOperationException exception)
            {
                Debug.LogError($"Owl update was cancelled: {exception.Message}");
                return;
            }
            CopyDirectory(source, destination, overwrite: destinationExists, guidRemaps: guidRemaps);

            if (!File.Exists(destinationMeta))
            {
                var saved = LoadGuidSnapshot();
                if (saved.TryGetValue(".meta", out var rootMeta))
                {
                    EnsureGuidAvailable(ReadGuidFromText(rootMeta), "Assets/Owl");
                    File.WriteAllText(destinationMeta, rootMeta);
                }
                else if (File.Exists(sourceMeta))
                    CopyFile(sourceMeta, destinationMeta, overwrite: false, guidRemaps);
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            RememberInstalledGuids();
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
            var saved = LoadGuidSnapshot();
            var metaPaths = new List<string>(Directory.GetFiles(source, "*.meta", SearchOption.AllDirectories));
            if (File.Exists(sourceMeta)) metaPaths.Add(sourceMeta);

            foreach (var metaPath in metaPaths)
            {
                var originalGuid = ReadMetaGuid(metaPath);
                if (originalGuid == null) continue;
                var targetMeta = metaPath == sourceMeta
                    ? destinationMeta
                    : Path.Combine(destination, metaPath.Substring(source.Length + 1));
                var snapshotKey = metaPath == sourceMeta ? ".meta" :
                    metaPath.Substring(source.Length + 1).Replace('\\', '/');
                var installedGuid = File.Exists(targetMeta) ? ReadMetaGuid(targetMeta) :
                    saved.TryGetValue(snapshotKey, out var oldMeta) ? ReadGuidFromText(oldMeta) : null;
                if (File.Exists(targetMeta) && installedGuid == null)
                    throw new InvalidOperationException($"Cannot read the GUID in {targetMeta}. No files were changed.");

                var targetGuid = installedGuid ?? originalGuid;
                if (installedGuid != null && !File.Exists(targetMeta))
                {
                    var relativeAsset = snapshotKey.Substring(0, snapshotKey.Length - ".meta".Length);
                    EnsureGuidAvailable(installedGuid, "Assets/Owl/" + relativeAsset);
                }
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
            return ReadGuidFromText(File.ReadAllText(path));
        }

        private static string ReadGuidFromText(string text)
        {
            var match = MetaGuidPattern.Match(text);
            return match.Success ? match.Groups[1].Value : null;
        }

        private static void EnsureGuidAvailable(string guid, string expectedPath)
        {
            if (string.IsNullOrEmpty(guid))
                throw new InvalidOperationException("A saved Owl GUID is invalid. Restore the original .meta files from backup.");
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path) && !string.Equals(path, expectedPath, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException($"Saved GUID {guid} is now used by {path}. Restore the original .meta files or resolve the conflict before reinstalling.");
        }

        [Serializable]
        private sealed class GuidSnapshot
        {
            public List<GuidSnapshotEntry> entries = new List<GuidSnapshotEntry>();
        }

        [Serializable]
        private sealed class GuidSnapshotEntry
        {
            public string path;
            public string meta;
        }

        private static string SnapshotPath => Path.Combine(Directory.GetParent(Application.dataPath).FullName,
            "ProjectSettings", "OwlInstalledAssetGuids.json");

        private static Dictionary<string, string> LoadGuidSnapshot()
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            if (!File.Exists(SnapshotPath)) return result;
            var snapshot = JsonUtility.FromJson<GuidSnapshot>(File.ReadAllText(SnapshotPath));
            if (snapshot?.entries == null) throw new InvalidOperationException("The saved Owl GUID snapshot is invalid.");
            foreach (var entry in snapshot.entries)
                if (entry != null && !string.IsNullOrEmpty(entry.path) && entry.meta != null)
                    result[entry.path] = entry.meta;
            return result;
        }

        internal static void RememberInstalledGuids()
        {
            var destination = Path.Combine(Application.dataPath, DestinationFolderName);
            if (!Directory.Exists(destination)) return;
            try
            {
                var saved = LoadGuidSnapshot();
                foreach (var meta in Directory.GetFiles(destination, "*.meta", SearchOption.AllDirectories))
                {
                    var text = File.ReadAllText(meta);
                    if (ReadGuidFromText(text) != null)
                        saved[meta.Substring(destination.Length + 1).Replace('\\', '/')] = text;
                }
                if (File.Exists(destination + ".meta"))
                    saved[".meta"] = File.ReadAllText(destination + ".meta");
                var snapshot = new GuidSnapshot();
                foreach (var entry in saved)
                    snapshot.entries.Add(new GuidSnapshotEntry { path = entry.Key, meta = entry.Value });
                Directory.CreateDirectory(Path.GetDirectoryName(SnapshotPath));
                File.WriteAllText(SnapshotPath, JsonUtility.ToJson(snapshot, true));
            }
            catch (Exception exception)
            {
                Debug.LogError("Owl could not save its installed asset GUIDs: " + exception.Message);
            }
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
    internal sealed class OwlAssetDeletionWatcher : AssetModificationProcessor
    {
        private static AssetDeleteResult OnWillDeleteAsset(string assetPath, RemoveAssetOptions options)
        {
            if (assetPath == "Assets/Owl")
                OwlPackageInstaller.RememberInstalledGuids();
            return AssetDeleteResult.DidNotDelete;
        }
    }

}
