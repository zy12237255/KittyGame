using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        buttonClose?.onClick.AddListener(HandleCloseClicked);
        buttonSupport?.onClick.AddListener(HandleSupportClicked);
        buttonRate?.onClick.AddListener(HandleRateClicked);
        buttonReplay?.onClick.AddListener(HandleReplayClicked);
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

    public void Show(SettingMode mode)
    {
        _currentMode = mode;
        ApplyModeLayout(mode);
        LoadSliderValues();
        base.Show();
    }

    private void ApplyModeLayout(SettingMode mode)
    {
        bool isPause = mode == SettingMode.Pause;
        if (textTitle != null)
            textTitle.text = isPause ? "Paused" : "Settings";
        if (groupButtonsSetting != null)
            groupButtonsSetting.gameObject.SetActive(!isPause);
        if (groupButtonsPause != null)
            groupButtonsPause.gameObject.SetActive(isPause);
    }

    /// <summary>
    /// Load saved volume from SoundManager into sliders (without triggering onValueChanged).
    /// </summary>
    protected virtual void LoadSliderValues()
    {
        SoundManager sm = SoundManager.Instance;
        if (sm == null)
            return;
        if (sliderSFX != null)
            sliderSFX.SetValueWithoutNotify(sm.GetSFXVolume());
        if (sliderMusic != null)
            sliderMusic.SetValueWithoutNotify(sm.GetMusicVolume());
    }

    public override void Show()
    {
        Show(_currentMode);
    }

    protected virtual void HandleCloseClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        Hide();
        if (_currentMode == SettingMode.Pause)
            GameManager.Instance?.ResumeGameFromPause();
    }

    protected virtual void HandleSupportClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        GameManager.Instance?.OpenSupport();
    }

    protected virtual void HandleRateClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        GameManager.Instance?.OpenRate();
    }

    protected virtual void HandleReplayClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        if (_currentMode != SettingMode.Pause)
            return;
        Hide();
        GameManager.Instance?.ShowLostEnergyFromPause();
    }

    public void SetTitle(string title)
    {
        if (textTitle != null)
            textTitle.text = title;
    }
}
