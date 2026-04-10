using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatView : MonoBehaviour
{
    [Header("References")]
    [SerializeField]
    private SpriteRenderer catSprite;
    [SerializeField]
    private SpriteRenderer catSpriteShadow;
    [SerializeField]
    private Collider2D catCollider;
    [SerializeField]
    private Transform centerPoint;

    [SerializeField]
    private ColorConfig colorConfig;

    [Header("Color Info")]
    [SerializeField]
    private int currentColorID = -1;

    [Header("Block Info")]
    [SerializeField]
    private bool inBlock = false;

    [Header("Star Bubble")]
    [SerializeField]
    private GameObject starBubble;

    /// <summary>
    /// Get current color ID
    /// </summary>
    public int CurrentColorID
    {
        get { return currentColorID; }
    }

    /// <summary>
    /// Get or set whether this cat is in a block
    /// </summary>
    public bool InBlock
    {
        get { return inBlock; }
        set { inBlock = value; }
    }

    /// <summary>
    /// Get cat collider
    /// </summary>
    /// <returns>Collider2D component or null if not found</returns>
    public Collider2D GetCollider()
    {
        if (catCollider == null)
        {
            catCollider = GetComponent<Collider2D>();
            if (catCollider == null)
            {
                catCollider = GetComponentInChildren<Collider2D>();
            }
        }

        return catCollider;
    }

    /// <summary>
    /// Set cat sprite by color index from ColorConfig
    /// </summary>
    /// <param name="colorIndex">Color index from ColorConfig</param>
    public void SetSpriteByColorIndex(int colorIndex)
    {
        // Set current color ID
        currentColorID = colorIndex;

        // Get ColorConfig if not assigned
        if (colorConfig == null)
        {
            colorConfig = Resources.Load<ColorConfig>("Configs/ColorConfig");
            if (colorConfig == null)
            {
                Debug.LogError($"ColorConfig not found at Resources/Configs/ColorConfig for CatView {gameObject.name}");
                return;
            }
        }

        // Get sprite from ColorConfig
        Sprite sprite = colorConfig.GetCatSpriteByIndex(colorIndex);

        if (sprite == null)
        {
            Debug.LogWarning($"Cat sprite not found for color index {colorIndex} in CatView {gameObject.name}");
            return;
        }

        // Get SpriteRenderer if not assigned
        if (catSprite == null)
        {
            catSprite = GetComponent<SpriteRenderer>();
            if (catSprite == null)
            {
                catSprite = GetComponentInChildren<SpriteRenderer>();
            }
        }

        if (catSprite != null)
        {
            catSprite.sprite = sprite;
        }
        else
        {
            Debug.LogError($"SpriteRenderer not found in CatView {gameObject.name}");
        }
    }

    /// <summary>
    /// Get current cat sprite
    /// </summary>
    /// <returns>Current sprite or null</returns>
    public Sprite GetCatSprite()
    {
        if (catSprite == null)
        {
            catSprite = GetComponent<SpriteRenderer>();
            if (catSprite == null)
            {
                catSprite = GetComponentInChildren<SpriteRenderer>();
            }
        }

        return catSprite != null ? catSprite.sprite : null;
    }
    public void SetSpriteLayer(int layer)
    {
        if (catSprite != null)
        {
            catSprite.sortingOrder = 2 * layer;
            catSpriteShadow.sortingOrder = 2 * layer - 1;
        }
    }
    public void EnableCollider(bool enable)
    {
        if (catCollider != null)
        {
            catCollider.enabled = enable;
        }
    }
    public void SetTopLayer()
    {
        if (catSprite != null)
        {
            catSprite.sortingLayerName = "CatBlockFront";
            catSpriteShadow.enabled = false;
            catSprite.sortingOrder = 1000;
        }
    }
    public void EnableStarBubble(bool enable)
    {
        if (starBubble != null)
        {
            starBubble.SetActive(enable);
        }
    }
    /// <summary>
    /// Make this cat jump to a matching hole (box) by star: find a BoxObject with currentCount > 0
    /// and same colorIndex, then the first BoxPartObject of that box receives this cat via GetCatFromStarBubble.
    /// </summary>
    public void CatJumpToHoleByStar()
    {
        BoardManager boardManager = FindObjectOfType<BoardManager>();
        if (boardManager == null)
        {
            return;
        }

        BoxPartObject targetBoxPart = boardManager.GetFirstBoxPartForStarCat(currentColorID);
        if (targetBoxPart == null)
        {
            return;
        }

        targetBoxPart.GetCatFromStarBubble(this);
    }
}
