using UnityEngine;
using UnityEngine.EventSystems;
using System;

public class Tile : MonoBehaviour, IPointerClickHandler
{
    [Header("Tile Settings")]
    [SerializeField] private Vector2Int gridPosition;
    [SerializeField] private bool isWalkable = true;
    [SerializeField] private bool debugMode = false;

    [Header("Occupancy")]
    [SerializeField] private UnitBase currentOccupant = null;

    [Header("Visual Feedback")]
    [SerializeField] private Renderer rend;
    [SerializeField] private SpriteRenderer spriteRenderer;
    private Color originalColor;
    [SerializeField] private Color occupiedColor = Color.red;
    [SerializeField] private Color blockedColor = Color.gray;

    [Header("Click Handling")]
    private Action<Tile> onClickCallback;

    public Vector2Int GridPosition => gridPosition;

    void Start()
    {
        if (rend == null) rend = GetComponent<Renderer>();
        if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();

        if (rend != null) originalColor = rend.material.color;
        else if (spriteRenderer != null) originalColor = spriteRenderer.color;

        UpdateVisualState();
    }

    public void Init(Vector2Int position) => Initialize(position);

    public void Initialize(Vector2Int position)
    {
        gridPosition = position;
        gameObject.name = $"Tile ({position.x}, {position.y})";
        UpdateVisualState();
    }

    public bool IsEmpty() => isWalkable && currentOccupant == null;
    public bool IsWalkable() => isWalkable;

    public void SetWalkable(bool walkable)
    {
        isWalkable = walkable;
        UpdateVisualState();
    }

    public UnitBase GetOccupant() => currentOccupant;

    public bool SetOccupant(UnitBase unit)
    {
        if (unit != null && currentOccupant != null && currentOccupant != unit)
        {
            if (debugMode)
                Debug.LogWarning($"Tile {gridPosition} already occupied by {currentOccupant.GetUnitName()}");
            return false;
        }

        if (unit != null && !isWalkable)
        {
            if (debugMode)
                Debug.LogWarning($"Tile {gridPosition} is not walkable");
            return false;
        }

        currentOccupant = unit;
        UpdateVisualState();

        if (debugMode)
        {
            string msg = unit != null
                ? $"Placed {unit.GetUnitName()} on tile {gridPosition}"
                : $"Cleared tile {gridPosition}";
            Debug.Log(msg);
        }

        return true;
    }

    public void ClearOccupant() => SetOccupant(null);

    public bool CanUnitMoveTo(UnitBase unit)
    {
        return isWalkable && (currentOccupant == null || currentOccupant == unit);
    }

    public void Highlight(Color color, Action<Tile> onClick)
    {
        SetTileColor(color);
        onClickCallback = onClick;
    }

    public void ClearHighlight()
    {
        SetTileColor(originalColor);
        onClickCallback = null;
        UpdateVisualState();
    }

    private void SetTileColor(Color color)
    {
        if (rend != null) rend.material.color = color;
        else if (spriteRenderer != null) spriteRenderer.color = color;
    }

    private void UpdateVisualState()
    {
        if (onClickCallback != null) return;

        Color target = originalColor;
        if (!isWalkable) target = blockedColor;
        else if (currentOccupant != null) target = occupiedColor;

        SetTileColor(target);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (onClickCallback != null)
        {
            if (debugMode)
                Debug.Log($"✅ Tile clicked at {gridPosition}");
            onClickCallback.Invoke(this);
        }
    }
}
