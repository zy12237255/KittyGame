using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Direction enum for CatBlock
/// </summary>
public enum CatBlockDirection
{
    Up,
    Down,
    Left,
    Right
}

public class CatBlock : MonoBehaviour
{
    [Header("Direction Settings")]
    [SerializeField]
    private CatBlockDirection currentDirection = CatBlockDirection.Up;

    [Header("View Objects")]
    [SerializeField]
    private GameObject viewObjectUp;

    [SerializeField]
    private GameObject viewObjectDown;

    [SerializeField]
    private GameObject viewObjectLeft;

    [SerializeField]
    private GameObject viewObjectRight;

    [Header("Cat Prefab")]
    [SerializeField]
    private GameObject catObjectPrefab;

    [Header("Cats Parent")]
    [SerializeField]
    private Transform catsParent;

    [Header("Cat Spawn Settings")]
    [SerializeField]
    private float colorChangeDeltaY = 0.5f; // Delta Y when color changes from previous cat

    [Header("Raycast Settings")]
    [SerializeField]
    private float raycastDistance = 5f; // Distance of the raycast

    // List of spawned cat objects
    public List<GameObject> spawnedCats = new List<GameObject>();

    // Cached layer mask for BoxPart layer
    private int boxPartLayer = -1;

    [Header("Count UI")]
    [SerializeField]
    private int currentCount = 0;

    [SerializeField]
    private TMP_Text countText;

    [SerializeField]
    private Image countTextBackground;

    [SerializeField]
    private ColorConfig colorConfig;

    /// <summary>
    /// Get or set current direction
    /// When direction is set, the corresponding viewObject will be activated
    /// </summary>
    public CatBlockDirection Direction
    {
        get { return currentDirection; }
        set
        {
            currentDirection = value;
            UpdateViewObjects();
        }
    }

    void Start()
    {
        // Initialize view objects based on current direction
        UpdateViewObjects();

        // Cache BoxPart layer
        boxPartLayer = LayerMask.NameToLayer("BoxPart");
        if (boxPartLayer == -1)
        {
            Debug.LogWarning($"Layer 'BoxPart' not found for CatBlock {gameObject.name}");
        }

        UpdateCurrentCountUI();
    }

    void Update()
    {
        // Always check raycast for BoxPart
        CheckRaycastForBoxPart();
    }

    /// <summary>
    /// Get direction vector based on current CatBlock direction
    /// </summary>
    /// <returns>Direction vector (normalized)</returns>
    private Vector2 GetDirectionVector()
    {
        switch (currentDirection)
        {
            case CatBlockDirection.Up:
                return Vector2.up;
            case CatBlockDirection.Down:
                return Vector2.down;
            case CatBlockDirection.Left:
                return Vector2.left;
            case CatBlockDirection.Right:
                return Vector2.right;
            default:
                return Vector2.up;
        }
    }

    /// <summary>
    /// Perform raycast in the direction of CatBlock and check if it hits BoxPart layer
    /// </summary>
    /// <returns>True if hits BoxPart, false otherwise</returns>
    private bool CheckRaycastForBoxPart()
    {
        if (boxPartLayer == -1)
        {
            return false;
        }

        // Get direction vector based on current direction
        Vector2 direction = GetDirectionVector();

        // Calculate raycast origin (from CatBlock position)
        Vector2 origin = transform.position;

        // Perform raycast
        RaycastHit2D hit = Physics2D.Raycast(origin, direction, raycastDistance, 1 << boxPartLayer);

        // Check if hit BoxPart
        if (hit.collider != null)
        {
            // Hit BoxPart layer
            Debug.DrawRay(origin, direction * raycastDistance, Color.red);

            // Get BoxPartObject component from hit
            BoxPartObject boxPartObject = hit.collider.GetComponent<BoxPartObject>();
            if (boxPartObject == null)
            {
                boxPartObject = hit.collider.GetComponentInParent<BoxPartObject>();
            }

            if (boxPartObject != null)
            {
                // Get BoxObject to check currentCount
                BoxObject boxObject = boxPartObject.GetComponentInParent<BoxObject>();
                
                // Only process if BoxObject exists and has currentCount > 0
                if (boxObject != null && boxObject.CurrentCount > 0)
                {
                    int boxPartColorID = boxPartObject.CurrentColorID;

                    // Only check the first cat in spawnedCats
                    if (spawnedCats.Count > 0)
                    {
                        GameObject catObj = spawnedCats[0];
                        if (catObj == null)
                        {
                            spawnedCats.RemoveAt(0);
                            UpdateCurrentCountUI();
                        }
                        else
                        {
                            CatView catView = catObj.GetComponent<CatView>();
                            if (catView == null)
                            {
                                catView = catObj.GetComponentInChildren<CatView>();
                            }

                            if (catView != null && catView.CurrentColorID == boxPartColorID)
                            {
                                // Same color index - remove from spawnedCats and call GetCatFromBox
                                spawnedCats.RemoveAt(0);
                                boxObject.DecrementCurrentCount(1);
                                boxPartObject.GetCatFromBox(catObj);
                                UpdateCurrentCountUI();
                            }
                        }
                    }
                }
            }

            return true;
        }
        else
        {
            // No hit
            Debug.DrawRay(origin, direction * raycastDistance, Color.green);
            return false;
        }
    }

    /// <summary>
    /// Update view objects visibility based on current direction
    /// </summary>
    private void UpdateViewObjects()
    {
        // Deactivate all view objects first
        if (viewObjectUp != null)
        {
            viewObjectUp.SetActive(false);
        }
        if (viewObjectDown != null)
        {
            viewObjectDown.SetActive(false);
        }
        if (viewObjectLeft != null)
        {
            viewObjectLeft.SetActive(false);
        }
        if (viewObjectRight != null)
        {
            viewObjectRight.SetActive(false);
        }

        // Activate the view object corresponding to current direction
        switch (currentDirection)
        {
            case CatBlockDirection.Up:
                if (viewObjectUp != null)
                {
                    viewObjectUp.SetActive(true);
                }
                break;
            case CatBlockDirection.Down:
                if (viewObjectDown != null)
                {
                    viewObjectDown.SetActive(true);
                }
                break;
            case CatBlockDirection.Left:
                if (viewObjectLeft != null)
                {
                    viewObjectLeft.SetActive(true);
                }
                break;
            case CatBlockDirection.Right:
                if (viewObjectRight != null)
                {
                    viewObjectRight.SetActive(true);
                }
                break;
        }
    }

    /// <summary>
    /// Set direction and update view objects
    /// </summary>
    /// <param name="direction">New direction</param>
    public void SetDirection(CatBlockDirection direction)
    {
        Direction = direction;
    }

    /// <summary>
    /// Get list of spawned cat objects
    /// </summary>
    public List<GameObject> SpawnedCats
    {
        get { return spawnedCats; }
    }

    /// <summary>
    /// Spawn catObject at CatBlock position (with optional Y offset when color changes)
    /// </summary>
    /// <param name="colorIndex">Color index for the cat</param>
    /// <returns>Spawned cat GameObject</returns>
    public GameObject AddCat(int colorIndex)
    {
        // Load cat prefab if not assigned
        if (catObjectPrefab == null)
        {
            catObjectPrefab = Resources.Load<GameObject>("Prefabs/Cats/CatObject");
            if (catObjectPrefab == null)
            {
                Debug.LogError($"CatObject prefab not found at Resources/Prefabs/Cats/CatObject for CatBlock {gameObject.name}");
                return null;
            }
        }

        int availableIndex = spawnedCats.Count;
        Vector3 spawnPos = transform.position;

        // Check if color is different from previous cat and adjust Y position
        if (spawnedCats.Count > 0)
        {
            GameObject previousCat = spawnedCats[spawnedCats.Count - 1];
            spawnPos = previousCat.transform.position;
            if (previousCat != null)
            {
                CatView previousCatView = previousCat.GetComponent<CatView>();
                if (previousCatView == null)
                {
                    previousCatView = previousCat.GetComponentInChildren<CatView>();
                }

                if (previousCatView != null)
                {
                    int previousColorID = previousCatView.CurrentColorID;
                    // If color is different, increase Y position by delta
                    if (previousColorID != colorIndex)
                    {
                        spawnPos.y += colorChangeDeltaY;
                    }
                }
            }
        }

        // Get cats parent if not assigned
        if (catsParent == null)
        {
            // Try to find Cats parent in scene
            GameObject catsParentObj = GameObject.Find("Cats");
            if (catsParentObj != null)
            {
                catsParent = catsParentObj.transform;
            }
            else
            {
                // Create a parent for cats spawned from this block
                GameObject parentObj = new GameObject("Cats");
                parentObj.transform.SetParent(transform);
                parentObj.transform.localPosition = Vector3.zero;
                catsParent = parentObj.transform;
            }
        }

        // Spawn cat at the position
        GameObject catObj = Instantiate(catObjectPrefab, spawnPos, Quaternion.identity, catsParent);
        catObj.name = $"Cat_Block_{gameObject.name}_Color_{colorIndex}_{availableIndex}";

        // Set sprite and inBlock using CatView component
        CatView catView = catObj.GetComponent<CatView>();
        if (catView == null)
        {
            catView = catObj.GetComponentInChildren<CatView>();
        }

        if (catView != null)
        {
            catView.SetSpriteLayer(-spawnedCats.Count);
            catView.EnableCollider(false);
            catView.SetSpriteByColorIndex(colorIndex);
            // Set inBlock to true for cats spawned in block
            catView.InBlock = true;
        }
        else
        {
            Debug.LogWarning($"CatView component not found in spawned cat {catObj.name}");
        }

        // Add to spawned cats list
        spawnedCats.Add(catObj);
        UpdateCurrentCountUI();

        return catObj;
    }

    /// <summary>
    /// Removes invalid cats from spawnedCats and repositions remaining cats: first at block position, then stacked with colorChangeDeltaY when color changes.
    /// Call after removing cats of invalid colors from spawnedCats.
    /// </summary>
    public void RefreshCatListAndPositions()
    {
        // Remove nulls
        spawnedCats.RemoveAll(c => c == null);

        if (spawnedCats.Count == 0)
        {
            UpdateCurrentCountUI();
            return;
        }

        Vector3 pos = transform.position;
        int previousColorID = -1;

        for (int i = 0; i < spawnedCats.Count; i++)
        {
            GameObject catObj = spawnedCats[i];
            if (catObj == null) continue;

            CatView catView = catObj.GetComponent<CatView>();
            if (catView == null) catView = catObj.GetComponentInChildren<CatView>();

            if (i > 0 && catView != null && previousColorID >= 0 && catView.CurrentColorID != previousColorID)
                pos.y += colorChangeDeltaY;

            catObj.transform.position = pos;
            pos = catObj.transform.position;

            if (catView != null)
            {
                previousColorID = catView.CurrentColorID;
                catView.SetSpriteLayer(-i);
            }
        }

        UpdateCurrentCountUI();
    }

    private void EnsureColorConfig()
    {
        if (colorConfig != null)
        {
            return;
        }

        colorConfig = Resources.Load<ColorConfig>("Configs/ColorConfig");
    }

    private void UpdateCurrentCountUI()
    {
        // Clean nulls at the front (so "first cat" is meaningful)
        while (spawnedCats.Count > 0 && spawnedCats[0] == null)
        {
            spawnedCats.RemoveAt(0);
        }

        if (spawnedCats.Count == 0)
        {
            currentCount = 0;
            if (countText != null)
            {
                countText.text = "0";
            }
            if (countTextBackground != null && countTextBackground.transform.parent != null)
            {
                countTextBackground.transform.parent.gameObject.SetActive(false);
            }
            return;
        }
        else
        {
            if (countTextBackground != null && countTextBackground.transform.parent != null)
            {
                countTextBackground.transform.parent.gameObject.SetActive(true);
            }
        }

        // Determine first color
        GameObject firstCatObj = spawnedCats[0];
        CatView firstCatView = firstCatObj != null ? firstCatObj.GetComponent<CatView>() : null;
        if (firstCatView == null && firstCatObj != null)
        {
            firstCatView = firstCatObj.GetComponentInChildren<CatView>();
        }

        if (firstCatView == null)
        {
            currentCount = 0;
            if (countText != null)
            {
                countText.text = "0";
            }
            return;
        }

        int firstColorID = firstCatView.CurrentColorID;

        // Count consecutive cats from start with same color
        int count = 0;
        for (int i = 0; i < spawnedCats.Count; i++)
        {
            GameObject cObj = spawnedCats[i];
            if (cObj == null)
            {
                break;
            }

            CatView cView = cObj.GetComponent<CatView>();
            if (cView == null)
            {
                cView = cObj.GetComponentInChildren<CatView>();
            }

            if (cView == null || cView.CurrentColorID != firstColorID)
            {
                break;
            }

            count++;
        }

        currentCount = count;
        if (countText != null)
        {
            countText.text = currentCount.ToString();
        }

        if (countTextBackground != null)
        {
            EnsureColorConfig();
            if (colorConfig != null)
            {
                countTextBackground.color = colorConfig.GetColorByIndex(firstColorID);
            }
        }
    }

    /// <summary>
    /// Check if raycast hits BoxPart (public method for external access)
    /// </summary>
    /// <returns>True if hits BoxPart, false otherwise</returns>
    public bool IsHittingBoxPart()
    {
        return CheckRaycastForBoxPart();
    }
}
