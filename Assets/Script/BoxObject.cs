using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

// Logical root for a movable/consumable box target.
/// <summary>
/// Box type enum based on block sprites
/// </summary>
public enum BoxType
{
    // Plus shape
    Plus,
    
    // I shapes (2 cells)
    I2,
    I2_90,
    
    // I shapes (3 cells)
    I3,
    I3_90,
    
    // J shapes (4 rotations)
    J_0,
    J_90,
    J_180,
    J_270,
    
    // L shapes (4 rotations)
    L_0,
    L_90,
    L_180,
    L_270,
    
    // Single square blocks
    Block_01,
    Block_02,
    
    // Reverse L shapes (4 rotations)
    R_0,
    R_90,
    R_180,
    R_270,
    
    // T shapes (4 rotations)
    T_0,
    T_90,
    T_180,
    T_270
}

public enum DirectionType
{
    None,
    Horizontal,
    Vertical
}

public class BoxObject : MonoBehaviour
{
    [Header("Box Settings")]
    [SerializeField]
    private BoxType boxType = BoxType.Block_01;
     [Header("Direction Settings")]
    [SerializeField]
    private DirectionType directionType = DirectionType.None;
    
    [Header("References")]
    [SerializeField]
    private Transform bodyPartRoot;
    
    [SerializeField]
    private SpriteRenderer mainShapeRenderer;
    
    [SerializeField]
    private BoxConfig boxConfig;

    [SerializeField]
    private ColorConfig colorConfig;
    
    public Transform directionBone;
    
    [Header("Color Info")]
    [SerializeField]
    private int currentColorID = -1;
    
    public Transform collidersParent; // Parent transform containing all collider GameObjects
    
    [Header("Count Canvas")]
    [SerializeField]
    private Transform countCanvas;
    
    /// <summary>
    /// Get count canvas transform
    /// </summary>
    public Transform CountCanvas
    {
        get { return countCanvas; }
    }

    [Header("Count")]
    [SerializeField]
    private int countMax = 0;

    [SerializeField]
    private int currentCount = 0;

    [SerializeField]
    private TMP_Text countText;

    [SerializeField]
    private Image countTextBackground;

    [Header("Destroy Settings")]
    [SerializeField]
    private float destroyScaleDuration = 0.2f;

    private bool isDestroying = false;

    /// <summary>
    /// Get max count value
    /// </summary>
    public int CountMax
    {
        get { return countMax; }
    }

    /// <summary>
    /// Get current count value
    /// </summary>
    public int CurrentCount
    {
        get { return currentCount; }
    }
    
    /// <summary>
    /// Get current color ID
    /// </summary>
    public int CurrentColorID
    {
        get { return currentColorID; }
    }
    
    /// <summary>
    /// Get or set box type
    /// </summary>
    public BoxType Type
    {
        get { return boxType; }
        set 
        { 
            boxType = value;
            ActivateColliderByType(value);
            ActivateDirectionBones();
        }
    }
    
    /// <summary>
    /// Get or set direction type
    /// </summary>
    public DirectionType DirectionType
    {
        get { return directionType; }
        set 
        { 
            directionType = value;
            ActivateDirectionBones();
        }
    }
    
    /// <summary>
    /// Get body part root transform
    /// </summary>
    public Transform BodyPartRoot
    {
        get { return bodyPartRoot; }
    }
    
    /// <summary>
    /// Get main shape renderer
    /// </summary>
    public SpriteRenderer MainShapeRenderer
    {
        get { return mainShapeRenderer; }
    }
    
    /// <summary>
    /// Get Rigidbody2D component
    /// </summary>
    public Rigidbody2D Rigidbody2D
    {
        get
        {
            if (GetComponent<Rigidbody2D>() == null)
            {
                return GetComponentInChildren<Rigidbody2D>();
            }
            return GetComponent<Rigidbody2D>();
        }
    }
    
    void Awake()
    {
        // Activate collider for initial box type
        ActivateColliderByType(boxType);
        UpdateCountText();
        UpdateCountBackgroundColor(currentColorID);
        ActivateDirectionBones();
    }

    /// <summary>
    /// Set initial max count and update UI
    /// </summary>
    public void SetCountMax(int value)
    {
        countMax = value;
        currentCount = value;
        UpdateCountText();
    }

    public void DecrementCurrentCount(int amount = 1)
    {
        if (isDestroying)
        {
            return;
        }

        currentCount = Mathf.Max(0, currentCount - amount);
        UpdateCountText();

        if (currentCount == 0)
        {
            isDestroying = true;

            // Optional scale effect before destroy
            transform.DOScale(Vector3.zero, destroyScaleDuration)
                .SetEase(Ease.InBack)
                .SetDelay(0.35f)
                .OnComplete(() =>
                {
                    if (GameManager.Instance != null)
                        GameManager.Instance.CheckWinCondition();
                    Destroy(gameObject);
                });
        }
    }

    public void  DestroyBox()
    {
        
    }

    private void EnsureCountText()
    {
        if (countText != null)
        {
            return;
        }

        if (countCanvas != null)
        {
            countText = countCanvas.GetComponentInChildren<TMP_Text>(true);
        }
    }

    private void EnsureCountBackground()
    {
        if (countTextBackground != null)
        {
            return;
        }

        if (countCanvas != null)
        {
            // Best-effort: find an Image under count canvas
            countTextBackground = countCanvas.GetComponentInChildren<Image>(true);
        }
    }

    private void UpdateCountText()
    {
        EnsureCountText();
        if (countText != null)
        {
            countText.text = currentCount.ToString();
        }
    }

    private void UpdateCountBackgroundColor(int colorIndex)
    {
        EnsureCountBackground();
        if (countTextBackground == null)
        {
            return;
        }

        if (colorIndex < 0)
        {
            return;
        }

        if (colorConfig == null)
        {
            colorConfig = Resources.Load<ColorConfig>("Configs/ColorConfig");
            if (colorConfig == null)
            {
                Debug.LogWarning($"ColorConfig not found at Resources/Configs/ColorConfig for BoxObject {gameObject.name}");
                return;
            }
        }

        Color c = colorConfig.GetColorByIndex(colorIndex);
        countTextBackground.color = c;
    }
    
    /// <summary>
    /// Convert BoxType enum to GameObject name
    /// </summary>
    /// <param name="type">BoxType enum value</param>
    /// <returns>GameObject name string</returns>
    private string GetColliderNameByType(BoxType type)
    {
        switch (type)
        {
            case BoxType.Plus:
                return "Block_Plus";
            case BoxType.I2:
                return "Block_I2";
            case BoxType.I2_90:
                return "Block_I2_90";
            case BoxType.I3:
                return "Block_I3";
            case BoxType.I3_90:
                return "Block_I3_90";
            case BoxType.J_0:
                return "Block_J_0";
            case BoxType.J_90:
                return "Block_J_90";
            case BoxType.J_180:
                return "Block_J_180";
            case BoxType.J_270:
                return "Block_J_270";
            case BoxType.L_0:
                return "Block_L_0";
            case BoxType.L_90:
                return "Block_L_90";
            case BoxType.L_180:
                return "Block_L_180";
            case BoxType.L_270:
                return "Block_L_270";
            case BoxType.Block_01:
                return "Block_O1";
            case BoxType.Block_02:
                return "Block_O2";
            case BoxType.R_0:
                return "Block_R_0";
            case BoxType.R_90:
                return "Block_R_90";
            case BoxType.R_180:
                return "Block_R_180";
            case BoxType.R_270:
                return "Block_R_270";
            case BoxType.T_0:
                return "Block_T_0";
            case BoxType.T_90:
                return "Block_T_90";
            case BoxType.T_180:
                return "Block_T_180";
            case BoxType.T_270:
                return "Block_T_270";
            default:
                Debug.LogWarning($"Unknown BoxType: {type}, using default Block_O1");
                return "Block_O1";
        }
    }
    
    /// <summary>
    /// Activate collider GameObject based on box type
    /// Deactivates all other colliders
    /// </summary>
    /// <param name="type">BoxType to activate</param>
    private void ActivateColliderByType(BoxType type)
    {
        if (collidersParent == null)
        {
            Debug.LogWarning($"CollidersParent is null in BoxObject {gameObject.name}, cannot activate collider");
            return;
        }
        
        string targetColliderName = GetColliderNameByType(type);
        
        // Deactivate all collider children first
        for (int i = 0; i < collidersParent.childCount; i++)
        {
            Transform child = collidersParent.GetChild(i);
            child.gameObject.SetActive(false);
        }
        
        // Find and activate the target collider
        Transform targetCollider = collidersParent.Find(targetColliderName);
        if (targetCollider != null)
        {
            targetCollider.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning($"Collider GameObject '{targetColliderName}' not found in CollidersParent of BoxObject {gameObject.name}");
        }
    }
    
    /// <summary>
    /// Get the currently active collider from collidersParent
    /// Returns the first collider found from the active child GameObject
    /// </summary>
    /// <returns>Active Collider2D or null if not found</returns>
    public Collider2D GetActiveCollider()
    {
        List<Collider2D> activeColliders = GetActiveColliders();
        return activeColliders.Count > 0 ? activeColliders[0] : null;
    }
    
    /// <summary>
    /// Get all active colliders from collidersParent
    /// A GameObject child can have multiple Collider2D components
    /// </summary>
    /// <returns>List of active Collider2D components</returns>
    public List<Collider2D> GetActiveColliders()
    {
        List<Collider2D> colliders = new List<Collider2D>();
        
        if (collidersParent == null)
        {
            return colliders;
        }
        
        // Find the active collider child
        for (int i = 0; i < collidersParent.childCount; i++)
        {
            Transform child = collidersParent.GetChild(i);
            if (child.gameObject.activeSelf)
            {
                // Get all Collider2D components from this child GameObject
                Collider2D[] childColliders = child.GetComponents<Collider2D>();
                if (childColliders != null && childColliders.Length > 0)
                {
                    colliders.AddRange(childColliders);
                }
            }
        }
        
        return colliders;
    }
    
    /// <summary>
    /// Activate HorizontalBone or VerticalBone based on box type and direction type
    /// </summary>
    private void ActivateDirectionBones()
    {
        if (directionBone == null)
        {
            // Try to find DirectionBone in children
            directionBone = transform.Find("DirectionBone");
            if (directionBone == null)
            {
                Debug.LogWarning($"DirectionBone not found in BoxObject {gameObject.name}");
                return;
            }
        }
        
        // Get block GameObject name based on box type
        string blockName = GetColliderNameByType(boxType);
        
        // Find the block GameObject under DirectionBone
        Transform blockTransform = directionBone.Find(blockName);
        if (blockTransform == null)
        {
            Debug.LogWarning($"Block GameObject '{blockName}' not found in DirectionBone of BoxObject {gameObject.name}");
            return;
        }
        
        // Find HorizontalBone and VerticalBone
        Transform horizontalBone = blockTransform.Find("HorizontalBone");
        Transform verticalBone = blockTransform.Find("VerticalBone");
        
        // Deactivate both first
        if (horizontalBone != null)
        {
            horizontalBone.gameObject.SetActive(false);
        }
        if (verticalBone != null)
        {
            verticalBone.gameObject.SetActive(false);
        }
        
        // Activate based on direction type
        switch (directionType)
        {
            case DirectionType.Horizontal:
                if (horizontalBone != null)
                {
                    horizontalBone.gameObject.SetActive(true);
                }
                else
                {
                    Debug.LogWarning($"HorizontalBone not found in {blockName} of BoxObject {gameObject.name}");
                }
                break;
                
            case DirectionType.Vertical:
                if (verticalBone != null)
                {
                    verticalBone.gameObject.SetActive(true);
                }
                else
                {
                    Debug.LogWarning($"VerticalBone not found in {blockName} of BoxObject {gameObject.name}");
                }
                break;
                
            case DirectionType.None:
                // Both are already deactivated
                break;
        }
    }
    
    /// <summary>
    /// Set sprite shape by BoxType from BoxConfig
    /// </summary>
    /// <param name="type">BoxType to set sprite for</param>
    public void SetSpriteByType(BoxType type)
    {
        SetSpriteByTypeAndColor(type, 0);
    }
    
    /// <summary>
    /// Set sprite shape by BoxType and color index from BoxConfig
    /// </summary>
    /// <param name="type">BoxType to set sprite for</param>
    /// <param name="colorIndex">Color index to get sprite from array</param>
    public void SetSpriteByTypeAndColor(BoxType type, int colorIndex)
    {
        // Set current color ID
        currentColorID = colorIndex;
        UpdateCountBackgroundColor(colorIndex);
        
        // Get BoxConfig if not assigned
        if (boxConfig == null)
        {
            boxConfig = Resources.Load<BoxConfig>("Configs/BoxConfig");
            if (boxConfig == null)
            {
                Debug.LogWarning($"BoxConfig not found at Resources/Configs/BoxConfig for BoxObject {gameObject.name}");
                return;
            }
        }
        
        // Get sprite from BoxConfig by type and color index
        Sprite boxSprite = boxConfig.GetBoxSpriteByTypeAndColor(type, colorIndex);
        
        // Get SpriteRenderer if not assigned
        if (mainShapeRenderer == null)
        {
            mainShapeRenderer = GetComponent<SpriteRenderer>();
            if (mainShapeRenderer == null)
            {
                mainShapeRenderer = GetComponentInChildren<SpriteRenderer>();
            }
        }
        
        if (mainShapeRenderer != null)
        {
            if (boxSprite != null)
            {
                mainShapeRenderer.sprite = boxSprite;
            }
            else
            {
                Debug.LogWarning($"Box sprite not found for BoxType {type} with color index {colorIndex} in BoxConfig for BoxObject {gameObject.name}");
            }
        }
        else
        {
            Debug.LogWarning($"MainShapeRenderer not found in BoxObject {gameObject.name}");
        }
    }
}
