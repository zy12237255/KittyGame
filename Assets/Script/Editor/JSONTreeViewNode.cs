using System.Collections.Generic;

public enum JSONValueType
{
    Object,
    Array,
    String,
    Number,
    Boolean,
    Null
}

public class JSONTreeViewNode
{
    public string Name { get; set; }
    public JSONValueType ValueType { get; set; }
    public object Value { get; set; }
    public List<JSONTreeViewNode> Children { get; private set; }
    public bool IsExpanded { get; set; }
    public bool IsArrayItem { get; set; }

    public bool IsPrimitive
    {
        get
        {
            return ValueType != JSONValueType.Object && ValueType != JSONValueType.Array;
        }
    }

    public JSONTreeViewNode(string name, JSONValueType valueType, object value = null, bool isArrayItem = false)
    {
        Name = name;
        ValueType = valueType;
        Value = value;
        Children = new List<JSONTreeViewNode>();
        IsExpanded = false;
        IsArrayItem = isArrayItem;
    }

    public void AddChild(JSONTreeViewNode child)
    {
        Children.Add(child);
    }
}