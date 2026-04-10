using UnityEngine;

// Scriptable defaults: economy, booster stock, unlock gates, energy cadence, store links.
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

    [Header("Energy")]
    [Tooltip("Initial energy max (max energy when starting the game).")]
    [SerializeField]
    private int energyMax = 10;
    [Tooltip("Time to refill 1 energy (seconds). e.g. 300 = 5 minutes. Shown as mm:ss countdown on StatusUI when energy < max.")]
    [SerializeField]
    private int energyRefillSeconds = 300;

    public string SupportUrl => string.IsNullOrEmpty(supportUrl) ? "https://your-website.com" : supportUrl;

    public string GooglePlayPackageName => string.IsNullOrEmpty(googlePlayPackageName) ? Application.identifier : googlePlayPackageName;

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
