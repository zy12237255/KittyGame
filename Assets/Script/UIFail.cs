using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Popup shown when the player fails a level. Inherits from BasePopup.
/// </summary>
public class UIFail : BasePopup
{
    [Header("Fail UI")]
    [SerializeField] protected TextMeshProUGUI textTitle;
    [SerializeField] protected Button buttonClose;
    [SerializeField] protected TextMeshProUGUI textInfo;
    [SerializeField] protected TextMeshProUGUI energyText;

    [Header("Replay")]
    [SerializeField] protected Button replayBtn;
    [SerializeField] protected TextMeshProUGUI replayText;

    protected override void Awake()
    {
        base.Awake();
        BindButtons();
    }

    protected virtual void BindButtons()
    {
        if (buttonClose != null)
            buttonClose.onClick.AddListener(HandleCloseClicked);
        if (replayBtn != null)
            replayBtn.onClick.AddListener(HandleReplayClicked);
    }

    protected virtual void HandleCloseClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        Hide();
        if (GameManager.Instance != null)
            GameManager.Instance.ReturnToHomeFromFail();
    }

    protected virtual void HandleReplayClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        Hide();
        if (GameManager.Instance != null)
            GameManager.Instance.ReplayLevelFromFail();
    }

    public virtual void SetTitle(string title)
    {
        if (textTitle != null)
            textTitle.text = title;
    }

    public virtual void SetInfo(string info)
    {
        if (textInfo != null)
            textInfo.text = info;
    }

    public virtual void SetEnergyText(string text)
    {
        if (energyText != null)
            energyText.text = text;
    }

    public virtual void SetReplayText(string text)
    {
        if (replayText != null)
            replayText.text = text;
    }
}
