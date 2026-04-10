using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System;
using System.Collections;

/// <summary>
/// Displays and updates Energy, Coin. Suggested structure: StatusView > StatusBar > Status_Energy (Bg, Text, Icon) | Status_Coin (Bg, Text, Icon).
/// Supports coin receive effect: coins fly from origin to coin target, then value updates.
/// </summary>
public class StatusUI : BasePanel
{
    [Header("Energy")]
    [SerializeField]
    private RectTransform statusEnergyRoot;
    [SerializeField]
    private Image energyBg;
    [Tooltip("Always displays the energy count.")]
    [SerializeField]
    private TextMeshProUGUI valueEnergyText;
    [Tooltip("Shows refill countdown (mm:ss) or \"FULL\" when energy = max.")]
    [SerializeField]
    private TextMeshProUGUI energyText;
    [SerializeField]
    private Image energyIcon;

    [Header("Coin")]
    [SerializeField]
    private RectTransform statusCoinRoot;
    [SerializeField]
    private Image coinBg;
    [SerializeField]
    private TextMeshProUGUI coinText;
    [SerializeField]
    private Image coinIcon;

    [Header("Coin Receive Effect (optional)")]
    [SerializeField] private GameObject coinVfx1;
    [SerializeField] private GameObject coinVfx2;
    [SerializeField] private GameObject coinVfx3;
    [SerializeField] private GameObject coinVfx4;
    [SerializeField] private GameObject coinVfx5;
    [SerializeField] private Transform coinTargetPos;
    [SerializeField] private float arcHeightMultiplier = 0.2f;
    [SerializeField] private int controlPointCount = 3;
    [SerializeField] private float delayBetweenCoins = 0.1f;
    [SerializeField] private float coinFlyDuration = 0.5f;

    private Coroutine _refillCoroutine;
    private float _refillTickAccum;

    /// <summary>
    /// Updates the displayed energy value (valueEnergyText).
    /// </summary>
    public void SetEnergy(int value)
    {
        if (valueEnergyText != null)
            valueEnergyText.text = value.ToString();
    }

    /// <summary>
    /// Updates the displayed energy value (valueEnergyText) as string.
    /// </summary>
    public void SetEnergy(string value)
    {
        if (valueEnergyText != null)
            valueEnergyText.text = value;
    }

    /// <summary>
    /// Updates displayed coin value.
    /// </summary>
    public void SetCoin(int value)
    {
        if (coinText != null)
            coinText.text = value.ToString();
    }

    /// <summary>
    /// Updates displayed coin value (string).
    /// </summary>
    public void SetCoin(string value)
    {
        if (coinText != null)
            coinText.text = value;
    }

    /// <summary>
    /// Refreshes energy and coin from GameManager data.
    /// When energy &lt; max: shows refill countdown mm:ss; when energy &gt;= max: shows energy count.
    /// </summary>
    public void RefreshFromGameData()
    {
        if (GameManager.Instance != null)
        {
            UpdateEnergyDisplay();
            SetCoin(GameManager.Instance.CurrentCoin);
        }
    }

    /// <summary>
    /// Updates valueEnergyText (always energy count) and energyText (refill timer mm:ss or "FULL").
    /// </summary>
    private void UpdateEnergyDisplay()
    {
        if (GameManager.Instance == null) return;
        int energy = GameManager.Instance.CurrentEnergy;
        int max = GameManager.Instance.GetEnergyMax();
        if (valueEnergyText != null)
            valueEnergyText.text = energy.ToString();
        if (energyText != null)
        {
            if (energy >= max)
                energyText.text = "FULL";
            else
            {
                int remaining = GameManager.Instance.GetRemainingRefillSeconds();
                int m = remaining / 60;
                int s = remaining % 60;
                energyText.text = $"{m:D2}:{s:D2}";
            }
        }
    }

    private IEnumerator RefillCountdownRoutine()
    {
        _refillTickAccum = 0f;
        while (GameManager.Instance != null)
        {
            yield return null;
            _refillTickAccum += Time.deltaTime;
            if (_refillTickAccum < 1f) continue;
            _refillTickAccum -= 1f;

            int energy = GameManager.Instance.CurrentEnergy;
            int max = GameManager.Instance.GetEnergyMax();
            if (energy >= max)
            {
                UpdateEnergyDisplay();
                continue;
            }
            int remaining = GameManager.Instance.GetRemainingRefillSeconds();
            remaining--;
            if (remaining <= 0)
            {
                GameManager.Instance.AddEnergy(1);
                UpdateEnergyDisplay();
                if (GameManager.Instance.CurrentEnergy >= max)
                {
                    _refillCoroutine = null;
                    yield break;
                }
                continue;
            }
            GameManager.Instance.SetRemainingRefillSeconds(remaining);
            UpdateEnergyDisplay();
        }
        _refillCoroutine = null;
    }

    /// <summary>
    /// Updates coin text from GameManager.
    /// </summary>
    public void UpdateCoinValueText()
    {
        if (GameManager.Instance != null)
            SetCoin(GameManager.Instance.GetCoin());
    }

    /// <summary>
    /// Add coin with visual effect: coins fly from origin to coin target, then value updates.
    /// </summary>
    public void AddCoinWithEffect(int coinAmount, Action onComplete = null)
    {
        Transform target = coinTargetPos != null ? coinTargetPos : (statusCoinRoot != null ? statusCoinRoot : (coinIcon != null ? coinIcon.transform : null));
        if (target == null)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoin(coinAmount);
                UpdateCoinValueText();
            }
            onComplete?.Invoke();
            return;
        }
        Vector3 targetPosition = target.position;
        GameObject[] coinVfxs = { coinVfx1, coinVfx2, coinVfx3, coinVfx4, coinVfx5 };
        int activeCoinCount = 0;
        for (int i = 0; i < coinVfxs.Length; i++)
        {
            if (coinVfxs[i] != null) activeCoinCount++;
        }
        if (activeCoinCount == 0)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoin(coinAmount);
                UpdateCoinValueText();
            }
            onComplete?.Invoke();
            return;
        }
        var tracker = new CoinAnimationTracker
        {
            completedCount = 0,
            activeCoinCount = activeCoinCount,
            coinAmount = coinAmount,
            onComplete = onComplete
        };
        for (int i = 0; i < coinVfxs.Length; i++)
        {
            if (coinVfxs[i] == null) continue;
            GameObject coinVfx = coinVfxs[i];
            coinVfx.SetActive(true);
            coinVfx.transform.localPosition = Vector3.zero;
            float delay = i * delayBetweenCoins;
            Vector3 startPos = coinVfx.transform.position;
            Vector3[] pathPoints = CreateCurvedPath(startPos, targetPosition, controlPointCount);
            Sequence seq = DOTween.Sequence();
            seq.AppendInterval(delay);
            seq.Append(coinVfx.transform.DOPath(pathPoints, coinFlyDuration, PathType.CatmullRom).SetEase(Ease.OutQuad));
            seq.OnComplete(() => OnCoinAnimationComplete(coinVfx, tracker));
        }
    }

    private void OnCoinAnimationComplete(GameObject coinVfx, CoinAnimationTracker tracker)
    {
        coinVfx.SetActive(false);
        tracker.completedCount++;
        if (tracker.completedCount >= tracker.activeCoinCount)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoin(tracker.coinAmount);
                UpdateCoinValueText();
            }
            tracker.onComplete?.Invoke();
        }
    }

    private Vector3[] CreateCurvedPath(Vector3 startPos, Vector3 targetPos, int pointCount)
    {
        pointCount = Mathf.Clamp(pointCount, 1, 5);
        float heightDifference = Mathf.Abs(targetPos.y - startPos.y);
        float arcHeight = heightDifference * arcHeightMultiplier;
        Vector3[] pathPoints = new Vector3[pointCount + 2];
        pathPoints[0] = startPos;
        pathPoints[pathPoints.Length - 1] = targetPos;
        for (int i = 1; i <= pointCount; i++)
        {
            float t = (float)i / (pointCount + 1);
            Vector3 basePoint = Vector3.Lerp(startPos, targetPos, t);
            float arcFactor = Mathf.Sin(t * Mathf.PI);
            basePoint.y += arcHeight * arcFactor;
            pathPoints[i] = basePoint;
        }
        return pathPoints;
    }

    private class CoinAnimationTracker
    {
        public int completedCount;
        public int activeCoinCount;
        public int coinAmount;
        public Action onComplete;
    }

    private void Start()
    {
        RefreshFromGameData();
        if (_refillCoroutine == null && GameManager.Instance != null && GameManager.Instance.CurrentEnergy < GameManager.Instance.GetEnergyMax())
            _refillCoroutine = StartCoroutine(RefillCountdownRoutine());
    }

    private void OnEnable()
    {
        if (_refillCoroutine == null && GameManager.Instance != null && GameManager.Instance.CurrentEnergy < GameManager.Instance.GetEnergyMax())
            _refillCoroutine = StartCoroutine(RefillCountdownRoutine());
    }

    private void OnDisable()
    {
        if (_refillCoroutine != null)
        {
            StopCoroutine(_refillCoroutine);
            _refillCoroutine = null;
        }
    }

    public override void Show()
    {
        base.Show();
    }

    public override void Hide()
    {
        base.Hide();
    }
}
