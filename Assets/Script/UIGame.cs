using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIGame : BasePanel
{
    [Header("Top")]
    [SerializeField] protected RectTransform banner;
    [SerializeField] protected Button pauseBtn;
    [SerializeField] protected RectTransform timer;
    [SerializeField] protected TextMeshProUGUI timerValueText;
    [SerializeField] protected RectTransform level;
    [SerializeField] protected TextMeshProUGUI levelValueText;

    [Header("Bottom - Boosters")]
    [SerializeField] protected BoosterButton starBooster;
    [SerializeField] protected BoosterButton freezeBooster;
    [SerializeField] protected BoosterButton magnetBooster;

    [Header("Tutorial")]
    [SerializeField] protected GameObject tutorialPanel;

    public System.Action OnStarBoosterClicked;
    public System.Action OnFreezeBoosterClicked;
    public System.Action OnMagnetBoosterClicked;
    public System.Action OnOutOfTime;

    private Coroutine _countdownCoroutine;
    private int _remainingSeconds;
    private bool _timerRunning;
    private bool _timerPaused;
    private bool _tutorialActive;

    protected override void Awake()
    {
        base.Awake();
        BindButtons();
    }

    protected virtual void BindButtons()
    {
        if (pauseBtn != null)
            pauseBtn.onClick.AddListener(HandlePauseClicked);

        if (starBooster != null)
            starBooster.OnUseBooster += _ =>
            {
                OnStarBoosterClicked?.Invoke();
                if (GameManager.Instance != null)
                    GameManager.Instance.TriggerStarBooster();
            };
        if (freezeBooster != null)
            freezeBooster.OnUseBooster += _ =>
            {
                OnFreezeBoosterClicked?.Invoke();
                if (GameManager.Instance != null)
                    GameManager.Instance.TriggerFreeze();
            };
        if (magnetBooster != null)
            magnetBooster.OnUseBooster += _ =>
            {
                OnMagnetBoosterClicked?.Invoke();
                if (GameManager.Instance != null)
                    GameManager.Instance.TriggerMagnet();
            };
    }

    protected virtual void HandlePauseClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        if (GameManager.Instance != null)
            GameManager.Instance.ShowPauseSetting();
    }

    /// <summary>
    /// Initializes UI Game (level display, booster counts). Called from GameManager.PlayGame(). Timer is started via StartCountdown(timeLimit).
    /// </summary>
    public virtual void Init()
    {
        int levelNum = 1;
        if (GameManager.Instance != null)
        {
            int.TryParse(GameManager.Instance.GetLevelToLoad(), out levelNum);
            SetLevel(levelNum);
        }
        // Chỉ bật tutorial ở level 1
        if (tutorialPanel != null)
        {
            _tutorialActive = (levelNum == 1);
            tutorialPanel.SetActive(_tutorialActive);
        }
        else
        {
            _tutorialActive = false;
        }
        RefreshBoosterButtons();
    }

    private void Update()
    {
        if (!_tutorialActive) return;
        // Chạm màn hình (mobile) hoặc click chuột (editor) -> tắt tutorial
        bool touched = (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            || Input.GetMouseButtonDown(0);
        if (touched)
        {
            _tutorialActive = false;
            if (tutorialPanel != null)
                tutorialPanel.SetActive(false);
        }
    }

    /// <summary>
    /// Refreshes all booster buttons from GameManager counts.
    /// </summary>
    public virtual void RefreshBoosterButtons()
    {
        if (starBooster != null) starBooster.RefreshFromGameData();
        if (freezeBooster != null) freezeBooster.RefreshFromGameData();
        if (magnetBooster != null) magnetBooster.RefreshFromGameData();
    }

    /// <summary>
    /// Starts countdown timer with format "mm:ss". When it reaches 00:00, stops and invokes OnOutOfTime.
    /// </summary>
    public virtual void StartCountdown(int totalSeconds)
    {
        StopCountdown();
        _remainingSeconds = Mathf.Max(0, totalSeconds);
        _timerRunning = true;
        UpdateTimerDisplay();
        _countdownCoroutine = StartCoroutine(CountdownRoutine());
    }

    /// <summary>
    /// Stops the countdown timer.
    /// </summary>
    public virtual void StopCountdown()
    {
        _timerRunning = false;
        _timerPaused = false;
        if (_countdownCoroutine != null)
        {
            StopCoroutine(_countdownCoroutine);
            _countdownCoroutine = null;
        }
    }

    /// <summary>
    /// Adds seconds to the timer and resumes countdown if it was stopped (e.g. after buying time).
    /// </summary>
    public virtual void AddTime(int seconds)
    {
        _remainingSeconds += seconds;
        UpdateTimerDisplay();
        if (!_timerRunning)
        {
            _timerRunning = true;
            _countdownCoroutine = StartCoroutine(CountdownRoutine());
        }
    }

    /// <summary>
    /// Pauses the timer count (e.g. when using freeze). Call ResumeTimer() to continue.
    /// </summary>
    public virtual void PauseTimer()
    {
        _timerPaused = true;
    }

    /// <summary>
    /// Resumes the timer count after pause (e.g. when freeze ends).
    /// </summary>
    public virtual void ResumeTimer()
    {
        _timerPaused = false;
    }

    public bool IsTimerPaused => _timerPaused;

    private IEnumerator CountdownRoutine()
    {
        while (_timerRunning && _remainingSeconds > 0)
        {
            yield return new WaitForSeconds(1f);
            if (!_timerRunning) yield break;
            if (_timerPaused)
                continue;
            _remainingSeconds--;
            UpdateTimerDisplay();
            if (_remainingSeconds <= 0)
            {
                _timerRunning = false;
                OnOutOfTime?.Invoke();
                yield break;
            }
        }
    }

    private void UpdateTimerDisplay()
    {
        int minutes = _remainingSeconds / 60;
        int seconds = _remainingSeconds % 60;
        string formatted = $"{minutes:D2}:{seconds:D2}";
        SetTimer(formatted);
    }

    public virtual void SetLevel(int levelValue)
    {
        if (levelValueText != null)
            levelValueText.text = levelValue.ToString();
    }

    public virtual void SetTimer(string value)
    {
        if (timerValueText != null)
            timerValueText.text = value;
    }

    public virtual void SetTimer(int seconds)
    {
        int m = seconds / 60;
        int s = seconds % 60;
        SetTimer($"{m:D2}:{s:D2}");
    }

    protected override void OnDestroy()
    {
        StopCountdown();
        base.OnDestroy();
    }
}
