using UnityEngine;
using DG.Tweening;

public abstract class BasePopup : BasePanel
{
    [Header("Popup Settings")]
    [SerializeField] protected float popupScale = 0.8f;
    [SerializeField] protected float popupDuration = 0.3f;
    [SerializeField] protected Ease popupEase = Ease.OutBack;

    protected override void Awake()
    {
        base.Awake();
        
        // Initialize popup state
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.zero;
        }
    }

    public override void Show()
    {
        gameObject.SetActive(true);
        
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            canvasGroup.DOFade(1f, popupDuration).SetEase(popupEase);
        }
        
        if (rectTransform != null)
        {
            rectTransform.DOScale(Vector3.one, popupScale).SetEase(popupEase);
        }
    }

    public override void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            canvasGroup.DOFade(0f, popupDuration).SetEase(fadeEase);
        }

        if (rectTransform != null)
        {
            rectTransform.DOScale(Vector3.zero, popupScale).SetEase(Ease.Linear);
        }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // Kill any ongoing tweens
        DOTween.Kill(rectTransform);
        DOTween.Kill(canvasGroup);
    }
} 