
using System;
using System.IO;
using System.Linq;
using Owl;
using Owl.Common;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[DefaultExecutionOrder(-10000000)]
public class Menu : EditorWindow
{
    [MenuItem("Owl/Settings", false, 0)]
    private static void ShowWindow()
    {
        var window = (OwlSettingWindow)GetWindow(typeof(OwlSettingWindow));
        window.titleContent = new GUIContent("Owl Settings");
        window.minSize = new Vector2(620, 520);
        window.Show();
    }

    [MenuItem("Owl/SDKs/Download/Airbridge", false, 20)]
    private static void DownloadAirbridge()
    {
        OpenSdkLink(links => links.AirbridgePackageDownloadableLink);
    }

    [MenuItem("Owl/SDKs/Download/Solar Engine", false, 21)]
    private static void DownloadSolarEngine()
    {
        OpenSdkLink(links => links.SolarEnginePackageDownloadableLink);
    }

    [MenuItem("Owl/SDKs/Download/Firebase", false, 22)]
    private static void DownloadFirebase()
    {
        OpenSdkLink(links => links.FirebasePackageDownloadableLink);
    }

    [MenuItem("Owl/SDKs/Download/Meta (Facebook)", false, 23)]
    private static void DownloadFacebook()
    {
        Application.OpenURL("https://developers.facebook.com/documentation/unity/downloads");
    }

    [MenuItem("Owl/SDKs/Install/GameAnalytics", false, 30)]
    [Obsolete("Obsolete")]
    private static void InstallGameAnalytics()
    {
        ScopedRegistryUtils.AddScopedRegistry("GameAnalytics Unity", "https://package.openupm.com",
            new[] { "com.gameanalytics.sdk" });
        ScopedRegistryUtils.AddScopedRegistry("External Dependency Manager", "https://package.openupm.com",
            new[] { "com.google.external-dependency-manager" });
        AutoPackageInstaller.Install("com.gameanalytics.sdk");
    }

    [MenuItem("Owl/SDKs/Install/In-App Purchasing", false, 31)]
    [Obsolete("Obsolete")]
    private static void InstallIap()
    {
        AutoPackageInstaller.Install("com.unity.purchasing");
    }

    [MenuItem("Owl/SDKs/Install/Google Mobile Ads", false, 32)]
    [Obsolete("Obsolete")]
    private static void InstallGoogleAds()
    {
        ScopedRegistryUtils.AddScopedRegistry("Admob Unity", "https://package.openupm.com",
            new[] { "com.google" });
        AutoPackageInstaller.Install("com.google.ads.mobile");
    }

    [MenuItem("Owl/SDKs/Install/AppLovin MAX", false, 33)]
    [Obsolete("Obsolete")]
    private static void InstallMax()
    {
        ScopedRegistryUtils.AddScopedRegistry("AppLovin MAX Unity", "https://unity.packages.applovin.com",
            new[] { "com.applovin.mediation.ads", "com.applovin.mediation.adapters", "com.applovin.mediation.dsp" });
        ScopedRegistryUtils.AddScopedRegistry("External Dependency Manager", "https://package.openupm.com",
            new[] { "com.google.external-dependency-manager" });
        AutoPackageInstaller.Install("com.applovin.mediation.ads");
    }

    [MenuItem("Owl/SDKs/Uninstall/GameAnalytics", false, 40)]
    [Obsolete("Obsolete")]
    private static void UninstallGameAnalytics()
    {
        UninstallSdk("com.gameanalytics.sdk", Constants.USE_GAMEANALYTICS, settings =>
        {
            if (settings.analyticsSdk == AnalyticsSDK.GameAnalytics)
                settings.analyticsSdk = AnalyticsSDK.WithoutData;
            else if (settings.analyticsSdk == AnalyticsSDK.FirebaseGameAnalytics)
                settings.analyticsSdk = AnalyticsSDK.Firebase;
        });
    }

    [MenuItem("Owl/SDKs/Uninstall/In-App Purchasing", false, 41)]
    [Obsolete("Obsolete")]
    private static void UninstallIap()
    {
        UninstallSdk("com.unity.purchasing", Constants.USE_IAP, settings => settings.useIap = false);
    }

    [MenuItem("Owl/SDKs/Uninstall/Google Mobile Ads", false, 42)]
    [Obsolete("Obsolete")]
    private static void UninstallGoogleAds()
    {
        UninstallSdk("com.google.ads.mobile", Constants.USE_ADMOB, settings =>
        {
            if (settings.mediation == Mediation.GoogleAds)
                settings.mediation = Mediation.NoAds;
        });
    }

    [MenuItem("Owl/SDKs/Uninstall/AppLovin MAX", false, 43)]
    [Obsolete("Obsolete")]
    private static void UninstallMax()
    {
        UninstallSdk("com.applovin.mediation.ads", Constants.USE_MAXADS, settings =>
        {
            if (settings.mediation == Mediation.Max)
                settings.mediation = Mediation.NoAds;
        });
    }

    [MenuItem("Owl/SDKs/Uninstall/All", false, 39)]
    [Obsolete("Obsolete")]
    private static void UninstallAllPackageSdks()
    {
        if (!AutoPackageInstaller.RemoveAll("com.gameanalytics.sdk", "com.applovin.mediation.ads",
                "com.google.ads.mobile", "com.unity.purchasing"))
            return;

        ResetPackageSdkSettings();
    }

    [Obsolete("Obsolete")]
    private static void ResetPackageSdkSettings()
    {
        UpdateOwlSettings(settings =>
        {
            if (settings.analyticsSdk == AnalyticsSDK.GameAnalytics)
                settings.analyticsSdk = AnalyticsSDK.WithoutData;
            else if (settings.analyticsSdk == AnalyticsSDK.FirebaseGameAnalytics)
                settings.analyticsSdk = AnalyticsSDK.Firebase;
            settings.useIap = false;
            if (settings.mediation == Mediation.GoogleAds || settings.mediation == Mediation.Max)
                settings.mediation = Mediation.NoAds;
        });
        DefineSymbolUtility.RemoveScriptingDefineSymbol(Constants.USE_GAMEANALYTICS);
        DefineSymbolUtility.RemoveScriptingDefineSymbol(Constants.USE_IAP);
        DefineSymbolUtility.RemoveScriptingDefineSymbol(Constants.USE_ADMOB);
        DefineSymbolUtility.RemoveScriptingDefineSymbol(Constants.USE_MAXADS);
    }

    [MenuItem("Owl/SDKs/Uninstall/External Dependency Manager", false, 44)]
    private static void UninstallExternalDependencyManager()
    {
        const string packageName = "com.google.external-dependency-manager";
        var usedByPackage = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
            .Any(package => package.name != packageName && package.dependencies != null &&
                            package.dependencies.Any(dependency => dependency.name == packageName));
        if (usedByPackage || Directory.Exists("Assets/Firebase"))
        {
            Debug.LogWarning("External Dependency Manager is still required by an installed SDK.");
            return;
        }

        AutoPackageInstaller.Remove(packageName);
    }

    [MenuItem("Owl/SDKs/Open Package Manager", false, 50)]
    private static void OpenPackageManager()
    {
        if (!EditorApplication.ExecuteMenuItem("Window/Package Manager"))
            Debug.LogError("Could not open Unity Package Manager.");
    }

    [Obsolete("Obsolete")]
    private static void UninstallSdk(string packageName, string defineSymbol, Action<OwlSettings> resetSettings)
    {
        var package = UnityEditor.PackageManager.PackageInfo.FindForPackageName(packageName);
        if (package != null && package.isDirectDependency && !AutoPackageInstaller.Remove(packageName))
            return;

        UpdateOwlSettings(resetSettings);
        DefineSymbolUtility.RemoveScriptingDefineSymbol(defineSymbol);
    }

    private static void UpdateOwlSettings(Action<OwlSettings> resetSettings)
    {
        var settings = Resources.Load<OwlSettings>("OwlSettings");
        if (settings == null) return;

        resetSettings(settings);
        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }

    private static void OpenSdkLink(Func<Links, string> getLink)
    {
        var links = Resources.Load<OwlSettings>("OwlSettings")?.SDKLinks;
        if (links == null)
        {
            Debug.LogError("Owl SDK links are missing from OwlSettings.");
            return;
        }

        var url = getLink(links);
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogError("The selected Owl SDK link is empty.");
            return;
        }

        Application.OpenURL(url);
    }

    [MenuItem("Owl/Create Owl", false, 1)]
    [Obsolete("Obsolete")]
    private static void InitAdManager()
    {
        if (Object.FindObjectOfType (typeof(OwlManager)) == null)
        {
            GameObject go = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath(Find("OwlManager.prefab", "Prefab"), typeof(GameObject))) as GameObject;
            go.name = "OwlManager";
            Selection.activeObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Created Owl Object");
        }
        else
        {
            Debug.LogWarning ("A OwlManager object already exists in this scene - you should never have more than one per scene!");

        }
        
    }


#if USE_GAMEANALYTICS
    [MenuItem("Owl/GameAnalytics/Create GameAnalytics", false, 2)]
    private static void InitGameAnalytics()
    {
        if (Object.FindObjectOfType (typeof(GameAnalyticsSDK.GameAnalytics)) == null)
        {
            var go = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath(Find("GameAnalytics.prefab", "Prefab"), typeof(GameObject))) as GameObject;
            go.name = "GameAnalytics";
            Selection.activeObject = go;
            Undo.RegisterCreatedObjectUndo(go, "Created GameAnalytics Object");
        }
        else
        {
            Debug.LogWarning ("A GameAnalytics object already exists in this scene - you should never have more than one per scene!");

        }
    }
    
    [MenuItem ("Owl/GameAnalytics/Settings", false, 2)]
    static void SelectGASettings ()
    {
        Selection.activeObject = GameAnalyticsSDK.GameAnalytics.SettingsGA;
    }
#endif
     
    [MenuItem("Owl/Monetization/AdUnits/Create MREC", false, 2)]
    private static void CreateMREC()
    {
        GameObject go = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath(Find("MRECPlacement.prefab", "Prefab"), typeof(GameObject))) as GameObject;
        go.name = "MRECPlacement";
        go.transform.parent = Selection.activeTransform;
        Selection.activeObject = go;
        Undo.RegisterCreatedObjectUndo(go, "Created MREC Object");
        
    }
    
 
        
    [MenuItem("Owl/ClearPlayerPrefs",false,10)]
    private static void ClearPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
    }
    
    private static  string Find(string file, string type)
    {

        var guids = AssetDatabase.FindAssets("t:" + type);
        foreach(var g in guids)
        {
            var p = AssetDatabase.GUIDToAssetPath(g);
            if(p.EndsWith(file))
            {
                return p;
            }
        }
        return "";
    }


}
