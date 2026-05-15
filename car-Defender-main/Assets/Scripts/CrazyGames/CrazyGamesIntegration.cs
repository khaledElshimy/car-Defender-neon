// ---------------------------------------------------------------------------------
// CrazyGamesIntegration.cs
//
// Drop-in MonoBehaviour wrapping the CrazyGames Unity SDK v5.x (module-based API).
//
// Usage:
//   1. Attach this component to a persistent GameObject in your bootstrap scene
//      (alongside your AdsManager / IapManager).
//   2. From your game code:
//        CrazyGamesIntegration.Instance.NotifyGameplayStart();
//        CrazyGamesIntegration.Instance.NotifyGameplayStop();
//        CrazyGamesIntegration.Instance.RequestMidrollAd();
//        CrazyGamesIntegration.Instance.RequestRewardedAd(onReward: GrantReward);
//
// The CrazyGames SDK is safe to call in the Editor and on localhost (with the
// provided WebGL template), so no #if UNITY_WEBGL gating is necessary. The SDK
// itself no-ops gracefully on non-WebGL builds.
// ---------------------------------------------------------------------------------

using System;
using CrazyGames;
using UnityEngine;

public class CrazyGamesIntegration : MonoBehaviour
{
    // -----------------------------------------------------------------------------
    // Singleton
    // -----------------------------------------------------------------------------

    public static CrazyGamesIntegration Instance { get; private set; }

    [Tooltip("If true, logs verbose CrazyGames lifecycle messages to the console.")]
    [SerializeField] private bool _verboseLogging = true;

    // Cached state we restore after an ad finishes, so we don't trample a game
    // that was already paused/muted for another reason (e.g. system menu open).
    private float _timeScaleBeforeAd        = 1f;
    private bool  _audioPausedBeforeAd      = false;
    private bool  _isShowingAd              = false;

    // Used by RequestRewardedAd to deliver the reward only on success.
    private Action _pendingRewardCallback;
    private Action _pendingAdCompleteCallback;
    private Action _pendingAdErrorCallback;

    // -----------------------------------------------------------------------------
    // Lifecycle
    // -----------------------------------------------------------------------------

    private void Awake ()
    {
        if (Instance != null && Instance != this)
        {
            Destroy (gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad (gameObject);

#if UNITY_WEBGL && !UNITY_EDITOR
        // CrazyGames optimization: let the browser drive the frame pacing.
        // -1 = uncapped from Unity's side; the browser's requestAnimationFrame
        // throttles us to the display refresh rate (and to 0 fps when the iframe
        // is hidden, complementing runInBackground=false).
        Application.targetFrameRate = -1;
#endif
    }

    private void Start ()
    {
        SafeInitialize ();
    }

    private void OnDestroy ()
    {
        if (Instance == this) Instance = null;
    }

    // -----------------------------------------------------------------------------
    // Initialization
    // -----------------------------------------------------------------------------

    /// <summary>
    /// Initializes the CrazyGames SDK if it's available on the current host.
    /// Init is async — the callback fires once the SDK is ready to take calls.
    /// </summary>
    private void SafeInitialize ()
    {
        if (!IsOnCrazyGames)
        {
            Log ("Not on a CrazyGames host (and no isCrazyGames=true iframe flag). Integration disabled.");
            return;
        }

        if (CrazySDK.IsInitialized)
        {
            Log ("CrazySDK already initialized.");
            return;
        }

        Log ("Initializing CrazySDK...");
        CrazySDK.Init (() => Log ("CrazySDK initialization complete."));
    }

    /// <summary>
    /// True when we should treat the game as running on a CrazyGames host.
    /// Combines two signals because CrazySDK.IsAvailable returns false when the
    /// game is loaded inside an iframe (partner sites). In that case the iframe
    /// URL is expected to include "isCrazyGames=true" as a query param, set when
    /// the build is submitted on the Developer Portal.
    /// </summary>
    private static bool IsOnCrazyGames
    {
        get
        {
            if (CrazySDK.IsAvailable) return true;

            string url = Application.absoluteURL;
            return !string.IsNullOrEmpty (url) && url.Contains ("isCrazyGames=true");
        }
    }

    // -----------------------------------------------------------------------------
    // Gameplay analytics events (required by CrazyGames QA)
    // -----------------------------------------------------------------------------

    /// <summary>
    /// Notify CrazyGames that an active gameplay session has begun. Call this when
    /// the player enters a level/run, NOT on menus, splash, or cutscenes.
    /// </summary>
    public void NotifyGameplayStart ()
    {
        if (!IsOnCrazyGames || !CrazySDK.IsInitialized) return;

        CrazySDK.Game.GameplayStart ();
        Log ("GameplayStart fired.");
    }

    /// <summary>
    /// Notify CrazyGames that the active gameplay session has ended (game over,
    /// player returns to main menu, etc.).
    /// </summary>
    public void NotifyGameplayStop ()
    {
        if (!IsOnCrazyGames || !CrazySDK.IsInitialized) return;

        CrazySDK.Game.GameplayStop ();
        Log ("GameplayStop fired.");
    }

    /// <summary>
    /// Notify CrazyGames of a positive moment for the player: an enemy killed,
    /// a level cleared, an upgrade purchased, etc. CrazyGames uses these signals
    /// to score engagement/retention. Call it generously — multiple times per
    /// session is expected.
    /// </summary>
    public void NotifyHappyTime ()
    {
        if (!IsOnCrazyGames || !CrazySDK.IsInitialized) return;

        CrazySDK.Game.HappyTime ();
        Log ("HappyTime fired.");
    }

    // -----------------------------------------------------------------------------
    // Banners
    //
    // CrazyGames banners are placed by attaching a CrazyBanner component to a
    // RectTransform in your UI (Assets/CrazySDK/Resources/CrazyBanner.prefab is
    // ready-made). The Banner module auto-registers these on scene load. The
    // wrappers below let runtime code force a refresh (e.g. after switching
    // scenes or showing/hiding a HUD section that contains a banner slot).
    // -----------------------------------------------------------------------------

    /// <summary>
    /// Forces a refresh of all CrazyBanner instances currently in the scene.
    /// Call this after instantiating UI that contains banners, or after toggling
    /// the visibility of banner containers.
    /// </summary>
    public void RefreshBanners ()
    {
        if (!IsOnCrazyGames || !CrazySDK.IsInitialized) return;

        CrazySDK.Banner.RefreshBanners ();
        Log ("Banners refreshed.");
    }

    // -----------------------------------------------------------------------------
    // Adblock detection
    // -----------------------------------------------------------------------------

    /// <summary>
    /// Asynchronously checks whether the player has an adblocker active. Useful
    /// for showing a "please disable adblocker" prompt before requesting a
    /// rewarded ad.
    /// </summary>
    /// <param name="callback">Invoked with true if an adblocker was detected.</param>
    public void CheckAdblock (Action<bool> callback)
    {
        if (callback == null) return;

        if (!IsOnCrazyGames || !CrazySDK.IsInitialized)
        {
            callback (false);
            return;
        }

        CrazySDK.Ad.HasAdblock (callback);
    }

    // -----------------------------------------------------------------------------
    // Ad requests
    // -----------------------------------------------------------------------------

    /// <summary>
    /// Trigger a midroll (interstitial) ad. The game is paused and muted while the
    /// ad is shown, and restored when the ad finishes or fails.
    /// </summary>
    /// <param name="onComplete">Optional callback when the ad finishes successfully.</param>
    /// <param name="onError">Optional callback when the ad fails or is unavailable.</param>
    public void RequestMidrollAd (Action onComplete = null, Action onError = null)
    {
        _pendingAdCompleteCallback = onComplete;
        _pendingAdErrorCallback    = onError;
        _pendingRewardCallback     = null;

        if (!IsOnCrazyGames || !CrazySDK.IsInitialized)
        {
            Log ("Midroll requested but CrazySDK unavailable; firing error callback.");
            HandleAdError ();
            return;
        }

        Log ("Requesting midroll ad...");
        CrazySDK.Ad.RequestAd (
            adType:    CrazyAdType.Midgame,
            adStarted: HandleAdStart,
            adError:   HandleAdError,
            adFinished: HandleAdFinish
        );
    }

    /// <summary>
    /// Trigger a rewarded ad. The reward callback is only invoked if the ad
    /// completes successfully — never on error.
    /// </summary>
    /// <param name="onReward">Callback fired when the ad completes and the player should receive the reward.</param>
    /// <param name="onError">Optional callback when the ad fails or is unavailable.</param>
    public void RequestRewardedAd (Action onReward, Action onError = null)
    {
        _pendingRewardCallback     = onReward;
        _pendingAdErrorCallback    = onError;
        _pendingAdCompleteCallback = null;

        if (!IsOnCrazyGames || !CrazySDK.IsInitialized)
        {
            Log ("Rewarded ad requested but CrazySDK unavailable; firing error callback.");
            HandleAdError ();
            return;
        }

        Log ("Requesting rewarded ad...");
        CrazySDK.Ad.RequestAd (
            adType:    CrazyAdType.Rewarded,
            adStarted: HandleAdStart,
            adError:   HandleAdError,
            adFinished: HandleAdFinish
        );
    }

    // -----------------------------------------------------------------------------
    // Ad lifecycle (pause / mute / resume) — wired to SDK callbacks
    // -----------------------------------------------------------------------------

    /// <summary>
    /// SDK callback: an ad has begun playing. Snapshots the current game state
    /// (time scale and audio listener pause flag) so we can faithfully restore it
    /// afterwards, then pauses time + mutes audio per CrazyGames QA requirements.
    /// </summary>
    private void HandleAdStart ()
    {
        if (_isShowingAd)
        {
            // Defensive: SDK shouldn't fire this twice, but avoid a double snapshot.
            return;
        }

        _isShowingAd            = true;
        _timeScaleBeforeAd      = Time.timeScale;
        _audioPausedBeforeAd    = AudioListener.pause;

        Time.timeScale       = 0f;
        AudioListener.pause  = true;

        Log ("Ad started: game paused and audio muted.");
    }

    /// <summary>
    /// SDK callback: the ad finished successfully. Restores time scale + audio,
    /// fires the reward callback if this was a rewarded ad, plus the generic
    /// onComplete callback if one was supplied.
    /// </summary>
    private void HandleAdFinish ()
    {
        RestoreGameState ();
        Log ("Ad finished successfully.");

        // Rewarded ad path: grant the reward
        if (_pendingRewardCallback != null)
        {
            Action cb = _pendingRewardCallback;
            _pendingRewardCallback = null;
            cb ();
        }

        // Midroll / generic completion path
        if (_pendingAdCompleteCallback != null)
        {
            Action cb = _pendingAdCompleteCallback;
            _pendingAdCompleteCallback = null;
            cb ();
        }

        _pendingAdErrorCallback = null;
    }

    /// <summary>
    /// SDK callback: the ad failed to load or play. Restores time scale + audio,
    /// fires the error callback (if any), and explicitly does NOT grant any reward.
    /// </summary>
    /// <param name="error">SDK error details from CrazyGames.</param>
    private void HandleAdError (SdkError error)
    {
        Log ("Ad error from SDK: " + error);
        HandleAdError ();
    }

    /// <summary>
    /// Overload used when the SDK isn't available at all (no error object to pass).
    /// </summary>
    private void HandleAdError ()
    {
        RestoreGameState ();

        if (_pendingAdErrorCallback != null)
        {
            Action cb = _pendingAdErrorCallback;
            _pendingAdErrorCallback = null;
            cb ();
        }

        _pendingRewardCallback     = null;
        _pendingAdCompleteCallback = null;
    }

    /// <summary>
    /// Restores the time scale and audio state captured in <see cref="HandleAdStart"/>.
    /// Safe to call even if no ad is currently in flight.
    /// </summary>
    private void RestoreGameState ()
    {
        if (!_isShowingAd) return;

        Time.timeScale      = _timeScaleBeforeAd;
        AudioListener.pause = _audioPausedBeforeAd;
        _isShowingAd        = false;
    }

    // -----------------------------------------------------------------------------
    // Cloud save (Data module)
    //
    // Thin wrappers around CrazySDK.Data, the CrazyGames cloud key-value store.
    // Falls back to PlayerPrefs when not on CrazyGames so call sites stay simple.
    //
    // To adopt cloud save in your existing PlayerData, swap your PlayerPrefs.*
    // calls for these. Be aware that CrazySDK.Data and PlayerPrefs are different
    // storage backends — migration between them is the caller's responsibility.
    // -----------------------------------------------------------------------------

    public void SaveInt (string key, int value)
    {
        if (UseCloudSave) CrazySDK.Data.SetInt (key, value);
        else              PlayerPrefs.SetInt (key, value);
    }

    public int LoadInt (string key, int defaultValue = 0)
    {
        return UseCloudSave
            ? CrazySDK.Data.GetInt (key, defaultValue)
            : PlayerPrefs.GetInt (key, defaultValue);
    }

    public void SaveFloat (string key, float value)
    {
        if (UseCloudSave) CrazySDK.Data.SetFloat (key, value);
        else              PlayerPrefs.SetFloat (key, value);
    }

    public float LoadFloat (string key, float defaultValue = 0f)
    {
        return UseCloudSave
            ? CrazySDK.Data.GetFloat (key, defaultValue)
            : PlayerPrefs.GetFloat (key, defaultValue);
    }

    public void SaveString (string key, string value)
    {
        if (UseCloudSave) CrazySDK.Data.SetString (key, value);
        else              PlayerPrefs.SetString (key, value);
    }

    public string LoadString (string key, string defaultValue = "")
    {
        return UseCloudSave
            ? CrazySDK.Data.GetString (key, defaultValue)
            : PlayerPrefs.GetString (key, defaultValue);
    }

    public bool HasKey (string key)
    {
        return UseCloudSave
            ? CrazySDK.Data.HasKey (key)
            : PlayerPrefs.HasKey (key);
    }

    public void DeleteKey (string key)
    {
        if (UseCloudSave) CrazySDK.Data.DeleteKey (key);
        else              PlayerPrefs.DeleteKey (key);
    }

    /// <summary>
    /// True when both we're on a CrazyGames host AND the SDK has finished init,
    /// so cloud-save calls would actually persist. Used to gate the routing in
    /// the Save/Load methods above.
    /// </summary>
    private static bool UseCloudSave => IsOnCrazyGames && CrazySDK.IsInitialized;

    // -----------------------------------------------------------------------------
    // Utilities
    // -----------------------------------------------------------------------------

    private void Log (string message)
    {
        if (_verboseLogging) Debug.Log ("[CrazyGames] " + message);
    }
}
