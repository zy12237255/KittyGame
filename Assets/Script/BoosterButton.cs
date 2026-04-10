using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Booster button: lock state by level (GameConfig unlock level); when unlocked: count > 0 show Number, count = 0 show Mask+Plus; click uses booster or shows MoreBooster.
/// </summary>
public class BoosterButton : MonoBehaviour
{
    [SerializeField] private BoosterType boosterType;
    [SerializeField] private Button button;
    [SerializeField] private GameObject mask;
    [SerializeField] private GameObject plus;
    [SerializeField] private GameObject number;
    [SerializeField] private TextMeshProUGUI textCount;
    [Tooltip("Shown when locked: displays \"LV\" + unlock level. Hidden when unlocked.")]
    [SerializeField] private TextMeshProUGUI unlockText;

    public System.Action<BoosterType> OnUseBooster;

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(HandleClick);
    }

    private void Start()
    {
        RefreshFromGameData();
    }

    /// <summary>
    /// Refreshes display from GameManager: lock state (unlock level) and booster count.
    /// </summary>
    public void RefreshFromGameData()
    {
        bool unlocked = GameManager.Instance != null && GameManager.Instance.IsBoosterUnlocked(boosterType);
        int count = GameManager.Instance != null ? GameManager.Instance.GetBoosterCount(boosterType) : 0;
        SetLockAndCount(unlocked, count);
    }

    /// <summary>
    /// Sets lock state and count. When locked: show mask, UnlockText "LV"+level, hide plus/number, button not interactable. When unlocked: hide mask+UnlockText, show number or plus by count.
    /// </summary>
    public void SetLockAndCount(bool unlocked, int count)
    {
        if (button != null)
            button.interactable = unlocked;
        if (!unlocked)
        {
            if (mask != null) mask.SetActive(true);
            if (unlockText != null)
            {
                unlockText.gameObject.SetActive(true);
                int levelUnlock = GameManager.Instance != null ? GameManager.Instance.GetBoosterUnlockLevel(boosterType) : 1;
                unlockText.text = "LV" + levelUnlock;
            }
            if (plus != null) plus.SetActive(false);
            if (number != null) number.SetActive(false);
            return;
        }
        if (unlockText != null) unlockText.gameObject.SetActive(false);
        bool hasCount = count > 0;
        if (mask != null) mask.SetActive(!hasCount);
        if (plus != null) plus.SetActive(!hasCount);
        if (number != null) number.SetActive(hasCount);
        if (textCount != null) textCount.text = count.ToString();
    }

    /// <summary>
    /// Sets count and updates visibility (assumes booster is unlocked). count > 0 => hide mask+plus, show number; count = 0 => show mask+plus, hide number.
    /// </summary>
    public void SetCount(int count)
    {
        bool unlocked = GameManager.Instance != null && GameManager.Instance.IsBoosterUnlocked(boosterType);
        SetLockAndCount(unlocked, count);
    }

    private void HandleClick()
    {
        if (GameManager.Instance == null) return;
        if (!GameManager.Instance.IsBoosterUnlocked(boosterType))
            return;
        SoundManager.Instance?.PlayButtonClick();
        int count = GameManager.Instance.GetBoosterCount(boosterType);
        if (count > 0)
        {
            if (GameManager.Instance.UseBooster(boosterType))
            {
                SetCount(count - 1);
                OnUseBooster?.Invoke(boosterType);
            }
        }
        else
        {
            // Count = 0: show More Booster popup only when user taps to "use" with no booster left
            GameManager.Instance.ShowMoreBoosterPopup(boosterType);
        }
    }
}
