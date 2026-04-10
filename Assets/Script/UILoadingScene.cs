using UnityEngine;
using DG.Tweening;
using System;

public class UILoadingScene : BasePanel
{
    [Header("Gate References")]
    [SerializeField] protected RectTransform gateLeft;
    [SerializeField] protected RectTransform gateRight;

    [Header("Gate Animation")]
    [SerializeField] protected float gateDelayBeforeSlide = 0.2f; // Gates stay still before sliding
    [SerializeField] protected float slideDuration = 0.5f;
    [SerializeField] protected float slideDistance = 1200f; // Slide distance (px)
    [SerializeField] protected Ease slideEase = Ease.OutQuad;

    private Vector2 _gateLeftClosedPos;
    private Vector2 _gateRightClosedPos;
    private Sequence _showSequence;

    protected override void Awake()
    {
        base.Awake();
        CaptureGatePositions();
    }

    /// <summary>
    /// Stores closed gate positions for reset on reuse.
    /// </summary>
    private void CaptureGatePositions()
    {
        if (gateLeft != null)
            _gateLeftClosedPos = gateLeft.anchoredPosition;
        if (gateRight != null)
            _gateRightClosedPos = gateRight.anchoredPosition;
    }

    /// <summary>
    /// Resets gates to closed position - used when showing again.
    /// </summary>
    public void ResetGates()
    {
        KillSequence();
        if (gateLeft != null)
            gateLeft.anchoredPosition = _gateLeftClosedPos;
        if (gateRight != null)
            gateRight.anchoredPosition = _gateRightClosedPos;
    }

    /// <summary>
    /// Show loading: show canvasGroup, gates slide left/right.
    /// onComplete called when animation ends (e.g. load scene, run task).
    /// </summary>
    public void Show(Action onComplete = null)
    {
        gameObject.SetActive(true);
        ResetGates();

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        _showSequence = DOTween.Sequence();

        if (canvasGroup != null)
            _showSequence.Append(canvasGroup.DOFade(1f, fadeDuration).SetEase(fadeEase));

        _showSequence.AppendInterval(gateDelayBeforeSlide);

        // Gates slide simultaneously
        if (gateLeft != null && gateRight != null)
        {
            _showSequence.Append(gateLeft.DOAnchorPosX(_gateLeftClosedPos.x - slideDistance, slideDuration).SetEase(slideEase));
            _showSequence.Join(gateRight.DOAnchorPosX(_gateRightClosedPos.x + slideDistance, slideDuration).SetEase(slideEase));
        }
        else if (gateLeft != null)
            _showSequence.Append(gateLeft.DOAnchorPosX(_gateLeftClosedPos.x - slideDistance, slideDuration).SetEase(slideEase));
        else if (gateRight != null)
            _showSequence.Append(gateRight.DOAnchorPosX(_gateRightClosedPos.x + slideDistance, slideDuration).SetEase(slideEase));

        _showSequence.OnComplete(() =>
        {
            _showSequence = null;
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// Ẩn loading (fade out canvas).
    /// </summary>
    public override void Hide()
    {
        KillSequence();
        base.Hide();
    }

    private void KillSequence()
    {
        if (_showSequence != null)
        {
            _showSequence.Kill();
            _showSequence = null;
        }
    }

    protected override void OnDestroy()
    {
        KillSequence();
        if (gateLeft != null) DOTween.Kill(gateLeft);
        if (gateRight != null) DOTween.Kill(gateRight);
        base.OnDestroy();
    }
}
