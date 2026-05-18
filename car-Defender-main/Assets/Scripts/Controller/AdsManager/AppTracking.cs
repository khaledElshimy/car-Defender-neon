// ---------------------------------------------------------------------------------
// AppTracking.cs
//
// C# wrapper around the AppTrackingTransparency.mm native bridge. Use this on
// iOS 14.5+ to request user permission to track via IDFA — required before
// AdMob can serve personalized ads (which earn ~2-3x more than non-personalized).
//
// On Android and other platforms the calls no-op and return Status.Unavailable.
// Safe to call from cross-platform code.
//
// Usage:
//   AppTracking.RequestAuthorization(status => {
//       Debug.Log("ATT status: " + status);
//       MobileAds.Initialize(...);
//   });
//
// The Info.plist key NSUserTrackingUsageDescription is injected automatically
// by iOSPostBuildProcessor — without it the prompt is silently denied.
// ---------------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;
using AOT;
using UnityEngine;

public static class AppTracking
{
    public enum Status
    {
        NotDetermined = 0,
        Restricted    = 1,
        Denied        = 2,
        Authorized    = 3,
        Unavailable   = 4
    }

    private const string LOG_TAG = "[ATT]";

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport ("__Internal")]
    private static extern int _ATT_GetStatus ();

    [DllImport ("__Internal")]
    private static extern void _ATT_Request (Action<int> callback);
#endif

    /// <summary>
    /// Returns the current ATT status without prompting the user. Safe to call
    /// on any platform — returns Unavailable when ATT isn't applicable.
    /// </summary>
    public static Status GetStatus ()
    {
#if UNITY_IOS && !UNITY_EDITOR
        return (Status) _ATT_GetStatus ();
#else
        return Status.Unavailable;
#endif
    }

    /// <summary>
    /// Show the ATT prompt if the user hasn't responded to it yet, then invoke
    /// onResult with the final status. If already responded, onResult fires
    /// immediately with the stored status. Always safe to call — invokes the
    /// callback even on platforms without ATT.
    /// </summary>
    public static void RequestAuthorization (Action<Status> onResult)
    {
        if (onResult == null) onResult = _ => { };

#if UNITY_IOS && !UNITY_EDITOR
        _pendingCallback = onResult;
        _ATT_Request (NativeCallbackTrampoline);
#else
        // Editor / Android / etc: no prompt to show; report Unavailable.
        onResult (Status.Unavailable);
#endif
    }

#if UNITY_IOS && !UNITY_EDITOR
    // The native side calls back with a function pointer. AOT-compiled targets
    // (IL2CPP/iOS) require the callback target to be a static method tagged with
    // [MonoPInvokeCallback], so we trampoline through a static field that holds
    // the user's actual delegate.
    private static Action<Status> _pendingCallback;

    [MonoPInvokeCallback (typeof (Action<int>))]
    private static void NativeCallbackTrampoline (int statusInt)
    {
        Action<Status> cb = _pendingCallback;
        _pendingCallback = null;
        if (cb != null)
        {
            try { cb ((Status) statusInt); }
            catch (Exception e) { Debug.LogError (LOG_TAG + " Callback threw: " + e); }
        }
    }
#endif
}
