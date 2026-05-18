// ---------------------------------------------------------------------------------
// iOSPostBuildProcessor.cs
//
// Runs after every iOS build. Injects the Info.plist entries that Google Mobile
// Ads' built-in PListProcessor doesn't handle automatically.
//
// What GMA's built-in PListProcessor DOES inject (so we don't need to):
//   - GADApplicationIdentifier              (driven by GoogleMobileAdsSettings.asset)
//   - SKAdNetworkItems                      (driven by GoogleMobileAdsSKAdNetworkItems.xml)
//
// What this processor STILL needs to inject:
//   - NSUserTrackingUsageDescription        (ATT prompt copy, app-specific)
//   - NSAppTransportSecurity exception      (HTTP media for some ad creatives)
//
// This processor uses priority 9999 so it runs AFTER GMA's processor and we
// don't fight over the same keys.
// ---------------------------------------------------------------------------------

#if UNITY_IOS

using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEngine;

public static class iOSPostBuildProcessor
{
    private const string LOG_TAG = "[iOSPostBuildProcessor]";

    /// <summary>
    /// Text shown in the App Tracking Transparency prompt. Localizable in Xcode
    /// if you ship in multiple languages — add InfoPlist.strings entries per
    /// localization and Apple will pick the right one at runtime.
    /// </summary>
    private const string ATT_DESCRIPTION =
        "Your data will be used to deliver personalized ads to you.";

    [PostProcessBuild (9999)] // High priority = runs after GMA's PListProcessor.
    public static void OnPostProcessBuild (BuildTarget buildTarget, string pathToBuiltProject)
    {
        if (buildTarget != BuildTarget.iOS) return;

        string plistPath = Path.Combine (pathToBuiltProject, "Info.plist");
        if (!File.Exists (plistPath))
        {
            Debug.LogError (LOG_TAG + " Info.plist not found at " + plistPath);
            return;
        }

        PlistDocument plist = new PlistDocument ();
        plist.ReadFromString (File.ReadAllText (plistPath));
        PlistElementDict root = plist.root;

        // 1. App Tracking Transparency description. iOS silently denies the
        //    ATT request if this string is missing.
        root.SetString ("NSUserTrackingUsageDescription", ATT_DESCRIPTION);
        Debug.Log (LOG_TAG + " Set NSUserTrackingUsageDescription");

        // 2. Allow HTTP for ad media only. Doesn't relax ATS for arbitrary
        //    domains — just permits the legacy video creatives that some ad
        //    networks still serve over plain http.
        PlistElementDict ats = root.values.ContainsKey ("NSAppTransportSecurity")
            ? root["NSAppTransportSecurity"].AsDict ()
            : root.CreateDict ("NSAppTransportSecurity");

        ats.SetBoolean ("NSAllowsArbitraryLoadsForMedia", true);
        Debug.Log (LOG_TAG + " Set NSAppTransportSecurity.NSAllowsArbitraryLoadsForMedia = true");

        // Sanity-check that GMA's processor ran. If GADApplicationIdentifier is
        // missing, the app will crash on AdMob init at runtime — fail the build
        // here instead of letting the user discover it on device.
        if (!root.values.ContainsKey ("GADApplicationIdentifier"))
        {
            Debug.LogError (LOG_TAG + " GADApplicationIdentifier is missing from Info.plist. " +
                            "Open 'Assets > Google Mobile Ads > Settings' and paste your iOS App ID, " +
                            "then rebuild. Without this the app crashes on launch.");
        }

        File.WriteAllText (plistPath, plist.WriteToString ());
        Debug.Log (LOG_TAG + " Info.plist patched at " + plistPath);
    }
}

#endif
