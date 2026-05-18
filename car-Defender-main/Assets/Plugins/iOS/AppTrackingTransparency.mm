// ---------------------------------------------------------------------------------
// AppTrackingTransparency.mm
//
// Tiny Objective-C++ bridge that wraps Apple's ATTrackingManager so Unity C#
// can request the App Tracking Transparency prompt on iOS 14.5+.
//
// Status codes (returned to C#):
//   0 - Not Determined (user hasn't seen the prompt yet)
//   1 - Restricted     (parental controls or MDM block tracking)
//   2 - Denied         (user tapped "Ask App Not to Track")
//   3 - Authorized     (user tapped "Allow")
//   4 - Unavailable    (iOS < 14.5; no prompt needed and IDFA is unrestricted)
//
// Pair with the Info.plist key NSUserTrackingUsageDescription (injected by
// iOSPostBuildProcessor) — without that string the system silently denies
// the request.
// ---------------------------------------------------------------------------------

#import <Foundation/Foundation.h>

#if defined(__IPHONE_14_0) && __IPHONE_OS_VERSION_MAX_ALLOWED >= __IPHONE_14_0
#import <AppTrackingTransparency/AppTrackingTransparency.h>
#endif

// C interface so Unity's DllImport can find these symbols.
extern "C" {

    /// Synchronously returns the current authorization status without prompting.
    int _ATT_GetStatus()
    {
#if defined(__IPHONE_14_0)
        if (@available(iOS 14, *))
        {
            return (int)[ATTrackingManager trackingAuthorizationStatus];
        }
#endif
        return 4; // Unavailable on this OS version
    }

    /// Asks the system to display the ATT prompt (if status == NotDetermined),
    /// then invokes the supplied callback with the resulting status code.
    /// Callbacks are dispatched on the main thread so Unity can update UI safely.
    void _ATT_Request(void (*callback)(int))
    {
#if defined(__IPHONE_14_0)
        if (@available(iOS 14, *))
        {
            [ATTrackingManager requestTrackingAuthorizationWithCompletionHandler:^(ATTrackingManagerAuthorizationStatus status) {
                dispatch_async(dispatch_get_main_queue(), ^{
                    if (callback != NULL) callback((int)status);
                });
            }];
            return;
        }
#endif
        // Pre-iOS-14: no prompt exists; report "Unavailable" so the C# side
        // proceeds straight to MobileAds.Initialize.
        if (callback != NULL) callback(4);
    }

} // extern "C"
