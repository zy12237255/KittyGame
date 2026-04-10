using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Setting popup with two modes: Pause (from in-game pause) and Setting (from Home screen).
/// </summary>
public class UISetting : BasePopup
{
    public enum SettingMode
    {
        Pause,
        Setting
    }

    [Header("Setting UI")]
    [SerializeField] protected RectTransform dimed;
    [SerializeField] protected RectTransform bg;
    [SerializeField] protected TextMeshProUGUI textTitle;
    [SerializeField] protected Button buttonClose;

    [Header("Common (SFX, Music)")]
    [SerializeField] protected RectTransform sfx;
    [SerializeField] protected RectTransform music;
    [Tooltip("Slider under SFX group (e.g. Slider_HandleType01). Range 0-1.")]
    [SerializeField] protected Slider sliderSFX;
    [Tooltip("Slider under Music group (e.g. Slider_HandleType01). Range 0-1.")]
    [SerializeField] protected Slider sliderMusic;

    [Header("Setting mode only (from Home)")]
    [SerializeField] protected RectTransform groupButtonsSetting;
    [SerializeField] protected Button buttonSupport;
    [SerializeField] protected Button buttonRate;

    [Header("Pause mode only (from in-game)")]
    [SerializeField] protected RectTransform groupButtonsPause;
    [SerializeField] protected Button buttonReplay;

    private SettingMode _currentMode;

    protected override void Awake()
    {
        base.Awake();
        BindButtons();
        BindSliders();
    }

    protected virtual void BindButtons()
    {
        if (buttonClose != null)
            buttonClose.onClick.AddListener(HandleCloseClicked);
        if (buttonSupport != null)
            buttonSupport.onClick.AddListener(HandleSupportClicked);
        if (buttonRate != null)
            buttonRate.onClick.AddListener(HandleRateClicked);
        if (buttonReplay != null)
            buttonReplay.onClick.AddListener(HandleReplayClicked);
    }

    protected virtual void BindSliders()
    {
        if (sliderSFX != null)
        {
            sliderSFX.minValue = 0f;
            sliderSFX.maxValue = 1f;
            sliderSFX.onValueChanged.AddListener(HandleSFXVolumeChanged);
        }
        if (sliderMusic != null)
        {
            sliderMusic.minValue = 0f;
            sliderMusic.maxValue = 1f;
            sliderMusic.onValueChanged.AddListener(HandleMusicVolumeChanged);
        }
    }

    protected virtual void HandleSFXVolumeChanged(float value)
    {
        SoundManager.Instance?.SetSFXVolume(value);
    }

    protected virtual void HandleMusicVolumeChanged(float value)
    {
        SoundManager.Instance?.SetMusicVolume(value);
    }

    /// <summary>
    /// Show setting popup in the given mode. Pause: from in-game pause. Setting: from Home screen.
    /// Loads saved SFX/Music volume into sliders.
    /// </summary>
    public void Show(SettingMode mode)
    {
        _currentMode = mode;
        if (textTitle != null)
            textTitle.text = mode == SettingMode.Pause ? "Paused" : "Settings";
        if (groupButtonsSetting != null)
            groupButtonsSetting.gameObject.SetActive(mode == SettingMode.Setting);
        if (groupButtonsPause != null)
            groupButtonsPause.gameObject.SetActive(mode == SettingMode.Pause);
        LoadSliderValues();
        base.Show();
    }

    /// <summary>
    /// Load saved volume from SoundManager into sliders (without triggering onValueChanged).
    /// </summary>
    protected virtual void LoadSliderValues()
    {
        if (SoundManager.Instance == null) return;
        if (sliderSFX != null)
            sliderSFX.SetValueWithoutNotify(SoundManager.Instance.GetSFXVolume());
        if (sliderMusic != null)
            sliderMusic.SetValueWithoutNotify(SoundManager.Instance.GetMusicVolume());
    }

    public override void Show()
    {
        Show(_currentMode);
    }

    protected virtual void HandleCloseClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        Hide();
        if (_currentMode == SettingMode.Pause && GameManager.Instance != null)
            GameManager.Instance.ResumeGameFromPause();
    }

    protected virtual void HandleSupportClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        if (GameManager.Instance != null)
            GameManager.Instance.OpenSupport();
    }

    protected virtual void HandleRateClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        if (GameManager.Instance != null)
            GameManager.Instance.OpenRate();
    }

    protected virtual void HandleReplayClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        if (_currentMode != SettingMode.Pause)
            return;
        Hide();
        if (GameManager.Instance != null)
            GameManager.Instance.ShowLostEnergyFromPause();
    }

    public void SetTitle(string title)
    {
        if (textTitle != null)
            textTitle.text = title;
    }
}
