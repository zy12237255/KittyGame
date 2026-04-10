using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using GoogleMobileAds.Api;

// Win panel flow: animate panel, grant rewards, then continue.
public class UIWin : BasePanel
{
    [Header("Background")]
    [SerializeField] protected RectTransform dimed;

    [Header("Group Title")]
    [SerializeField] protected RectTransform groupTitle;
    [SerializeField] protected RectTransform deco;
    [SerializeField] protected RectTransform imageCrown;
    [SerializeField] protected TextMeshProUGUI levelText;

    [Header("Horizontal Frame (rewardFrame)")]
    [SerializeField] protected RectTransform rewatdFrame;
    [SerializeField] protected CanvasGroup rewardFrameCanvasGroup;
    [SerializeField] protected TextMeshProUGUI textTitle;
    [SerializeField] protected RectTransform groupList;

    [Header("Rewards")]
    [SerializeField] protected RectTransform coinReward;
    [SerializeField] protected TextMeshProUGUI coinValueText;

    [Header("Claim X2")]
    [SerializeField] protected Button claimX2Btn;
    [SerializeField] protected TextMeshProUGUI claimX2Text;
    [SerializeField] protected RectTransform rwIcon;

    [Header("Continue")]
    [SerializeField] protected Button continueBtn;

    [Header("Win Animation")]
    [SerializeField] protected float bounceDuration = 0.4f;
    [SerializeField] protected float rewardFadeDuration = 0.3f;
    [SerializeField] protected Ease bounceEase = Ease.OutBack;

    private Sequence _showSequence;
    private int _coinRewardAmount;

    protected override void Awake()
    {
        base.Awake();
        BindButtons();
        ResetAnimationState();
    }

    private void ResetAnimationState()
    {
        if (groupTitle != null) groupTitle.localScale = Vector3.zero;
        CanvasGroup rewardCg = ResolveRewardFrameCanvasGroup();
        if (rewardCg != null) rewardCg.alpha = 0f;
        if (claimX2Btn != null) claimX2Btn.transform.localScale = Vector3.zero;
        if (continueBtn != null) continueBtn.transform.localScale = Vector3.zero;
    }

    public override void Show()
    {
        base.Show();
        SetButtonsInteractable(true);
        ResetAnimationState();
        KillShowSequence();
        _showSequence = DOTween.Sequence();

        if (groupTitle != null)
            _showSequence.Append(groupTitle.DOScale(Vector3.one, bounceDuration).SetEase(bounceEase));

        CanvasGroup rewardCg = ResolveRewardFrameCanvasGroup();
        if (rewardCg != null)
            _showSequence.Append(rewardCg.DOFade(1f, rewardFadeDuration).SetEase(fadeEase));

        if (claimX2Btn != null)
            _showSequence.Append(claimX2Btn.transform.DOScale(Vector3.one, bounceDuration).SetEase(bounceEase));
        if (continueBtn != null)
            _showSequence.Append(continueBtn.transform.DOScale(Vector3.one, bounceDuration).SetEase(bounceEase));

        _showSequence.OnComplete(() => _showSequence = null);
    }

    private CanvasGroup ResolveRewardFrameCanvasGroup()
    {
        if (rewardFrameCanvasGroup != null)
            return rewardFrameCanvasGroup;
        if (rewatdFrame != null)
            return rewatdFrame.GetComponent<CanvasGroup>();
        return null;
    }

    private void KillShowSequence()
    {
        if (_showSequence != null)
        {
            _showSequence.Kill();
            _showSequence = null;
        }
    }

    protected override void OnDestroy()
    {
        KillShowSequence();
        if (groupTitle != null) DOTween.Kill(groupTitle);
        if (rewatdFrame != null) DOTween.Kill(rewatdFrame);
        if (claimX2Btn != null) DOTween.Kill(claimX2Btn.transform);
        if (continueBtn != null) DOTween.Kill(continueBtn.transform);
        base.OnDestroy();
    }

    protected virtual void BindButtons()
    {
        if (continueBtn != null)
            continueBtn.onClick.AddListener(HandleContinueClicked);

        if (claimX2Btn != null)
            claimX2Btn.onClick.AddListener(HandleClaimX2Clicked);
    }

    protected virtual void HandleContinueClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        SetButtonsInteractable(false);
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        if (_coinRewardAmount > 0)
            SoundManager.Instance?.PlayCoinSound();
        GrantCoinsWithOptionalEffect(gm, _coinRewardAmount);
    }

    private void OnCoinEffectComplete()
    {
        Hide();
        if (GameManager.Instance?.uiManager?.StatusUI != null)
            GameManager.Instance.uiManager.StatusUI.Hide();
        GameManager.Instance?.PlayGame();
    }

    private void GrantCoinsWithOptionalEffect(GameManager gm, int amount)
    {
        StatusUI statusUI = gm.uiManager != null ? gm.uiManager.StatusUI : null;
        if (statusUI != null)
        {
            statusUI.AddCoinWithEffect(amount, OnCoinEffectComplete);
            return;
        }

        if (amount > 0)
            gm.AddCoin(amount);
        OnCoinEffectComplete();
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (continueBtn != null) continueBtn.interactable = interactable;
        if (claimX2Btn != null) claimX2Btn.interactable = interactable;
        if (canvasGroup != null) canvasGroup.interactable = interactable;
    }

    protected virtual void HandleClaimX2Clicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        WatchAds();
    }

    public virtual void SetLevel(int level)
    {
        if (levelText != null)
            levelText.text = level.ToString();
    }

    public virtual void SetLevel(string text)
    {
        if (levelText != null)
            levelText.text = text;
    }

    public virtual void SetTitle(string title)
    {
        if (textTitle != null)
            textTitle.text = title;
    }

    /// <summary>
    /// Sets coin reward amount (configurable quantity). Stored for claim on Continue.
    /// </summary>
    public virtual void SetCoinReward(int value)
    {
        _coinRewardAmount = value;
        if (coinValueText != null)
            coinValueText.text = value.ToString();
    }

    /// <summary>
    /// Sets rewards (coins only).
    /// </summary>
    public virtual void SetRewards(int coinAmount)
    {
        SetCoinReward(coinAmount);
    }

    public virtual void SetClaimX2Text(string text)
    {
        if (claimX2Text != null)
            claimX2Text.text = text;
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
            ReceiveX2Coin();
    }

    private void ReceiveX2Coin()
    {
        SetButtonsInteractable(false);
        GameManager gm = GameManager.Instance;
        if (gm == null) return;
        int coinX2 = _coinRewardAmount * 2;
        if (coinX2 > 0)
            SoundManager.Instance?.PlayCoinSound();
        GrantCoinsWithOptionalEffect(gm, coinX2);
    }
}
