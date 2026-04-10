using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using GoogleMobileAds.Api;
using System;
using GoogleMobileAds.Common;
/*
 * 
 * Document for Unity Ads : https://docs.unity.com/ads/ImplementingBasicAdsUnity.html
 */
/*
 * 
 * Document for Google Admob : https://developers.google.com/admob/unity/quick-start
 */

public class AdsControl : MonoBehaviour
{

    private static AdsControl instance;

    //for Admob
    #region ADMOB_KEY
    public string Android_AppID, IOS_AppID;

    public string Android_Banner_Key, IOS_Banner_Key;

    public string Android_Interestital_Key, IOS_Interestital_Key;

    public string Android_RW_Key, IOS_RW_Key;

    #endregion


    private AppOpenAd appOpenAd;
    private BannerView bannerView;
    private InterstitialAd interstitialAd;
    [HideInInspector]
    public RewardedAd rewardedAd;
    private RewardedInterstitialAd rewardedInterstitialAd;
    private bool isShowingAppOpenAd;
    private const string KeyShowAdsCounter = "ShowAds";
    private const string KeyRemoveAds = "removeAds";

    public static AdsControl Instance { get { return instance; } }

    void Awake()
    {
        if (FindObjectsOfType(typeof(AdsControl)).Length > 1)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }


    private void Start()
    {

        MobileAds.SetiOSAppPauseOnBackground(true);
        // Initialize the Google Mobile Ads SDK.
        MobileAds.Initialize(HandleInitCompleteAction);

    }

    private void HandleInitCompleteAction(InitializationStatus initstatus)
    {
        Debug.Log("Initialization complete.");

        // Callbacks from GoogleMobileAds are not guaranteed to be called on
        // the main thread.
        // In this example we use MobileAdsEventExecutor to schedule these calls on
        // the next Update() loop.
        MobileAdsEventExecutor.ExecuteInUpdate(() =>
        {

            if (!IsRemoveAds())
            {
                LoadBannerAd();
                LoadInterstitialAd();
            }

            LoadRewardAd();
        });
    }

    #region HELPER METHODS
    /*
    private AdRequest CreateAdRequest()
    {
        return new AdRequest.Builder()
            .AddKeyword("unity-admob-sample")
            .Build();
    }
    */
    public void OnApplicationPause(bool paused)
    {
        // Display the app open ad when the app is foregrounded.
        if (!paused)
        {
            // ShowAppOpenAd();
        }
    }

    #endregion

    #region BANNER ADS


    /// <summary>
    /// Creates a 320x50 banner at top of the screen.
    /// </summary>
    public void CreateBannerView()
    {
        //Debug.Log("Creating banner view.");


        // These ad units are configured to always serve test ads.
#if UNITY_EDITOR
        string adUnitId = "unused";

#elif UNITY_ANDROID
        string adUnitId =  Android_Banner_Key;
#elif UNITY_IPHONE
        string adUnitId = IOS_Banner_Key;
#else
        string adUnitId = "unexpected_platform";
#endif

        // If we already have a banner, destroy the old one.
        if (bannerView != null)
        {
            DestroyBannerAd();
        }

        // Create a 320x50 banner at top of the screen.
        bannerView = new BannerView(adUnitId, AdSize.Banner, AdPosition.Bottom);

        // Listen to events the banner may raise.
        ListenToAdEvents();

        //Debug.Log("Banner view created.");
    }


    /// <summary>
    /// Creates the banner view and loads a banner ad.
    /// </summary>
    public void LoadBannerAd()
    {
        // Create an instance of a banner view first.
        if (bannerView == null)
        {
            CreateBannerView();
        }

        // Create our request used to load the ad.
        var adRequest = new AdRequest();

        // Send the request to load the ad.
        //Debug.Log("Loading banner ad.");
        bannerView.LoadAd(adRequest);
    }

    /// <summary>
    /// Shows the ad.
    /// </summary>
    public void ShowBannerAd()
    {
        if (bannerView != null)
        {
            Debug.Log("Showing banner view.");
            bannerView.Show();
        }
    }

    /// <summary>
    /// Hides the ad.
    /// </summary>
    public void HideBannerAd()
    {
        if (bannerView != null)
        {
            Debug.Log("Hiding banner view.");
            bannerView.Hide();
        }
    }

    /// <summary>
    /// Destroys the ad.
    /// When you are finished with a BannerView, make sure to call
    /// the Destroy() method before dropping your reference to it.
    /// </summary>
    public void DestroyBannerAd()
    {
        if (bannerView != null)
        {
            Debug.Log("Destroying banner view.");
            bannerView.Destroy();
            bannerView = null;
        }
    }

    /// <summary>
    /// Listen to events the banner may raise.
    /// </summary>
    private void ListenToAdEvents()
    {
        // Raised when an ad is loaded into the banner view.
        bannerView.OnBannerAdLoaded += () =>
        {
            Debug.Log("Banner view loaded an ad with response : "
                + bannerView.GetResponseInfo());
            ShowBannerAd();
            //HideBannerAd();

        };
        // Raised when an ad fails to load into the banner view.
        bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            Debug.LogError("Banner view failed to load an ad with error : " + error);
        };
        // Raised when the ad is estimated to have earned money.
        bannerView.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Banner view paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        bannerView.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Banner view recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        bannerView.OnAdClicked += () =>
        {
            Debug.Log("Banner view was clicked.");
        };
        // Raised when an ad opened full screen content.
        bannerView.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Banner view full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        bannerView.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Banner view full screen content closed.");
        };
    }

    #endregion

    #region INTERSTITIAL ADS

    /// <summary>
    /// Loads the ad.
    /// </summary>
    public void LoadInterstitialAd()
    {

#if UNITY_EDITOR
        string adUnitId = "unused";
#elif UNITY_ANDROID
        string adUnitId = Android_Interestital_Key;
#elif UNITY_IPHONE
        string adUnitId = IOS_Interestital_Key;
#else
        string adUnitId = "unexpected_platform";
#endif
        // Clean up the old ad before loading a new one.
        if (interstitialAd != null)
        {
            DestroyInterstitialAd();
        }

        Debug.Log("Loading interstitial ad.");

        // Create our request used to load the ad.
        var adRequest = new AdRequest();

        // Send the request to load the ad.
        InterstitialAd.Load(adUnitId, adRequest, (InterstitialAd ad, LoadAdError error) =>
        {
            // If the operation failed with a reason.
            if (error != null)
            {
                Debug.LogError("Interstitial ad failed to load an ad with error : " + error);
                return;
            }
            // If the operation failed for unknown reasons.
            // This is an unexpected error, please report this bug if it happens.
            if (ad == null)
            {
                Debug.LogError("Unexpected error: Interstitial load event fired with null ad and null error.");
                return;
            }

            // The operation completed successfully.
            Debug.Log("Interstitial ad loaded with response : " + ad.GetResponseInfo());
            interstitialAd = ad;

            // Register to ad events to extend functionality.
            RegisterEventHandlers(ad);


        });
    }

    /// <summary>
    /// Shows the ad.
    /// </summary>
    public void ShowInterstitialAd()
    {
        if (interstitialAd != null && interstitialAd.CanShowAd())
        {
            Debug.Log("Showing interstitial ad.");
            interstitialAd.Show();
        }
        else
        {
            Debug.LogError("Interstitial ad is not ready yet.");
        }


    }

    /// <summary>
    /// Destroys the ad.
    /// </summary>
    public void DestroyInterstitialAd()
    {
        if (interstitialAd != null)
        {
            Debug.Log("Destroying interstitial ad.");
            interstitialAd.Destroy();
            interstitialAd = null;
        }

    }

    private void RegisterEventHandlers(InterstitialAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Interstitial ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
        };
        // Raised when an impression is recorded for an ad.
        ad.OnAdImpressionRecorded += () =>
        {
            Debug.Log("Interstitial ad recorded an impression.");
        };
        // Raised when a click is recorded for an ad.
        ad.OnAdClicked += () =>
        {
            Debug.Log("Interstitial ad was clicked.");
        };
        // Raised when an ad opened full screen content.
        ad.OnAdFullScreenContentOpened += () =>
        {
            Debug.Log("Interstitial ad full screen content opened.");
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Interstitial ad full screen content closed.");
            LoadInterstitialAd();
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Interstitial ad failed to open full screen content with error : "
                + error);
        };
    }

    #endregion

    #region REWARDED ADS

    /// <summary>
    /// Loads the ad.
    /// </summary>
    public void LoadRewardAd()
    {
        // PrintStatus("Requesting Rewarded ad.");
#if UNITY_EDITOR
        string adUnitId = "unused";
#elif UNITY_ANDROID
        string adUnitId = Android_RW_Key;
#elif UNITY_IPHONE
        string adUnitId = IOS_RW_Key;
#else
        string adUnitId = "unexpected_platform";
#endif

        // Clean up the old ad before loading a new one.
        if (rewardedAd != null)
        {
            DestroyRewardAd();
        }

        Debug.Log("Loading rewarded ad.");

        // Create our request used to load the ad.
        var adRequest = new AdRequest();

        // Send the request to load the ad.
        RewardedAd.Load(adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            // If the operation failed with a reason.
            if (error != null)
            {
                Debug.LogError("Rewarded ad failed to load an ad with error : " + error);
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
            rewardedAd = ad;

            // Register to ad events to extend functionality.
            RegisterEventHandlers(ad);


        });
    }

    /// <summary>
    /// Shows the ad.
    /// </summary>
    public void ShowRewardAd(Action<Reward> showRewardAdConplete)
    {
        if (rewardedAd != null && rewardedAd.CanShowAd())
        {
            Debug.Log("Showing rewarded ad.");
            /*
            rewardedAd.Show((Reward reward) =>
            {
                Debug.Log(String.Format("Rewarded ad granted a reward: {0} {1}",
                                        reward.Amount,
                                        reward.Type));
            });
            */
            rewardedAd.Show(showRewardAdConplete);
        }
        else
        {
            Debug.LogError("Rewarded ad is not ready yet.");
        }


    }

    /// <summary>
    /// Destroys the ad.
    /// </summary>
    public void DestroyRewardAd()
    {
        if (rewardedAd != null)
        {
            Debug.Log("Destroying rewarded ad.");
            rewardedAd.Destroy();
            rewardedAd = null;
        }
    }

    public bool IsRewardedVideoAvailable()
    {
        bool isAvailable = false;

        isAvailable = rewardedAd.CanShowAd();

        return isAvailable;
    }

    private void RegisterEventHandlers(RewardedAd ad)
    {
        // Raised when the ad is estimated to have earned money.
        ad.OnAdPaid += (AdValue adValue) =>
        {
            Debug.Log(String.Format("Rewarded ad paid {0} {1}.",
                adValue.Value,
                adValue.CurrencyCode));
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
        };
        // Raised when the ad closed full screen content.
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("Rewarded ad full screen content closed.");
            LoadRewardAd();
        };
        // Raised when the ad failed to open full screen content.
        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogError("Rewarded ad failed to open full screen content with error : "
                + error);
        };
    }

    #endregion


    public void ShowInterstital()
    {
        if (IsRemoveAds())
            return;

        /*
                if (currentAdsType == ADS_TYPE.ADMOB)
                {
                    if (interstitialAd != null && interstitialAd.CanShowAd())
                    {
                        interstitialAd.Show();
                    }
                }
                else if (currentAdsType == ADS_TYPE.UNITY)
                {
                    ShowUnityAd();
                }
                else if (currentAdsType == ADS_TYPE.MEDIATION)
                {
                    if (interstitialAd != null && interstitialAd.CanShowAd())
                    {
                        interstitialAd.Show();
                    }
                    else
                    {
                        ShowUnityAd();
                    }
                }
        */

        int numberShow = PlayerPrefs.GetInt(KeyShowAdsCounter);

        if (numberShow < 2)
        {
            numberShow++;
            PlayerPrefs.SetInt(KeyShowAdsCounter, numberShow);
            return;
        }
        else
        {
            numberShow = 0;
            PlayerPrefs.SetInt(KeyShowAdsCounter, numberShow);


            if (interstitialAd != null && interstitialAd.CanShowAd())
            {
                interstitialAd.Show();
            }

        }

    }

    public void RemoveAds()
    {
        PlayerPrefs.SetInt(KeyRemoveAds, 1);
        //if banner is active and user bought remove ads the banner will automatically hide
        HideBannerAd();
        DestroyBannerAd();

    }

    public bool IsRemoveAds()
    {
        if (!PlayerPrefs.HasKey(KeyRemoveAds))
            return false;
        return PlayerPrefs.GetInt(KeyRemoveAds) == 1;
    }
}
