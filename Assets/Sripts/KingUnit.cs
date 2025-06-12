using UnityEngine;

public class KingUnit : UnitBase
{
    [Header("King Settings")]
    [SerializeField] private bool debugMode = true;

    private GridManager gridManager;

    // REMOVED: public override Vector2Int GridPosition { get; set; }
    // We use the inherited GridPosition from UnitBase instead

    protected override void Start()
    {
        base.Start();
        gridManager = Object.FindAnyObjectByType<GridManager>();

        if (gridManager == null)
        {
            Debug.LogError("KingUnit: No GridManager found in scene!");
        }

        // Set default values for King
        if (string.IsNullOrEmpty(unitName))
        {
            unitName = "King";
        }
        unitType = UnitType.King;
    }

    public void InitializePosition(Vector2Int position)
    {
        GridPosition = position;

        if (debugMode)
        {
            Debug.Log($"King initialized at position: {GridPosition}");
        }
    }

    public bool TryMoveTowardTop()
    {
        if (gridManager == null)
        {
            Debug.LogError("KingUnit: Cannot move - no GridManager reference!");
            return false;
        }

        Vector2Int targetPosition = GridPosition + Vector2Int.up;
        Tile targetTile = gridManager.GetTileAt(targetPosition);
        Tile currentTile = gridManager.GetTileAt(GridPosition);

        if (targetTile != null && targetTile.CanUnitMoveTo(this))
        {
            currentTile?.ClearOccupant();
            targetTile.SetOccupant(this);

            GridPosition = targetPosition;
            transform.position = targetTile.transform.position;

            if (debugMode)
            {
                Debug.Log($"King moved to {GridPosition}");
            }

            return true;
        }
        else
        {
            if (debugMode)
            {
                Debug.Log($"King blocked at {GridPosition}");
            }

            return false;
        }
    }

    public bool HasReachedTop()
    {
        int gridHeight = gridManager.GetGridHeight();
        return GridPosition.y >= gridHeight - 1;
    }

    protected override void Die()
    {
        base.Die();
        Debug.Log("The King has fallen! Game Over!");
        // You could trigger game over logic here
    }
}