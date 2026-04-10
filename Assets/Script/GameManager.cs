using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Game state: HOME, PLAYING, PAUSE, OUT_TIME, GAME_WIN, GAME_FAIL.
/// </summary>
public enum GameState
{
    HOME,
    PLAYING,
    PAUSE,
    OUT_TIME,
    GAME_WIN,
    GAME_FAIL
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    [SerializeField]
    public UIManager uiManager;
    [SerializeField]
    private BoardManager boardManager;
    [Tooltip("Config for initial values (coin, boosters, energy). Leave empty to load from Resources/Configs/GameConfig or use defaults.")]
    [SerializeField]
    private GameConfig gameConfig;

    [Header("Game Data (loaded by LoadGameData)")]
    [SerializeField]
    private string currentLevel = "1";
    [SerializeField]
    private int currentCoin = 0;
    [SerializeField]
    private int currentEnergy = 0;
    private int _boosterFreeze;
    private int _boosterStar;
    private int _boosterMagnet;

    public string CurrentLevel => currentLevel;
    /// <summary>
    /// Level to load for play: when testLevel is true returns testLevelIndex, otherwise currentLevel.
    /// </summary>
    public string GetLevelToLoad()
    {
        return testLevel ? testLevelIndex.ToString() : currentLevel;
    }
    public int CurrentCoin => currentCoin;
    public int CurrentEnergy => currentEnergy;

    [Header("Test Level")]
    [Tooltip("When true, always play the level at testLevelIndex; level is not incremented on win.")]
    [SerializeField]
    private bool testLevel = false;
    [Tooltip("Level index to play when testLevel is true (e.g. 1 = 1.json).")]
    [SerializeField]
    private int testLevelIndex = 1;

    [Header("State")]
    [SerializeField]
    private GameState currentState = GameState.HOME;

    /// <summary>
    /// Current game state: HOME, PLAYING, PAUSE, OUT_TIME, GAME_WIN, GAME_FAIL.
    /// </summary>
    public GameState CurrentState
    {
        get => currentState;
        set => currentState = value;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        LoadGameData();
        Application.targetFrameRate = 60;
    }

    void Start()
    {
        InitGame();
    }

    private const string KeyGameInitialized = "GameInitialized";
    private const string KeyBoosterFreeze = "BoosterFreeze";
    private const string KeyBoosterStar = "BoosterStar";
    private const string KeyBoosterMagnet = "BoosterMagnet";
    private const string KeyEnergyRefillRemaining = "EnergyRefillRemaining";

    private int GetDefaultCoin() => gameConfig != null ? gameConfig.InitialCoin : 200;
    private int GetDefaultEnergyMax() => gameConfig != null ? gameConfig.EnergyMax : 10;
    private int GetDefaultBoosterFreeze() => gameConfig != null ? gameConfig.InitialBoosterFreeze : 3;
    private int GetDefaultBoosterStar() => gameConfig != null ? gameConfig.InitialBoosterStar : 3;
    private int GetDefaultBoosterMagnet() => gameConfig != null ? gameConfig.InitialBoosterMagnet : 1;

    /// <summary>
    /// Loads currentLevel, currentCoin, currentEnergy, booster counts from PlayerPrefs (or from GameConfig / default values).
    /// </summary>
    public void LoadGameData()
    {
        if (gameConfig == null)
            gameConfig = Resources.Load<GameConfig>("Configs/GameConfig");

        if (!PlayerPrefs.HasKey(KeyGameInitialized))
        {
            PlayerPrefs.SetInt(KeyGameInitialized, 1);
            currentLevel = "1";
            currentEnergy = GetDefaultEnergyMax();
            currentCoin = GetDefaultCoin();
            _boosterFreeze = GetDefaultBoosterFreeze();
            _boosterStar = GetDefaultBoosterStar();
            _boosterMagnet = GetDefaultBoosterMagnet();
            PlayerPrefs.SetString("CurrentLevel", currentLevel);
            PlayerPrefs.SetInt("CurrentEnergy", currentEnergy);
            PlayerPrefs.SetInt("CurrentCoin", currentCoin);
            PlayerPrefs.SetInt(KeyBoosterFreeze, _boosterFreeze);
            PlayerPrefs.SetInt(KeyBoosterStar, _boosterStar);
            PlayerPrefs.SetInt(KeyBoosterMagnet, _boosterMagnet);
        }
        else
        {
            currentLevel = PlayerPrefs.GetString("CurrentLevel", "1");
            currentCoin = PlayerPrefs.GetInt("CurrentCoin", 0);
            currentEnergy = PlayerPrefs.GetInt("CurrentEnergy", 0);
            if (currentEnergy < GetEnergyMax() && !PlayerPrefs.HasKey(KeyEnergyRefillRemaining))
                SetRemainingRefillSeconds(GetEnergyRefillSeconds());
            if (!PlayerPrefs.HasKey(KeyBoosterFreeze))
            {
                _boosterFreeze = GetDefaultBoosterFreeze();
                _boosterStar = GetDefaultBoosterStar();
                _boosterMagnet = GetDefaultBoosterMagnet();
                PlayerPrefs.SetInt(KeyBoosterFreeze, _boosterFreeze);
                PlayerPrefs.SetInt(KeyBoosterStar, _boosterStar);
                PlayerPrefs.SetInt(KeyBoosterMagnet, _boosterMagnet);
            }
            else
            {
                _boosterFreeze = PlayerPrefs.GetInt(KeyBoosterFreeze, 0);
                _boosterStar = PlayerPrefs.GetInt(KeyBoosterStar, 0);
                _boosterMagnet = PlayerPrefs.GetInt(KeyBoosterMagnet, 0);
            }
        }
    }

    public int GetBoosterCount(BoosterType type)
    {
        switch (type)
        {
            case BoosterType.Freeze: return _boosterFreeze;
            case BoosterType.Star: return _boosterStar;
            case BoosterType.Magnet: return _boosterMagnet;
            default: return 0;
        }
    }

    /// <summary>
    /// Level required to unlock the given booster (from GameConfig). Player must reach this level for the booster to be usable.
    /// </summary>
    public int GetBoosterUnlockLevel(BoosterType type)
    {
        if (gameConfig == null)
            gameConfig = Resources.Load<GameConfig>("Configs/GameConfig");
        if (gameConfig == null)
            return 1;
        switch (type)
        {
            case BoosterType.Freeze: return gameConfig.UnlockBoosterFreezeLevel;
            case BoosterType.Star: return gameConfig.UnlockBoosterStarLevel;
            case BoosterType.Magnet: return gameConfig.UnlockBoosterMagnetLevel;
            default: return 1;
        }
    }

    /// <summary>
    /// True if current level >= unlock level for the given booster.
    /// </summary>
    public bool IsBoosterUnlocked(BoosterType type)
    {
        int current = 1;
        int.TryParse(currentLevel, out current);
        return current >= GetBoosterUnlockLevel(type);
    }

    public void AddBooster(BoosterType type, int amount)
    {
        switch (type)
        {
            case BoosterType.Freeze: _boosterFreeze += amount; PlayerPrefs.SetInt(KeyBoosterFreeze, _boosterFreeze); break;
            case BoosterType.Star: _boosterStar += amount; PlayerPrefs.SetInt(KeyBoosterStar, _boosterStar); break;
            case BoosterType.Magnet: _boosterMagnet += amount; PlayerPrefs.SetInt(KeyBoosterMagnet, _boosterMagnet); break;
        }
    }

    /// <summary>
    /// Uses one booster of the given type. Returns true if used, false if count was 0.
    /// </summary>
    public bool UseBooster(BoosterType type)
    {
        switch (type)
        {
            case BoosterType.Freeze:
                if (_boosterFreeze <= 0) return false;
                _boosterFreeze--; PlayerPrefs.SetInt(KeyBoosterFreeze, _boosterFreeze); return true;
            case BoosterType.Star:
                if (_boosterStar <= 0) return false;
                _boosterStar--; PlayerPrefs.SetInt(KeyBoosterStar, _boosterStar); return true;
            case BoosterType.Magnet:
                if (_boosterMagnet <= 0) return false;
                _boosterMagnet--; PlayerPrefs.SetInt(KeyBoosterMagnet, _boosterMagnet); return true;
            default: return false;
        }
    }

    /// <summary>
    /// Shows More Booster popup for the given type (Freeze, Star, Magnet). Called from BoosterButton when count is 0.
    /// If currently PLAYING, switches to PAUSE and pauses the timer.
    /// </summary>
    public void ShowMoreBoosterPopup(BoosterType type)
    {
        if (currentState == GameState.PLAYING)
        {
            currentState = GameState.PAUSE;
            if (uiManager?.UiGame != null)
                uiManager.UiGame.PauseTimer();
        }
        SetStatusUIVisible(true, true);
        if (uiManager?.UiMoreBooster != null)
            uiManager.UiMoreBooster.Show(type);
    }

    /// <summary>
    /// Called when More Booster popup is closed. If state is PAUSE, resumes game (PLAYING + timer). Hides StatusUI.
    /// </summary>
    public void OnMoreBoosterClosed()
    {
        if (currentState == GameState.PAUSE)
        {
            currentState = GameState.PLAYING;
            if (uiManager?.UiGame != null)
                uiManager.UiGame.ResumeTimer();
        }
        SetStatusUIVisible(false);
    }

    /// <summary>
    /// Refreshes UIGame booster button counts (e.g. after buying from MoreBooster popup).
    /// </summary>
    public void RefreshBoosterButtonsIfInGame()
    {
        if (uiManager?.UiGame != null)
            uiManager.UiGame.RefreshBoosterButtons();
    }

    /// <summary>
    /// Initializes game: InitHome(), ShowHome(). Subscribes to Home Setting button.
    /// </summary>
    public void InitGame()
    {
        currentState = GameState.HOME;
        uiManager.UiHome.Init();
        uiManager.UiHome.Show();
        SetStatusUIVisible(true, true);
        uiManager.UiHome.OnSettingClicked += HandleSettingClicked;
    }

    private void HandleSettingClicked()
    {
        if (uiManager?.UiSetting != null)
            uiManager.UiSetting.Show(UISetting.SettingMode.Setting);
    }

    private void SetStatusUIVisible(bool visible, bool refresh = false)
    {
        if (uiManager?.StatusUI == null)
            return;
        uiManager.StatusUI.gameObject.SetActive(visible);
        if (refresh)
            uiManager.StatusUI.RefreshFromGameData();
        if (visible)
            uiManager.StatusUI.Show();
        else
            uiManager.StatusUI.Hide();
    }

    private void ReturnHomeUI()
    {
        if (uiManager?.UiHome == null)
            return;
        uiManager.UiHome.Init();
        uiManager.UiHome.Show();
    }

    /// <summary>
    /// Called when Play is tapped from Home. If energy is 0, shows RefillEnergyUI and returns; otherwise starts PlayGame().
    /// </summary>
    public void TryPlayGame()
    {
        if (CurrentEnergy < 1)
        {
            if (uiManager?.UiRefillEnergy != null)
            {
                uiManager.UiRefillEnergy.gameObject.SetActive(true);
                uiManager.UiRefillEnergy.Show();
            }
            return;
        }
        PlayGame();
    }

    /// <summary>
    /// Called on Play: hide Home, Init + Show GameUI, Show LoadingScene, Init BoardManager, start countdown from level TimeLimit.
    /// </summary>
    public void PlayGame()
    {
        currentState = GameState.PLAYING;
        if (uiManager?.UiHome != null)
            uiManager.UiHome.Hide();
        SetStatusUIVisible(false);

        if (uiManager?.UiGame != null)
        {
            uiManager.UiGame.Init();
            uiManager.UiGame.Show();
            uiManager.UiGame.OnOutOfTime -= HandleOutOfTime;
            uiManager.UiGame.OnOutOfTime += HandleOutOfTime;
        }
        uiManager.UiLoadingScene.Show();

        if (boardManager != null)
            boardManager.Init();

        if (uiManager?.UiGame != null && boardManager != null)
        {
            int timeLimit = boardManager.LevelData != null ? boardManager.LevelData.TimeLimit : 60;
            uiManager.UiGame.StartCountdown(timeLimit);
        }
    }

    /// <summary>
    /// Called when Pause is clicked in-game (from UIGame). Shows Setting popup in Pause mode and pauses timer.
    /// </summary>
    public void ShowPauseSetting()
    {
        if (uiManager?.UiSetting != null)
        {
            currentState = GameState.PAUSE;
            uiManager.UiSetting.Show(UISetting.SettingMode.Pause);
            if (uiManager.UiGame != null)
                uiManager.UiGame.PauseTimer();
        }
    }

    /// <summary>
    /// Called when Close is clicked on Setting popup in Pause mode. Hides Setting, resumes timer, sets state back to PLAYING.
    /// </summary>
    public void ResumeGameFromPause()
    {
        currentState = GameState.PLAYING;
        if (uiManager?.UiSetting != null)
            uiManager.UiSetting.Hide();
        if (uiManager?.UiGame != null)
            uiManager.UiGame.ResumeTimer();
    }

    /// <summary>
    /// Opens support / homepage URL (from GameConfig or default).
    /// </summary>
    public void OpenSupport()
    {
        string url = gameConfig != null ? gameConfig.SupportUrl : "https://your-website.com";
        if (!string.IsNullOrEmpty(url))
            Application.OpenURL(url);
    }

    /// <summary>
    /// Opens Google Play store page (Android) or App Store (iOS) for rating. Uses bundle ID from GameConfig or Application.identifier.
    /// </summary>
    public void OpenRate()
    {
#if UNITY_ANDROID
        string packageName = gameConfig != null ? gameConfig.GooglePlayPackageName : Application.identifier;
        if (!string.IsNullOrEmpty(packageName))
        {
            string url = "https://play.google.com/store/apps/details?id=" + packageName;
            Application.OpenURL(url);
        }
#elif UNITY_IOS
        // TODO: set your App Store ID for iOS
        // string appStoreId = "YOUR_APP_STORE_ID";
        // Application.OpenURL("https://apps.apple.com/app/id" + appStoreId);
#endif
    }

    /// <summary>
    /// Called when timer reaches 00:00. Stops game, hides UIGame, shows UIOutOfTime.
    /// </summary>
    private void HandleOutOfTime()
    {
        if (boardManager != null)
            boardManager.StopFreeze();
        currentState = GameState.OUT_TIME;
        if (uiManager?.UiGame != null)
        {
            uiManager.UiGame.OnOutOfTime -= HandleOutOfTime;
            uiManager.UiGame.StopCountdown();
            uiManager.UiGame.Hide();
        }
        if (uiManager?.UiOutOfTime != null)
        {
            uiManager.UiOutOfTime.gameObject.SetActive(true);
            uiManager.UiOutOfTime.Show();
        }
        SetStatusUIVisible(true, true);
        SoundManager.Instance?.PlayLose();
        StartCoroutine(ShowInterstitialAfterDelay());
    }

    /// <summary>
    /// Waits 1 second then shows interstitial ad (Game Win / Game Lose). Used after HandleOutOfTime or CheckWinCondition.
    /// </summary>
    private IEnumerator ShowInterstitialAfterDelay()
    {
        yield return new WaitForSeconds(2.0f);
        if (AdsControl.Instance != null)
            AdsControl.Instance.ShowInterstital();
    }

    /// <summary>
    /// Increments current level and saves to PlayerPrefs. Call when player wins (before showing UIWin).
    /// No-op when testLevel is true.
    /// </summary>
    public void IncrementAndSaveLevel()
    {
        if (testLevel)
            return;
        int next = 1;
        int.TryParse(currentLevel, out next);
        next++;
        currentLevel = next.ToString();
        PlayerPrefs.SetString("CurrentLevel", currentLevel);
    }

    /// <summary>
    /// Called when Close is clicked on OutOfTime popup. Hides OutOfTime, sets state to HOME, shows Home.
    /// </summary>
    public void ReturnToHomeFromOutOfTime()
    {
        currentState = GameState.HOME;
        if (uiManager?.UiOutOfTime != null)
            uiManager.UiOutOfTime.Hide();
        SetStatusUIVisible(true, true);
        ReturnHomeUI();
    }

    /// <summary>
    /// Called when Close is clicked on UIOutOfTime. Hides UIOutOfTime and shows UIFail.
    /// </summary>
    public void ShowFailFromOutOfTime()
    {
        currentState = GameState.GAME_FAIL;
        if (uiManager?.UiOutOfTime != null)
            uiManager.UiOutOfTime.Hide();
        if (uiManager?.UIFail != null)
            uiManager.UIFail.Show();
    }

    /// <summary>
    /// Called when Close is clicked on Fail UI. Hides UIFail, sets state to HOME, shows Home.
    /// </summary>
    public void ReturnToHomeFromFail()
    {
        currentState = GameState.HOME;
        if (uiManager?.UIFail != null)
            uiManager.UIFail.Hide();
        if (boardManager != null)
            boardManager.CleanBoard();
        SetStatusUIVisible(true, true);
        ReturnHomeUI();
    }

    /// <summary>
    /// Called when Replay is clicked on Fail UI. Hides UIFail, sets state to PLAYING, restarts level (PlayGame).
    /// </summary>
    public void ReplayLevelFromFail()
    {
        if (boardManager != null)
            boardManager.StopFreeze();
        if (uiManager?.UIFail != null)
            uiManager.UIFail.Hide();
        PlayGame();
    }

    /// <summary>
    /// Called when Replay is clicked in Pause mode (UISetting). Hides Setting, shows UILostEnergy.
    /// </summary>
    public void ShowLostEnergyFromPause()
    {
        if (uiManager?.UiSetting != null)
            uiManager.UiSetting.Hide();
        if (uiManager?.UiLostEnergy != null)
        {
            uiManager.UiLostEnergy.gameObject.SetActive(true);
            uiManager.UiLostEnergy.Show();
        }
    }

    /// <summary>
    /// Called when Resume is clicked on LostEnergy popup. Returns to Home, updates currentState.
    /// </summary>
    public void OnLostEnergyResume()
    {
        if (!SpendEnergy(1))
            return;
        if (boardManager != null)
            boardManager.CleanBoard();
        if (uiManager?.UiGame != null)
            uiManager.UiGame.Hide();
        currentState = GameState.HOME;
        if (uiManager?.UiLostEnergy != null)
            uiManager.UiLostEnergy.Hide();
        SetStatusUIVisible(true, true);
        ReturnHomeUI();
    }

    /// <summary>
    /// Called when Replay is clicked on LostEnergy popup. Deducts 1 energy, saves, then replays game.
    /// </summary>
    public void OnLostEnergyReplay()
    {
        if (!SpendEnergy(1))
            return;
        if (boardManager != null)
            boardManager.StopFreeze();
        if (uiManager?.UiLostEnergy != null)
            uiManager.UiLostEnergy.Hide();
        SetStatusUIVisible(true, true);
        PlayGame();
    }

    /// <summary>
    /// Called when RefillAds is clicked on RefillEnergyUI. Adds 1 energy, refreshes StatusUI.
    /// </summary>
    public void OnRefillEnergyByAds()
    {
        AddEnergy(1);
        SetStatusUIVisible(true, true);
    }

    /// <summary>
    /// Called when RefillCoins is clicked on RefillEnergyUI. If coins >= 900: deducts 900, adds 3 energy, refreshes StatusUI, returns true; otherwise returns false.
    /// </summary>
    public bool TryRefillEnergyByCoins()
    {
        if (GetCoin() < RefillEnergyCoinsCost)
        {
            ShowNotEnoughCoinsToast();
            return false;
        }
        AddCoin(-RefillEnergyCoinsCost);
        AddEnergy(RefillEnergyCoinsAmount);
        if (uiManager?.StatusUI != null)
        {
            uiManager.StatusUI.UpdateCoinValueText();
            uiManager.StatusUI.RefreshFromGameData();
        }
        return true;
    }

    /// <summary>
    /// Called when Close is clicked on RefillEnergyUI. Sets state to HOME, shows Home and StatusUI.
    /// </summary>
    public void OnRefillEnergyClose()
    {
        currentState = GameState.HOME;
        SetStatusUIVisible(true, true);
        ReturnHomeUI();
    }

    /// <summary>
    /// Spends energy. Returns true if had enough and deducted, false otherwise.
    /// When energy &lt; max after deducting, starts refill timer if not already running.
    /// </summary>
    public bool SpendEnergy(int amount)
    {
        if (currentEnergy < amount)
            return false;
        currentEnergy -= amount;
        PlayerPrefs.SetInt("CurrentEnergy", currentEnergy);
        if (currentEnergy < GetEnergyMax() && GetRemainingRefillSeconds() <= 0)
            SetRemainingRefillSeconds(GetEnergyRefillSeconds());
        return true;
    }

    /// <summary>
    /// Energy max from GameConfig (or 10 if no config).
    /// </summary>
    public int GetEnergyMax()
    {
        return GetDefaultEnergyMax();
    }

    /// <summary>
    /// Time to refill 1 energy (seconds) from GameConfig.
    /// </summary>
    public int GetEnergyRefillSeconds()
    {
        return gameConfig != null ? gameConfig.EnergyRefillSeconds : 300;
    }

    /// <summary>
    /// Remaining seconds until refill +1 energy. Used for mm:ss countdown on StatusUI.
    /// </summary>
    public int GetRemainingRefillSeconds()
    {
        return PlayerPrefs.GetInt(KeyEnergyRefillRemaining, 0);
    }

    /// <summary>
    /// Sets remaining refill seconds (StatusUI calls when counting down or when refill completes).
    /// </summary>
    public void SetRemainingRefillSeconds(int seconds)
    {
        PlayerPrefs.SetInt(KeyEnergyRefillRemaining, Mathf.Max(0, seconds));
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Adds energy (capped at GetEnergyMax()). Called when refill countdown reaches 00:00.
    /// </summary>
    public void AddEnergy(int amount)
    {
        int max = GetEnergyMax();
        currentEnergy = Mathf.Min(currentEnergy + amount, max);
        PlayerPrefs.SetInt("CurrentEnergy", currentEnergy);
        if (currentEnergy < max)
            SetRemainingRefillSeconds(GetEnergyRefillSeconds());
        else
            SetRemainingRefillSeconds(0);
    }

    private const int OutOfTimeClaimCost = 900;
    private const int OutOfTimeClaimBonusSeconds = 30;
    private const int RefillEnergyCoinsCost = 900;
    private const int RefillEnergyCoinsAmount = 3;

    private const int WinCoinReward = 100;

    /// <summary>
    /// Called when Claim is clicked on UIOutOfTime. If enough coins: deduct 900, hide OutOfTime, add 30s, resume game, set state to PLAYING.
    /// </summary>
    public void RequestOutOfTimeClaimReward()
    {
        if (GetCoin() < OutOfTimeClaimCost)
        {
            ShowNotEnoughCoinsToast();
            return;
        }
        AddCoin(-OutOfTimeClaimCost);
        if (uiManager?.StatusUI != null)
            uiManager.StatusUI.UpdateCoinValueText();
        SetStatusUIVisible(false);
        if (uiManager?.UiOutOfTime != null)
            uiManager.UiOutOfTime.Hide();
        if (uiManager?.UiGame != null)
        {
            uiManager.UiGame.OnOutOfTime -= HandleOutOfTime;
            uiManager.UiGame.OnOutOfTime += HandleOutOfTime;
            uiManager.UiGame.AddTime(OutOfTimeClaimBonusSeconds);
            uiManager.UiGame.Show();
        }
        currentState = GameState.PLAYING;
    }

    /// <summary>
    /// Called when a BoxObject is about to be destroyed. If last box: set state GAME_WIN, stop timer, hide UIGame, increment level, show UIWin.
    /// </summary>
    public void CheckWinCondition()
    {
        if (boardManager == null || uiManager?.UiWin == null)
            return;
        if (boardManager.GetBoxObjectCount() == 1)
        {
            if (boardManager != null)
                boardManager.StopFreeze();
            currentState = GameState.GAME_WIN;
            if (uiManager.UiGame != null)
            {
                uiManager.UiGame.OnOutOfTime -= HandleOutOfTime;
                uiManager.UiGame.StopCountdown();
                uiManager.UiGame.Hide();
            }
            int completedLevel = 1;
            if (testLevel)
                completedLevel = testLevelIndex;
            else
                int.TryParse(currentLevel, out completedLevel);
            IncrementAndSaveLevel();
            uiManager.UiWin.SetLevel(completedLevel);
            uiManager.UiWin.SetRewards(WinCoinReward);
            uiManager.UiWin.Show();
            SoundManager.Instance?.PlayWin();
            StartCoroutine(ShowInterstitialAfterDelay());
            if (uiManager.StatusUI != null)
                SetStatusUIVisible(true, true);
        }
    }

    /// <summary>
    /// Called when Continue is clicked on UIWin (after coin effect). Hides UIWin, sets state to PLAYING (next level).
    /// </summary>
    public void ContinueFromWin()
    {
        currentState = GameState.PLAYING;
        if (uiManager?.UiWin != null)
            uiManager.UiWin.Hide();
        SetStatusUIVisible(false);
    }

    /// <summary>
    /// Triggers freeze booster: pause timer + snowLayer (called from UIGame when freeze booster is pressed).
    /// </summary>
    public void TriggerFreeze()
    {
        if (boardManager != null)
            boardManager.UseFreeze();
    }

    /// <summary>
    /// Triggers star booster: pick one cat on board and call CatJumpToHoleByStar (called from UIGame when star booster is pressed).
    /// </summary>
    public void TriggerStarBooster()
    {
        if (boardManager != null)
            boardManager.UseStarBooster();
    }

    /// <summary>
    /// Triggers magnet booster: pull matching-color cats to box (called from UIGame when magnet booster is pressed).
    /// </summary>
    public void TriggerMagnet()
    {
        if (boardManager != null)
            boardManager.UseMagnet();
    }

    void Update()
    {
        // Test: press G to add 10000 coins
        if (Input.GetKeyDown(KeyCode.G))
        {
            AddCoin(10000);
            if (uiManager?.StatusUI != null)
                uiManager.StatusUI.RefreshFromGameData();
        }
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public int GetCoin()
    {
        return currentCoin;
    }

    public void AddCoin(int coinAmount)
    {
        currentCoin += coinAmount;
        PlayerPrefs.SetInt("CurrentCoin", currentCoin);
    }

    /// <summary>
    /// Shows a toast message when the player does not have enough coins.
    /// Used by RefillEnergyUI and UIOutOfTime flows.
    /// </summary>
    public void ShowNotEnoughCoinsToast()
    {
        if (uiManager?.ToastUI != null)
        {
            uiManager.ToastUI.Show("Not enough coins.");
        }
    }
}
