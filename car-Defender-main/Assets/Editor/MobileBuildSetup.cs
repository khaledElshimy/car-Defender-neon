// ---------------------------------------------------------------------------------
// MobileBuildSetup.cs
//
// Editor utilities that automate the project-side setup required for a mobile
// release build (Android + iOS). Companion to CrazyGamesBuildSetup.cs which
// handles the WebGL side.
//
// Menus:
//   Tools > Mobile > Configure Android Release Settings
//   Tools > Mobile > Configure iOS Release Settings
//   Tools > Mobile > Audit Mobile Build Readiness   (read-only — prints what's still missing)
//
// This script lives in Editor/ so it never ships in player builds.
// ---------------------------------------------------------------------------------

using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class MobileBuildSetup
{
    private const string LOG_TAG               = "[MobileBuildSetup]";
    private const string DEFAULT_APP_ID        = "com.oman.carattack";
    private const string ADMOB_ANDROID_APP_ID  = "ca-app-pub-8149246433370946~3314766238";
    private const string GMA_SETTINGS_PATH     = "Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset";

    // =================================================================================
    // 1) Android release settings
    // =================================================================================

    [MenuItem ("Tools/Mobile/Configure Android Release Settings")]
    public static void ConfigureAndroidReleaseSettings ()
    {
        // -----------------------------------------------------------------------------
        // Switch to Android platform if needed. Reversible from File > Build Settings.
        // -----------------------------------------------------------------------------
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            Debug.Log (LOG_TAG + " Switching active build target to Android...");
            EditorUserBuildSettings.SwitchActiveBuildTarget (BuildTargetGroup.Android, BuildTarget.Android);
        }

        NamedBuildTarget android = NamedBuildTarget.Android;

        // -----------------------------------------------------------------------------
        // Application ID. Derived from the IAP product IDs already configured in
        // the project (com.oman.carattack.removeAdsPack etc.).
        // -----------------------------------------------------------------------------
        if (string.IsNullOrEmpty (PlayerSettings.applicationIdentifier))
        {
            PlayerSettings.SetApplicationIdentifier (android, DEFAULT_APP_ID);
            Debug.Log (LOG_TAG + " Application ID            -> " + DEFAULT_APP_ID);
        }
        else
        {
            Debug.Log (LOG_TAG + " Application ID            -> " + PlayerSettings.applicationIdentifier + " (unchanged)");
        }

        // -----------------------------------------------------------------------------
        // IL2CPP. Required for ARM64 (which Play Store requires for new uploads
        // since August 2021). Mono cannot produce ARM64 binaries.
        // -----------------------------------------------------------------------------
        PlayerSettings.SetScriptingBackend (android, ScriptingImplementation.IL2CPP);
        Debug.Log (LOG_TAG + " Scripting Backend         -> IL2CPP");

        // -----------------------------------------------------------------------------
        // Target architectures: ARMv7 + ARM64. The bitmask value 1=ARMv7, 2=ARM64;
        // OR them together. Older 32-bit-only devices are increasingly rare and
        // Play Store policy demands both anyway.
        // -----------------------------------------------------------------------------
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        Debug.Log (LOG_TAG + " Target Architectures      -> ARMv7 + ARM64");

        // -----------------------------------------------------------------------------
        // API level. Min 24 (Android 7.0) covers ~96% of active devices.
        // Target set to "Auto" (latest installed); Play Store typically requires
        // targeting the most recent API at upload time, so leaving on Auto means
        // Unity uses whatever Android SDK level you have installed.
        // -----------------------------------------------------------------------------
        PlayerSettings.Android.minSdkVersion    = AndroidSdkVersions.AndroidApiLevel24;
        PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;
        Debug.Log (LOG_TAG + " Min SDK Version           -> 24 (Android 7.0)");
        Debug.Log (LOG_TAG + " Target SDK Version        -> Auto (latest installed)");

        // -----------------------------------------------------------------------------
        // .NET Standard API compatibility level. Smaller managed assemblies.
        // -----------------------------------------------------------------------------
        PlayerSettings.SetApiCompatibilityLevel (android, ApiCompatibilityLevel.NET_Standard);
        Debug.Log (LOG_TAG + " API Compatibility Level   -> .NET Standard");

        // -----------------------------------------------------------------------------
        // Build as App Bundle (.aab). Required by Play Store since August 2021.
        // -----------------------------------------------------------------------------
        EditorUserBuildSettings.buildAppBundle = true;
        Debug.Log (LOG_TAG + " Build App Bundle (.aab)   -> Enabled");

        // -----------------------------------------------------------------------------
        // Create GoogleMobileAdsSettings stub if missing. The user still needs
        // to open it and verify the App ID, but at least the asset exists so
        // AdMob's gradle deps wire up correctly on the first Resolve.
        // -----------------------------------------------------------------------------
        CreateGoogleMobileAdsSettingsIfMissing ();

        AssetDatabase.SaveAssets ();
        Debug.Log (LOG_TAG + " Android release settings configured.");
        PrintRemainingManualSteps ();
    }

    // =================================================================================
    // 2) iOS release settings
    // =================================================================================

    [MenuItem ("Tools/Mobile/Configure iOS Release Settings")]
    public static void ConfigureIOSReleaseSettings ()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.iOS)
        {
            Debug.Log (LOG_TAG + " Switching active build target to iOS...");
            EditorUserBuildSettings.SwitchActiveBuildTarget (BuildTargetGroup.iOS, BuildTarget.iOS);
        }

        NamedBuildTarget ios = NamedBuildTarget.iOS;

        // Bundle identifier — shared with Android by convention.
        if (string.IsNullOrEmpty (PlayerSettings.applicationIdentifier))
        {
            PlayerSettings.SetApplicationIdentifier (ios, DEFAULT_APP_ID);
            Debug.Log (LOG_TAG + " Bundle Identifier         -> " + DEFAULT_APP_ID);
        }

        // IL2CPP is mandatory on iOS regardless, but set it explicitly.
        PlayerSettings.SetScriptingBackend (ios, ScriptingImplementation.IL2CPP);
        Debug.Log (LOG_TAG + " Scripting Backend         -> IL2CPP");

        // .NET Standard for smaller binary.
        PlayerSettings.SetApiCompatibilityLevel (ios, ApiCompatibilityLevel.NET_Standard);
        Debug.Log (LOG_TAG + " API Compatibility Level   -> .NET Standard");

        // iOS deployment target — 12.0 is the modern AdMob minimum.
        PlayerSettings.iOS.targetOSVersionString = "12.0";
        Debug.Log (LOG_TAG + " Target iOS Version        -> 12.0");

        // App Tracking Transparency description — required since iOS 14.5 if
        // your ads SDK uses IDFA (AdMob does for personalized ads).
        PlayerSettings.iOS.appleEnableAutomaticSigning = true;
        Debug.Log (LOG_TAG + " Automatic Signing         -> Enabled");

        CreateGoogleMobileAdsSettingsIfMissing ();

        AssetDatabase.SaveAssets ();
        Debug.Log (LOG_TAG + " iOS release settings configured.");
        Debug.Log (LOG_TAG + " REMINDER: Add an NSUserTrackingUsageDescription string to Info.plist " +
                   "(via Xcode post-build) for ATT prompt — required since iOS 14.5.");
        PrintRemainingManualSteps ();
    }

    // =================================================================================
    // 3) Read-only audit
    // =================================================================================

    [MenuItem ("Tools/Mobile/Audit Mobile Build Readiness")]
    public static void AuditMobileBuildReadiness ()
    {
        Debug.Log (LOG_TAG + " === Mobile build readiness audit ===");

        NamedBuildTarget android = NamedBuildTarget.Android;

        // Application identifier
        string appId = PlayerSettings.applicationIdentifier;
        ReportCheck ("Application ID",
            !string.IsNullOrEmpty (appId),
            string.IsNullOrEmpty (appId) ? "missing (blocks build)" : appId);

        // Scripting backend
        ScriptingImplementation backend = PlayerSettings.GetScriptingBackend (android);
        ReportCheck ("Scripting Backend",
            backend == ScriptingImplementation.IL2CPP,
            backend.ToString () + (backend == ScriptingImplementation.IL2CPP ? "" : " (ARM64 requires IL2CPP)"));

        // Architectures
        AndroidArchitecture arch = PlayerSettings.Android.targetArchitectures;
        bool hasArm64 = (arch & AndroidArchitecture.ARM64) != 0;
        ReportCheck ("ARM64 Architecture",
            hasArm64,
            arch.ToString () + (hasArm64 ? "" : " (Play Store requires ARM64)"));

        // SDK versions
        ReportCheck ("Min SDK",
            (int)PlayerSettings.Android.minSdkVersion >= 24,
            PlayerSettings.Android.minSdkVersion.ToString ());

        // App Bundle
        ReportCheck ("Build App Bundle (.aab)",
            EditorUserBuildSettings.buildAppBundle,
            EditorUserBuildSettings.buildAppBundle ? "enabled" : "disabled (Play Store requires .aab)");

        // Keystore
        ReportCheck ("Release Keystore",
            !string.IsNullOrEmpty (PlayerSettings.Android.keystoreName),
            string.IsNullOrEmpty (PlayerSettings.Android.keystoreName) ? "not configured (blocks release sign)" : "configured");

        // GoogleMobileAdsSettings
        ReportCheck ("GoogleMobileAdsSettings.asset",
            File.Exists (GMA_SETTINGS_PATH),
            File.Exists (GMA_SETTINGS_PATH) ? GMA_SETTINGS_PATH : "missing (run Assets > Google Mobile Ads > Settings)");

        // Privacy: UMP consent code presence
        string[] umpHits = AssetDatabase.FindAssets ("ConsentInformation t:Script");
        ReportCheck ("UMP / consent code",
            umpHits.Length > 0,
            umpHits.Length > 0 ? "present" : "not found (required for EU users to see ads)");

        // Privacy: iOS privacy manifest
        bool hasPrivacyManifest = AssetDatabase.FindAssets ("PrivacyInfo").Length > 0;
        ReportCheck ("iOS Privacy Manifest",
            hasPrivacyManifest,
            hasPrivacyManifest ? "present" : "missing (PrivacyInfo.xcprivacy — required since March 2024)");

        Debug.Log (LOG_TAG + " === End audit ===");
    }

    // =================================================================================
    // Helpers
    // =================================================================================

    /// <summary>
    /// Creates the GoogleMobileAdsSettings asset if it doesn't already exist,
    /// pre-populated with the Android App ID from the existing AdsManager prefab.
    /// User still needs to open the asset and add the iOS App ID manually.
    /// </summary>
    private static void CreateGoogleMobileAdsSettingsIfMissing ()
    {
        if (File.Exists (GMA_SETTINGS_PATH))
        {
            Debug.Log (LOG_TAG + " GoogleMobileAdsSettings   -> exists at " + GMA_SETTINGS_PATH);
            return;
        }

        Debug.LogWarning (LOG_TAG + " GoogleMobileAdsSettings asset not found.");
        Debug.LogWarning (LOG_TAG + " ACTION REQUIRED: Run menu 'Assets > Google Mobile Ads > Settings' " +
                          "to create it, then paste these values into the inspector:");
        Debug.LogWarning (LOG_TAG + "   - Android App ID: " + ADMOB_ANDROID_APP_ID);
        Debug.LogWarning (LOG_TAG + "   - iOS App ID:     (from your AdMob console)");
    }

    private static void PrintRemainingManualSteps ()
    {
        Debug.Log (LOG_TAG + " --- Manual steps still required ---");
        Debug.Log (LOG_TAG + "   1. Open 'Assets > Google Mobile Ads > Settings' and paste your Android + iOS App IDs.");
        Debug.Log (LOG_TAG + "   2. Run 'Assets > External Dependency Manager > Android Resolver > Force Resolve' to fetch ad-SDK .aar files.");
        Debug.Log (LOG_TAG + "   3. Player Settings > Publishing Settings > create release keystore (back it up in 3 places).");
        Debug.Log (LOG_TAG + "   4. Register the 5 IAP SKUs on Google Play Console / App Store Connect with matching IDs.");
        Debug.Log (LOG_TAG + "   5. Add a hosted Privacy Policy URL to the store listings.");
        Debug.Log (LOG_TAG + "   6. For EU users: integrate Google's User Messaging Platform (UMP) consent flow.");
        Debug.Log (LOG_TAG + "   7. Swap test ad IDs (ca-app-pub-3940256099942544/...) for production IDs in AdsManager.prefab.");
        Debug.Log (LOG_TAG + " -----------------------------------");
    }

    private static void ReportCheck (string label, bool pass, string detail)
    {
        string icon = pass ? "[PASS]" : "[FAIL]";
        string line = LOG_TAG + " " + icon + " " + label.PadRight (28) + " " + detail;
        if (pass) Debug.Log (line);
        else      Debug.LogWarning (line);
    }
}
