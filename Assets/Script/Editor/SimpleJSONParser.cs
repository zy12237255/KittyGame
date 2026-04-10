using System;
using System.Collections.Generic;
using System.Text;

public static class SimpleJSONParser
{
    public static JSONTreeViewNode Parse(string jsonString)
    {
        int index = 0;
        SkipWhitespace(jsonString, ref index);
        
        if (index >= jsonString.Length)
            return new JSONTreeViewNode("Root", JSONValueType.Null);
            
        if (jsonString[index] == '{')
        {
            return ParseObject("Root", jsonString, ref index);
        }
        else if (jsonString[index] == '[')
        {
            return ParseArray("Root", jsonString, ref index);
        }
        else
        {
            return ParseValue("Root", jsonString, ref index);
        }
    }

    private static JSONTreeViewNode ParseObject(string name, string json, ref int index)
    {
        JSONTreeViewNode node = new JSONTreeViewNode(name, JSONValueType.Object);
        index++; // Skip '{'
        
        SkipWhitespace(json, ref index);
        
        while (index < json.Length && json[index] != '}')
        {
            string key = ParseString(json, ref index);
            
            SkipWhitespace(json, ref index);
            if (index < json.Length && json[index] == ':')
            {
                index++; // Skip ':'
            }
            SkipWhitespace(json, ref index);
            
            JSONTreeViewNode value = ParseAny(key, json, ref index);
            node.AddChild(value);
            
            SkipWhitespace(json, ref index);
            if (index < json.Length && json[index] == ',')
            {
                index++; // Skip ','
                SkipWhitespace(json, ref index);
            }
        }
        
        if (index < json.Length && json[index] == '}')
        {
            index++; // Skip '}'
        }
        
        return node;
    }

    private static JSONTreeViewNode ParseArray(string name, string json, ref int index)
    {
        JSONTreeViewNode node = new JSONTreeViewNode(name, JSONValueType.Array);
        index++; // Skip '['
        
        SkipWhitespace(json, ref index);
        
        int arrayIndex = 0;
        while (index < json.Length && json[index] != ']')
        {
            JSONTreeViewNode item = ParseAny(arrayIndex.ToString(), json, ref index);
            item.IsArrayItem = true;
            node.AddChild(item);
            arrayIndex++;
            
            SkipWhitespace(json, ref index);
            if (index < json.Length && json[index] == ',')
            {
                index++; // Skip ','
                SkipWhitespace(json, ref index);
            }
        }
        
        if (index < json.Length && json[index] == ']')
        {
            index++; // Skip ']'
        }
        
        return node;
    }

    private static JSONTreeViewNode ParseAny(string name, string json, ref int index)
    {
        SkipWhitespace(json, ref index);
        
        if (index >= json.Length)
            return new JSONTreeViewNode(name, JSONValueType.Null);
        
        char c = json[index];
        
        if (c == '{')
        {
            return ParseObject(name, json, ref index);
        }
        else if (c == '[')
        {
            return ParseArray(name, json, ref index);
        }
        else if (c == '"')
        {
            string value = ParseString(json, ref index);
            return new JSONTreeViewNode(name, JSONValueType.String, value);
        }
        else if (c == 't' || c == 'f')
        {
            return ParseBoolean(name, json, ref index);
        }
        else if (c == 'n')
        {
            ParseNull(json, ref index);
            return new JSONTreeViewNode(name, JSONValueType.Null);
        }
        else
        {
            return ParseNumber(name, json, ref index);
        }
    }

    private static JSONTreeViewNode ParseValue(string name, string json, ref int index)
    {
        return ParseAny(name, json, ref index);
    }

    private static string ParseString(string json, ref int index)
    {
        if (json[index] != '"')
            return "";
        
        index++; // Skip opening quote
        StringBuilder sb = new StringBuilder();
        
        while (index < json.Length)
        {
            char c = json[index];
            
            if (c == '\\' && index + 1 < json.Length)
            {
                index++; // Skip escape
                char escaped = json[index];
                switch (escaped)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case '\\': sb.Append('\\'); break;
                    case '"': sb.Append('"'); break;
                    default: sb.Append(escaped); break;
                }
                index++;
            }
            else if (c == '"')
            {
                index++; // Skip closing quote
                break;
            }
            else
            {
                sb.Append(c);
                index++;
            }
        }
        
        return sb.ToString();
    }

    private static JSONTreeViewNode ParseNumber(string name, string json, ref int index)
    {
        StringBuilder sb = new StringBuilder();
        bool hasDecimal = false;
        
        while (index < json.Length)
        {
            char c = json[index];
            
            if (char.IsDigit(c) || c == '-' || c == '+' || c == 'e' || c == 'E')
            {
                sb.Append(c);
                index++;
            }
            else if (c == '.')
            {
                hasDecimal = true;
                sb.Append(c);
                index++;
            }
            else
            {
                break;
            }
        }
        
        string numStr = sb.ToString();
        if (hasDecimal || numStr.Contains("e") || numStr.Contains("E"))
        {
            if (double.TryParse(numStr, out double d))
                return new JSONTreeViewNode(name, JSONValueType.Number, d);
        }
        else
        {
            if (long.TryParse(numStr, out long l))
                return new JSONTreeViewNode(name, JSONValueType.Number, l);
        }
        
        return new JSONTreeViewNode(name, JSONValueType.Number, 0);
    }

    private static JSONTreeViewNode ParseBoolean(string name, string json, ref int index)
    {
        if (json.Substring(index).StartsWith("true"))
        {
            index += 4;
            return new JSONTreeViewNode(name, JSONValueType.Boolean, true);
        }
        else if (json.Substring(index).StartsWith("false"))
        {
            index += 5;
            return new JSONTreeViewNode(name, JSONValueType.Boolean, false);
        }
        
        return new JSONTreeViewNode(name, JSONValueType.Boolean, false);
    }

    private static void ParseNull(string json, ref int index)
    {
        if (json.Substring(index).StartsWith("null"))
        {
            index += 4;
        }
    }

    private static void SkipWhitespace(string json, ref int index)
    {
        while (index < json.Length && char.IsWhiteSpace(json[index]))
        {
            index++;
        }
    }
}