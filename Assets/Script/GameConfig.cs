using UnityEngine;

/// <summary>
/// Config for initial values: coin, boosters, energy max.
/// Create asset: Right-click in Project → Create → Config → Game Config.
/// Place asset in Resources/Configs/GameConfig to load by code, or assign to GameManager.
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "Config/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("Coin")]
    [Tooltip("Initial coin count when playing for the first time.")]
    [SerializeField]
    private int initialCoin = 200;

    [Header("Boosters (initial count)")]
    [SerializeField]
    private int initialBoosterFreeze = 3;
    [SerializeField]
    private int initialBoosterStar = 3;
    [SerializeField]
    private int initialBoosterMagnet = 1;

    [Header("Boosters (unlock level)")]
    [Tooltip("Level required to unlock Freeze booster. Player must reach this level for the booster to be usable.")]
    [SerializeField]
    private int unlockBoosterFreezeLevel = 1;
    [Tooltip("Level required to unlock Star booster. Player must reach this level for the booster to be usable.")]
    [SerializeField]
    private int unlockBoosterStarLevel = 1;
    [Tooltip("Level required to unlock Magnet booster. Player must reach this level for the booster to be usable.")]
    [SerializeField]
    private int unlockBoosterMagnetLevel = 5;

    [Header("Links")]
    [Tooltip("Support / homepage URL. Opened when user taps Support in Settings.")]
    [SerializeField]
    private string supportUrl = "https://your-website.com";
    [Tooltip("Google Play package name (bundle ID). Leave empty to use Application.identifier. Used for Rate link.")]
    [SerializeField]
    private string googlePlayPackageName = "";

    public string SupportUrl => !string.IsNullOrEmpty(supportUrl) ? supportUrl : "https://your-website.com";
    /// <summary>Bundle ID for Google Play rate link. Uses googlePlayPackageName if set, otherwise Application.identifier.</summary>
    public string GooglePlayPackageName => !string.IsNullOrEmpty(googlePlayPackageName) ? googlePlayPackageName : Application.identifier;

    [Header("Energy")]
    [Tooltip("Initial energy max (max energy when starting the game).")]
    [SerializeField]
    private int energyMax = 10;
    [Tooltip("Time to refill 1 energy (seconds). e.g. 300 = 5 minutes. Shown as mm:ss countdown on StatusUI when energy < max.")]
    [SerializeField]
    private int energyRefillSeconds = 300;

    public int InitialCoin => initialCoin;
    public int InitialBoosterFreeze => initialBoosterFreeze;
    public int InitialBoosterStar => initialBoosterStar;
    public int InitialBoosterMagnet => initialBoosterMagnet;
    public int UnlockBoosterFreezeLevel => unlockBoosterFreezeLevel;
    public int UnlockBoosterStarLevel => unlockBoosterStarLevel;
    public int UnlockBoosterMagnetLevel => unlockBoosterMagnetLevel;
    public int EnergyMax => energyMax;
    public int EnergyRefillSeconds => energyRefillSeconds;
}
