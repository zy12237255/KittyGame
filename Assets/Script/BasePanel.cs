using UnityEngine;
using DG.Tweening;
using System.Collections;

public abstract class BasePanel : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] protected float fadeDuration = 0.3f;
    [SerializeField] protected float scaleDuration = 0.3f;
    [SerializeField] protected Ease fadeEase = Ease.OutQuad;
    [SerializeField] protected Ease scaleEase = Ease.OutBack;

    [SerializeField] protected CanvasGroup canvasGroup;
    [SerializeField] protected RectTransform rectTransform;


    protected virtual void Awake()
    {
        // Initialize components
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.0f;
        }
        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.one;
        }

    }

    public virtual void Show()
    {

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            canvasGroup.DOFade(1f, fadeDuration).SetEase(fadeEase);
        }
    }

    public virtual void Hide()
    {

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1.0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            canvasGroup.DOFade(0f, fadeDuration).SetEase(fadeEase);
        }
        // Deactivate after animation
        //DOVirtual.DelayedCall(scaleDuration, () => gameObject.SetActive(false));
    }

    protected virtual void OnDestroy()
    {
        // Kill any ongoing tweens
        DOTween.Kill(rectTransform);
        DOTween.Kill(canvasGroup);
    }
}