using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [Header("Grid Settings")]
    [SerializeField] private int gridWidth = 5;
    [SerializeField] private int gridHeight = 5;
    [SerializeField] private float tileSize = 1f;

    [Header("Unit Prefabs")]
    [SerializeField] private GameObject kingPrefab;
    [SerializeField] private GameObject playerPrefab;  // ADD THIS TO INSPECTOR
    [SerializeField] private GameObject tilePrefab;

    private Tile[,] gridTiles;
    private KingUnit king;
    private PlayerPawn player;  // Track the player reference
    private Dictionary<Vector2Int, UnitBase> unitPositions;
    private List<EnemyUnit> enemies;

    void Start()
    {
        InitializeGrid();
        SpawnKing();
        SpawnPlayer();  // Spawn the player after king
    }

    private void InitializeGrid()
    {
        gridTiles = new Tile[gridWidth, gridHeight];
        unitPositions = new Dictionary<Vector2Int, UnitBase>();
        enemies = new List<EnemyUnit>();

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector2Int gridPos = new Vector2Int(x, y);
                Vector3 worldPos = GridToWorldPosition(gridPos);

                GameObject tileObject = tilePrefab != null
                    ? Instantiate(tilePrefab, worldPos, Quaternion.identity, transform)
                    : new GameObject($"Tile ({x}, {y})");

                if (tileObject.transform.parent == null)
                    tileObject.transform.parent = transform;

                tileObject.transform.position = worldPos;
                Tile tile = tileObject.GetComponent<Tile>() ?? tileObject.AddComponent<Tile>();
                tile.Initialize(gridPos);
                gridTiles[x, y] = tile;
            }
        }
    }

    private void SpawnKing()
    {
        if (kingPrefab == null)
        {
            Debug.LogError("GridManager: King prefab is not assigned!");
            return;
        }

        Vector2Int spawnPosition = new Vector2Int(gridWidth / 2, 0);
        Vector3 worldPosition = GridToWorldPosition(spawnPosition);

        GameObject kingObject = Instantiate(kingPrefab, worldPosition, Quaternion.identity);
        king = kingObject.GetComponent<KingUnit>();

        if (king == null)
        {
            Debug.LogError("GridManager: King prefab doesn't have KingUnit component!");
            return;
        }

        king.GridPosition = spawnPosition;
        king.transform.position = worldPosition;
        GetTileAt(spawnPosition)?.SetOccupant(king);

        Debug.Log($"King spawned at grid position: {spawnPosition}");
    }

    private void SpawnPlayer()
    {
        if (playerPrefab == null)
        {
            Debug.LogError("GridManager: Player prefab is not assigned! Please assign the PlayerPawn prefab in the inspector.");
            return;
        }

        // Spawn player at bottom-left corner, offset from king
        Vector2Int spawnPosition = new Vector2Int(0, 0);

        // If bottom-left is occupied, find an alternative position
        if (!IsPositionEmpty(spawnPosition))
        {
            // Try other corners first
            Vector2Int[] alternativePositions = new Vector2Int[]
            {
                new Vector2Int(gridWidth - 1, 0),  // Bottom-right
                new Vector2Int(0, 1),               // One up from bottom-left
                new Vector2Int(gridWidth - 1, 1),  // One up from bottom-right
            };

            bool foundPosition = false;
            foreach (Vector2Int pos in alternativePositions)
            {
                if (IsInBounds(pos) && IsPositionEmpty(pos))
                {
                    spawnPosition = pos;
                    foundPosition = true;
                    break;
                }
            }

            // If still no position, search the entire grid
            if (!foundPosition)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    for (int x = 0; x < gridWidth; x++)
                    {
                        Vector2Int testPos = new Vector2Int(x, y);
                        if (IsPositionEmpty(testPos))
                        {
                            spawnPosition = testPos;
                            foundPosition = true;
                            break;
                        }
                    }
                    if (foundPosition) break;
                }
            }
        }

        Vector3 worldPosition = GridToWorldPosition(spawnPosition);
        GameObject playerObject = Instantiate(playerPrefab, worldPosition, Quaternion.identity);
        player = playerObject.GetComponent<PlayerPawn>();

        if (player == null)
        {
            Debug.LogError("GridManager: Player prefab doesn't have PlayerPawn component!");
            Destroy(playerObject);
            return;
        }

        // Initialize the player with grid reference - this triggers the card system
        player.Init(spawnPosition, this);

        // Update grid tracking
        unitPositions[spawnPosition] = player;
        GetTileAt(spawnPosition)?.SetOccupant(player);

        Debug.Log($"Player spawned and initialized at grid position: {spawnPosition}");
    }

    // FIXED: Grid positioned on XZ plane (horizontal surface) instead of XY plane (vertical wall)
    public Vector3 GridToWorldPosition(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * tileSize, 0f, gridPos.y * tileSize);
    }

    // FIXED: Convert from XZ world coordinates back to grid coordinates
    public Vector2Int WorldToGridPosition(Vector3 worldPos)
    {
        return new Vector2Int(
            Mathf.RoundToInt(worldPos.x / tileSize),
            Mathf.RoundToInt(worldPos.z / tileSize)  // Changed from worldPos.y to worldPos.z
        );
    }

    public bool IsInBounds(Vector2Int gridPos)
    {
        return gridPos.x >= 0 && gridPos.x < gridWidth &&
               gridPos.y >= 0 && gridPos.y < gridHeight;
    }

    public Tile GetTileAt(Vector2Int gridPos)
    {
        return IsInBounds(gridPos) ? gridTiles[gridPos.x, gridPos.y] : null;
    }

    public GameObject GetTileGameObjectAt(Vector2Int gridPos)
    {
        return GetTileAt(gridPos)?.gameObject;
    }

    public UnitBase GetUnitAt(Vector2Int gridPos)
    {
        return unitPositions.TryGetValue(gridPos, out var unit) ? unit : null;
    }

    public bool IsPositionEmpty(Vector2Int gridPos)
    {
        Tile tile = GetTileAt(gridPos);
        return tile != null && tile.IsEmpty();
    }

    public void UpdateUnitPosition(UnitBase unit, Vector2Int oldPos, Vector2Int newPos)
    {
        if (unitPositions.ContainsKey(oldPos) && unitPositions[oldPos] == unit)
        {
            unitPositions.Remove(oldPos);
            GetTileAt(oldPos)?.ClearOccupant();
        }

        if (IsInBounds(newPos))
        {
            unitPositions[newPos] = unit;
            GetTileAt(newPos)?.SetOccupant(unit);
        }
    }

    public void RemoveUnitFromGrid(UnitBase unit)
    {
        Vector2Int unitPos = unit.GridPosition;

        if (unitPositions.ContainsKey(unitPos) && unitPositions[unitPos] == unit)
        {
            unitPositions.Remove(unitPos);
            GetTileAt(unitPos)?.ClearOccupant();
        }

        if (unit is EnemyUnit enemy)
        {
            enemies.Remove(enemy);
        }
    }

    public void AddEnemy(EnemyUnit enemy)
    {
        if (!enemies.Contains(enemy))
        {
            enemies.Add(enemy);
        }
    }

    public List<EnemyUnit> GetEnemies()
    {
        return new List<EnemyUnit>(enemies);
    }

    public void SetTileAt(Vector2Int gridPos, Tile tile)
    {
        if (IsInBounds(gridPos))
        {
            gridTiles[gridPos.x, gridPos.y] = tile;
        }
    }

    public int GetGridWidth() => gridWidth;
    public int GetGridHeight() => gridHeight;
    public KingUnit GetKing() => king;
    public PlayerPawn GetPlayer() => player;  // Getter for player reference

    public bool ProcessKingTurn()
    {
        if (king == null)
        {
            Debug.LogError("GridManager: No King to process turn for!");
            return false;
        }

        return king.TryMoveTowardTop();
    }

    public bool IsLevelComplete()
    {
        return king != null && king.HasReachedTop();
    }

    // FIXED: Gizmos now draw on XZ plane for horizontal grid visualization
    void OnDrawGizmos()
    {
        Gizmos.color = Color.white;

        // Draw grid bounds on XZ plane (horizontal surface)
        Vector3 center = new Vector3(
            (gridWidth - 1) * tileSize * 0.5f,
            0f,  // Y = 0 for flat surface
            (gridHeight - 1) * tileSize * 0.5f
        );
        Vector3 size = new Vector3(gridWidth * tileSize, 0.1f, gridHeight * tileSize);
        Gizmos.DrawWireCube(center, size);

        Gizmos.color = Color.gray;

        // Draw vertical grid lines (along Z-axis)
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = new Vector3(x * tileSize - tileSize * 0.5f, 0f, -tileSize * 0.5f);
            Vector3 end = new Vector3(x * tileSize - tileSize * 0.5f, 0f, (gridHeight - 0.5f) * tileSize);
            Gizmos.DrawLine(start, end);
        }

        // Draw horizontal grid lines (along X-axis)
        for (int z = 0; z <= gridHeight; z++)
        {
            Vector3 start = new Vector3(-tileSize * 0.5f, 0f, z * tileSize - tileSize * 0.5f);
            Vector3 end = new Vector3((gridWidth - 0.5f) * tileSize, 0f, z * tileSize - tileSize * 0.5f);
            Gizmos.DrawLine(start, end);
        }
    }
}