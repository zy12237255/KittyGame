using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private UIGame uiGame;

    [SerializeField]
    private UIHome uiHome;
    [SerializeField]
    private UILoadingScene uiLoadingScene;
    [SerializeField]
    private UIWin uIWin;
    [SerializeField]
    private StatusUI statusUI;
    [SerializeField]
    private UIOutOfTime uiOutOfTime;
    [SerializeField]
    private UIMoreBooster uiMoreBooster;
    [SerializeField]
    private UIFail uIFail;
    [SerializeField]
    private UISetting uiSetting;
    [SerializeField]
    private UILostEnergy uiLostEnergy;
    [SerializeField]
    private RefillEnergyUI uiRefillEnergy;
    [SerializeField]
    private ToastUI toastUI;
    /// <summary>UI Game panel - access via GameManager.uiManager.</summary>
    public UIGame UiGame => uiGame;
    /// <summary>UI Home panel - access via GameManager.uiManager.</summary>
    public UIHome UiHome => uiHome;
    public UILoadingScene UiLoadingScene => uiLoadingScene;
    public UIWin UiWin => uIWin;
    /// <summary>Status bar (energy, coin) - access via GameManager.uiManager.</summary>
    public StatusUI StatusUI => statusUI;
    /// <summary>Out of time panel - shown when timer reaches 00:00.</summary>
    public UIOutOfTime UiOutOfTime => uiOutOfTime;
    /// <summary>More booster popup - shown when tapping booster with count 0.</summary>
    public UIMoreBooster UiMoreBooster => uiMoreBooster;
    /// <summary>Fail popup - shown when player fails a level.</summary>
    public UIFail UIFail => uIFail;
    /// <summary>Setting popup - Pause mode (in-game) or Setting mode (from Home).</summary>
    public UISetting UiSetting => uiSetting;
    /// <summary>Lost energy popup - shown when player has no energy.</summary>
    public UILostEnergy UiLostEnergy => uiLostEnergy;
    /// <summary>Refill energy popup - shown when energy is 0 and player taps Play from Home.</summary>
    public RefillEnergyUI UiRefillEnergy => uiRefillEnergy;
    public ToastUI ToastUI => toastUI;
}
