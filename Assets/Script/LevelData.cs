using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Data model for level JSON file
/// </summary>
[System.Serializable]
public class LevelData
{
    [JsonProperty("Index")]
    public int Index { get; set; }
    
    [JsonProperty("Difficulty")]
    public string Difficulty { get; set; }
    
    [JsonProperty("TimeLimit")]
    public int TimeLimit { get; set; }
    
    [JsonProperty("Cells")]
    public List<CellData> Cells { get; set; }
    
    [JsonProperty("Boxes")]
    public List<BoxData> Boxes { get; set; }
    
    [JsonProperty("Groups")]
    public List<object> Groups { get; set; }
    
    [JsonProperty("Scratchers")]
    public List<object> Scratchers { get; set; }
    
    [JsonProperty("Ropes")]
    public List<object> Ropes { get; set; }
    
    [JsonProperty("Barricades")]
    public object Barricades { get; set; }
}

/// <summary>
/// Data model for cell in level
/// </summary>
[System.Serializable]
public class CellData
{
    [JsonProperty("Index")]
    public IndexData Index { get; set; }
    
    [JsonProperty("Cat")]
    public CatData Cat { get; set; }
    
    [JsonProperty("Block")]
    public BlockData Block { get; set; }
    
    [JsonProperty("ColorPath")]
    public int ColorPath { get; set; }
}

/// <summary>
/// Data model for index coordinates (x, y)
/// </summary>
[System.Serializable]
public class IndexData
{
    [JsonProperty("x")]
    public int x { get; set; }
    
    [JsonProperty("y")]
    public int y { get; set; }
}

/// <summary>
/// Data model for cat data
/// </summary>
[System.Serializable]
public class CatData
{
    [JsonProperty("Color")]
    public int Color { get; set; }
    
    [JsonProperty("Gimmicks")]
    public List<object> Gimmicks { get; set; }
}

/// <summary>
/// Data model for block data (CatBlock)
/// </summary>
[System.Serializable]
public class BlockData
{
    [JsonProperty("Type")]
    public int Type { get; set; }
    
    [JsonProperty("Direction")]
    public int Direction { get; set; }
    
    [JsonProperty("Cats")]
    public List<CatData> Cats { get; set; }
}

/// <summary>
/// Data model for gimmick data
/// </summary>
[System.Serializable]
public class GimmickData
{
    [JsonProperty("Type")]
    public int Type { get; set; }
    
    [JsonProperty("Orientation")]
    public int Orientation { get; set; }
    
    [JsonProperty("Color")]
    public int Color { get; set; }
    
    [JsonProperty("Count")]
    public int Count { get; set; }
}

/// <summary>
/// Data model for box data
/// </summary>
[System.Serializable]
public class BoxData
{
    [JsonProperty("Index")]
    public int Index { get; set; }
    
    [JsonProperty("Color")]
    public int Color { get; set; }
    
    [JsonProperty("Cells")]
    public List<IndexData> Cells { get; set; }
    
    [JsonProperty("Count")]
    public int Count { get; set; }
    
    [JsonProperty("Gimmicks")]
    public List<GimmickData> Gimmicks { get; set; }
}
