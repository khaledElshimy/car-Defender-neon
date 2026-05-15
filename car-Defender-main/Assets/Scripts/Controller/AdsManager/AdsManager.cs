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

    #endregion

    [Header ("Action")] public bool IsRewardVideoAvailable;
    public                     bool IsBannerAvailable;

    [Header ("Config")] [SerializeField] private bool IsAutoReloadAgain;

    public System.Action OnCompletedRewardVideo;
    public System.Action OnFailedRewardVideo;

    public System.Action OnFailedFullScreen;
    public System.Action OnCompletedFullScreen;

    private bool IsCompletedTheRewards;
    private bool IsRewardClosed;
    private bool IsRewardValid;

    private RewardedAd reward;
    private BannerView banner;

    private bool IsRemoveAds;

    private bool IsWatchedRewardAds;
    private bool IsFirstTimeLoadBanner;

    private CoroutineHandle handleLoadAds;

    private void Init ()
    {
        MobileAds.Initialize (initStatus =>
        {
            LogGame.Log ("[Ad Manager] Init Event Completed!");
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
        IsRewardVideoAvailable = true;
        IsBannerAvailable      = true;
        return;
        #endif

        RefreshRewardVideo ();
        RefreshBanner ();
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

        #if UNITY_WEBGL
        // On the web build we route rewarded ads through the CrazyGames SDK
        // instead of AdMob. AdMob's WebGL support is unofficial and CrazyGames
        // is the ad partner for the portal anyway.
        if (CrazyGamesIntegration.Instance != null)
        {
            CrazyGamesIntegration.Instance.RequestRewardedAd (
                onReward: DoCompletedRewardVideo,
                onError:  DoFailedRewardVideo);
            return;
        }
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

        #if UNITY_WEBGL && !UNITY_EDITOR
        // CrazyGames requests ads on-demand; no preload step required.
        // We mark the reward video available so the rest of the game flow
        // (which gates buttons on this bool) keeps working.
        IsRewardVideoAvailable = true;
        return;
        #endif

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

        #if UNITY_WEBGL && !UNITY_EDITOR
        // Banners on WebGL are placed via the CrazyBanner component in the UI
        // (Assets/CrazySDK/Resources/CrazyBanner.prefab). The SDK manages
        // refresh internally; no AdMob banner is created.
        if (CrazyGamesIntegration.Instance != null)
            CrazyGamesIntegration.Instance.RefreshBanners ();
        IsBannerAvailable = true;
        return;
        #endif

        if (banner == null)
        {
            banner = new BannerView (_BannerId, AdSize.Banner, AdPosition.Bottom);
            RegisterBannerCallBack ();
        }

        AdRequest request = new AdRequest ();

        banner.LoadAd (request);
    }
}
