using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// BoardManager handles the main gameplay logic
/// Not a singleton - should be assigned via Inspector or passed through GameManager
/// </summary>
public class BoardManager : MonoBehaviour
{
    [Header("Level Settings")]
    [SerializeField]
    private string currentLevelFallback = "1"; // Fallback when GameManager.Instance is not ready

    [Header("Prefab References")]
    [SerializeField]
    private GameObject cellObjectPrefab;

    [SerializeField]
    private GameObject catObjectPrefab;

    [SerializeField]
    private GameObject boxPartObjectPrefab;

    [SerializeField]
    private GameObject boxObjectPrefab;

    [SerializeField]
    private GameObject obstacleObjectPrefab;

    [SerializeField]
    private GameObject catBlockObjectPrefab;

    [Header("Config References")]
    [SerializeField]
    private ColorConfig colorConfig;

    [Header("Cell Settings")]
    [SerializeField]
    private float cellSize = 1.28f; // Size of each cell

    [SerializeField]
    private bool useFirstCellAsBoxCenter = false; // If true, use first cell position as box center; otherwise use bounding box center

    [Header("Board Settings")]
    [SerializeField]
    private Transform cellsParent; // Parent transform for all cells

    [SerializeField]
    private Transform catsParent; // Parent transform for all cats

    [SerializeField]
    private Transform boxesParent; // Parent transform for all boxes

    [SerializeField]
    private Transform obstaclesParent; // Parent transform for all obstacles

    [SerializeField]
    private Transform catBlocksParent; // Parent transform for all cat blocks

    [Header("Freeze")]
    [SerializeField]
    private GameObject snowLayer;
    [SerializeField]
    private float freezeDuration = 15f;

    private Coroutine _freezeCoroutine;

    // Runtime data
    private LevelData levelData;
    private Dictionary<Vector2Int, GameObject> cells = new Dictionary<Vector2Int, GameObject>();
    private Dictionary<Vector2Int, GameObject> cats = new Dictionary<Vector2Int, GameObject>(); // Cats by grid position
    private Dictionary<Vector2Int, GameObject> boxes = new Dictionary<Vector2Int, GameObject>(); // Box parts by grid position
    private Dictionary<Vector2Int, GameObject> obstacles = new Dictionary<Vector2Int, GameObject>(); // Obstacles by grid position
    private Dictionary<Vector2Int, GameObject> catBlocks = new Dictionary<Vector2Int, GameObject>(); // CatBlocks by grid position
    private Vector3 boardOffset = Vector3.zero; // Offset to center the board
    private BoxObject selectedBoxObject; // Currently selected box object
    private bool isDragging = false; // Whether currently dragging a box object
    private Vector2 dragOffset = Vector2.zero; // Offset from mouse position to box center when drag starts
    private Vector2 initialBoxPosition = Vector2.zero; // Initial box position when drag starts

    /// <summary>
    /// Dictionary of all cells on the board, keyed by their grid position (x, y)
    /// </summary>
    public Dictionary<Vector2Int, GameObject> Cells
    {
        get { return cells; }
    }

    /// <summary>
    /// Current level data
    /// </summary>
    public LevelData LevelData
    {
        get { return levelData; }
    }

    /// <summary>
    /// Currently selected box object
    /// </summary>
    public BoxObject SelectedBoxObject
    {
        get { return selectedBoxObject; }
    }

    void Start()
    {
        //Init();
    }

    void Update()
    {
        if (GameManager.Instance.CurrentState != GameState.PLAYING)
            return;
        // Handle mouse click to start drag
        if (Input.GetMouseButtonDown(0)) // Left mouse button
        {
            Vector2 mousePosition = Input.mousePosition;
            BoxObject hitBox = RaycastBoxObject(mousePosition);

            if (hitBox != null)
            {
                selectedBoxObject = hitBox;
                isDragging = true;

                // Get Rigidbody2D and set to Dynamic before dragging
                Rigidbody2D rb = hitBox.Rigidbody2D;
                if (rb != null)
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;

                    // Store initial box position for direction constraint
                    initialBoxPosition = hitBox.transform.position;

                    // Calculate offset from mouse world position to box center
                    Camera camera = Camera.main;
                    if (camera != null)
                    {
                        float cameraDistance = Mathf.Abs(camera.transform.position.z);
                        Vector3 mouseWorldPos = camera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, cameraDistance));
                        Vector2 boxWorldPos = hitBox.transform.position;
                        dragOffset = boxWorldPos - new Vector2(mouseWorldPos.x, mouseWorldPos.y);
                    }
                }
                else
                {
                    Debug.LogWarning($"Rigidbody2D not found in BoxObject {hitBox.gameObject.name}");
                    isDragging = false;
                }

                Debug.Log($"BoxObject selected and dragging started: {hitBox.gameObject.name}");
            }
            else
            {
                // No box hit - deselect (tap on cat no longer triggers star booster)
                selectedBoxObject = null;
                isDragging = false;
            }
        }

        // Handle drag
        if (isDragging && selectedBoxObject != null && Input.GetMouseButton(0))
        {
            Vector2 mousePosition = Input.mousePosition;
            Camera camera = Camera.main;

            if (camera != null)
            {
                Rigidbody2D rb = selectedBoxObject.Rigidbody2D;
                if (rb != null)
                {
                    // Convert mouse position to world position
                    float cameraDistance = Mathf.Abs(camera.transform.position.z);
                    Vector3 mouseWorldPos = camera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, cameraDistance));
                    Vector2 targetPosition = new Vector2(mouseWorldPos.x, mouseWorldPos.y) + dragOffset;

                    // Apply direction constraint based on DirectionType
                    DirectionType directionType = selectedBoxObject.DirectionType;
                    switch (directionType)
                    {
                        case DirectionType.Vertical:
                            // Only allow movement on Y axis (vertical)
                            targetPosition.x = initialBoxPosition.x;
                            break;

                        case DirectionType.Horizontal:
                            // Only allow movement on X axis (horizontal)
                            targetPosition.y = initialBoxPosition.y;
                            break;

                        case DirectionType.None:
                            // Free movement, no constraint
                            break;
                    }

                    // Move box object using Rigidbody2D
                    rb.MovePosition(targetPosition);
                }
            }
        }

        // Handle drop
        if (isDragging && Input.GetMouseButtonUp(0))
        {
            if (selectedBoxObject != null)
            {
                Rigidbody2D rb = selectedBoxObject.Rigidbody2D;
                if (rb != null)
                {
                    // Get current position
                    Vector3 currentPosition = selectedBoxObject.transform.position;

                    // Convert to grid position and snap
                    Vector2Int gridPos = WorldToGridPosition(currentPosition);
                    Vector3 snapPosition = GridToWorldPosition(gridPos);

                    // Preserve z position
                    snapPosition.z = currentPosition.z;

                    // Set Rigidbody2D back to Static first
                    rb.bodyType = RigidbodyType2D.Static;

                    // Move to snapped position directly using transform (Static Rigidbody2D can't use MovePosition)
                    selectedBoxObject.transform.position = snapPosition;

                    //Debug.Log($"BoxObject dropped and snapped to grid position ({gridPos.x}, {gridPos.y}): {selectedBoxObject.gameObject.name}");
                }
            }

            isDragging = false;
            dragOffset = Vector2.zero;
        }

        // Magnet: M key - pull matching cats to first box with currentCount > 0
        if (Input.GetKeyDown(KeyCode.M))
        {
            UseMagnet();
        }

        // Freeze: P key - pause timer 15s, active snowLayer
        if (Input.GetKeyDown(KeyCode.P))
        {
            UseFreeze();
        }
    }

    /// <summary>
    /// Use magnet: find a box with currentCount > 0 and at least one matching-color cat (not InBlock), pull those cats to first BoxPartObject.
    /// If current box does not satisfy (no matching cat), try next box until one satisfies.
    /// </summary>
    public void UseMagnet()
    {
        if (boxesParent == null)
        {
            return;
        }

        BoxObject[] allBoxObjects = boxesParent.GetComponentsInChildren<BoxObject>();

        foreach (BoxObject targetBox in allBoxObjects)
        {
            if (targetBox == null || targetBox.CurrentCount <= 0)
            {
                continue;
            }

            BoxPartObject[] parts = targetBox.GetComponentsInChildren<BoxPartObject>();
            if (parts == null || parts.Length == 0)
            {
                continue;
            }

            int colorIndex = targetBox.CurrentColorID;

            // Collect cats matching this box color (excluding InBlock)
            List<CatView> matchingCats = new List<CatView>();
            foreach (GameObject catObj in cats.Values)
            {
                if (catObj == null)
                {
                    continue;
                }

                CatView catView = catObj.GetComponent<CatView>();
                if (catView == null)
                {
                    catView = catObj.GetComponentInChildren<CatView>();
                }

                if (catView != null && catView.CurrentColorID == colorIndex && !catView.InBlock)
                {
                    matchingCats.Add(catView);
                }
            }

            // Only process when at least one cat matches
            if (matchingCats.Count == 0)
            {
                continue;
            }

            BoxPartObject firstBoxPart = parts[0];
            firstBoxPart.EnableMagnet(true);

            for (int i = 0; i < matchingCats.Count; i++)
            {
                firstBoxPart.GetCatFromStarBubble(matchingCats[i]);
            }

            return;
        }
    }

    /// <summary>
    /// Freeze: pause timer for freezeDuration seconds, activate snowLayer; then resume timer and deactivate snowLayer.
    /// Called when P is pressed.
    /// </summary>
    public void UseFreeze()
    {
        if (_freezeCoroutine != null)
            return;
        if (GameManager.Instance == null || GameManager.Instance.uiManager == null || GameManager.Instance.uiManager.UiGame == null)
            return;
        UIGame uiGame = GameManager.Instance.uiManager.UiGame;
        uiGame.PauseTimer();
        if (snowLayer != null)
            snowLayer.SetActive(true);
        _freezeCoroutine = StartCoroutine(FreezeRoutine(uiGame));
    }

    private IEnumerator FreezeRoutine(UIGame uiGame)
    {
        yield return new WaitForSeconds(freezeDuration);
        if (uiGame != null)
            uiGame.ResumeTimer();
        if (snowLayer != null)
            snowLayer.SetActive(false);
        _freezeCoroutine = null;
    }

    /// <summary>
    /// Stops freeze effect immediately: stops coroutine, resumes timer, deactivates snow layer. Call on game win, game lose, or clean board.
    /// </summary>
    public void StopFreeze()
    {
        if (_freezeCoroutine != null)
        {
            StopCoroutine(_freezeCoroutine);
            _freezeCoroutine = null;
        }
        if (GameManager.Instance != null && GameManager.Instance.uiManager != null && GameManager.Instance.uiManager.UiGame != null)
            GameManager.Instance.uiManager.UiGame.ResumeTimer();
        if (snowLayer != null)
            snowLayer.SetActive(false);
    }

    /// <summary>
    /// Star booster: pick one catObject on the board (not InBlock) and call CatJumpToHoleByStar().
    /// </summary>
    public void UseStarBooster()
    {
        foreach (GameObject catObj in cats.Values)
        {
            if (catObj == null) continue;
            CatView catView = catObj.GetComponent<CatView>();
            if (catView == null) catView = catObj.GetComponentInChildren<CatView>();
            if (catView != null && !catView.InBlock)
            {
                catView.CatJumpToHoleByStar();
                return;
            }
        }
    }

    /// <summary>
    /// Initialize the BoardManager
    /// Should be called from GameManager.Start
    /// </summary>
    public void Init()
    {
        // Load prefab if not assigned
        if (cellObjectPrefab == null)
        {
            cellObjectPrefab = Resources.Load<GameObject>("Prefabs/Board/CellObject");
            if (cellObjectPrefab == null)
            {
                Debug.LogError("CellObject prefab not found at Resources/Prefabs/Board/CellObject");
                return;
            }
        }

        // Create cells parent if not assigned
        if (cellsParent == null)
        {
            GameObject parentObj = new GameObject("Cells");
            parentObj.transform.SetParent(transform);
            parentObj.transform.localPosition = Vector3.zero;
            cellsParent = parentObj.transform;
        }

        // Create cats parent if not assigned
        if (catsParent == null)
        {
            GameObject parentObj = new GameObject("Cats");
            parentObj.transform.SetParent(transform);
            parentObj.transform.localPosition = Vector3.zero;
            catsParent = parentObj.transform;
        }

        // Create boxes parent if not assigned
        if (boxesParent == null)
        {
            GameObject parentObj = new GameObject("Boxes");
            parentObj.transform.SetParent(transform);
            parentObj.transform.localPosition = Vector3.zero;
            boxesParent = parentObj.transform;
        }

        // Create obstacles parent if not assigned
        if (obstaclesParent == null)
        {
            GameObject parentObj = new GameObject("Obstacles");
            parentObj.transform.SetParent(transform);
            parentObj.transform.localPosition = Vector3.zero;
            obstaclesParent = parentObj.transform;
        }

        // Create cat blocks parent if not assigned
        if (catBlocksParent == null)
        {
            GameObject parentObj = new GameObject("CatBlocks");
            parentObj.transform.SetParent(transform);
            parentObj.transform.localPosition = Vector3.zero;
            catBlocksParent = parentObj.transform;
        }

        // Load ColorConfig if not assigned
        if (colorConfig == null)
        {
            colorConfig = Resources.Load<ColorConfig>("Configs/ColorConfig");
            if (colorConfig == null)
            {
                Debug.LogWarning("ColorConfig not found at Resources/Configs/ColorConfig");
            }
        }

        // Load CatObject prefab if not assigned
        if (catObjectPrefab == null)
        {
            catObjectPrefab = Resources.Load<GameObject>("Prefabs/Cats/CatObject");
            if (catObjectPrefab == null)
            {
                Debug.LogWarning("CatObject prefab not found at Resources/Prefabs/Cats/CatObject");
            }
        }

        // Load BoxPartObject prefab if not assigned
        if (boxPartObjectPrefab == null)
        {
            boxPartObjectPrefab = Resources.Load<GameObject>("Prefabs/Board/BoxPartObject");
            if (boxPartObjectPrefab == null)
            {
                Debug.LogWarning("BoxPartObject prefab not found at Resources/Prefabs/Board/BoxPartObject");
            }
        }

        // Load BoxObject prefab if not assigned
        if (boxObjectPrefab == null)
        {
            boxObjectPrefab = Resources.Load<GameObject>("Prefabs/Board/BoxObject");
            if (boxObjectPrefab == null)
            {
                Debug.LogWarning("BoxObject prefab not found at Resources/Prefabs/Board/BoxObject");
            }
        }

        // Load ObstacleObject prefab if not assigned
        if (obstacleObjectPrefab == null)
        {
            obstacleObjectPrefab = Resources.Load<GameObject>("Prefabs/Board/CellObstacle");
            if (obstacleObjectPrefab == null)
            {
                Debug.LogWarning("CellObstacle prefab not found at Resources/Prefabs/Board/CellObstacle");
            }
        }

        // Load CatBlock prefab if not assigned
        if (catBlockObjectPrefab == null)
        {
            catBlockObjectPrefab = Resources.Load<GameObject>("Prefabs/Board/CatBlock");
            if (catBlockObjectPrefab == null)
            {
                Debug.LogWarning("CatBlock prefab not found at Resources/Prefabs/Board/CatBlock");
            }
        }

        // Load and generate level (get currentLevel from GameManager if available)
        LoadLevel(GetLevelToLoad());
    }

    /// <summary>
    /// Gets level name to load: from GameManager (uses GetLevelToLoad() for testLevel support), otherwise uses currentLevelFallback.
    /// </summary>
    private string GetLevelToLoad()
    {
        return GameManager.Instance != null ? GameManager.Instance.GetLevelToLoad() : currentLevelFallback;
    }

    /// <summary>
    /// Load level from JSON file in Resources/JsonLevels
    /// </summary>
    /// <param name="levelName">Level file name without extension (e.g., "1" for "1.json")</param>
    public void LoadLevel(string levelName)
    {
        // Clear existing cells
        ClearCells();

        // Load JSON file
        string jsonPath = $"JsonLevels/{levelName}";
        TextAsset jsonFile = Resources.Load<TextAsset>(jsonPath);

        if (jsonFile == null)
        {
            Debug.LogError($"Level file not found: {jsonPath}");
            return;
        }

        try
        {
            // Parse JSON
            levelData = JsonConvert.DeserializeObject<LevelData>(jsonFile.text);

            if (levelData == null)
            {
                Debug.LogError($"Failed to parse level data from {jsonPath}");
                return;
            }

            // Generate cells
            GenerateCells();

            // Generate cats
            GenerateCats();

            // Generate boxes
            GenerateBoxes();

            // Generate obstacles
            GenerateObstacles();

            // Generate cat blocks
            GenerateCatBlocks();

            // Setup collision ignores between boxes and cats with same color ID
            SetupColorCollisions();

            if (!CheckLevelValid())
            {
                Debug.LogError($"Invalid level: {levelName}. Cat counts per color must match box CountMax; every cat color must have a box and every box color must have cats.");
                FixInvalidLevel();
            }

            Debug.Log($"Level {levelName} loaded successfully. Cells count: {cells.Count}, Cats count: {cats.Count}, Boxes count: {boxes.Count}, Obstacles count: {obstacles.Count}, CatBlocks count: {catBlocks.Count}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading level {levelName}: {e.Message}");
        }
    }

    /// <summary>
    /// Collects per-color counts: box CountMax and total cat count (free + in catBlocks). Used by CheckLevelValid and FixInvalidLevel.
    /// </summary>
    private void CollectLevelColorCounts(out Dictionary<int, int> boxCountMaxByColor, out Dictionary<int, int> catCountByColor)
    {
        boxCountMaxByColor = new Dictionary<int, int>();
        catCountByColor = new Dictionary<int, int>();

        if (boxesParent != null)
        {
            BoxObject[] allBoxObjects = boxesParent.GetComponentsInChildren<BoxObject>();
            foreach (BoxObject box in allBoxObjects)
            {
                if (box == null) continue;
                int colorId = box.CurrentColorID;
                if (!boxCountMaxByColor.ContainsKey(colorId))
                    boxCountMaxByColor[colorId] = 0;
                boxCountMaxByColor[colorId] += box.CountMax;
            }
        }

        foreach (GameObject catObj in cats.Values)
        {
            if (catObj == null) continue;
            CatView catView = catObj.GetComponent<CatView>();
            if (catView == null) catView = catObj.GetComponentInChildren<CatView>();
            if (catView == null) continue;
            int colorId = catView.CurrentColorID;
            if (!catCountByColor.ContainsKey(colorId))
                catCountByColor[colorId] = 0;
            catCountByColor[colorId]++;
        }

        if (catBlocksParent != null)
        {
            CatBlock[] allCatBlocks = catBlocksParent.GetComponentsInChildren<CatBlock>();
            foreach (CatBlock block in allCatBlocks)
            {
                if (block == null || block.spawnedCats == null) continue;
                foreach (GameObject catObj in block.spawnedCats)
                {
                    if (catObj == null) continue;
                    CatView catView = catObj.GetComponent<CatView>();
                    if (catView == null) catView = catObj.GetComponentInChildren<CatView>();
                    if (catView == null) continue;
                    int colorId = catView.CurrentColorID;
                    if (!catCountByColor.ContainsKey(colorId))
                        catCountByColor[colorId] = 0;
                    catCountByColor[colorId]++;
                }
            }
        }
    }

    /// <summary>
    /// Returns set of color IDs that are invalid: box with no cats or count mismatch, or cats with no box.
    /// </summary>
    private HashSet<int> GetInvalidColorIds(Dictionary<int, int> boxCountMaxByColor, Dictionary<int, int> catCountByColor)
    {
        HashSet<int> invalid = new HashSet<int>();
        foreach (int colorId in boxCountMaxByColor.Keys)
        {
            int required = boxCountMaxByColor[colorId];
            int have = catCountByColor.TryGetValue(colorId, out int c) ? c : 0;
            if (have == 0 || have != required)
                invalid.Add(colorId);
        }
        foreach (int colorId in catCountByColor.Keys)
        {
            if (!boxCountMaxByColor.ContainsKey(colorId))
                invalid.Add(colorId);
        }
        return invalid;
    }

    /// <summary>
    /// Validates level: for each color, total cat count (free cats + cats in catBlocks) must equal box CountMax.
    /// Invalid if: counts don't match; cat exists with no box of same color; box exists with no cat of same color.
    /// </summary>
    /// <returns>True if valid, false otherwise.</returns>
    private bool CheckLevelValid()
    {
        CollectLevelColorCounts(out Dictionary<int, int> boxCountMaxByColor, out Dictionary<int, int> catCountByColor);
        HashSet<int> invalid = GetInvalidColorIds(boxCountMaxByColor, catCountByColor);
        if (invalid.Count == 0)
            return true;

        List<string> errors = new List<string>();
        foreach (int colorId in boxCountMaxByColor.Keys)
        {
            if (!invalid.Contains(colorId)) continue;
            int required = boxCountMaxByColor[colorId];
            int have = catCountByColor.TryGetValue(colorId, out int c) ? c : 0;
            if (have == 0)
                errors.Add($"[Color {colorId}] Box CountMax = {required}, Cat count = 0 → INVALID: no cats of this color on board.");
            else
                errors.Add($"[Color {colorId}] Box CountMax = {required}, Cat count = {have} → INVALID: counts do not match (expected {required} cats).");
        }
        foreach (int colorId in catCountByColor.Keys)
        {
            if (!invalid.Contains(colorId)) continue;
            if (!boxCountMaxByColor.ContainsKey(colorId))
                errors.Add($"[Color {colorId}] Cat count = {catCountByColor[colorId]}, Box CountMax = none → INVALID: cats exist but no box of this color on board.");
        }

        Debug.LogError($"[CheckLevelValid] INVALID LEVEL. Total errors: {errors.Count}");
        Debug.LogError("--- Per-color breakdown ---");
        HashSet<int> allColors = new HashSet<int>(boxCountMaxByColor.Keys);
        foreach (int colorId in catCountByColor.Keys)
            allColors.Add(colorId);
        foreach (int colorId in allColors)
        {
            int boxMax = boxCountMaxByColor.TryGetValue(colorId, out int b) ? b : 0;
            int catCount = catCountByColor.TryGetValue(colorId, out int c) ? c : 0;
            string status = (boxMax == catCount && boxMax > 0) ? "OK" : "MISMATCH";
            Debug.LogError($"  Color {colorId}: Box CountMax = {boxMax}, Cat count = {catCount} → {status}");
        }
        Debug.LogError("--- Invalid reasons ---");
        for (int i = 0; i < errors.Count; i++)
            Debug.LogError($"  {i + 1}. {errors[i]}");
        return false;
    }

    /// <summary>
    /// Removes all catObjects and boxObjects related to invalid colors. Call only when CheckLevelValid() returned false.
    /// For cats inside CatBlock.spawnedCats, removes them from the list, destroys them, then refreshes cat list and repositions remaining cats.
    /// </summary>
    private void FixInvalidLevel()
    {
        CollectLevelColorCounts(out Dictionary<int, int> boxCountMaxByColor, out Dictionary<int, int> catCountByColor);
        HashSet<int> invalid = GetInvalidColorIds(boxCountMaxByColor, catCountByColor);
        if (invalid.Count == 0)
            return;

        Debug.Log($"[FixInvalidLevel] Removing invalid colors: {string.Join(", ", invalid)}");

        // 1. Destroy BoxObjects with invalid color and remove from boxes dictionary
        if (boxesParent != null)
        {
            BoxObject[] allBoxObjects = boxesParent.GetComponentsInChildren<BoxObject>();
            List<BoxObject> toDestroy = new List<BoxObject>();
            foreach (BoxObject box in allBoxObjects)
            {
                if (box == null) continue;
                if (invalid.Contains(box.CurrentColorID))
                    toDestroy.Add(box);
            }
            foreach (BoxObject box in toDestroy)
            {
                if (box == null || box.gameObject == null) continue;
                foreach (var kv in new Dictionary<Vector2Int, GameObject>(boxes))
                {
                    if (kv.Value != null)
                    {
                        BoxObject parentBox = kv.Value.GetComponentInParent<BoxObject>();
                        if (parentBox != null && parentBox == box)
                            boxes.Remove(kv.Key);
                    }
                }
                Destroy(box.gameObject);
            }
        }

        // 2. Destroy free cats with invalid color and remove from cats dictionary
        List<Vector2Int> catKeysToRemove = new List<Vector2Int>();
        foreach (var kv in cats)
        {
            if (kv.Value == null) { catKeysToRemove.Add(kv.Key); continue; }
            CatView catView = kv.Value.GetComponent<CatView>();
            if (catView == null) catView = kv.Value.GetComponentInChildren<CatView>();
            if (catView != null && invalid.Contains(catView.CurrentColorID))
            {
                catKeysToRemove.Add(kv.Key);
                Destroy(kv.Value);
            }
        }
        foreach (Vector2Int key in catKeysToRemove)
            cats.Remove(key);

        // 3. Remove invalid cats from CatBlocks, destroy them, then refresh list and positions
        if (catBlocksParent != null)
        {
            CatBlock[] allCatBlocks = catBlocksParent.GetComponentsInChildren<CatBlock>();
            foreach (CatBlock block in allCatBlocks)
            {
                if (block == null || block.spawnedCats == null) continue;
                List<GameObject> toRemove = new List<GameObject>();
                foreach (GameObject catObj in block.spawnedCats)
                {
                    if (catObj == null) { toRemove.Add(catObj); continue; }
                    CatView catView = catObj.GetComponent<CatView>();
                    if (catView == null) catView = catObj.GetComponentInChildren<CatView>();
                    if (catView != null && invalid.Contains(catView.CurrentColorID))
                        toRemove.Add(catObj);
                }
                foreach (GameObject catObj in toRemove)
                {
                    if (catObj != null)
                    {
                        block.spawnedCats.Remove(catObj);
                        Destroy(catObj);
                    }
                }
                block.RefreshCatListAndPositions();
            }
        }
    }

    /// <summary>
    /// Generate CellObject prefabs based on level data
    /// </summary>
    private void GenerateCells()
    {
        if (levelData == null || levelData.Cells == null)
        {
            Debug.LogError("Level data or Cells is null");
            return;
        }

        // Calculate board bounds to center it
        CalculateBoardOffset();

        foreach (var cellData in levelData.Cells)
        {
            if (cellData.Index == null)
            {
                Debug.LogWarning("Cell data has null Index, skipping");
                continue;
            }

            // Get grid position
            int x = cellData.Index.x;
            int y = cellData.Index.y;
            Vector2Int gridPos = new Vector2Int(x, y);

            // Calculate world position
            Vector3 worldPos = GridToWorldPosition(gridPos);

            // Instantiate cell object
            GameObject cellObj = Instantiate(cellObjectPrefab, worldPos, Quaternion.identity, cellsParent);
            cellObj.name = $"Cell_{x}_{y}";

            // Store in dictionary
            cells[gridPos] = cellObj;
        }
    }

    /// <summary>
    /// Generate CatObject prefabs based on level data
    /// </summary>
    private void GenerateCats()
    {
        if (levelData == null || levelData.Cells == null)
        {
            return;
        }

        if (catObjectPrefab == null)
        {
            Debug.LogWarning("CatObject prefab is null, cannot generate cats");
            return;
        }

        foreach (var cellData in levelData.Cells)
        {
            // Skip if no cat data
            if (cellData.Cat == null || cellData.Index == null)
            {
                continue;
            }

            // Get grid position
            int x = cellData.Index.x;
            int y = cellData.Index.y;
            Vector2Int gridPos = new Vector2Int(x, y);

            // Check if cell exists
            if (!cells.ContainsKey(gridPos))
            {
                Debug.LogWarning($"Cell at ({x}, {y}) not found, cannot place cat");
                continue;
            }

            // Get world position (same as cell position)
            Vector3 worldPos = GridToWorldPosition(gridPos);

            // Instantiate cat object
            GameObject catObj = Instantiate(catObjectPrefab, worldPos, Quaternion.identity, catsParent);
            catObj.name = $"Cat_{x}_{y}_Color_{cellData.Cat.Color}";

            // Set sprite using CatView component
            CatView catView = catObj.GetComponent<CatView>();
            if (catView == null)
            {
                catView = catObj.GetComponentInChildren<CatView>();
            }

            if (catView != null)
            {
                catView.SetSpriteByColorIndex(cellData.Cat.Color);
                // Set inBlock to false by default when spawning cat
                catView.InBlock = false;
                catView.EnableCollider(true);
            }
            else
            {
                // Fallback: Apply color and sprite directly if CatView not found
                if (colorConfig != null)
                {
                    Color catColor = colorConfig.GetColorByIndex(cellData.Cat.Color);
                    Sprite catSprite = colorConfig.GetCatSpriteByIndex(cellData.Cat.Color);
                    ApplyColorAndSpriteToCat(catObj, catColor, catSprite);
                }
            }

            // Store in dictionary
            cats[gridPos] = catObj;
        }
    }

    /// <summary>
    /// Apply color and sprite to cat GameObject
    /// </summary>
    /// <param name="catObj">Cat GameObject</param>
    /// <param name="color">Color to apply</param>
    /// <param name="catSprite">Cat sprite to apply (can be null)</param>
    private void ApplyColorAndSpriteToCat(GameObject catObj, Color color, Sprite catSprite)
    {
        // Try to find SpriteRenderer in cat object or its children
        SpriteRenderer spriteRenderer = catObj.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = catObj.GetComponentInChildren<SpriteRenderer>();
        }

        if (spriteRenderer != null)
        {
            // Apply sprite if available
            if (catSprite != null)
            {
                spriteRenderer.sprite = catSprite;
            }

            // Apply color
            spriteRenderer.color = color;
        }
        else
        {
            Debug.LogWarning($"No SpriteRenderer found in CatObject {catObj.name}");
        }
    }

    /// <summary>
    /// Generate BoxObject prefabs based on level data
    /// Each box will spawn at center position with BoxPartObjects as children
    /// </summary>
    private void GenerateBoxes()
    {
        if (levelData == null || levelData.Boxes == null)
        {
            return;
        }

        if (boxObjectPrefab == null)
        {
            Debug.LogWarning("BoxObject prefab is null, cannot generate boxes");
            return;
        }

        if (boxPartObjectPrefab == null)
        {
            Debug.LogWarning("BoxPartObject prefab is null, cannot generate boxes");
            return;
        }

        foreach (var boxData in levelData.Boxes)
        {
            // Skip if no cells data
            if (boxData.Cells == null || boxData.Cells.Count == 0)
            {
                continue;
            }

            // Calculate bounding box of the box
            int minX = int.MaxValue;
            int maxX = int.MinValue;
            int minY = int.MaxValue;
            int maxY = int.MinValue;
            int validCellCount = 0;

            foreach (var cellIndex in boxData.Cells)
            {
                int x = cellIndex.x;
                int y = cellIndex.y;
                Vector2Int gridPos = new Vector2Int(x, y);

                // Check if cell exists
                if (!cells.ContainsKey(gridPos))
                {
                    continue;
                }

                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
                validCellCount++;
            }

            if (validCellCount == 0)
            {
                Debug.LogWarning($"No valid cells found for box {boxData.Index}");
                continue;
            }

            // Calculate box center world position
            Vector3 boxCenterWorldPos;
            Vector3 boxCollidersCenterWorldPos;

            float centerX = (minX + maxX) * 0.5f;
            float centerY = (minY + maxY) * 0.5f;
            boxCollidersCenterWorldPos = new Vector3(centerX * cellSize, centerY * cellSize, 0) + boardOffset;

            if (useFirstCellAsBoxCenter && boxData.Cells.Count > 0)
            {
                // Use first cell position as box center
                var firstCell = boxData.Cells[0];
                int firstCellX = firstCell.x;
                int firstCellY = firstCell.y;
                boxCenterWorldPos = GridToWorldPosition(new Vector2Int(firstCellX, firstCellY));
            }
            else
            {
                // Use bounding box center       
                boxCenterWorldPos = new Vector3(centerX * cellSize, centerY * cellSize, 0) + boardOffset;
            }

            // Instantiate BoxObject at center position
            GameObject boxObj = Instantiate(boxObjectPrefab, boxCenterWorldPos, Quaternion.identity, boxesParent);
            boxObj.name = $"Box_{boxData.Index}_Color_{boxData.Color}";

            // Get BoxObject component
            BoxObject boxObject = boxObj.GetComponent<BoxObject>();
            if (boxObject == null)
            {
                Debug.LogWarning($"BoxObject component not found in {boxObj.name}");
                Destroy(boxObj);
                continue;
            }
            boxObject.collidersParent.transform.position = boxCollidersCenterWorldPos;
            boxObject.directionBone.transform.position = boxCollidersCenterWorldPos + new Vector3(0.0f, 0.5f, 0);
            boxObject.MainShapeRenderer.transform.position = boxCollidersCenterWorldPos;
            // Detect and set box type based on cell positions
            BoxType detectedType = BoxShapeDetector.DetectBoxType(boxData.Cells);
            boxObject.Type = detectedType;
            boxObject.SetSpriteByTypeAndColor(detectedType, boxData.Color);
            boxObject.SetCountMax(boxData.Count);

            // Set direction type from Gimmicks
            DirectionType directionType = DirectionType.None;
            if (boxData.Gimmicks != null && boxData.Gimmicks.Count > 0)
            {
                var firstGimmick = boxData.Gimmicks[0];
                if (firstGimmick != null)
                {
                    if (firstGimmick.Type == 0)
                    {
                        // Orientation: 0 = Vertical, 1 = Horizontal
                        if (firstGimmick.Orientation == 0)
                        {
                            directionType = DirectionType.Vertical;
                        }
                        else if (firstGimmick.Orientation == 1)
                        {
                            directionType = DirectionType.Horizontal;
                        }
                    }
                    else
                    {
                        directionType = DirectionType.None;
                    }

                }
            }
            boxObject.DirectionType = directionType;

            // Get bodyPartRoot
            Transform bodyPartRoot = boxObject.BodyPartRoot;
            if (bodyPartRoot == null)
            {
                Debug.LogWarning($"BodyPartRoot not found in BoxObject {boxObj.name}");
                Destroy(boxObj);
                continue;
            }

            // Find bottom-right cell (smallest y, largest x)
            IndexData bottomRightCell = null;
            int bottomRightMinY = int.MaxValue;
            int bottomRightMaxX = int.MinValue;

            foreach (var cellIndex in boxData.Cells)
            {
                int x = cellIndex.x;
                int y = cellIndex.y;

                // Check if this cell is bottom-right candidate
                if (y < bottomRightMinY || (y == bottomRightMinY && x > bottomRightMaxX))
                {
                    bottomRightMinY = y;
                    bottomRightMaxX = x;
                    bottomRightCell = cellIndex;
                }
            }

            // Generate box parts as children of bodyPartRoot
            foreach (var cellIndex in boxData.Cells)
            {
                int x = cellIndex.x;
                int y = cellIndex.y;
                Vector2Int gridPos = new Vector2Int(x, y);

                // Check if cell exists
                if (!cells.ContainsKey(gridPos))
                {
                    Debug.LogWarning($"Cell at ({x}, {y}) not found, cannot place box part");
                    continue;
                }

                // Get world position of cell
                Vector3 cellWorldPos = GridToWorldPosition(gridPos);

                // Calculate local position relative to bodyPartRoot
                Vector3 localPos = bodyPartRoot.InverseTransformPoint(cellWorldPos);

                // Instantiate box part object as child of bodyPartRoot
                GameObject boxPartObj = Instantiate(boxPartObjectPrefab, bodyPartRoot);
                boxPartObj.transform.localPosition = localPos;
                boxPartObj.name = $"BoxPart_{boxData.Index}_Cell_{x}_{y}_Color_{boxData.Color}";

                // Set color using BoxPartObject component
                BoxPartObject boxPartObject = boxPartObj.GetComponent<BoxPartObject>();
                if (boxPartObject == null)
                {
                    boxPartObject = boxPartObj.GetComponentInChildren<BoxPartObject>();
                }

                if (boxPartObject != null)
                {
                    boxPartObject.SetColorByIndex(boxData.Color);
                }
                else
                {
                    Debug.LogWarning($"BoxPartObject component not found in {boxPartObj.name}");
                }

                // Store box part in dictionary (key by grid position)
                boxes[gridPos] = boxPartObj;
            }

            // Set countCanvas position to bottom-right of bottom-right cell
            if (bottomRightCell != null && boxObject.CountCanvas != null)
            {
                Vector2Int bottomRightGridPos = new Vector2Int(bottomRightCell.x, bottomRightCell.y);
                Vector3 bottomRightCellWorldPos = GridToWorldPosition(bottomRightGridPos);

                // Calculate bottom-right position of cell (cell center + offset to bottom-right corner)
                // Bottom-right corner: x + cellSize/2, y - cellSize/2
                Vector3 bottomRightPosition = bottomRightCellWorldPos + new Vector3(cellSize * 0.5f, -cellSize * 0.5f, 0);

                // Set countCanvas position
                boxObject.CountCanvas.position = bottomRightPosition + new Vector3(-0.325f, 0.325f, 0.0f);
            }
            else if (boxObject.CountCanvas == null)
            {
                Debug.LogWarning($"CountCanvas is null for BoxObject {boxObj.name}");
            }
        }
    }

    /// <summary>
    /// Generate ObstacleObject prefabs at grid positions without cells
    /// </summary>
    private void GenerateObstacles()
    {
        if (levelData == null || levelData.Cells == null)
        {
            return;
        }

        if (obstacleObjectPrefab == null)
        {
            Debug.LogWarning("ObstacleObject prefab is null, cannot generate obstacles");
            return;
        }

        // Calculate grid bounds from cells
        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minY = int.MaxValue;
        int maxY = int.MinValue;

        foreach (var cellData in levelData.Cells)
        {
            if (cellData.Index == null) continue;

            int x = cellData.Index.x;
            int y = cellData.Index.y;

            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        // Generate obstacles for all positions in grid that don't have cells
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);

                // Skip if cell exists at this position
                if (cells.ContainsKey(gridPos))
                {
                    continue;
                }

                // Calculate world position
                Vector3 worldPos = GridToWorldPosition(gridPos);

                // Instantiate obstacle object
                GameObject obstacleObj = Instantiate(obstacleObjectPrefab, worldPos, Quaternion.identity, obstaclesParent);
                obstacleObj.name = $"Obstacle_{x}_{y}";

                // Store in dictionary
                obstacles[gridPos] = obstacleObj;
            }
        }

        // Generate boundary obstacles (top, bottom, left, right edges)
        // Top edge: y = maxY + 1, x from minX - 1 to maxX + 1
        for (int x = minX - 1; x <= maxX + 1; x++)
        {
            int y = maxY + 1;
            Vector2Int gridPos = new Vector2Int(x, y);

            // Skip if obstacle already exists at this position
            if (obstacles.ContainsKey(gridPos))
            {
                continue;
            }

            Vector3 worldPos = GridToWorldPosition(gridPos);
            GameObject obstacleObj = Instantiate(obstacleObjectPrefab, worldPos, Quaternion.identity, obstaclesParent);
            obstacleObj.name = $"Obstacle_Boundary_Top_{x}_{y}";
            obstacles[gridPos] = obstacleObj;
        }

        // Bottom edge: y = minY - 1, x from minX - 1 to maxX + 1
        for (int x = minX - 1; x <= maxX + 1; x++)
        {
            int y = minY - 1;
            Vector2Int gridPos = new Vector2Int(x, y);

            // Skip if obstacle already exists at this position
            if (obstacles.ContainsKey(gridPos))
            {
                continue;
            }

            Vector3 worldPos = GridToWorldPosition(gridPos);
            GameObject obstacleObj = Instantiate(obstacleObjectPrefab, worldPos, Quaternion.identity, obstaclesParent);
            obstacleObj.name = $"Obstacle_Boundary_Bottom_{x}_{y}";
            obstacles[gridPos] = obstacleObj;
        }

        // Left edge: x = minX - 1, y from minY to maxY (excluding corners which are already generated)
        for (int y = minY; y <= maxY; y++)
        {
            int x = minX - 1;
            Vector2Int gridPos = new Vector2Int(x, y);

            // Skip if obstacle already exists at this position
            if (obstacles.ContainsKey(gridPos))
            {
                continue;
            }

            Vector3 worldPos = GridToWorldPosition(gridPos);
            GameObject obstacleObj = Instantiate(obstacleObjectPrefab, worldPos, Quaternion.identity, obstaclesParent);
            obstacleObj.name = $"Obstacle_Boundary_Left_{x}_{y}";
            obstacles[gridPos] = obstacleObj;
        }

        // Right edge: x = maxX + 1, y from minY to maxY (excluding corners which are already generated)
        for (int y = minY; y <= maxY; y++)
        {
            int x = maxX + 1;
            Vector2Int gridPos = new Vector2Int(x, y);

            // Skip if obstacle already exists at this position
            if (obstacles.ContainsKey(gridPos))
            {
                continue;
            }

            Vector3 worldPos = GridToWorldPosition(gridPos);
            GameObject obstacleObj = Instantiate(obstacleObjectPrefab, worldPos, Quaternion.identity, obstaclesParent);
            obstacleObj.name = $"Obstacle_Boundary_Right_{x}_{y}";
            obstacles[gridPos] = obstacleObj;
        }
    }

    /// <summary>
    /// Generate CatBlock prefabs based on level data
    /// </summary>
    private void GenerateCatBlocks()
    {
        if (levelData == null || levelData.Cells == null)
        {
            return;
        }

        if (catBlockObjectPrefab == null)
        {
            Debug.LogWarning("CatBlock prefab is null, cannot generate cat blocks");
            return;
        }

        foreach (var cellData in levelData.Cells)
        {
            // Skip if no block data
            if (cellData.Block == null || cellData.Index == null)
            {
                continue;
            }

            // Get grid position
            int x = cellData.Index.x;
            int y = cellData.Index.y;
            Vector2Int gridPos = new Vector2Int(x, y);

            // Check if cell exists
            if (!cells.ContainsKey(gridPos))
            {
                Debug.LogWarning($"Cell at ({x}, {y}) not found, cannot place cat block");
                continue;
            }

            // Get world position (same as cell position)
            Vector3 worldPos = GridToWorldPosition(gridPos);

            // Instantiate cat block object
            GameObject catBlockObj = Instantiate(catBlockObjectPrefab, worldPos, Quaternion.identity, catBlocksParent);
            catBlockObj.name = $"CatBlock_{x}_{y}_Type_{cellData.Block.Type}";

            // Set direction using CatBlock component
            CatBlock catBlock = catBlockObj.GetComponent<CatBlock>();
            if (catBlock == null)
            {
                catBlock = catBlockObj.GetComponentInChildren<CatBlock>();
            }

            if (catBlock != null)
            {
                // Convert direction from JSON (0=Up, 1=Down, 2=Left, 3=Right) to CatBlockDirection enum
                CatBlockDirection direction = CatBlockDirection.Up;
                switch (cellData.Block.Direction)
                {
                    case 0:
                        direction = CatBlockDirection.Up;
                        break;
                    case 1:
                        direction = CatBlockDirection.Right;
                        break;
                    case 2:
                        direction = CatBlockDirection.Down;
                        break;
                    case 3:
                        direction = CatBlockDirection.Left;
                        break;
                    default:
                        Debug.LogWarning($"Unknown direction value {cellData.Block.Direction} for CatBlock at ({x}, {y}), defaulting to Up");
                        break;
                }
                catBlock.SetDirection(direction);

                // Add cats from Block.Cats data
                if (cellData.Block.Cats != null && cellData.Block.Cats.Count > 0)
                {
                    foreach (var catData in cellData.Block.Cats)
                    {
                        if (catData != null)
                        {
                            GameObject spawnedCat = catBlock.AddCat(catData.Color);
                            if (spawnedCat == null)
                            {
                                Debug.LogWarning($"Failed to spawn cat with color {catData.Color} in CatBlock at ({x}, {y})");
                            }
                        }
                    }
                }
            }
            else
            {
                Debug.LogWarning($"CatBlock component not found in {catBlockObj.name}");
            }

            // Store in dictionary
            catBlocks[gridPos] = catBlockObj;
        }
    }

    /// <summary>
    /// Setup collision ignores between boxes and cats with the same color ID
    /// </summary>
    private void SetupColorCollisions()
    {
        if (boxesParent == null || catsParent == null)
        {
            return;
        }

        // Get all BoxObjects from boxesParent
        BoxObject[] allBoxObjects = boxesParent.GetComponentsInChildren<BoxObject>();

        // Get all CatViews from cats dictionary
        List<CatView> allCatViews = new List<CatView>();
        foreach (var catObj in cats.Values)
        {
            if (catObj != null)
            {
                CatView catView = catObj.GetComponent<CatView>();
                if (catView == null)
                {
                    catView = catObj.GetComponentInChildren<CatView>();
                }
                if (catView != null)
                {
                    allCatViews.Add(catView);
                }
            }
        }

        // For each box, ignore collision with cats that have the same color ID
        foreach (BoxObject boxObject in allBoxObjects)
        {
            if (boxObject == null)
            {
                continue;
            }

            // Get all active colliders from the box (a GameObject can have multiple Collider2D components)
            List<Collider2D> boxColliders = boxObject.GetActiveColliders();
            if (boxColliders == null || boxColliders.Count == 0)
            {
                continue;
            }

            int boxColorID = boxObject.CurrentColorID;

            // Ignore collision with cats that have the same color ID
            foreach (CatView catView in allCatViews)
            {
                if (catView == null)
                {
                    continue;
                }

                if (catView.CurrentColorID == boxColorID)
                {
                    Collider2D catCollider = catView.GetCollider();
                    if (catCollider != null)
                    {
                        // Ignore collision for all box colliders with this cat collider
                        foreach (Collider2D boxCollider in boxColliders)
                        {
                            if (boxCollider != null)
                            {
                                //Debug.Log($"Ignoring collision between box {boxCollider.name} and cat {catCollider.name}");
                                Physics2D.IgnoreCollision(boxCollider, catCollider, true);
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Calculate offset to center the board on screen
    /// </summary>
    private void CalculateBoardOffset()
    {
        if (levelData == null || levelData.Cells == null || levelData.Cells.Count == 0)
        {
            boardOffset = Vector3.zero;
            return;
        }

        // Find min and max coordinates
        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minY = int.MaxValue;
        int maxY = int.MinValue;

        foreach (var cellData in levelData.Cells)
        {
            if (cellData.Index == null) continue;

            int x = cellData.Index.x;
            int y = cellData.Index.y;

            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
            if (y < minY) minY = y;
            if (y > maxY) maxY = y;
        }

        // Calculate center of board in grid coordinates
        float centerX = (minX + maxX) * 0.5f;
        float centerY = (minY + maxY) * 0.5f;

        // Calculate offset to center board at origin (0, 0, 0)
        boardOffset = new Vector3(-centerX * cellSize, -centerY * cellSize, 0);
    }

    /// <summary>
    /// Convert grid position (x, y) to world position
    /// </summary>
    /// <param name="gridPos">Grid position (x, y)</param>
    /// <returns>World position</returns>
    private Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        Vector3 basePos = new Vector3(gridPos.x * cellSize, gridPos.y * cellSize, 0);
        return basePos + boardOffset;
    }

    /// <summary>
    /// Convert world position to grid position (x, y)
    /// </summary>
    /// <param name="worldPos">World position</param>
    /// <returns>Grid position</returns>
    private Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        // Remove board offset first
        Vector3 localPos = worldPos - boardOffset;

        // Convert to grid coordinates (round to nearest)
        int gridX = Mathf.RoundToInt(localPos.x / cellSize);
        int gridY = Mathf.RoundToInt(localPos.y / cellSize);

        return new Vector2Int(gridX, gridY);
    }

    /// <summary>
    /// Get world position from grid coordinates
    /// </summary>
    /// <param name="x">Grid X coordinate</param>
    /// <param name="y">Grid Y coordinate</param>
    /// <returns>World position</returns>
    public Vector3 GetWorldPosition(int x, int y)
    {
        return GridToWorldPosition(new Vector2Int(x, y));
    }

    /// <summary>
    /// Get cell GameObject at grid position
    /// </summary>
    /// <param name="x">Grid X coordinate</param>
    /// <param name="y">Grid Y coordinate</param>
    /// <returns>Cell GameObject or null if not found</returns>
    public GameObject GetCell(int x, int y)
    {
        Vector2Int gridPos = new Vector2Int(x, y);
        return cells.ContainsKey(gridPos) ? cells[gridPos] : null;
    }

    /// <summary>
    /// Clean entire game board (cells, cats, boxes, obstacles, cat blocks). Call when returning to home from fail.
    /// Removes freeze effect immediately if active.
    /// </summary>
    public void CleanBoard()
    {
        StopFreeze();
        ClearCells();
    }

    /// <summary>
    /// Clear all cells from the board. Uses DestroyImmediate so the hierarchy is clean before LoadLevel continues with Generate* (avoids Replay seeing stale objects from GetComponentsInChildren).
    /// </summary>
    private void ClearCells()
    {
        // Destroy all BoxObject roots under boxesParent (boxes dictionary only holds BoxPartObject children)
        if (boxesParent != null)
        {
            BoxObject[] allBoxObjects = boxesParent.GetComponentsInChildren<BoxObject>();
            foreach (BoxObject boxObject in allBoxObjects)
            {
                if (boxObject != null && boxObject.gameObject != null)
                    DestroyImmediate(boxObject.gameObject);
            }
        }
        boxes.Clear();

        foreach (var cell in cells.Values)
        {
            if (cell != null)
                DestroyImmediate(cell);
        }
        cells.Clear();

        foreach (var cat in cats.Values)
        {
            if (cat != null)
                DestroyImmediate(cat);
        }
        cats.Clear();

        foreach (var obstacle in obstacles.Values)
        {
            if (obstacle != null)
                DestroyImmediate(obstacle);
        }
        obstacles.Clear();

        foreach (var catBlock in catBlocks.Values)
        {
            if (catBlock != null)
                DestroyImmediate(catBlock);
        }
        catBlocks.Clear();
    }

    /// <summary>
    /// Get cat GameObject at grid position
    /// </summary>
    /// <param name="x">Grid X coordinate</param>
    /// <param name="y">Grid Y coordinate</param>
    /// <returns>Cat GameObject or null if not found</returns>
    public GameObject GetCat(int x, int y)
    {
        Vector2Int gridPos = new Vector2Int(x, y);
        return cats.ContainsKey(gridPos) ? cats[gridPos] : null;
    }

    /// <summary>
    /// Get box part GameObject at grid position
    /// </summary>
    /// <param name="x">Grid X coordinate</param>
    /// <param name="y">Grid Y coordinate</param>
    /// <returns>Box part GameObject or null if not found</returns>
    public GameObject GetBox(int x, int y)
    {
        Vector2Int gridPos = new Vector2Int(x, y);
        return boxes.ContainsKey(gridPos) ? boxes[gridPos] : null;
    }

    /// <summary>
    /// Sets level and reloads (actual level is stored in GameManager; called externally when level changes).
    /// </summary>
    /// <param name="levelName">Level file name without extension</param>
    public void SetLevel(string levelName)
    {
        LoadLevel(levelName);
    }

    /// <summary>
    /// Raycast from screen position to find BoxObject
    /// Uses raycast2D to detect collider on Box layer, then finds parent BoxObject
    /// </summary>
    /// <param name="screenPosition">Screen position (e.g., Input.mousePosition or touch position)</param>
    /// <param name="camera">Camera to use for raycast (if null, uses Camera.main)</param>
    /// <returns>BoxObject if found, null otherwise</returns>
    public BoxObject RaycastBoxObject(Vector2 screenPosition, Camera camera = null)
    {
        if (camera == null)
        {
            camera = Camera.main;
            if (camera == null)
            {
                Debug.LogWarning("Camera.main is null and no camera provided to RaycastBoxObject");
                return null;
            }
        }

        // Convert screen position to world position
        Vector3 worldPos = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, camera.nearClipPlane));
        Vector2 rayOrigin = new Vector2(worldPos.x, worldPos.y);

        // Create layer mask for Box layer
        int boxLayer = LayerMask.NameToLayer("Box");
        if (boxLayer == -1)
        {
            Debug.LogWarning("Layer 'Box' not found, trying layer index 6");
            boxLayer = 6; // Fallback to layer 6 from prefab
        }

        LayerMask layerMask = 1 << boxLayer;

        // Perform raycast
        RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.zero, 0f, layerMask);

        if (hit.collider != null)
        {
            // Get GameObject from hit collider
            GameObject hitObject = hit.collider.gameObject;

            // Find BoxObject in parent hierarchy
            BoxObject boxObject = hitObject.GetComponentInParent<BoxObject>();

            if (boxObject != null)
            {
                return boxObject;
            }
            else
            {
                Debug.LogWarning($"BoxObject not found in parent hierarchy of {hitObject.name}");
            }
        }

        return null;
    }

    /// <summary>
    /// Raycast from screen position to find CatView (cat object)
    /// </summary>
    /// <param name="screenPosition">Screen position (e.g., Input.mousePosition)</param>
    /// <param name="camera">Camera to use (if null, uses Camera.main)</param>
    /// <returns>CatView if found, null otherwise</returns>
    public CatView RaycastCatView(Vector2 screenPosition, Camera camera = null)
    {
        if (camera == null)
        {
            camera = Camera.main;
            if (camera == null)
            {
                return null;
            }
        }

        Vector3 worldPos = camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, camera.nearClipPlane));
        Vector2 rayOrigin = new Vector2(worldPos.x, worldPos.y);

        int catLayer = LayerMask.NameToLayer("Cat");
        if (catLayer == -1)
        {
            // No Cat layer - raycast all and look for CatView
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.zero, 0f);
            if (hit.collider != null)
            {
                CatView catView = hit.collider.GetComponent<CatView>();
                if (catView == null)
                {
                    catView = hit.collider.GetComponentInParent<CatView>();
                }
                return catView;
            }
            return null;
        }

        LayerMask layerMask = 1 << catLayer;
        RaycastHit2D hitCat = Physics2D.Raycast(rayOrigin, Vector2.zero, 0f, layerMask);

        if (hitCat.collider != null)
        {
            CatView catView = hitCat.collider.GetComponent<CatView>();
            if (catView == null)
            {
                catView = hitCat.collider.GetComponentInParent<CatView>();
            }
            return catView;
        }

        return null;
    }

    /// <summary>
    /// Raycast from world position to find BoxObject
    /// Uses raycast2D to detect collider on Box layer, then finds parent BoxObject
    /// </summary>
    /// <param name="worldPosition">World position for raycast</param>
    /// <returns>BoxObject if found, null otherwise</returns>
    public BoxObject RaycastBoxObjectFromWorld(Vector2 worldPosition)
    {
        // Create layer mask for Box layer
        int boxLayer = LayerMask.NameToLayer("Box");
        if (boxLayer == -1)
        {
            Debug.LogWarning("Layer 'Box' not found, trying layer index 6");
            boxLayer = 6; // Fallback to layer 6 from prefab
        }

        LayerMask layerMask = 1 << boxLayer;

        // Perform raycast from world position
        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero, 0f, layerMask);

        if (hit.collider != null)
        {
            // Get GameObject from hit collider
            GameObject hitObject = hit.collider.gameObject;

            // Find BoxObject in parent hierarchy
            BoxObject boxObject = hitObject.GetComponentInParent<BoxObject>();

            if (boxObject != null)
            {
                return boxObject;
            }
            else
            {
                Debug.LogWarning($"BoxObject not found in parent hierarchy of {hitObject.name}");
            }
        }

        return null;
    }

    /// <summary>
    /// Find the first BoxPartObject of a BoxObject that has currentCount > 0 and colorIndex matching the given color.
    /// Used for star-bubble cat jump (cat jumps to hole by star).
    /// </summary>
    /// <param name="colorIndex">Color ID to match (e.g. cat's CurrentColorID)</param>
    /// <returns>First BoxPartObject of the matching BoxObject, or null if none found</returns>
    public BoxPartObject GetFirstBoxPartForStarCat(int colorIndex)
    {
        if (boxesParent == null)
        {
            return null;
        }

        BoxObject[] allBoxObjects = boxesParent.GetComponentsInChildren<BoxObject>();

        foreach (BoxObject boxObject in allBoxObjects)
        {
            if (boxObject == null || boxObject.CurrentCount <= 0 || boxObject.CurrentColorID != colorIndex)
            {
                continue;
            }

            BoxPartObject[] parts = boxObject.GetComponentsInChildren<BoxPartObject>();
            if (parts != null && parts.Length > 0)
            {
                return parts[0];
            }
        }

        return null;
    }

    /// <summary>
    /// Number of BoxObjects currently on the board (used to check level complete).
    /// </summary>
    public int GetBoxObjectCount()
    {
        if (boxesParent == null)
            return 0;
        return boxesParent.GetComponentsInChildren<BoxObject>().Length;
    }
}
