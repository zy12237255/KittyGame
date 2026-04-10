using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIOutOfTime : BasePopup
{
    [Header("OutOfTime UI")]
    [SerializeField] protected RectTransform dimed;
    [SerializeField] protected RectTransform bg;
    [SerializeField] protected TextMeshProUGUI textTitle;
    [SerializeField] protected Button buttonClose;
    [SerializeField] protected TextMeshProUGUI textInfo;
    [SerializeField] protected Button buttonClaim;
    [SerializeField] protected RectTransform icon;
    [SerializeField] protected RectTransform line;

    protected override void Awake()
    {
        base.Awake();
        BindButtons();
    }

    protected virtual void BindButtons()
    {
        if (buttonClose != null)
            buttonClose.onClick.AddListener(HandleCloseClicked);
        if (buttonClaim != null)
            buttonClaim.onClick.AddListener(HandleClaimClicked);
    }

    protected virtual void HandleCloseClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        Hide();
        GameManager.Instance?.ShowFailFromOutOfTime();
    }

    protected virtual void HandleClaimClicked()
    {
        SoundManager.Instance?.PlayButtonClick();
        GameManager.Instance?.RequestOutOfTimeClaimReward();
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
}
