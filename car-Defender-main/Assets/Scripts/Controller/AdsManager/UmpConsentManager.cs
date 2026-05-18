// ---------------------------------------------------------------------------------
// UmpConsentManager.cs
//
// Thin wrapper around Google's User Messaging Platform (UMP) consent flow.
// Required by Google for AdMob to serve personalized ads to users in:
//   - European Economic Area (GDPR)
//   - California (CCPA / CPRA)
//   - Other privacy-regulated jurisdictions
//
// Without consent collection, AdMob refuses to serve personalized ads in these
// regions, which can cut ad revenue by ~30% for affected users.
//
// Usage:
//   In your bootstrap code (before MobileAds.Initialize), call:
//       UmpConsentManager.RequestConsent(onComplete: () => MobileAds.Initialize(...));
//
// UMP lives in the GoogleMobileAds.Ump.Api namespace (shipped inside the
// com.google.ads.mobile 11.x package — no extra install needed).
// ---------------------------------------------------------------------------------

using System;
using UnityEngine;
using GoogleMobileAds.Ump.Api;

public static class UmpConsentManager
{
    private const string LOG_TAG = "[UMP]";

    /// <summary>
    /// Starts the consent flow. Shows a consent form to the user if required by
    /// their jurisdiction, then invokes onComplete (success or otherwise — the
    /// caller should always proceed to MobileAds.Initialize afterwards; AdMob
    /// will fall back to non-personalized ads if consent is denied).
    /// </summary>
    /// <param name="onComplete">Always invoked when the flow finishes, regardless of outcome.</param>
    /// <param name="testGeographyEEA">If true, simulates an EEA user for testing the consent UI in Editor / non-EEA devices.</param>
    public static void RequestConsent (Action onComplete, bool testGeographyEEA = false)
    {
        if (onComplete == null) onComplete = () => { };

        ConsentRequestParameters request = new ConsentRequestParameters
        {
            TagForUnderAgeOfConsent = false,
        };

#if UNITY_EDITOR
        if (testGeographyEEA)
        {
            // Force the consent form to appear in Editor so designers can iterate
            // on the integration without flying to Europe.
            request.ConsentDebugSettings = new ConsentDebugSettings
            {
                DebugGeography = DebugGeography.EEA,
            };
        }
#endif

        Debug.Log (LOG_TAG + " Requesting consent info update...");
        ConsentInformation.Update (request, (FormError updateError) =>
        {
            if (updateError != null)
            {
                Debug.LogWarning (LOG_TAG + " ConsentInformation.Update failed: " + updateError.Message);
                onComplete ();
                return;
            }

            // If a form is available and required, present it now. Otherwise
            // we're done.
            ConsentForm.LoadAndShowConsentFormIfRequired ((FormError showError) =>
            {
                if (showError != null)
                {
                    Debug.LogWarning (LOG_TAG + " ConsentForm.LoadAndShowConsentFormIfRequired failed: " + showError.Message);
                }
                else
                {
                    Debug.Log (LOG_TAG + " Consent flow complete. CanRequestAds=" + ConsentInformation.CanRequestAds ());
                }
                onComplete ();
            });
        });
    }

    /// <summary>
    /// True when the user's consent state allows AdMob to request ads. Use this
    /// to gate MobileAds.Initialize: only call it once this returns true (or
    /// after the consent callback fires, whichever comes first).
    /// </summary>
    public static bool CanRequestAds => ConsentInformation.CanRequestAds ();

    /// <summary>
    /// Resets stored consent state. Useful for re-testing the consent UI without
    /// uninstalling the app. Not for shipping code paths.
    /// </summary>
    public static void ResetConsent ()
    {
        ConsentInformation.Reset ();
        Debug.Log (LOG_TAG + " Consent state reset.");
    }
}
