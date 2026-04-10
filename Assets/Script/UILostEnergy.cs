using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Popup shown when the player has no energy (e.g. tried to play without enough energy). Inherits from BasePopup.
/// </summary>
public class UILostEnergy : BasePopup
{
    [Header("LostEnergy UI")]
    [SerializeField] protected RectTransform dimed;
    [SerializeField] protected RectTransform bg;
    [SerializeField] protected TextMeshProUGUI textTitle;
    [SerializeField] protected TextMeshProUGUI textInfo;
    [SerializeField] protected TextMeshProUGUI energyText;

    [Header("Buttons")]
    [SerializeField] protected Button resumeBtn;
    [SerializeField] protected Button replayBtn;

    [Header("Decoration")]
    [SerializeField] protected RectTransform icon;
    [SerializeField] protected RectTransform line;

    protected override void Awake()
    {
        base.Awake();
        BindButtons();
    }

    protected virtual void BindButtons()
    {
        if (resumeBtn != null)
            resumeBtn.onClick.AddListener(HandleResumeClicked);
        if (replayBtn != null)
            replayBtn.onClick.AddListener(HandleReplayClicked);
    }

    protected virtual void HandleResumeClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        Hide();
        if (GameManager.Instance != null)
            GameManager.Instance.OnLostEnergyResume();
    }

    protected virtual void HandleReplayClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        Hide();
        if (GameManager.Instance != null)
            GameManager.Instance.OnLostEnergyReplay();
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
}
