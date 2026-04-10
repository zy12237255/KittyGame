using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System;

public class CoinUI : BasePanel
{
    [Header("UI References")]
    [SerializeField] private Button plusBtn; // plus button
    [SerializeField] private Text coinValueText; // coinValueText in CurrencyView

    [Header("Coin VFX References")]
    [SerializeField] private GameObject coinVfx1;
    [SerializeField] private GameObject coinVfx2;
    [SerializeField] private GameObject coinVfx3;
    [SerializeField] private GameObject coinVfx4;
    [SerializeField] private GameObject coinVfx5;
    [SerializeField] private Transform coinTargetPos; // Target position for coins to fly to

    [Header("Coin Effect Settings")]
    [SerializeField] private float arcHeightMultiplier = 0.2f; // Control arc height (default: 0.2f for lower arc)
    [SerializeField] private int controlPointCount = 3; // Number of control points for smooth curve (1-5, default: 3)

    protected override void Awake()
    {
        base.Awake();
        AutoAssignReferences();
    }

    /// <summary>
    /// Automatically assign UI references by searching in children
    /// </summary>
    private void AutoAssignReferences()
    {
        Transform topTransform = transform.Find("Top");
        if (topTransform == null) return;

        Transform currencyViewTransform = topTransform.Find("CurrencyView");
        if (currencyViewTransform == null) return;

        // Find coinValueText in Top/CurrencyView/coinValueText
        if (coinValueText == null)
        {
            Transform coinValueTextTransform = currencyViewTransform.Find("coinValueText");
            if (coinValueTextTransform != null)
            {
                coinValueText = coinValueTextTransform.GetComponent<Text>();
            }
        }

        // Find plus button in Top/CurrencyView/plus
        if (plusBtn == null)
        {
            Transform plusTransform = currencyViewTransform.Find("plus");
            if (plusTransform != null)
            {
                plusBtn = plusTransform.GetComponent<Button>();
            }
        }
    }

    /// <summary>
    /// Register this panel to UIManager
    /// </summary>
    private void Start()
    {
      
    }


    /// <summary>
    /// Initialize CoinUI - called when showing coin panel
    /// </summary>
    public void Init()
    {
        UpdateCoinValueText();
    }

    /// <summary>
    /// Update coinValueText with current coin from GameManager
    /// </summary>
    public void UpdateCoinValueText()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager.Instance is null. Cannot update coin value text.");
            return;
        }

        int coin = GameManager.Instance.GetCoin();
        if (coinValueText != null)
        {
            coinValueText.text = coin.ToString();
        }
    }

    /// <summary>
    /// Update method for testing - press G to test coin effect
    /// </summary>
    private void Update()
    {
        // Test: Press G to add 50 coins with effect
        if (Input.GetKeyDown(KeyCode.G))
        {
            AddCoinWithEffect(50, () =>
            {
                Debug.Log("Test: Added 50 coins with effect!");
            });
        }
    }

    /// <summary>
    /// Add coin with visual effect: coins fly from origin to target position
    /// </summary>
    /// <param name="coinAmount">Amount of coins to add</param>
    /// <param name="onComplete">Callback when animation completes</param>
    public void AddCoinWithEffect(int coinAmount, Action onComplete = null)
    {
        if (coinTargetPos == null)
        {
            Debug.LogWarning("coinTargetPos is null. Cannot play coin effect.");
            // Still add coin even if effect cannot play
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoin(coinAmount);
                UpdateCoinValueText();
            }
            onComplete?.Invoke();
            return;
        }

        // Array of coin VFX objects
        GameObject[] coinVfxs = { coinVfx1, coinVfx2, coinVfx3, coinVfx4, coinVfx5 };

        // Get target position in world space
        Vector3 targetPosition = coinTargetPos.position;

        // Count how many coins will animate
        int activeCoinCount = 0;
        for (int i = 0; i < coinVfxs.Length; i++)
        {
            if (coinVfxs[i] != null)
            {
                activeCoinCount++;
            }
        }

        if (activeCoinCount == 0)
        {
            Debug.LogWarning("No coin VFX objects assigned. Cannot play coin effect.");
            // Still add coin even if effect cannot play
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoin(coinAmount);
                UpdateCoinValueText();
            }
            onComplete?.Invoke();
            return;
        }

        // Calculate total animation duration
        float delayBetweenCoins = 0.1f;
        float coinFlyDuration = 0.5f;

        // Use a wrapper class to safely track completed count
        CoinAnimationTracker tracker = new CoinAnimationTracker
        {
            completedCount = 0,
            activeCoinCount = activeCoinCount,
            coinAmount = coinAmount,
            onComplete = onComplete
        };

        // Animate each coin
        for (int i = 0; i < coinVfxs.Length; i++)
        {
            if (coinVfxs[i] == null) continue;

            // Capture coinVfx reference to avoid closure issues
            GameObject coinVfx = coinVfxs[i];

            // Set active and reset position to zero
            coinVfx.SetActive(true);
            coinVfx.transform.localPosition = Vector3.zero;

            // Calculate delay for this coin
            float delay = i * delayBetweenCoins;

            // Get starting position (world space)
            Vector3 startPos = coinVfx.transform.position;

            // Create curved path with multiple control points for smoother trajectory
            Vector3[] pathPoints = CreateCurvedPath(startPos, targetPosition, controlPointCount);

            // Animate coin along curved path
            Sequence coinSequence = DOTween.Sequence();
            coinSequence.AppendInterval(delay);

            Tween pathTween = coinVfx.transform.DOPath(
                pathPoints,
                coinFlyDuration,
                PathType.CatmullRom
            ).SetEase(Ease.OutQuad);

            coinSequence.Append(pathTween);

            // Set callback when sequence completes - use separate method to handle completion
            coinSequence.OnComplete(() =>
            {
                OnCoinAnimationComplete(coinVfx, tracker);
            });
        }
    }

    /// <summary>
    /// Handle completion of individual coin animation
    /// </summary>
    /// <param name="coinVfx">The coin VFX GameObject that completed animation</param>
    /// <param name="tracker">Tracker object to manage completion state</param>
    private void OnCoinAnimationComplete(GameObject coinVfx, CoinAnimationTracker tracker)
    {
        // Set active false when coin reaches target
        coinVfx.SetActive(false);

        // Play coin sound
        tracker.completedCount++;

        // When all coins finish, add coin value and call callback
        if (tracker.completedCount >= tracker.activeCoinCount)
        {
            // Add coin value
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddCoin(tracker.coinAmount);
                UpdateCoinValueText();
            }

            // Call completion callback
            tracker.onComplete?.Invoke();
        }
    }

    /// <summary>
    /// Helper class to track coin animation completion state
    /// </summary>
    private class CoinAnimationTracker
    {
        public int completedCount;
        public int activeCoinCount;
        public int coinAmount;
        public Action onComplete;
    }

    /// <summary>
    /// Create curved path with multiple control points for smooth trajectory
    /// </summary>
    /// <param name="startPos">Starting position</param>
    /// <param name="targetPos">Target position</param>
    /// <param name="pointCount">Number of control points (1-5)</param>
    /// <returns>Array of path points including start, control points, and target</returns>
    private Vector3[] CreateCurvedPath(Vector3 startPos, Vector3 targetPos, int pointCount)
    {
        // Clamp point count between 1 and 5
        pointCount = Mathf.Clamp(pointCount, 1, 5);

        // Calculate total path length
        float totalDistance = Vector3.Distance(startPos, targetPos);
        float heightDifference = Mathf.Abs(targetPos.y - startPos.y);
        float arcHeight = heightDifference * arcHeightMultiplier;

        // Create path points array: start + control points + target
        Vector3[] pathPoints = new Vector3[pointCount + 2];
        pathPoints[0] = startPos; // First point is start position
        pathPoints[pathPoints.Length - 1] = targetPos; // Last point is target position

        // Generate control points evenly distributed along the path
        for (int i = 1; i <= pointCount; i++)
        {
            // Calculate t value (0 to 1) for this control point
            float t = (float)i / (pointCount + 1);

            // Linear interpolation between start and target
            Vector3 basePoint = Vector3.Lerp(startPos, targetPos, t);

            // Add arc height - higher in the middle, lower at edges
            float arcFactor = Mathf.Sin(t * Mathf.PI); // Sin curve: 0 at start/end, 1 at middle
            basePoint.y += arcHeight * arcFactor;

            pathPoints[i] = basePoint;
        }

        return pathPoints;
    }

    // Public getters for UI components
    public Button PlusBtn => plusBtn;
    public Text CoinValueText => coinValueText;
}

