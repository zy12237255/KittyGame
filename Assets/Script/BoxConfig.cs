using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BoxElement
{
    public BoxType boxType;
    public Sprite[] boxSprites; // Array of sprites, each element corresponds to a color index
    
    public BoxElement(BoxType boxType, Sprite[] boxSprites)
    {
        this.boxType = boxType;
        this.boxSprites = boxSprites;
    }
}

[CreateAssetMenu(fileName = "BoxConfig", menuName = "Config/Box Config")]
public class BoxConfig : ScriptableObject
{
    [SerializeField]
    private List<BoxElement> boxElements = new List<BoxElement>();
    
    public List<BoxElement> BoxElements
    {
        get { return boxElements; }
        set { boxElements = value; }
    }
    
    /// <summary>
    /// Get box sprite by BoxType and color index. Returns null if type not found or color index out of range.
    /// </summary>
    /// <param name="type">BoxType to get sprite for</param>
    /// <param name="colorIndex">Color index (array index)</param>
    /// <returns>Sprite at the specified color index, or null if not found</returns>
    public Sprite GetBoxSpriteByTypeAndColor(BoxType type, int colorIndex)
    {
        foreach (var element in boxElements)
        {
            if (element.boxType == type)
            {
                if (element.boxSprites != null && colorIndex >= 0 && colorIndex < element.boxSprites.Length)
                {
                    return element.boxSprites[colorIndex];
                }
                return null;
            }
        }
        return null; // Default sprite if not found
    }
    
    /// <summary>
    /// Get box sprite by BoxType (uses color index 0). Returns null if type not found.
    /// </summary>
    /// <param name="type">BoxType to get sprite for</param>
    /// <returns>Sprite at color index 0, or null if not found</returns>
    public Sprite GetBoxSpriteByType(BoxType type)
    {
        return GetBoxSpriteByTypeAndColor(type, 0);
    }
    
    /// <summary>
    /// Get box element by BoxType. Returns null if not found.
    /// </summary>
    public BoxElement GetBoxElementByType(BoxType type)
    {
        foreach (var element in boxElements)
        {
            if (element.boxType == type)
            {
                return element;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Check if box type exists in config
    /// </summary>
    public bool HasBoxType(BoxType type)
    {
        return GetBoxElementByType(type) != null;
    }
    
    /// <summary>
    /// Add or update box element with sprite array
    /// </summary>
    /// <param name="type">BoxType to set sprites for</param>
    /// <param name="boxSprites">Array of sprites, each element corresponds to a color index</param>
    public void SetBoxSprites(BoxType type, Sprite[] boxSprites)
    {
        BoxElement existing = GetBoxElementByType(type);
        if (existing != null)
        {
            existing.boxSprites = boxSprites;
        }
        else
        {
            boxElements.Add(new BoxElement(type, boxSprites));
        }
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    
    /// <summary>
    /// Set sprite for a specific color index of a box type
    /// </summary>
    /// <param name="type">BoxType to set sprite for</param>
    /// <param name="colorIndex">Color index (array index)</param>
    /// <param name="boxSprite">Sprite to set at the specified color index</param>
    public void SetBoxSprite(BoxType type, int colorIndex, Sprite boxSprite)
    {
        BoxElement existing = GetBoxElementByType(type);
        if (existing != null)
        {
            if (existing.boxSprites == null)
            {
                existing.boxSprites = new Sprite[colorIndex + 1];
            }
            else if (colorIndex >= existing.boxSprites.Length)
            {
                // Resize array to accommodate the new index
                Sprite[] newSprites = new Sprite[colorIndex + 1];
                System.Array.Copy(existing.boxSprites, newSprites, existing.boxSprites.Length);
                existing.boxSprites = newSprites;
            }
            existing.boxSprites[colorIndex] = boxSprite;
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
        else
        {
            // Create new element with array
            Sprite[] newSprites = new Sprite[colorIndex + 1];
            newSprites[colorIndex] = boxSprite;
            boxElements.Add(new BoxElement(type, newSprites));
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
    }
    
    /// <summary>
    /// Remove box element by type
    /// </summary>
    public bool RemoveBoxType(BoxType type)
    {
        for (int i = boxElements.Count - 1; i >= 0; i--)
        {
            if (boxElements[i].boxType == type)
            {
                boxElements.RemoveAt(i);
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
                return true;
            }
        }
        return false;
    }
}