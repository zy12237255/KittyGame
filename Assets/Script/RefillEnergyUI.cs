using GoogleMobileAds.Api;
using UnityEngine;
using UnityEngine.UI;

public class RefillEnergyUI : BasePopup
{
    [Header("Buttons")]
    [SerializeField] protected Button refillAdsBtn;
    [SerializeField] protected Button refillCoinsBtn;
    [SerializeField] protected Button closeBtn;

    protected override void Awake()
    {
        base.Awake();
        BindButtons();
    }

    protected virtual void BindButtons()
    {
        if (refillAdsBtn != null)
            refillAdsBtn.onClick.AddListener(HandleRefillAdsClicked);
        if (refillCoinsBtn != null)
            refillCoinsBtn.onClick.AddListener(HandleRefillCoinsClicked);
        if (closeBtn != null)
            closeBtn.onClick.AddListener(HandleCloseClicked);
    }

    protected virtual void HandleRefillAdsClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        WatchAds();
    }

    protected virtual void HandleRefillCoinsClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        if (GameManager.Instance != null && GameManager.Instance.TryRefillEnergyByCoins())
        {
            Hide();
        }
    }

    protected virtual void HandleCloseClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        GameManager.Instance?.OnRefillEnergyClose();
        Hide();
    }

    public void WatchAds()
    {
        if (AdsControl.Instance.rewardedAd != null)
        {
            if (AdsControl.Instance.rewardedAd.CanShowAd())
            {
                AdsControl.Instance.ShowRewardAd(EarnReward);
            }
        }
    }

    public void EarnReward(Reward reward)
    {
        if (GameManager.Instance == null) return;
        GameManager.Instance.OnRefillEnergyByAds();
        Hide();
    }

}
