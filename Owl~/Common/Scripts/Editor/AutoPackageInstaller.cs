using System;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using System.Collections.Generic;

namespace Owl.Common
{
    [InitializeOnLoad]
    public static class AutoPackageInstaller
    {
        private static AddRequest addRequest;
        private static RemoveRequest removeRequest;
        private static AddAndRemoveRequest removeAllRequest;
        private static string removingPackage;
        private static readonly Queue<PendingInstall> pendingInstalls = new Queue<PendingInstall>();
        private static readonly HashSet<string> requestedPackages = new HashSet<string>();
        private static PendingInstall currentInstall;

        private sealed class PendingInstall
        {
            public string PackageName;
            public string DefineSymbol;
        }
        
        [Obsolete("Obsolete")]
        static AutoPackageInstaller()
        {
            // Run after editor finishes loading
            EditorApplication.delayCall += RunOnce;
        }

        [Obsolete("Obsolete")]
        private static void RunOnce()
        {
            // Check if we already ran before
            if (EditorPrefs.GetBool("AutoPackageInstaller_Ran", false))
                return;

            // Mark as run
            EditorPrefs.SetBool("AutoPackageInstaller_Ran", true);

            // Install packages
#if UNITY_IOS
            Install("com.unity.ads.ios-support", "PACKAGE_IOS_ADS_SUPPORT");
#endif
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Install a package if missing and add its define symbol
        /// </summary>
        [Obsolete("Obsolete")]
        public static void Install(string packageName, string defineSymbol = null)
        {
            if (IsInstalled(packageName))
            {
                if (!string.IsNullOrEmpty(defineSymbol))
                    DefineSymbolUtility.AddScriptingDefineSymbol(defineSymbol);
                return;
            }

            if (!requestedPackages.Add(packageName))
                return;

            pendingInstalls.Enqueue(new PendingInstall { PackageName = packageName, DefineSymbol = defineSymbol });
            StartNextInstall();
        }

        private static void StartNextInstall()
        {
            if (addRequest != null || pendingInstalls.Count == 0)
                return;

            currentInstall = pendingInstalls.Dequeue();
            CustomDebug.Log($"Installing package: {currentInstall.PackageName}...");
            addRequest = Client.Add(currentInstall.PackageName);
            EditorApplication.update += ProgressAdd;
        }

        [Obsolete("Obsolete")]
        private static void ProgressAdd()
        {
            if (!addRequest.IsCompleted) return;

            EditorApplication.update -= ProgressAdd;

            if (addRequest.Status == StatusCode.Success)
            {
                CustomDebug.Log($"{currentInstall.PackageName} installed successfully!");
                if (!string.IsNullOrEmpty(currentInstall.DefineSymbol))
                    DefineSymbolUtility.AddScriptingDefineSymbol(currentInstall.DefineSymbol);
            }
            else
            {
                CustomDebug.LogError($"Failed to install {currentInstall.PackageName}: {addRequest.Error.message}");
            }

            requestedPackages.Remove(currentInstall.PackageName);
            addRequest = null;
            currentInstall = null;
            StartNextInstall();
        }

        /// <summary>
        /// Remove a package if installed and remove its define symbol
        /// </summary>
        public static bool Remove(string packageName)
        {
            if (!IsInstalled(packageName))
            {
                CustomDebug.LogWarning($"Package is not installed: {packageName}");
                return false;
            }

            if (removeRequest != null || removeAllRequest != null)
            {
                CustomDebug.LogWarning($"Wait for {removingPackage} to finish uninstalling.");
                return false;
            }

            CustomDebug.Log($"Removing package: {packageName}...");
            removingPackage = packageName;
            removeRequest = Client.Remove(packageName);
            EditorApplication.update += ProgressRemove;
            return true;
        }

        public static bool RemoveAll(params string[] packageNames)
        {
            if (removeRequest != null || removeAllRequest != null || addRequest != null || pendingInstalls.Count != 0)
            {
                CustomDebug.LogWarning("Wait for the current package operation to finish before uninstalling SDKs.");
                return false;
            }

            var installed = new List<string>();
            foreach (var packageName in packageNames)
            {
                if (IsInstalled(packageName))
                    installed.Add(packageName);
            }

            if (installed.Count == 0)
            {
                CustomDebug.Log("No GameAnalytics, AppLovin MAX, Google Mobile Ads, or In-App Purchasing packages are installed.");
                return true;
            }

            CustomDebug.Log($"Removing packages: {string.Join(", ", installed)}...");
            removeAllRequest = Client.AddAndRemove(new string[0], installed.ToArray());
            EditorApplication.update += ProgressRemoveAll;
            return true;
        }

        private static void ProgressRemoveAll()
        {
            if (!removeAllRequest.IsCompleted) return;

            EditorApplication.update -= ProgressRemoveAll;
            if (removeAllRequest.Status == StatusCode.Success)
                CustomDebug.Log("Selected Owl SDK packages removed successfully.");
            else
                CustomDebug.LogError($"Failed to remove Owl SDK packages: {removeAllRequest.Error.message}");

            removeAllRequest = null;
        }

        private static void ProgressRemove()
        {
            if (!removeRequest.IsCompleted) return;

            EditorApplication.update -= ProgressRemove;

            if (removeRequest.Status == StatusCode.Success)
            {
                CustomDebug.Log($"{removingPackage} removed successfully!");
            }
            else
            {
                CustomDebug.LogError($"Failed to remove {removingPackage}: {removeRequest.Error.message}");
            }

            removeRequest = null;
            removingPackage = null;
        }

        /// <summary>
        /// Check if a package is installed
        /// </summary>
        private static bool IsInstalled(string packageName)
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForPackageName(packageName);
            return package != null && package.isDirectDependency;
        }
    }
}
