using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
using MEC;

public class AdsManager : Singleton<AdsManager>
{
    #region Admobs

    // App ID is configured in Assets > Google Mobile Ads > Settings (Google Mobile Ads SDK v9+);
    // this field is kept so the Inspector value isn't lost on existing prefabs.
    [Header ("CONFIG")] [SerializeField] private string _AppId;
    [SerializeField]                     private string _BannerId;
    [SerializeField]                     private string _RewardVideoId;
    [SerializeField]                     private string _InterstitialId;

    #endregion

    [Header ("Action")] public bool IsRewardVideoAvailable;
    public                     bool IsBannerAvailable;
    public                     bool IsInterstitialAvailable;

    [Header ("Config")] [SerializeField] private bool IsAutoReloadAgain;
    [Tooltip ("Show interstitial every Nth game over. Lower = more revenue, " +
              "but hurts retention. 3 is a common compromise.")]
    [SerializeField] private int  _InterstitialEveryNthGameOver = 3;
    private int _gameOverCounter;

    public System.Action OnCompletedRewardVideo;
    public System.Action OnFailedRewardVideo;

    public System.Action OnFailedFullScreen;
    public System.Action OnCompletedFullScreen;

    private bool IsCompletedTheRewards;
    private bool IsRewardClosed;
    private bool IsRewardValid;

    private RewardedAd     reward;
    private BannerView     banner;
    private InterstitialAd interstitial;

    private bool IsRemoveAds;

    private bool IsWatchedRewardAds;
    private bool IsFirstTimeLoadBanner;

    private CoroutineHandle handleLoadAds;

    private void Init ()
    {
        // Init order (Apple + Google policy):
        //   1. ATT prompt  (iOS only; collects IDFA permission)
        //   2. UMP consent (GDPR/CCPA form if user is in a regulated region)
        //   3. MobileAds.Initialize (now allowed to use whatever signals
        //      consent + ATT granted)
        //
        // Each step's callback fires regardless of outcome, so the chain always
        // reaches MobileAds.Initialize even if the user denies tracking.

        AppTracking.RequestAuthorization (attStatus =>
        {
            LogGame.Log ("[Ad Manager] ATT status: " + attStatus);

            UmpConsentManager.RequestConsent (onComplete: () =>
            {
                MobileAds.Initialize (initStatus =>
                {
                    LogGame.Log ("[Ad Manager] Init Event Completed!");
                });
            });
        });

        RefreshRemoveAds ();
    }

    public void RefreshRemoveAds ()
    {
        IsRemoveAds = PlayerData.IsRemoveAds;

        if (IsRemoveAds)
        {
            HideBanner ();
        }
    }

    private void RegisterEvent ()
    {
        #if UNITY_EDITOR
        IsRewardVideoAvailable  = true;
        IsBannerAvailable       = true;
        IsInterstitialAvailable = true;
        return;
        #endif

        RefreshRewardVideo ();
        RefreshBanner ();
        RefreshInterstitial ();
    }

    #region System

    protected override void Awake ()
    {
        base.Awake ();

        Init ();

        RegisterEvent ();
    }

    private IEnumerator<float> _LoadAds ()
    {
        while (IsWatchedRewardAds == false)
        {
            yield return Timing.WaitForOneFrame;
        }

        if (IsWatchedRewardAds)
        {
            IsWatchedRewardAds = false;

            yield return Timing.WaitUntilDone (Timing.RunCoroutine (_ReloadRewardAds ()));
        }
    }

    private IEnumerator<float> _ReloadRewardAds ()
    {
        yield return Timing.WaitForOneFrame;

        IsRewardVideoAvailable = false;

        if (IsRewardClosed && IsRewardValid)
        {
            DoCompletedRewardVideo ();

            IsRewardClosed = false;
            IsRewardValid  = false;
        }
        else
        {
            DoFailedRewardVideo ();

            IsRewardClosed = false;
            IsRewardValid  = false;
        }

        if (IsAutoReloadAgain)
            RefreshRewardVideo ();
    }

    #endregion

    #region Reward Callback

    private void RegisterRewardCallBack (RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += RewardOnFullScreenContentClosed;
        ad.OnAdFullScreenContentFailed += RewardOnFullScreenContentFailed;
    }

    private void RewardOnFullScreenContentClosed ()
    {
        IsRewardClosed     = true;
        IsWatchedRewardAds = true;
    }

    private void RewardOnFullScreenContentFailed (AdError error)
    {
        IsRewardVideoAvailable = false;

        DoFailedRewardVideo ();
    }

    #endregion

    #region Banner Callback

    private void RegisterBannerCallBack ()
    {
        banner.OnBannerAdLoaded            += BannerOnAdLoaded;
        banner.OnBannerAdLoadFailed        += BannerOnAdLoadFailed;
        banner.OnAdFullScreenContentClosed += BannerOnAdClosed;
    }

    private void BannerOnAdLoadFailed (LoadAdError error)
    {
        IsBannerAvailable = false;
    }

    private void BannerOnAdLoaded ()
    {
        IsBannerAvailable = true;
    }

    private void BannerOnAdClosed ()
    {
        IsBannerAvailable = false;
    }

    #endregion

    private void DoFailedFullScreen ()
    {
        if (OnFailedFullScreen != null)
        {
            OnFailedFullScreen ();
            OnFailedFullScreen = null;
        }
    }

    private void DoCompletedFullScreen ()
    {
        if (OnCompletedFullScreen != null)
        {
            OnCompletedFullScreen ();
            OnCompletedFullScreen = null;
        }
    }

    private void DoFailedRewardVideo ()
    {
        if (OnFailedRewardVideo != null)
        {
            OnFailedRewardVideo ();
            OnFailedRewardVideo = null;
        }

        LogGame.Log ("[Ad Manager] Reward Video Is Failed!");
    }

    private void DoCompletedRewardVideo ()
    {
        if (OnCompletedRewardVideo != null)
        {
            OnCompletedRewardVideo ();
            OnCompletedRewardVideo = null;
        }

        LogGame.Log ("[Ad Manager] Reward Video Is Completed!");
    }

    public void RegisterEvent (System.Action OnCompleted, System.Action OnFailed)
    {
        OnCompletedRewardVideo = OnCompleted;
        OnFailedRewardVideo    = OnFailed;
    }

    public void RegisterEventFullScreen (System.Action OnCompleted, System.Action OnFailed)
    {
        OnFailedFullScreen    = OnFailed;
        OnCompletedFullScreen = OnCompleted;
    }

    public void ShowRewardVideo ()
    {
        #if UNITY_EDITOR || UNITY_STANDALONE

        DoCompletedRewardVideo ();

        return;

        #endif

        if (reward != null && reward.CanShowAd ())
        {
            Timing.KillCoroutines (handleLoadAds);

            handleLoadAds = Timing.RunCoroutine (_LoadAds ());

            reward.Show (r => { IsRewardValid = true; });
        }
        else
        {
            RefreshRewardVideo ();

            DoFailedRewardVideo ();
        }
    }

    public void ShowBanner ()
    {
        if (IsRemoveAds)
        {
            return;
        }

        #if UNITY_EDITOR || UNITY_STANDALONE
        return;
        #endif

        if (IsBannerAvailable && banner != null)
        {
            banner.Show ();
        }
        else
        {
            RefreshBanner ();
        }
    }

    public void HideBanner ()
    {
        if (banner != null)
            banner.Hide ();
    }

    public void RefreshRewardVideo ()
    {
        if (IsRewardVideoAvailable) return;

        if (reward != null)
        {
            reward.Destroy ();
            reward = null;
        }

        AdRequest request = new AdRequest ();

        RewardedAd.Load (_RewardVideoId, request, (RewardedAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                IsRewardVideoAvailable = false;
                DoFailedRewardVideo ();
                return;
            }

            reward                 = ad;
            IsRewardVideoAvailable = true;
            IsRewardClosed         = false;
            IsRewardValid          = false;

            RegisterRewardCallBack (ad);
        });
    }

    public void RefreshBanner ()
    {
        if (IsRemoveAds)
            return;

        if (IsBannerAvailable) return;

        if (banner == null)
        {
            banner = new BannerView (_BannerId, AdSize.Banner, AdPosition.Bottom);
            RegisterBannerCallBack ();
        }

        AdRequest request = new AdRequest ();

        banner.LoadAd (request);
    }

    // -----------------------------------------------------------------------------
    // Interstitial ads
    //
    // Shown periodically on game over via TryShowInterstitialOnGameOver(). Bypassed
    // entirely if the player has bought the Remove Ads IAP. Skipped in Editor and
    // routed through the CrazyGames midroll on WebGL so we don't pay AdMob for
    // impressions that won't fill on web.
    // -----------------------------------------------------------------------------

    public void RefreshInterstitial ()
    {
        if (IsRemoveAds)         return;
        if (IsInterstitialAvailable) return;
        if (string.IsNullOrEmpty (_InterstitialId)) return;

        #if UNITY_WEBGL && !UNITY_EDITOR
        // CrazyGames serves the equivalent ad via RequestMidrollAd; no preload here.
        IsInterstitialAvailable = true;
        return;
        #endif

        if (interstitial != null)
        {
            interstitial.Destroy ();
            interstitial = null;
        }

        AdRequest request = new AdRequest ();

        InterstitialAd.Load (_InterstitialId, request, (InterstitialAd ad, LoadAdError error) =>
        {
            if (error != null || ad == null)
            {
                IsInterstitialAvailable = false;
                return;
            }

            interstitial             = ad;
            IsInterstitialAvailable  = true;

            ad.OnAdFullScreenContentClosed += () =>
            {
                IsInterstitialAvailable = false;
                if (IsAutoReloadAgain) RefreshInterstitial ();
            };
            ad.OnAdFullScreenContentFailed += (AdError err) =>
            {
                IsInterstitialAvailable = false;
                if (IsAutoReloadAgain) RefreshInterstitial ();
            };
        });
    }

    /// <summary>
    /// Force-show an interstitial now. Returns true if one was actually displayed.
    /// </summary>
    public bool ShowInterstitial ()
    {
        if (IsRemoveAds) return false;

        #if UNITY_EDITOR || UNITY_STANDALONE
        // No real ad in editor; pretend we showed one so the caller's pacing
        // logic still iterates correctly.
        return true;
        #endif

        #if UNITY_WEBGL && !UNITY_EDITOR
        if (CrazyGamesIntegration.Instance != null)
        {
            CrazyGamesIntegration.Instance.RequestMidrollAd ();
            return true;
        }
        return false;
        #endif

        if (interstitial != null && interstitial.CanShowAd ())
        {
            interstitial.Show ();
            IsInterstitialAvailable = false;
            return true;
        }

        // Couldn't show; kick off a preload so the next attempt has one ready.
        RefreshInterstitial ();
        return false;
    }

    /// <summary>
    /// Show an interstitial every Nth game over, where N is _InterstitialEveryNthGameOver.
    /// Game code calls this from GameManager.EnableGameOver so the placement
    /// strategy lives in one place and can be tuned in the inspector.
    /// </summary>
    public void TryShowInterstitialOnGameOver ()
    {
        if (IsRemoveAds) return;
        if (_InterstitialEveryNthGameOver <= 0) return;

        _gameOverCounter++;
        if (_gameOverCounter % _InterstitialEveryNthGameOver != 0) return;

        ShowInterstitial ();
    }
}
