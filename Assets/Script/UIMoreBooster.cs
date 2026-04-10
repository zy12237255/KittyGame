using UnityEngine;
using UnityEngine.UI;
using TMPro;
using GoogleMobileAds.Api;

// Shop popup for acquiring additional boosters.
/// <summary>
/// Popup shown when player taps a booster button with count 0 (get more boosters).
/// Show by type: Freeze, Star, Magnet. GetByCoins: -900 coins, +3 boosters (toast if not enough). GetByAds: +1 booster. Close: hide.
/// </summary>
public class UIMoreBooster : BasePopup
{
    [Header("MoreBooster UI")]
    [SerializeField] protected RectTransform dimed;
    [SerializeField] protected RectTransform bg;
    [SerializeField] protected TextMeshProUGUI textTitle;
    [SerializeField] protected Button buttonClose;
    [SerializeField] protected TextMeshProUGUI textInfo;

    [Header("Get by Coins / Ads")]
    [SerializeField] protected Button buttonGetByCoins;
    [SerializeField] protected Button buttonGetByAds;

    [Header("Booster icon (set by type)")]
    [SerializeField] protected Image boosterIconImage;
    [SerializeField] protected Sprite freezeIconSprite;
    [SerializeField] protected Sprite starIconSprite;
    [SerializeField] protected Sprite magnetIconSprite;

    [Header("Optional")]
    [SerializeField] protected RectTransform line;

    private const int GetByCoinsCost = 900;
    private const int GetByCoinsAmount = 3;
    private const int GetByAdsAmount = 1;

    private BoosterType _currentType;

    protected override void Awake()
    {
        base.Awake();
        BindButtons();
    }

    protected virtual void BindButtons()
    {
        if (buttonClose != null)
            buttonClose.onClick.AddListener(HandleCloseClicked);
        if (buttonGetByCoins != null)
            buttonGetByCoins.onClick.AddListener(HandleGetByCoinsClicked);
        if (buttonGetByAds != null)
            buttonGetByAds.onClick.AddListener(HandleGetByAdsClicked);
    }

    /// <summary>
    /// Show popup for the given booster type. Sets icon and title/info by type.
    /// </summary>
    public virtual void Show(BoosterType type)
    {
        _currentType = type;
        SetIconByType(type);
        SetTitleByType(type);
        base.Show();
    }

    /// <summary>
    /// Legacy: show without type (defaults to Freeze). Prefer Show(BoosterType).
    /// </summary>
    public override void Show()
    {
        Show(BoosterType.Freeze);
    }

    protected virtual void SetIconByType(BoosterType type)
    {
        if (boosterIconImage == null) return;
        Sprite sprite = GetBoosterIconSprite(type);
        if (sprite != null)
        {
            boosterIconImage.sprite = sprite;
            boosterIconImage.enabled = true;
        }
        else
        {
            boosterIconImage.enabled = false;
        }
    }

    protected virtual Sprite GetBoosterIconSprite(BoosterType type)
    {
        switch (type)
        {
            case BoosterType.Freeze: return freezeIconSprite;
            case BoosterType.Star: return starIconSprite;
            case BoosterType.Magnet: return magnetIconSprite;
            default: return null;
        }
    }

    protected virtual void SetTitleByType(BoosterType type)
    {
        string name = GetBoosterDisplayName(type);
        if (textTitle != null)
            textTitle.text = "Get More " + name;
        if (textInfo != null)
            textInfo.text = "Get more boosters";
    }

    private static string GetBoosterDisplayName(BoosterType type)
    {
        switch (type)
        {
            case BoosterType.Freeze: return "Freeze";
            case BoosterType.Star: return "Star";
            case BoosterType.Magnet: return "Magnet";
            default: return "Booster";
        }
    }

    protected virtual void HandleCloseClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        if (GameManager.Instance != null)
            GameManager.Instance.OnMoreBoosterClosed();
        Hide();
    }

    /// <summary>
    /// -900 coins, +3 boosters. Shows Toast "Not enough coins." if insufficient.
    /// </summary>
    protected virtual void HandleGetByCoinsClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.GetCoin() < GetByCoinsCost)
        {
            GameManager.Instance.ShowNotEnoughCoinsToast();
            return;
        }
        GameManager.Instance.AddCoin(-GetByCoinsCost);
        GameManager.Instance.AddBooster(_currentType, GetByCoinsAmount);
        if (GameManager.Instance.uiManager?.StatusUI != null)
            GameManager.Instance.uiManager.StatusUI.RefreshFromGameData();
        GameManager.Instance.RefreshBoosterButtonsIfInGame();
        if (GameManager.Instance != null)
            GameManager.Instance.OnMoreBoosterClosed();
        Hide();
    }

    /// <summary>
    /// Free watch ads, +1 booster. TODO: integrate ad SDK then grant reward.
    /// </summary>
    protected virtual void HandleGetByAdsClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        if (GameManager.Instance == null) return;
        // TODO: show rewarded ad, on success call OnGetByAdsRewarded()
        OnGetByAdsRewarded();
    }

    /// <summary>
    /// Call this when rewarded ad completed (or call directly for testing without ad).
    /// </summary>
    public virtual void OnGetByAdsRewarded()
    {
        WatchAds();
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
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddBooster(_currentType, GetByAdsAmount);
            if (GameManager.Instance.uiManager?.StatusUI != null)
                GameManager.Instance.uiManager.StatusUI.RefreshFromGameData();
            GameManager.Instance.RefreshBoosterButtonsIfInGame();
            GameManager.Instance.OnMoreBoosterClosed();
        }
        Hide();
    }

}
