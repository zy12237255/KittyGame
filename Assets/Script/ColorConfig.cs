using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ColorElement
{
    public int index;
    public Color color;
    public Sprite catSprite;
    
    public ColorElement(int index, Color color)
    {
        this.index = index;
        this.color = color;
        this.catSprite = null;
    }
    
    public ColorElement(int index, Color color, Sprite catSprite)
    {
        this.index = index;
        this.color = color;
        this.catSprite = catSprite;
    }
}

[CreateAssetMenu(fileName = "ColorConfig", menuName = "Config/Color Config")]
public class ColorConfig : ScriptableObject
{
    [SerializeField]
    private List<ColorElement> colorElements = new List<ColorElement>();
    
    public List<ColorElement> ColorElements
    {
        get { return colorElements; }
        set { colorElements = value; }
    }
    
    /// <summary>
    /// Get color by index. Returns white if index not found.
    /// </summary>
    public Color GetColorByIndex(int index)
    {
        foreach (var element in colorElements)
        {
            if (element.index == index)
            {
                return element.color;
            }
        }
        return Color.white; // Default color if not found
    }
    
    /// <summary>
    /// Get cat sprite by index. Returns null if index not found.
    /// </summary>
    public Sprite GetCatSpriteByIndex(int index)
    {
        foreach (var element in colorElements)
        {
            if (element.index == index)
            {
                return element.catSprite;
            }
        }
        return null; // Default sprite if not found
    }
    
    /// <summary>
    /// Get color element by index. Returns null if not found.
    /// </summary>
    public ColorElement GetColorElementByIndex(int index)
    {
        foreach (var element in colorElements)
        {
            if (element.index == index)
            {
                return element;
            }
        }
        return null;
    }
    
    /// <summary>
    /// Check if color with given index exists
    /// </summary>
    public bool HasColorIndex(int index)
    {
        return GetColorElementByIndex(index) != null;
    }
    
    /// <summary>
    /// Add or update color element
    /// </summary>
    public void SetColor(int index, Color color)
    {
        ColorElement existing = GetColorElementByIndex(index);
        if (existing != null)
        {
            existing.color = color;
        }
        else
        {
            colorElements.Add(new ColorElement(index, color));
        }
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    
    /// <summary>
    /// Add or update color element with cat sprite
    /// </summary>
    public void SetColor(int index, Color color, Sprite catSprite)
    {
        ColorElement existing = GetColorElementByIndex(index);
        if (existing != null)
        {
            existing.color = color;
            existing.catSprite = catSprite;
        }
        else
        {
            colorElements.Add(new ColorElement(index, color, catSprite));
        }
        
        #if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        #endif
    }
    
    /// <summary>
    /// Set cat sprite for existing color element
    /// </summary>
    public void SetCatSprite(int index, Sprite catSprite)
    {
        ColorElement existing = GetColorElementByIndex(index);
        if (existing != null)
        {
            existing.catSprite = catSprite;
            
            #if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
            #endif
        }
        else
        {
            Debug.LogWarning($"Color element with index {index} not found, cannot set cat sprite");
        }
    }
    
    /// <summary>
    /// Remove color element by index
    /// </summary>
    public bool RemoveColor(int index)
    {
        for (int i = colorElements.Count - 1; i >= 0; i--)
        {
            if (colorElements[i].index == index)
            {
                colorElements.RemoveAt(i);
                #if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
                #endif
                return true;
            }
        }
        return false;
    }
}
