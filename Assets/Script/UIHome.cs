using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIHome : BasePanel
{
    [Header("Top")]
    [SerializeField] protected Button settingBtn;
    [SerializeField] protected RectTransform border;

    [Header("Play")]
    [SerializeField] protected Button playBtn;
    [SerializeField] protected TextMeshProUGUI playText;

    [Header("Level")]
    [SerializeField] protected RectTransform levelBorder;
    [SerializeField] protected TextMeshProUGUI levelValueText;

    public System.Action OnPlayClicked;
    public System.Action OnSettingClicked;

    protected override void Awake()
    {
        base.Awake();
        BindButtons();
    }

    public virtual void Init()
    {
        SetLevel();
    }

    protected virtual void BindButtons()
    {
        if (settingBtn != null)
            settingBtn.onClick.AddListener(HandleSettingClicked);

        if (playBtn != null)
            playBtn.onClick.AddListener(HandlePlayClicked);
    }

    protected virtual void HandlePlayClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        OnPlayClicked?.Invoke();
        GameManager.Instance?.TryPlayGame();
    }

    protected virtual void HandleSettingClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        OnSettingClicked?.Invoke();
    }

    public virtual void SetLevel()
    {
        if (levelValueText == null || GameManager.Instance == null)
            return;
        levelValueText.text = GameManager.Instance.CurrentLevel;
    }

    public virtual void SetPlayText(string text)
    {
        if (playText != null)
            playText.text = text;
    }
}
