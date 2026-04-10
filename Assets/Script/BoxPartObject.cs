using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class BoxPartObject : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    private GameObject magnetObject;

    [SerializeField]
    private ColorConfig colorConfig;

    [Header("Color Info")]
    [SerializeField]
    private int currentColorID = -1;

    [Header("Cat Detection")]
    [SerializeField]
    private float detectionRadius = 1.0f;

    [Header("Cat Movement")]
    [SerializeField]
    private float movementDuration = 1.0f;

    [SerializeField]
    private float scaleDuration = 1.0f;

    private LayerMask catLayerMask;
    private HashSet<CatView> processedCats = new HashSet<CatView>(); // Track cats that are already being processed
    private BoxObject cachedBoxObject;

    public ParticleSystem starExplosion;

    /// <summary>
    /// Get current color ID
    /// </summary>
    public int CurrentColorID
    {
        get { return currentColorID; }
    }

    void Awake()
    {
        // Cache BoxObject once to avoid repeated GetComponentInParent calls
        cachedBoxObject = GetComponentInParent<BoxObject>();

        // Setup cat layer mask
        int catLayer = LayerMask.NameToLayer("Cat");
        if (catLayer == -1)
        {
            Debug.LogWarning($"Layer 'Cat' not found, trying to detect cats on default layer");
            catLayerMask = -1; // Detect all layers if Cat layer not found
        }
        else
        {
            catLayerMask = 1 << catLayer;
        }
    }

    void Update()
    {
        // Continuously detect cats with the same color index
        DetectCatsWithSameColor();
    }

    /// <summary>
    /// Detect cats with the same color index using OverlapCircle
    /// </summary>
    private void DetectCatsWithSameColor()
    {
        if (currentColorID < 0)
        {
            return; // No color ID assigned yet
        }

        // Use OverlapCircle to detect cats in range
        Collider2D[] detectedColliders = Physics2D.OverlapCircleAll(transform.position, detectionRadius, catLayerMask);

        foreach (Collider2D detectedCollider in detectedColliders)
        {
            if (detectedCollider == null)
            {
                continue;
            }

            // Try to find CatView component
            CatView catView = detectedCollider.GetComponent<CatView>();
            if (catView == null)
            {
                catView = detectedCollider.GetComponentInParent<CatView>();
            }

            if (catView != null && catView.CurrentColorID == currentColorID)
            {
                if (cachedBoxObject != null && cachedBoxObject.CurrentCount > 0)
                {
                    cachedBoxObject.DecrementCurrentCount(1);
                    if (!processedCats.Contains(catView))
                    {
                        processedCats.Add(catView);
                        OnCatDetected(catView);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Called when a cat with the same color index is detected
    /// Moves cat to box part object with scale animation, then destroys it
    /// </summary>
    /// <param name="catView">Detected CatView</param>
    private void OnCatDetected(CatView catView)
    {
        if (catView == null || catView.gameObject == null)
        {
            return;
        }

        catView.SetSpriteLayer(1000);
        GameObject catObject = catView.gameObject;
        Transform catTransform = catObject.transform;

        // Store world position before changing parent
        Vector3 worldPosition = catTransform.position;

        // Set parent to this box part object
        catTransform.SetParent(transform);

        // Convert world position to local position relative to new parent
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        catTransform.localPosition = localPosition;

        // Target local position is center of parent (Vector3.zero)
        Vector3 targetLocalPosition = Vector3.zero;

        // Disable collider to prevent further detection
        Collider2D catCollider = catView.GetCollider();
        if (catCollider != null)
        {
            catCollider.enabled = false;
        }

        // Create sequence: Move and Scale simultaneously, then destroy
        Sequence sequence = DOTween.Sequence();

        // Add movement and scale animations (run simultaneously)
        sequence.Join(catTransform.DOLocalMove(targetLocalPosition, movementDuration).SetEase(Ease.InOutQuad));
        sequence.Join(catTransform.DOScale(Vector3.zero, scaleDuration).SetEase(Ease.InBack));

        // On complete, destroy the cat object
        sequence.OnComplete(() =>
        {
            if (catObject != null)
            {
                starExplosion.Play();
                SoundManager.Instance.PlayDropCatSound();
                Destroy(catObject);
            }
            // Remove from processed set (in case object is destroyed before this)
            processedCats.Remove(catView);

            // // Decrement currentCount and update countText on the attached BoxObject
            // if (cachedBoxObject != null)
            // {
            //     Debug.Log($"BoxPartObject {gameObject.name} (ColorID: {currentColorID}) detected Cat {catObject.name} - Decrementing currentCount on BoxObject {cachedBoxObject.name}");
            //     cachedBoxObject.DecrementCurrentCount(1);
            // }
        });

        //Debug.Log($"BoxPartObject {gameObject.name} (ColorID: {currentColorID}) detected Cat {catObject.name} (ColorID: {catView.CurrentColorID}) - Moving to box part");
    }

    /// <summary>
    /// Set sprite color by color index from ColorConfig
    /// </summary>
    /// <param name="colorIndex">Color index from ColorConfig</param>
    public void SetColorByIndex(int colorIndex)
    {
        // Set current color ID
        currentColorID = colorIndex;

        // Get ColorConfig if not assigned
        if (colorConfig == null)
        {
            colorConfig = Resources.Load<ColorConfig>("Configs/ColorConfig");
            if (colorConfig == null)
            {
                Debug.LogError($"ColorConfig not found at Resources/Configs/ColorConfig for BoxPartObject {gameObject.name}");
                return;
            }
        }

        // Get color from ColorConfig
        Color color = colorConfig.GetColorByIndex(colorIndex);

        // Get SpriteRenderer if not assigned
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = color;
        }
        else
        {
            Debug.LogError($"SpriteRenderer not found in BoxPartObject {gameObject.name}");
        }
    }

    /// <summary>
    /// Get cat from box: Move cat to boxBodyPart (this transform) with animation, then destroy
    /// </summary>
    /// <param name="catObject">Cat GameObject to move</param>
    public void GetCatFromBox(GameObject catObject)
    {
        if (catObject == null)
        {
            Debug.LogWarning($"CatObject is null for GetCatFromBox in BoxPartObject {gameObject.name}");
            return;
        }

        Transform catTransform = catObject.transform;

        // Store world position before changing parent
        Vector3 worldPosition = catTransform.position;

        // Set parent to this boxBodyPart (this transform)
        catTransform.SetParent(transform);

        // Convert world position to local position relative to new parent
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        catTransform.localPosition = localPosition;

        // Target local position is center of parent (Vector3.zero)
        Vector3 targetLocalPosition = Vector3.zero;

        // Disable collider to prevent further detection
        CatView catView = catObject.GetComponent<CatView>();
        if (catView == null)
        {
            catView = catObject.GetComponentInChildren<CatView>();
        }

        if (catView != null)
        {
            catView.SetTopLayer();
            Collider2D catCollider = catView.GetCollider();
            if (catCollider != null)
            {
                catCollider.enabled = false;
            }
        }

        // Create sequence: Move and Scale simultaneously, then destroy
        Sequence sequence = DOTween.Sequence();

        // Add movement and scale animations (run simultaneously)
        sequence.Join(catTransform.DOLocalMove(targetLocalPosition, movementDuration).SetEase(Ease.InOutQuad));
        sequence.Join(catTransform.DOScale(Vector3.zero, scaleDuration).SetEase(Ease.InBack));

        // On complete, destroy the cat object
        sequence.OnComplete(() =>
        {
            if (catObject != null)
            {
                starExplosion.Play();
                SoundManager.Instance.PlayDropCatSound();
                Destroy(catObject);
            }
        });

        Debug.Log($"BoxPartObject {gameObject.name} (ColorID: {currentColorID}) getting Cat {catObject.name} - Moving to box part");
    }
    public void GetCatFromStarBubble(CatView catView)
    {
        if (catView == null || catView.gameObject == null)
        {
            return;
        }

        catView.SetSpriteLayer(1000);
        catView.EnableStarBubble(true);
        GameObject catObject = catView.gameObject;
        Transform catTransform = catObject.transform;

        // Store world position before changing parent
        Vector3 worldPosition = catTransform.position;

        // Set parent to this box part object
        catTransform.SetParent(transform);

        // Convert world position to local position relative to new parent
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        catTransform.localPosition = localPosition;

        // Target local position is center of parent (Vector3.zero)
        Vector3 targetLocalPosition = Vector3.zero;

        // Disable collider to prevent further detection
        Collider2D catCollider = catView.GetCollider();
        if (catCollider != null)
        {
            catCollider.enabled = false;
        }

        catTransform.DOLocalMove(targetLocalPosition, movementDuration * 2).SetEase(Ease.InOutQuad).OnComplete(() =>
        {
            // After catView has moved to boxPartObject, turn off magnet
            EnableMagnet(false);
            catView.EnableStarBubble(false);
            catTransform.DOScale(Vector3.zero, scaleDuration).SetEase(Ease.InBack).OnComplete(() =>
            {
                if (catObject != null)
                {
                    if (catObject != null)
                    {
                        starExplosion.Play();
                        SoundManager.Instance.PlayDropCatSound();
                        Destroy(catObject);
                    }
                    if (cachedBoxObject != null)
                    {
                        cachedBoxObject.DecrementCurrentCount(1);
                    }
                    // Remove from processed set (in case object is destroyed before this)
                    processedCats.Remove(catView);
                }
            });
        });
    }
    public void EnableMagnet(bool enable)
    {
        if (magnetObject != null)
        {
            magnetObject.SetActive(enable);
        }
    }
}
