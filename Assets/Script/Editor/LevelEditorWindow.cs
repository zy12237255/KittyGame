using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;

public enum ViewMode
{
    Tree,
    Grid
}

public class CellData
{
    public int x;
    public int y;
    public int? catColor;
    public int? boxColor; // Color index from Boxes
    public int colorPath;
    public bool hasCat;
    
    public CellData(int x, int y, int colorPath)
    {
        this.x = x;
        this.y = y;
        this.colorPath = colorPath;
        this.hasCat = false;
        this.catColor = null;
        this.boxColor = null;
    }
}

public class BoxData
{
    public int index;
    public int colorIndex;
    public int count;
    public List<string> gimmicks = new List<string>();
}

public class LevelEditorWindow : EditorWindow
{
    private ViewMode currentViewMode = ViewMode.Tree;
    private Vector2 treeViewScrollPosition;
    private Vector2 gridScrollPosition;
    private Dictionary<string, JSONTreeViewNode> levelNodes = new Dictionary<string, JSONTreeViewNode>();
    private List<string> sortedLevelNames = new List<string>();
    private string selectedLevelPath = null;
    private JSONTreeViewNode selectedNode = null;
    private Dictionary<string, List<CellData>> levelCellData = new Dictionary<string, List<CellData>>();
    private Dictionary<string, List<BoxData>> levelBoxData = new Dictionary<string, List<BoxData>>();
    private ColorConfig colorConfig;
    private Texture2D circleTexture;
    private bool showBoxesGimmicks = true;
    private Vector2 boxesInfoScroll;
    
    private const float INDENT_WIDTH = 16f;
    private const float CELL_SIZE = 60f;
    private const float CELL_PADDING = 2f;
    private const float CAT_CIRCLE_SIZE = 24f;

    [MenuItem("Tools/Level Editor")]
    public static void ShowWindow()
    {
        LevelEditorWindow window = GetWindow<LevelEditorWindow>("Level Editor");
        window.minSize = new Vector2(600, 400);
        window.LoadAllLevels();
    }

    private void OnEnable()
    {
        LoadColorConfig();
        LoadAllLevels();
    }

    private void LoadColorConfig()
    {
        colorConfig = Resources.Load<ColorConfig>("Configs/ColorConfig");
        if (colorConfig == null)
        {
            Debug.LogWarning("ColorConfig not found at Resources/Configs/ColorConfig");
        }
        
        // Create circle texture for cat rendering
        CreateCircleTexture();
    }

    private void CreateCircleTexture()
    {
        int size = 64;
        circleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size / 2f;
        float centerX = size / 2f;
        float centerY = size / 2f;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distX = x - centerX;
                float distY = y - centerY;
                float distance = Mathf.Sqrt(distX * distX + distY * distY);
                
                if (distance <= radius)
                {
                    // Smooth edge
                    float alpha = 1f;
                    if (distance > radius - 2f)
                    {
                        alpha = (radius - distance) / 2f;
                    }
                    circleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    circleTexture.SetPixel(x, y, Color.clear);
                }
            }
        }
        
        circleTexture.Apply();
    }

    private void LoadAllLevels()
    {
        levelNodes.Clear();
        
        string jsonLevelsPath = Path.Combine(Application.dataPath, "Resources", "JsonLevels");
        if (!Directory.Exists(jsonLevelsPath))
        {
            Debug.LogWarning($"Directory not found: {jsonLevelsPath}");
            return;
        }

        string[] jsonFiles = Directory.GetFiles(jsonLevelsPath, "*.json", SearchOption.TopDirectoryOnly);
        
        foreach (string filePath in jsonFiles)
        {
            string fileName = Path.GetFileName(filePath);
            try
            {
                string jsonContent = File.ReadAllText(filePath);
                JSONTreeViewNode rootNode = SimpleJSONParser.Parse(jsonContent);
                rootNode.Name = fileName;
                levelNodes[fileName] = rootNode;
                
                // Parse cells data for grid view
                ParseCellsData(fileName, rootNode);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error parsing {fileName}: {e.Message}");
            }
        }

        // Sort level names
        sortedLevelNames = levelNodes.Keys.OrderBy(k => {
            string numStr = Path.GetFileNameWithoutExtension(k);
            return int.TryParse(numStr, out int num) ? num : int.MaxValue;
        }).ToList();

        // Select first level if available
        if (sortedLevelNames.Count > 0 && selectedLevelPath == null)
        {
            selectedLevelPath = sortedLevelNames[0];
        }
        // Keep selected level if it still exists, otherwise select first
        else if (sortedLevelNames.Count > 0 && !levelNodes.ContainsKey(selectedLevelPath))
        {
            selectedLevelPath = sortedLevelNames[0];
        }
    }

    private void OnGUI()
    {
        // Handle keyboard shortcuts
        HandleKeyboardInput();

        EditorGUILayout.BeginVertical();

        // Compact Level Navigation Toolbar
        DrawLevelNavigation();

        // Content View (Tree or Grid)
        if (currentViewMode == ViewMode.Tree)
        {
            DrawTreeView();
        }
        else
        {
            DrawGridView();
        }

        EditorGUILayout.EndVertical();
    }

    private void HandleKeyboardInput()
    {
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode != KeyCode.None)
        {
            // Left Arrow or A key - Previous level
            if ((e.keyCode == KeyCode.LeftArrow || e.keyCode == KeyCode.A) && e.modifiers == EventModifiers.None)
            {
                NavigateToPreviousLevel();
                e.Use();
                Repaint();
            }
            // Right Arrow or D key - Next level
            else if ((e.keyCode == KeyCode.RightArrow || e.keyCode == KeyCode.D) && e.modifiers == EventModifiers.None)
            {
                NavigateToNextLevel();
                e.Use();
                Repaint();
            }
        }
    }

    private void DrawLevelNavigation()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

        // Previous Button
        GUI.enabled = sortedLevelNames.Count > 0 && GetCurrentLevelIndex() > 0;
        if (GUILayout.Button("◀", EditorStyles.toolbarButton, GUILayout.Width(30)))
        {
            NavigateToPreviousLevel();
        }

        // Level Info and Quick Jump
        GUI.enabled = true;
        if (sortedLevelNames.Count > 0 && selectedLevelPath != null)
        {
            int currentIndex = GetCurrentLevelIndex();
            string levelNumber = Path.GetFileNameWithoutExtension(selectedLevelPath);
            string levelInfo = $"Level {levelNumber} ({currentIndex + 1} / {sortedLevelNames.Count})";
            
            // Level name with dropdown to jump
            if (GUILayout.Button(levelInfo, EditorStyles.toolbarDropDown, GUILayout.Width(200)))
            {
                ShowLevelJumpMenu();
            }
        }
        else
        {
            GUILayout.Label("No levels loaded", EditorStyles.toolbarButton, GUILayout.Width(200));
        }

        // Next Button
        GUI.enabled = sortedLevelNames.Count > 0 && GetCurrentLevelIndex() < sortedLevelNames.Count - 1;
        if (GUILayout.Button("▶", EditorStyles.toolbarButton, GUILayout.Width(30)))
        {
            NavigateToNextLevel();
        }

        GUI.enabled = true;

        // Refresh Button
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
        {
            LoadAllLevels();
        }

        GUILayout.FlexibleSpace();

        // View Mode Toggle
        EditorGUI.BeginChangeCheck();
        currentViewMode = (ViewMode)EditorGUILayout.EnumPopup(currentViewMode, EditorStyles.toolbarPopup, GUILayout.Width(80));
        if (EditorGUI.EndChangeCheck())
        {
            gridScrollPosition = Vector2.zero;
        }

        EditorGUILayout.EndHorizontal();
    }

    private int GetCurrentLevelIndex()
    {
        if (selectedLevelPath == null || sortedLevelNames.Count == 0)
            return -1;
        
        return sortedLevelNames.IndexOf(selectedLevelPath);
    }

    private void NavigateToPreviousLevel()
    {
        int currentIndex = GetCurrentLevelIndex();
        if (currentIndex > 0)
        {
            selectedLevelPath = sortedLevelNames[currentIndex - 1];
            treeViewScrollPosition = Vector2.zero; // Reset scroll position
        }
    }

    private void NavigateToNextLevel()
    {
        int currentIndex = GetCurrentLevelIndex();
        if (currentIndex >= 0 && currentIndex < sortedLevelNames.Count - 1)
        {
            selectedLevelPath = sortedLevelNames[currentIndex + 1];
            treeViewScrollPosition = Vector2.zero; // Reset scroll position
        }
    }

    private void ShowLevelJumpMenu()
    {
        GenericMenu menu = new GenericMenu();
        
        foreach (string levelName in sortedLevelNames)
        {
            string levelNumber = Path.GetFileNameWithoutExtension(levelName);
            bool isSelected = levelName == selectedLevelPath;
            
            menu.AddItem(new GUIContent($"Level {levelNumber}"), isSelected, () => {
                selectedLevelPath = levelName;
                treeViewScrollPosition = Vector2.zero;
            });
        }
        
        menu.ShowAsContext();
    }

    private void DrawTreeView()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        if (selectedLevelPath != null && levelNodes.ContainsKey(selectedLevelPath))
        {
            treeViewScrollPosition = EditorGUILayout.BeginScrollView(treeViewScrollPosition, GUILayout.ExpandHeight(true));
            
            DrawTreeNode(levelNodes[selectedLevelPath], 0);
            
            EditorGUILayout.EndScrollView();
        }
        else
        {
            EditorGUILayout.LabelField("No level selected", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawTreeNode(JSONTreeViewNode node, int depth)
    {
        EditorGUILayout.BeginHorizontal();
        
        // Indentation
        GUILayout.Space(depth * INDENT_WIDTH);

        // Expand/Collapse Button
        bool hasChildren = node.Children.Count > 0;
        if (hasChildren)
        {
            node.IsExpanded = EditorGUILayout.Foldout(node.IsExpanded, "", true);
        }
        else
        {
            GUILayout.Space(14f); // Space for foldout when no children
        }

        // Node Name and Value
        GUIStyle nameStyle = EditorStyles.label;
        string[] displayParts = GetNodeDisplayParts(node);
        
        // Draw the key name
        if (displayParts[0].Length > 0)
        {
            EditorGUILayout.LabelField(displayParts[0], nameStyle);
        }

        // Draw the value with red coloring if it's a primitive value
        if (displayParts.Length > 1 && displayParts[1].Length > 0)
        {
            if (node.IsPrimitive)
            {
                GUI.color = new Color(1f, 0.4f, 0.4f); // Red for values
                EditorGUILayout.LabelField(displayParts[1], nameStyle);
                GUI.color = Color.white;
            }
            else
            {
                // For collapsed objects/arrays, draw the summary with grey background style
                GUIStyle summaryStyle = new GUIStyle(EditorStyles.miniButton);
                summaryStyle.normal.textColor = Color.grey;
                summaryStyle.normal.background = null;
                summaryStyle.alignment = TextAnchor.MiddleLeft;
                summaryStyle.padding = new RectOffset(4, 4, 0, 0);
                
                EditorGUILayout.LabelField(displayParts[1], summaryStyle);
            }
        }

        EditorGUILayout.EndHorizontal();

        // Draw children if expanded
        if (hasChildren && node.IsExpanded)
        {
            foreach (var child in node.Children)
            {
                DrawTreeNode(child, depth + 1);
            }
        }
    }

    private string[] GetNodeDisplayParts(JSONTreeViewNode node)
    {
        if (node.IsPrimitive)
        {
            // For primitive values, return ["Key:", "Value"]
            string valueStr = GetValueDisplay(node);
            return new string[] { $"{node.Name}:", valueStr };
        }
        else if (node.ValueType == JSONValueType.Object)
        {
            int propCount = node.Children.Count;
            if (node.IsExpanded)
            {
                return new string[] { $"{node.Name}:", "{" };
            }
            else
            {
                return new string[] { $"{node.Name}:", $"{{ {propCount} props }}" };
            }
        }
        else if (node.ValueType == JSONValueType.Array)
        {
            int itemCount = node.Children.Count;
            if (node.IsExpanded)
            {
                return new string[] { $"{node.Name}:", "[" };
            }
            else
            {
                return new string[] { $"{node.Name}:", $"[ {itemCount} items ]" };
            }
        }

        return new string[] { node.Name, "" };
    }

    private string GetValueDisplay(JSONTreeViewNode node)
    {
        switch (node.ValueType)
        {
            case JSONValueType.Null:
                return "null";
            case JSONValueType.String:
                return $"\"{node.Value}\"";
            case JSONValueType.Boolean:
                return node.Value?.ToString().ToLower() ?? "false";
            case JSONValueType.Number:
                return node.Value?.ToString() ?? "0";
            default:
                return "";
        }
    }

    private void ParseCellsData(string fileName, JSONTreeViewNode rootNode)
    {
        List<CellData> cells = new List<CellData>();
        
        // Find Cells array in the tree
        JSONTreeViewNode cellsNode = FindChildNode(rootNode, "Cells");
        if (cellsNode != null && cellsNode.ValueType == JSONValueType.Array)
        {
            foreach (var cellNode in cellsNode.Children)
            {
                if (cellNode.ValueType == JSONValueType.Object)
                {
                    // Find Index node
                    JSONTreeViewNode indexNode = FindChildNode(cellNode, "Index");
                    if (indexNode != null && indexNode.ValueType == JSONValueType.Object)
                    {
                        int x = 0, y = 0;
                        int colorPath = -1;
                        int? catColor = null;
                        
                        // Get x and y from Index
                        JSONTreeViewNode xNode = FindChildNode(indexNode, "x");
                        JSONTreeViewNode yNode = FindChildNode(indexNode, "y");
                        if (xNode != null && xNode.ValueType == JSONValueType.Number)
                            x = Convert.ToInt32(xNode.Value);
                        if (yNode != null && yNode.ValueType == JSONValueType.Number)
                            y = Convert.ToInt32(yNode.Value);
                        
                        // Get ColorPath
                        JSONTreeViewNode colorPathNode = FindChildNode(cellNode, "ColorPath");
                        if (colorPathNode != null && colorPathNode.ValueType == JSONValueType.Number)
                            colorPath = Convert.ToInt32(colorPathNode.Value);
                        
                        // Get Cat Color if exists
                        JSONTreeViewNode catNode = FindChildNode(cellNode, "Cat");
                        if (catNode != null && catNode.ValueType == JSONValueType.Object)
                        {
                            JSONTreeViewNode colorNode = FindChildNode(catNode, "Color");
                            if (colorNode != null && colorNode.ValueType == JSONValueType.Number)
                            {
                                catColor = Convert.ToInt32(colorNode.Value);
                            }
                        }
                        
                        CellData cell = new CellData(x, y, colorPath);
                        cell.hasCat = catColor.HasValue;
                        cell.catColor = catColor;
                        cells.Add(cell);
                    }
                }
            }
        }
        
        // Parse Boxes and assign colors to cells
        ParseBoxesData(fileName, rootNode, cells);
        
        levelCellData[fileName] = cells;
    }

    private void ParseBoxesData(string fileName, JSONTreeViewNode rootNode, List<CellData> cells)
    {
        List<BoxData> boxes = new List<BoxData>();

        // Create a map of cells by coordinate for quick lookup
        Dictionary<string, CellData> cellMap = new Dictionary<string, CellData>();
        foreach (var cell in cells)
        {
            string key = $"{cell.x},{cell.y}";
            cellMap[key] = cell;
        }
        
        // Find Boxes array in the tree
        JSONTreeViewNode boxesNode = FindChildNode(rootNode, "Boxes");
        if (boxesNode != null && boxesNode.ValueType == JSONValueType.Array)
        {
            foreach (var boxNode in boxesNode.Children)
            {
                if (boxNode.ValueType == JSONValueType.Object)
                {
                    BoxData boxData = new BoxData();

                    // Box index
                    JSONTreeViewNode indexNode = FindChildNode(boxNode, "Index");
                    if (indexNode != null && indexNode.ValueType == JSONValueType.Number)
                    {
                        boxData.index = Convert.ToInt32(indexNode.Value);
                    }

                    // Get Box Color
                    int boxColorIndex = -1;
                    JSONTreeViewNode colorNode = FindChildNode(boxNode, "Color");
                    if (colorNode != null && colorNode.ValueType == JSONValueType.Number)
                    {
                        boxColorIndex = Convert.ToInt32(colorNode.Value);
                    }
                    boxData.colorIndex = boxColorIndex;

                    // Box count (optional)
                    JSONTreeViewNode countNode = FindChildNode(boxNode, "Count");
                    if (countNode != null && countNode.ValueType == JSONValueType.Number)
                    {
                        boxData.count = Convert.ToInt32(countNode.Value);
                    }
                    else
                    {
                        boxData.count = 0;
                    }

                    // Box gimmicks
                    JSONTreeViewNode gimmicksNode = FindChildNode(boxNode, "Gimmicks");
                    if (gimmicksNode != null && gimmicksNode.ValueType == JSONValueType.Array)
                    {
                        foreach (var gimmick in gimmicksNode.Children)
                        {
                            boxData.gimmicks.Add(FormatJsonInline(gimmick));
                        }
                    }
                    
                    // Get Cells array from Box
                    JSONTreeViewNode boxCellsNode = FindChildNode(boxNode, "Cells");
                    if (boxCellsNode != null && boxCellsNode.ValueType == JSONValueType.Array)
                    {
                        foreach (var boxCellNode in boxCellsNode.Children)
                        {
                            if (boxCellNode.ValueType == JSONValueType.Object)
                            {
                                // Get x and y from box cell
                                int x = 0, y = 0;
                                JSONTreeViewNode xNode = FindChildNode(boxCellNode, "x");
                                JSONTreeViewNode yNode = FindChildNode(boxCellNode, "y");
                                if (xNode != null && xNode.ValueType == JSONValueType.Number)
                                    x = Convert.ToInt32(xNode.Value);
                                if (yNode != null && yNode.ValueType == JSONValueType.Number)
                                    y = Convert.ToInt32(yNode.Value);
                                
                                // Assign box color to the corresponding cell
                                string key = $"{x},{y}";
                                if (cellMap.ContainsKey(key) && boxColorIndex >= 0)
                                {
                                    cellMap[key].boxColor = boxColorIndex;
                                }
                            }
                        }
                    }

                    boxes.Add(boxData);
                }
            }
        }

        levelBoxData[fileName] = boxes;
    }

    private JSONTreeViewNode FindChildNode(JSONTreeViewNode parent, string name)
    {
        foreach (var child in parent.Children)
        {
            if (child.Name == name)
                return child;
        }
        return null;
    }

    private string FormatJsonInline(JSONTreeViewNode node)
    {
        if (node == null) return "null";

        if (node.ValueType == JSONValueType.Object)
        {
            // Only show primitive fields for compactness
            List<string> parts = new List<string>();
            foreach (var child in node.Children)
            {
                if (child.IsPrimitive)
                {
                    parts.Add($"{child.Name}:{GetValueDisplay(child)}");
                }
            }
            return parts.Count > 0 ? "{ " + string.Join(", ", parts) + " }" : "{ }";
        }

        if (node.ValueType == JSONValueType.Array)
        {
            if (node.Children.Count == 0) return "[]";
            // show count only to avoid spam
            return $"[ {node.Children.Count} items ]";
        }

        return GetValueDisplay(node);
    }

    private void DrawGridView()
    {
        EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

        if (selectedLevelPath != null && levelCellData.ContainsKey(selectedLevelPath))
        {
            List<CellData> cells = levelCellData[selectedLevelPath];
            
            if (cells.Count == 0)
            {
                EditorGUILayout.LabelField("No cells found in this level", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                // Calculate grid bounds
                int minX = cells.Min(c => c.x);
                int maxX = cells.Max(c => c.x);
                int minY = cells.Min(c => c.y);
                int maxY = cells.Max(c => c.y);
                int gridWidth = maxX - minX + 1;
                int gridHeight = maxY - minY + 1;

                int catCount = cells.Count(c => c.hasCat);
                int boxesCount = levelBoxData.TryGetValue(selectedLevelPath, out var bx) ? bx.Count : 0;

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"Grid: {gridWidth} x {gridHeight}", GUILayout.Width(120));
                EditorGUILayout.LabelField($"Cats: {catCount}", GUILayout.Width(80));
                EditorGUILayout.LabelField($"Boxes: {boxesCount}", GUILayout.Width(90));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                
                // Create cell map for quick lookup
                Dictionary<string, CellData> cellMap = new Dictionary<string, CellData>();
                foreach (var cell in cells)
                {
                    string key = $"{cell.x},{cell.y}";
                    cellMap[key] = cell;
                }
                
                gridScrollPosition = EditorGUILayout.BeginScrollView(gridScrollPosition, GUILayout.ExpandHeight(true));
                
                // Calculate total grid size with padding for labels
                // Y-axis labels at left, X-axis labels at bottom
                float labelWidth = 30f;
                float labelHeight = 20f;
                float totalWidth = gridWidth * (CELL_SIZE + CELL_PADDING) + labelWidth;
                float totalHeight = gridHeight * (CELL_SIZE + CELL_PADDING) + labelHeight; // X-axis labels at bottom
                
                Rect gridRect = GUILayoutUtility.GetRect(totalWidth, totalHeight);
                
                // Adjust grid rect to account for labels
                // Y-axis labels at left, X-axis labels at bottom
                Rect actualGridRect = new Rect(
                    gridRect.x + labelWidth,
                    gridRect.y,
                    gridWidth * (CELL_SIZE + CELL_PADDING),
                    gridHeight * (CELL_SIZE + CELL_PADDING)
                );
                
                // Draw Y-axis labels (left side, bottom to top - since origin is bottom-left)
                // Loop from minY (bottom) to maxY (top)
                for (int y = minY; y <= maxY; y++)
                {
                    // Calculate position from bottom
                    // minY should be at bottom, maxY should be at top
                    int gridY = y - minY; // 0 = minY (bottom), gridHeight-1 = maxY (top)
                    // Reverse: gridY = 0 should be at bottom, so we use (gridHeight - 1 - gridY)
                    float screenY = actualGridRect.y + (gridHeight - 1 - gridY) * (CELL_SIZE + CELL_PADDING);
                    Rect labelRect = new Rect(gridRect.x, screenY, labelWidth - 5, CELL_SIZE);
                    GUIStyle yLabelStyle = new GUIStyle(EditorStyles.miniLabel);
                    yLabelStyle.alignment = TextAnchor.MiddleRight;
                    EditorGUI.LabelField(labelRect, $"y:{y}", yLabelStyle);
                }
                
                // Draw grid cells
                // Origin (0,0) is at bottom-left
                // x increases from left to right
                // y increases from bottom to top
                for (int gridY = 0; gridY < gridHeight; gridY++)
                {
                    // gridY = 0 is maxY (top on screen), gridY = gridHeight-1 is minY (bottom on screen)
                    int y = maxY - gridY; // Actual y coordinate
                    
                    for (int gridX = 0; gridX < gridWidth; gridX++)
                    {
                        // gridX = 0 is minX (left), gridX = gridWidth-1 is maxX (right)
                        int x = minX + gridX; // Actual x coordinate (left to right)
                        
                        float screenX = actualGridRect.x + gridX * (CELL_SIZE + CELL_PADDING);
                        // Screen Y: gridY = 0 is at top, so we reverse the order
                        float screenY = actualGridRect.y + gridY * (CELL_SIZE + CELL_PADDING);
                        
                        string key = $"{x},{y}";
                        CellData cell = cellMap.ContainsKey(key) ? cellMap[key] : null;
                        
                        Rect cellRect = new Rect(screenX, screenY, CELL_SIZE, CELL_SIZE);
                        
                        // Draw cell background
                        if (cell != null)
                        {
                            // If cell has box color, use that color from ColorConfig (with transparency)
                            Color bgColor;
                            if (cell.boxColor.HasValue && colorConfig != null)
                            {
                                Color boxColor = colorConfig.GetColorByIndex(cell.boxColor.Value);
                                bgColor = new Color(boxColor.r * 0.3f, boxColor.g * 0.3f, boxColor.b * 0.3f, 1f); // Darker version for background
                            }
                            else
                            {
                                // Cell with data - darker background if has cat
                                bgColor = cell.hasCat ? new Color(0.3f, 0.3f, 0.3f) : new Color(0.25f, 0.25f, 0.25f);
                            }
                            EditorGUI.DrawRect(cellRect, bgColor);
                            
                            // Draw border with box color if available
                            Color borderColor = Color.gray;
                            if (cell.boxColor.HasValue && colorConfig != null)
                            {
                                Color boxColor = colorConfig.GetColorByIndex(cell.boxColor.Value);
                                borderColor = new Color(boxColor.r, boxColor.g, boxColor.b, 0.8f);
                            }
                            EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, cellRect.width, 2), borderColor);
                            EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y, 2, cellRect.height), borderColor);
                            EditorGUI.DrawRect(new Rect(cellRect.x + cellRect.width - 2, cellRect.y, 2, cellRect.height), borderColor);
                            EditorGUI.DrawRect(new Rect(cellRect.x, cellRect.y + cellRect.height - 2, cellRect.width, 2), borderColor);
                        }
                        else
                        {
                            // Empty cell - lighter background
                            EditorGUI.DrawRect(cellRect, new Color(0.2f, 0.2f, 0.2f));
                        }
                        
                        // Draw cell content
                        if (cell != null)
                        {
                            // Draw coordinates
                            GUIStyle coordStyle = new GUIStyle(EditorStyles.miniLabel);
                            coordStyle.alignment = TextAnchor.UpperLeft;
                            coordStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
                            Rect coordRect = new Rect(cellRect.x + 2, cellRect.y + 2, cellRect.width - 4, 12);
                            EditorGUI.LabelField(coordRect, $"({x},{y})", coordStyle);
                            
                            // Draw box color indicator if exists
                            if (cell.boxColor.HasValue && colorConfig != null)
                            {
                                Color boxColor = colorConfig.GetColorByIndex(cell.boxColor.Value);
                                GUIStyle boxStyle = new GUIStyle(EditorStyles.boldLabel);
                                boxStyle.alignment = TextAnchor.MiddleCenter;
                                boxStyle.fontSize = 14;
                                boxStyle.normal.textColor = boxColor;
                                // Draw a colored box indicator at the center
                                Rect boxIndicatorRect = new Rect(cellRect.x + cellRect.width / 2 - 15, cellRect.y + cellRect.height / 2 - 15, 30, 30);
                                EditorGUI.DrawRect(boxIndicatorRect, new Color(boxColor.r, boxColor.g, boxColor.b, 0.6f));
                                GUI.color = Color.white;
                            }
                            
                            // Draw cat circle if exists
                            if (cell.hasCat && cell.catColor.HasValue)
                            {
                                Color catColor = colorConfig != null 
                                    ? colorConfig.GetColorByIndex(cell.catColor.Value)
                                    : GetColorForIndex(cell.catColor.Value);
                                
                                // Draw circle at center of cell
                                float circleX = cellRect.x + (cellRect.width - CAT_CIRCLE_SIZE) / 2f;
                                float circleY = cellRect.y + (cellRect.height - CAT_CIRCLE_SIZE) / 2f;
                                Rect circleRect = new Rect(circleX, circleY, CAT_CIRCLE_SIZE, CAT_CIRCLE_SIZE);
                                
                                // Draw circle with cat color
                                if (circleTexture != null)
                                {
                                    Color originalColor = GUI.color;
                                    GUI.color = catColor;
                                    GUI.DrawTexture(circleRect, circleTexture);
                                    GUI.color = originalColor;
                                }
                                else
                                {
                                    // Fallback: draw solid circle using Handles
                                    Handles.BeginGUI();
                                    Handles.color = catColor;
                                    Vector3 center = new Vector3(circleRect.x + CAT_CIRCLE_SIZE / 2f, circleRect.y + CAT_CIRCLE_SIZE / 2f, 0);
                                    Handles.DrawSolidDisc(center, Vector3.forward, CAT_CIRCLE_SIZE / 2f);
                                    Handles.color = Color.white;
                                    Handles.EndGUI();
                                }
                            }
                            
                            // Draw ColorPath if not -1
                            if (cell.colorPath != -1)
                            {
                                GUIStyle pathStyle = new GUIStyle(EditorStyles.miniLabel);
                                pathStyle.alignment = TextAnchor.LowerRight;
                                pathStyle.normal.textColor = Color.yellow;
                                Rect pathRect = new Rect(cellRect.x + 2, cellRect.y + cellRect.height - 12, cellRect.width - 4, 12);
                                EditorGUI.LabelField(pathRect, $"P:{cell.colorPath}", pathStyle);
                            }
                        }
                        else
                        {
                            // Draw empty indicator
                            GUIStyle emptyStyle = new GUIStyle(EditorStyles.miniLabel);
                            emptyStyle.alignment = TextAnchor.MiddleCenter;
                            emptyStyle.normal.textColor = new Color(0.4f, 0.4f, 0.4f);
                            EditorGUI.LabelField(cellRect, "-", emptyStyle);
                        }
                    }
                }
                
                // Draw X-axis labels (bottom, left to right - since origin is bottom-left)
                for (int gridX = 0; gridX < gridWidth; gridX++)
                {
                    int x = minX + gridX; // Actual x coordinate (left to right)
                    float screenX = actualGridRect.x + gridX * (CELL_SIZE + CELL_PADDING);
                    Rect labelRect = new Rect(screenX, gridRect.y + actualGridRect.height, CELL_SIZE, labelHeight);
                    GUIStyle xLabelStyle = new GUIStyle(EditorStyles.miniLabel);
                    xLabelStyle.alignment = TextAnchor.MiddleCenter;
                    EditorGUI.LabelField(labelRect, $"x:{x}", xLabelStyle);
                }
                
                // Draw corner label (0,0 at bottom-left)
                Rect cornerRect = new Rect(gridRect.x, gridRect.y + actualGridRect.height, labelWidth - 5, labelHeight);
                GUIStyle cornerStyle = new GUIStyle(EditorStyles.miniLabel);
                cornerStyle.alignment = TextAnchor.MiddleRight;
                EditorGUI.LabelField(cornerRect, "O", cornerStyle);
                
                EditorGUILayout.EndScrollView();

                DrawBoxesGimmicksPanel(selectedLevelPath);
            }
        }
        else
        {
            EditorGUILayout.LabelField("No level selected", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawBoxesGimmicksPanel(string fileName)
    {
        if (!levelBoxData.TryGetValue(fileName, out var boxes))
            return;

        showBoxesGimmicks = EditorGUILayout.Foldout(showBoxesGimmicks, "Boxes -> Gimmicks", true);
        if (!showBoxesGimmicks) return;

        boxesInfoScroll = EditorGUILayout.BeginScrollView(boxesInfoScroll, GUILayout.Height(180));
        if (boxes.Count == 0)
        {
            EditorGUILayout.LabelField("No boxes", EditorStyles.centeredGreyMiniLabel);
        }
        else
        {
            foreach (var b in boxes.OrderBy(b => b.index))
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

                // color swatch
                Color swatch = Color.gray;
                if (colorConfig != null && b.colorIndex >= 0)
                    swatch = colorConfig.GetColorByIndex(b.colorIndex);
                Rect swatchRect = GUILayoutUtility.GetRect(14, 14, GUILayout.Width(14), GUILayout.Height(14));
                EditorGUI.DrawRect(swatchRect, swatch);

                EditorGUILayout.LabelField($"Box {b.index}  (Color {b.colorIndex}, Count {b.count})", GUILayout.Width(210));

                string gimmicksText = (b.gimmicks != null && b.gimmicks.Count > 0)
                    ? string.Join(" | ", b.gimmicks)
                    : "-";
                EditorGUILayout.LabelField(gimmicksText, EditorStyles.wordWrappedLabel);

                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();
    }

    private Color GetColorForIndex(int colorIndex)
    {
        // Fallback color mapping if ColorConfig is not available
        Color[] colors = new Color[]
        {
            Color.red,        // 0
            Color.green,      // 1
            Color.blue,       // 2
            Color.yellow,     // 3
            Color.magenta,    // 4
            Color.cyan,       // 5
            Color.white,      // 6
            new Color(1f, 0.5f, 0f), // 7 - Orange
            new Color(0.5f, 0f, 0.5f), // 8 - Purple
            new Color(0f, 0.5f, 0.5f), // 9 - Teal
        };
        
        if (colorIndex >= 0 && colorIndex < colors.Length)
            return colors[colorIndex];
        
        return Color.white;
    }
}