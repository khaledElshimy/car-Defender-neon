using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds;
using GoogleMobileAds.Api;
using MEC;

public class AdsManager : Singleton<AdsManager>
{
    #region Admobs
    // These ad units are configured to always serve test ads.

#if UNITY_ANDROID
    private string _RewardVideoId = "ca-app-pub-7737342360692738/8305286185";
#elif UNITY_IPHONE
  private string _RewardVideoId = "ca-app-pub-7737342360692738/9291565806";
#else
  private string _RewardVideoId = "unused";
#endif

#if UNITY_ANDROID
    private string _BannerId = "ca-app-pub-7737342360692738/3109300833";
#elif UNITY_IPHONE
  private string _BannerId = "ca-app-pub-7737342360692738/8361627511";
#else
  private string _BannerId = "unused";
#endif

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

    private RewardedAd _rewardedAd;
    private BannerView _bannerView;

    private bool IsRemoveAds;

    private bool IsWatchedRewardAds;
    private bool IsFirstTimeLoadBanner;

    private CoroutineHandle handleLoadAds;

    private void Init ()
    {
        // =============================== INIT THE AD ================================ //

        //MobileAds.Initialize (_AppId);
        MobileAds.Initialize(initStatus => {
            CreateBannerView();
        });

        LogGame.Log ("[Ad Manager] Init Event Completed!");
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


    protected override void Awake ()
    {
        base.Awake ();

        Init ();
    }
  

    #region RewaredAds
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

        // Clean up the old ad before loading a new one.
        if (_rewardedAd != null)
        {
            _rewardedAd.Destroy();
            _rewardedAd = null;
        }

        Debug.Log("Loading the rewarded ad.");

        // create our request used to load the ad.
        var adRequest = new AdRequest();

        // send the request to load the ad.
        RewardedAd.Load(_RewardVideoId, adRequest,
            (RewardedAd ad, LoadAdError error) =>
            {
              // if error is not null, the load request failed.
              if (error != null || ad == null)
                {
                    Debug.LogError("Rewarded ad failed to load an ad " +
                                   "with error : " + error);
                    return;
                }

                // If the operation failed for unknown reasons.
                // This is an unexpected error, please report this bug if it happens.
                if (ad == null)
                {
                    Debug.LogError("Unexpected error: Rewarded load event fired with null ad and null error.");
                    return;
                }

                // The operation completed successfully.
                Debug.Log("Rewarded ad loaded with response : " + ad.GetResponseInfo());
                _rewardedAd = ad;

                // Register to ad events to extend functionality.
                RegisterEventHandlers(ad);
            });
    }

    /// <summary>
    /// Shows the ad.
    /// </summary>
    public void ShowRewarededAd()
    {
        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            Debug.Log("Showing rewarded ad.");
            _rewardedAd.Show((Reward reward) =>
            {
                Debug.Log(String.Format("Rewarded ad granted a reward: {0} {1}",
                                        reward.Amount,
                                        reward.Type));
            });
        }
        else
        {
            Debug.LogError("Rewarded ad is not ready yet.");
        }
    }

    /// <summary>
    /// Destroys the ad.
    /// </summary>
    public void DestroyAd()
    {
        if (_rewardedAd != null)
        {
            Debug.Log("Destroying rewarded ad.");
            _rewardedAd.Destroy();
            _rewardedAd = null;
        }
    }

    /// <summary>
    /// Logs the ResponseInfo.
    /// </summary>
    public void LogResponseInfo()
    {
        if (_rewardedAd != null)
        {
            var responseInfo = _rewardedAd.GetResponseInfo();
            UnityEngine.Debug.Log(responseInfo);
        }
    }

    private void RegisterEventHandlers(RewardedAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Rewarded ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
            IsRewardValid = true;
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Rewarded ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Debug.Log("Rewarded ad was clicked.");
        };
        // Raised when the ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Rewarded ad full screen content opened.");
            IsRewardVideoAvailable = true;
            IsRewardClosed = false;
            IsRewardValid = false;
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Rewarded ad full screen content closed.");
            IsRewardClosed = true;
            IsWatchedRewardAds = true;
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Rewarded ad failed to open full screen content with error : "
                + error);

            IsRewardVideoAvailable = false;

            DoFailedRewardVideo();
        };
    }
    #endregion

    #region BannerAds
    /// <summary>
    /// Creates a 320x50 banner at top of the screen.
    /// </summary>
    public void CreateBannerView()
    {
        Debug.Log("Creating banner view.");

        // If we already have a banner, destroy the old one.
        if (_bannerView != null)
        {
            DestroyAd();
        }

        // Create a 320x50 banner at top of the screen.
        _bannerView = new BannerView(_BannerId, AdSize.Banner, AdPosition.Bottom);

        // Listen to events the banner may raise.
        RegisterBannerCallBack();

        Debug.Log("Banner view created.");
    }

    /// <summary>
    /// Creates the banner view and loads a banner ad.
    /// </summary>
    public void LoadAd()
    {
        // Create an instance of a banner view first.
        if (_bannerView == null)
        {
            CreateBannerView();
        }

        // Create our request used to load the ad.
        var adRequest = new AdRequest();

        // Send the request to load the ad.
        Debug.Log("Loading banner ad.");
        _bannerView.LoadAd(adRequest);
    }
    /// <summary>
    /// Shows the ad.
    /// </summary>
    public void ShowBannerAd()
    {
        if (_bannerView != null)
        {
            Debug.Log("Showing banner view.");
            _bannerView.Show();
        }
    }

    /// <summary>
    /// Hides the ad.
    /// </summary>
    public void HideBanner()
    {
        if (_bannerView != null)
        {
            Debug.Log("Hiding banner view.");
            _bannerView.Hide();
        }
    }
    /// <summary>
    /// Destroys the ad.
    /// When you are finished with a BannerView, make sure to call
    /// the Destroy() method before dropping your reference to it.
    /// </summary>
    public void DestroyBannerAd()
    {
        if (_bannerView != null)
        {
            Debug.Log("Destroying banner view.");
            _bannerView.Destroy();
            _bannerView = null;
        }
    }

    /// <summary>
    /// Logs the ResponseInfo.
    /// </summary>
    public void LogRBanneresponseInfo()
    {
        if (_bannerView != null)
        {
            var responseInfo = _bannerView.GetResponseInfo();
            if (responseInfo != null)
            {
                UnityEngine.Debug.Log(responseInfo);
            }
        }
    }

    private void RegisterBannerCallBack ()
    {
        // Raised when an ad is loaded into the banner view.
        _bannerView.OnBannerAdLoaded += () =>
        {
            Debug.Log("Banner view loaded an ad with response : "
                + _bannerView.GetResponseInfo());
            IsBannerAvailable = true;

        };
        // Raised when an ad fails to load into the banner view.
        _bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            Debug.LogError("Banner view failed to load an ad with error : " + error);
            IsBannerAvailable = false;

        };
        // Raised when the ad is estimated to have earned money.
        _bannerView.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Banner view paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        _bannerView.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Banner view recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        _bannerView.OnAdClicked += () =>
        {
            Debug.Log("Banner view was clicked.");
        };
        // Raised when an ad opened full screen content.
        _bannerView.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Banner view full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        _bannerView.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Banner view full screen content closed.");
        };
    }

    private void BannerOnOnAdClosed (object sender, EventArgs e)
    {
       // IsBannerAvailable = false;
    }
    #endregion

    public void ShowRewardVideo()
    {
#if UNITY_EDITOR || UNITY_STANDALONE

        DoCompletedRewardVideo();

        return;

#endif

        if (_rewardedAd.CanShowAd())
        {
            Timing.KillCoroutines(handleLoadAds);

            handleLoadAds = Timing.RunCoroutine(_LoadAds());

            ShowRewarededAd();
        }
        else
        {
            RefreshRewardVideo();

            DoFailedRewardVideo();
        }
    }

    public void RefreshRewardVideo()
    {
        if (IsRewardVideoAvailable) return;

        AdRequest adRequest = new AdRequest();

        // send the request to load the ad.
        RewardedAd.Load(_RewardVideoId, adRequest,
            (RewardedAd ad, LoadAdError error) =>
            {
                // if error is not null, the load request failed.
                if (error != null || ad == null)
                {
                    Debug.LogError("Rewarded ad failed to load an ad " +
                                   "with error : " + error);
                    return;
                }

                // If the operation failed for unknown reasons.
                // This is an unexpected error, please report this bug if it happens.
                if (ad == null)
                {
                    Debug.LogError("Unexpected error: Rewarded load event fired with null ad and null error.");
                    return;
                }

                // The operation completed successfully.
                Debug.Log("Rewarded ad loaded with response : " + ad.GetResponseInfo());
                _rewardedAd = ad;

                // Register to ad events to extend functionality.
                RegisterEventHandlers(ad);
            });
    }

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
 
}