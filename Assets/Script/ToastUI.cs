using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;
using TMPro;

public class ToastUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Animation Settings")]
    [SerializeField] private float showDuration = 0.3f;
    [SerializeField] private float hideDuration = 0.3f;
    [SerializeField] private float displayDuration = 2.0f; // How long to show the message
    [SerializeField] private Ease showEase = Ease.OutBack;
    [SerializeField] private Ease hideEase = Ease.InBack;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector3 originalScale;
    private Vector3 originalPosition;
    private Coroutine autoHideCoroutine;
    private Tween currentTween;

    private void Awake()
    {
        // Get or add required components
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = gameObject.AddComponent<RectTransform>();
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Store original values
        if (rectTransform != null)
        {
            originalScale = rectTransform.localScale;
            originalPosition = rectTransform.anchoredPosition;
        }

        // Initialize hidden state
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.zero;
        }

        // Auto assign references
        AutoAssignReferences();
    }

    /// <summary>
    /// Automatically assign UI references by searching in children
    /// </summary>
    private void AutoAssignReferences()
    {
        // Find messageText in Content/background/messageText
        if (messageText == null)
        {
            Transform contentTransform = transform.Find("Content");
            if (contentTransform != null)
            {
                Transform backgroundTransform = contentTransform.Find("background");
                if (backgroundTransform != null)
                {
                    Transform messageTextTransform = backgroundTransform.Find("messageText");
                    if (messageTextTransform != null)
                    {
                        messageText = messageTextTransform.GetComponent<TextMeshProUGUI>();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Show toast with message
    /// </summary>
    /// <param name="message">Message to display</param>
    /// <param name="autoHide">Whether to automatically hide after displayDuration</param>
    public void Show(string message, bool autoHide = true)
    {
        // Stop any existing animation
        KillCurrentTween();

        // Set message text
        if (messageText != null)
        {
            messageText.text = message;
        }

        // Activate game object
        gameObject.SetActive(true);

        // Reset to initial hidden state
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        if (rectTransform != null)
        {
            rectTransform.localScale = Vector3.zero;
        }

        // Animate show
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(1f, showDuration).SetEase(showEase);
        }

        if (rectTransform != null)
        {
            rectTransform.DOScale(originalScale, showDuration).SetEase(showEase);
        }

        // Auto hide after display duration
        if (autoHide)
        {
            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
            }
            autoHideCoroutine = StartCoroutine(AutoHideCoroutine());
        }
    }

    /// <summary>
    /// Hide toast with animation
    /// </summary>
    public void Hide()
    {
        // Stop auto hide coroutine
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
            autoHideCoroutine = null;
        }

        // Stop any existing animation
        KillCurrentTween();

        // Animate hide
        if (canvasGroup != null)
        {
            canvasGroup.DOFade(0f, hideDuration).SetEase(hideEase);
        }

        if (rectTransform != null)
        {
            rectTransform.DOScale(Vector3.zero, hideDuration).SetEase(hideEase)
                .OnComplete(() =>
                {
                    // Deactivate after animation
                    gameObject.SetActive(false);
                    if (canvasGroup != null)
                    {
                        canvasGroup.blocksRaycasts = false;
                        canvasGroup.interactable = false;
                    }
                });
        }
        else
        {
            // If no rectTransform, just deactivate after fade
            if (canvasGroup != null)
            {
                canvasGroup.DOFade(0f, hideDuration).SetEase(hideEase)
                    .OnComplete(() =>
                    {
                        gameObject.SetActive(false);
                        canvasGroup.blocksRaycasts = false;
                        canvasGroup.interactable = false;
                    });
            }
        }
    }

    /// <summary>
    /// Coroutine to automatically hide toast after display duration
    /// </summary>
    private IEnumerator AutoHideCoroutine()
    {
        yield return new WaitForSeconds(displayDuration);
        Hide();
    }

    /// <summary>
    /// Kill current tween to prevent conflicts
    /// </summary>
    private void KillCurrentTween()
    {
        if (currentTween != null && currentTween.IsActive())
        {
            currentTween.Kill();
        }

        if (canvasGroup != null)
        {
            DOTween.Kill(canvasGroup);
        }

        if (rectTransform != null)
        {
            DOTween.Kill(rectTransform);
        }
    }

    private void OnDestroy()
    {
        // Kill all tweens on destroy
        KillCurrentTween();

        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
        }
    }

    // Public getters
    public TextMeshProUGUI MessageText => messageText;
}

