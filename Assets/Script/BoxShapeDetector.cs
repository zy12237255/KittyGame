using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Detects BoxType based on cell position patterns
/// </summary>
public static class BoxShapeDetector
{
    /// <summary>
    /// Detect BoxType based on cell positions pattern
    /// </summary>
    /// <param name="cells">List of cell indices</param>
    /// <returns>Detected BoxType</returns>
    public static BoxType DetectBoxType(List<IndexData> cells)
    {
        if (cells == null || cells.Count == 0)
        {
            return BoxType.Block_01;
        }
        
        int count = cells.Count;
        
        // Normalize cells to start from (0,0)
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        foreach (var cell in cells)
        {
            if (cell.x < minX) minX = cell.x;
            if (cell.y < minY) minY = cell.y;
        }
        
        HashSet<Vector2Int> normalizedCells = new HashSet<Vector2Int>();
        foreach (var cell in cells)
        {
            normalizedCells.Add(new Vector2Int(cell.x - minX, cell.y - minY));
        }
        
        // Detect based on count
        switch (count)
        {
            case 1:
                return BoxType.Block_01;
            
            case 2:
                return DetectI2Shape(normalizedCells);
            
            case 3:
                return DetectI3Shape(normalizedCells);
            
            case 4:
                return Detect4CellShape(normalizedCells);
            
            case 5:
                return DetectPlusShape(normalizedCells);
        }
        
        // Default fallback
        return BoxType.Block_01;
    }
    
    /// <summary>
    /// Detect I2 shape (2 cells line)
    /// </summary>
    private static BoxType DetectI2Shape(HashSet<Vector2Int> cells)
    {
        if (cells.Count != 2) return BoxType.Block_01;
        
        var cellList = new List<Vector2Int>(cells);
        cellList.Sort((a, b) => {
            if (a.x != b.x) return a.x.CompareTo(b.x);
            return a.y.CompareTo(b.y);
        });
        
        Vector2Int diff = cellList[1] - cellList[0];
        
        // Horizontal if same Y, adjacent X
        if (diff.y == 0 && diff.x == 1) return BoxType.I2_90;
        // Vertical if same X, adjacent Y
        if (diff.x == 0 && diff.y == 1) return BoxType.I2;
        
        return BoxType.Block_01;
    }
    
    /// <summary>
    /// Detect I3 shape (3 cells line) or R shapes (3 cells L-shape)
    /// </summary>
    private static BoxType DetectI3Shape(HashSet<Vector2Int> cells)
    {
        var cellList = new List<Vector2Int>(cells);
        if (cellList.Count != 3) return BoxType.Block_01;
        
        // Check if all cells have same Y (horizontal line)
        bool allSameY = true;
        int firstY = cellList[0].y;
        foreach (var cell in cellList)
        {
            if (cell.y != firstY) allSameY = false;
        }
        
        // Check if all cells have same X (vertical line)
        bool allSameX = true;
        int firstX = cellList[0].x;
        foreach (var cell in cellList)
        {
            if (cell.x != firstX) allSameX = false;
        }
        
        // If all cells in a line, it's I3
        if (allSameY) return BoxType.I3_90; // Horizontal
        if (allSameX) return BoxType.I3; // Vertical
        
        // Check for R shapes (L-shape with 3 cells)
        BoxType rShape = DetectRShape3Cells(cells);
        if (rShape != BoxType.Block_01) return rShape;
        
        return BoxType.Block_01;
    }
    
    /// <summary>
    /// Detect R shapes with 3 cells (L-shape patterns)
    /// </summary>
    private static BoxType DetectRShape3Cells(HashSet<Vector2Int> cells)
    {
        if (cells.Count != 3) return BoxType.Block_01;
        
        // Debug: Log normalized cells for inspection
        var cellList = new List<Vector2Int>(cells);
        cellList.Sort((a, b) => {
            if (a.x != b.x) return a.x.CompareTo(b.x);
            return a.y.CompareTo(b.y);
        });
        string cellsStr = string.Join(", ", cellList.ConvertAll(c => $"({c.x},{c.y})"));
        //Debug.Log($"[BoxShapeDetector] DetectRShape3Cells - Normalized cells: {cellsStr}");
        
        // R_0: (0,0), (0,1), (1,0) - vertical line left (2 cells) + extension top right
        if (cells.Contains(new Vector2Int(0, 0)) && cells.Contains(new Vector2Int(0, 1)) &&
            cells.Contains(new Vector2Int(1, 0)))
            return BoxType.R_0;
        
        // R_90: (0,0), (1,0), (0,1) - horizontal line top (2 cells) + extension bottom left
        if (cells.Contains(new Vector2Int(0, 0)) && cells.Contains(new Vector2Int(1, 0)) &&
            cells.Contains(new Vector2Int(1, 1)))
            return BoxType.R_90;
        
        // R_180: (1,0), (1,1), (2,1) - vertical line right (2 cells) + extension bottom right
        if (cells.Contains(new Vector2Int(1, 0)) && cells.Contains(new Vector2Int(1, 1)) &&
            cells.Contains(new Vector2Int(0, 1)))
            return BoxType.R_180;
        
        // R_270: (0,1), (1,1), (1,0) - horizontal line bottom (2 cells) + extension top right
        if (cells.Contains(new Vector2Int(0, 0)) && cells.Contains(new Vector2Int(0, 1)) &&
            cells.Contains(new Vector2Int(1, 1)))
            return BoxType.R_270;
        
        return BoxType.Block_01;
    }
    
    /// <summary>
    /// Detect shape type for 4-cell patterns (Block_02, T, L, J, R)
    /// </summary>
    private static BoxType Detect4CellShape(HashSet<Vector2Int> cells)
    {
        // Try to detect Block_02 (2x2 square) first
        BoxType block02 = DetectBlock02(cells);
        if (block02 != BoxType.Block_01) return block02;
        
        // Try to detect T shape (has unique structure: 3 in line + 1 at middle)
        BoxType tShape = DetectTShape(cells);
        if (tShape != BoxType.Block_01) return tShape;
        
        // Try to detect L shape
        BoxType lShape = DetectLShape(cells);
        if (lShape != BoxType.Block_01) return lShape;
        
        // Try to detect J shape
        BoxType jShape = DetectJShape(cells);
        if (jShape != BoxType.Block_01) return jShape;
        
        // Try to detect R (reverse L) shape
        BoxType rShape = DetectRShape(cells);
        if (rShape != BoxType.Block_01) return rShape;
        
        // Default: single block
        return BoxType.Block_01;
    }
    
    /// <summary>
    /// Detect Block_02 shape (2x2 square: 4 cells forming a square)
    /// </summary>
    private static BoxType DetectBlock02(HashSet<Vector2Int> cells)
    {
        if (cells.Count != 4) return BoxType.Block_01;
        
        // Block_02 pattern: (0,0), (0,1), (1,0), (1,1) - 2x2 square
        if (cells.Contains(new Vector2Int(0, 0)) && cells.Contains(new Vector2Int(0, 1)) &&
            cells.Contains(new Vector2Int(1, 0)) && cells.Contains(new Vector2Int(1, 1)))
        {
            return BoxType.Block_02;
        }
        
        return BoxType.Block_01;
    }
    
    /// <summary>
    /// Detect T shape and rotation
    /// T shape: 3 cells in a line + 1 cell at middle of line
    /// </summary>
    private static BoxType DetectTShape(HashSet<Vector2Int> cells)
    {
        var cellList = new List<Vector2Int>(cells);
        
        // Try horizontal line (3 cells same Y)
        var horizontalLine = new List<Vector2Int>();
        var otherCells = new List<Vector2Int>();
        
        // Group by Y coordinate
        Dictionary<int, List<Vector2Int>> byY = new Dictionary<int, List<Vector2Int>>();
        foreach (var cell in cellList)
        {
            if (!byY.ContainsKey(cell.y))
                byY[cell.y] = new List<Vector2Int>();
            byY[cell.y].Add(cell);
        }
        
        // Find Y with 3 cells (horizontal line)
        foreach (var kvp in byY)
        {
            if (kvp.Value.Count == 3)
            {
                horizontalLine = kvp.Value;
                foreach (var cell in cellList)
                {
                    if (cell.y != kvp.Key)
                        otherCells.Add(cell);
                }
                break;
            }
        }
        
        if (horizontalLine.Count == 3 && otherCells.Count == 1)
        {
            horizontalLine.Sort((a, b) => a.x.CompareTo(b.x));
            int middleX = horizontalLine[1].x;
            Vector2Int stem = otherCells[0];
            
            // Stem below (T): stem.y < lineY
            if (stem.x == middleX && stem.y < horizontalLine[0].y)
                return BoxType.T_0;
            // Stem above (T_180): stem.y > lineY
            if (stem.x == middleX && stem.y > horizontalLine[0].y)
                return BoxType.T_180;
        }
        
        // Try vertical line (3 cells same X)
        Dictionary<int, List<Vector2Int>> byX = new Dictionary<int, List<Vector2Int>>();
        foreach (var cell in cellList)
        {
            if (!byX.ContainsKey(cell.x))
                byX[cell.x] = new List<Vector2Int>();
            byX[cell.x].Add(cell);
        }
        
        // Find X with 3 cells (vertical line)
        List<Vector2Int> verticalLine = new List<Vector2Int>();
        otherCells = new List<Vector2Int>();
        foreach (var kvp in byX)
        {
            if (kvp.Value.Count == 3)
            {
                verticalLine = kvp.Value;
                foreach (var cell in cellList)
                {
                    if (cell.x != kvp.Key)
                        otherCells.Add(cell);
                }
                break;
            }
        }
        
        if (verticalLine.Count == 3 && otherCells.Count == 1)
        {
            verticalLine.Sort((a, b) => a.y.CompareTo(b.y));
            int middleY = verticalLine[1].y;
            Vector2Int stem = otherCells[0];
            
            // Stem right (T_90): stem.x > lineX
            if (stem.y == middleY && stem.x > verticalLine[0].x)
                return BoxType.T_90;
            // Stem left (T_270): stem.x < lineX
            if (stem.y == middleY && stem.x < verticalLine[0].x)
                return BoxType.T_270;
        }
        
        return BoxType.Block_01;
    }
    
    /// <summary>
    /// Detect L shape and rotation using pattern matching
    /// </summary>
    private static BoxType DetectLShape(HashSet<Vector2Int> cells)
    {
        if (cells.Count != 4) return BoxType.Block_01;
        
        // L_0: (0,0), (0,1), (0,2), (1,2) - vertical line right + extension bottom right
        if (cells.Contains(new Vector2Int(0, 0)) && cells.Contains(new Vector2Int(1, 0)) &&
            cells.Contains(new Vector2Int(0, 1)) && cells.Contains(new Vector2Int(0, 2)))
            return BoxType.L_0;
        
        // L_90: (0,0), (1,0), (2,0), (0,1) - horizontal line top + extension top left
        if (cells.Contains(new Vector2Int(0, 0)) && cells.Contains(new Vector2Int(1, 0)) &&
            cells.Contains(new Vector2Int(2, 0)) && cells.Contains(new Vector2Int(2, 1)))
            return BoxType.L_90;
        
        // L_180: (0,0), (0,1), (0,2), (1,0) - vertical line left + extension top right
        if (cells.Contains(new Vector2Int(1, 0)) && cells.Contains(new Vector2Int(1, 1)) &&
            cells.Contains(new Vector2Int(1, 2)) && cells.Contains(new Vector2Int(0, 2)))
            return BoxType.L_180;
        
        // L_270: (0,1), (1,1), (2,1), (2,0) - horizontal line bottom + extension bottom right
        if (cells.Contains(new Vector2Int(0, 0)) && cells.Contains(new Vector2Int(0, 1)) &&
            cells.Contains(new Vector2Int(1, 1)) && cells.Contains(new Vector2Int(2, 1)))
            return BoxType.L_270;
        
        return BoxType.Block_01;
    }
    
    /// <summary>
    /// Detect J shape and rotation using pattern matching
    /// </summary>
    private static BoxType DetectJShape(HashSet<Vector2Int> cells)
    {
        if (cells.Count != 4) return BoxType.Block_01;
        
        // J_0: (1,0), (1,1), (1,2), (0,2) - vertical line right + extension bottom left
        if (cells.Contains(new Vector2Int(0, 0)) && cells.Contains(new Vector2Int(1, 0)) &&
            cells.Contains(new Vector2Int(1, 1)) && cells.Contains(new Vector2Int(1, 2)))
            return BoxType.J_0;
        
        // J_90: (0,0), (1,0), (2,0), (2,1) - horizontal line top + extension top right
        if (cells.Contains(new Vector2Int(0, 1)) && cells.Contains(new Vector2Int(1, 1)) &&
            cells.Contains(new Vector2Int(2, 1)) && cells.Contains(new Vector2Int(2, 0)))
            return BoxType.J_90;
        
        // J_180: (0,0), (0,1), (0,2), (1,0) - vertical line left + extension top right
        if (cells.Contains(new Vector2Int(0, 0)) && cells.Contains(new Vector2Int(0, 1)) &&
            cells.Contains(new Vector2Int(0, 2)) && cells.Contains(new Vector2Int(1, 2)))
            return BoxType.J_180;
        
        // J_270: (0,1), (1,1), (2,1), (0,0) - horizontal line bottom + extension top left
        if (cells.Contains(new Vector2Int(0, 0)) && cells.Contains(new Vector2Int(0, 1)) &&
            cells.Contains(new Vector2Int(1, 0)) && cells.Contains(new Vector2Int(2, 0)))
            return BoxType.J_270;
        
        return BoxType.Block_01;
    }
    
    /// <summary>
    /// Detect R (reverse L) shape and rotation using pattern matching
    /// </summary>
    private static BoxType DetectRShape(HashSet<Vector2Int> cells)
    {
        if (cells.Count != 4) return BoxType.Block_01;
        
        // R_0: (0,0), (0,1), (0,2), (1,0) - vertical line left + extension top right
        if (cells.Contains(new Vector2Int(0, 0)) && cells.Contains(new Vector2Int(0, 1)) &&
            cells.Contains(new Vector2Int(0, 2)) && cells.Contains(new Vector2Int(1, 0)))
            return BoxType.R_0;
        
        // R_90: (0,0), (1,0), (2,0), (0,2) - horizontal line top + extension bottom left
        if (cells.Contains(new Vector2Int(0, 0)) && cells.Contains(new Vector2Int(1, 0)) &&
            cells.Contains(new Vector2Int(2, 0)) && cells.Contains(new Vector2Int(0, 2)))
            return BoxType.R_90;
        
        // R_180: (1,0), (1,1), (1,2), (2,2) - vertical line right + extension bottom right
        if (cells.Contains(new Vector2Int(1, 0)) && cells.Contains(new Vector2Int(1, 1)) &&
            cells.Contains(new Vector2Int(1, 2)) && cells.Contains(new Vector2Int(2, 2)))
            return BoxType.R_180;
        
        // R_270: (0,1), (1,1), (2,1), (2,2) - horizontal line bottom + extension top right
        if (cells.Contains(new Vector2Int(0, 1)) && cells.Contains(new Vector2Int(1, 1)) &&
            cells.Contains(new Vector2Int(2, 1)) && cells.Contains(new Vector2Int(2, 2)))
            return BoxType.R_270;
        
        return BoxType.Block_01;
    }
    
    /// <summary>
    /// Detect Plus shape (5 cells: center + 4 directions)
    /// </summary>
    private static BoxType DetectPlusShape(HashSet<Vector2Int> cells)
    {
        // Plus pattern: center at (1,1) + (0,1), (2,1), (1,0), (1,2)
        if (cells.Contains(new Vector2Int(1, 1)) &&
            cells.Contains(new Vector2Int(0, 1)) &&
            cells.Contains(new Vector2Int(2, 1)) &&
            cells.Contains(new Vector2Int(1, 0)) &&
            cells.Contains(new Vector2Int(1, 2)))
        {
            return BoxType.Plus;
        }
        
        return BoxType.Block_01;
    }
}
