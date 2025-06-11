using UnityEngine;
using System.Collections.Generic;
using System;

public class PlayerPawn : MonoBehaviour
{
    public Vector2Int gridPosition;
    private GridManager grid;
    private List<Tile> highlightedTiles = new();
    public static event Action<PlayerPawn> OnInitialized;
    public bool IsInitialized { get; private set; }

    public void Init(Vector2Int startPos, GridManager gridManager)
    {
        // Prevent multiple initialization
        if (IsInitialized)
        {
            Debug.LogWarning($"PlayerPawn.Init() called but already initialized at frame {Time.frameCount}");
            return;
        }

        grid = gridManager;
        gridPosition = startPos;
        transform.position = new Vector3(startPos.x, 0.5f, startPos.y);

        Debug.Log($"PlayerPawn initializing at {startPos} with GridManager: {grid != null} at frame {Time.frameCount}");

        // Set initialized flag BEFORE triggering event
        IsInitialized = true;

        Debug.Log($"PlayerPawn initialization complete - triggering OnInitialized event at frame {Time.frameCount}");

        // Trigger the event to notify other systems
        OnInitialized?.Invoke(this);

        Debug.Log($"PlayerPawn OnInitialized event triggered at frame {Time.frameCount}");
    }

    public void TryUseCard(MoveCard card)
    {
        Debug.Log($"Attempting to use card: {card.cardName} at frame {Time.frameCount}");

        if (grid == null)
        {
            Debug.LogError($"TryUseCard failed: GridManager reference is null at frame {Time.frameCount}.");
            return;
        }

        ClearHighlights();

        if (card.isSliding)
        {
            foreach (Vector2Int dir in card.slideDirections)
            {
                Vector2Int testPos = gridPosition + dir;
                while (grid.IsInBounds(testPos))
                {
                    Tile tile = grid.GetTileAt(testPos);
                    if (tile != null)
                    {
                        tile.Highlight(Color.cyan, OnTileClicked);
                        highlightedTiles.Add(tile);
                    }

                    testPos += dir;
                }
            }
        }
        else
        {
            foreach (Vector2Int offset in card.moveOffsets)
            {
                Vector2Int testPos = gridPosition + offset;
                if (grid.IsInBounds(testPos))
                {
                    Tile tile = grid.GetTileAt(testPos);
                    if (tile != null)
                    {
                        tile.Highlight(Color.green, OnTileClicked);
                        highlightedTiles.Add(tile);
                    }
                }
            }
        }
    }

    private void OnTileClicked(Tile tile)
    {
        gridPosition = tile.gridPosition;
        transform.position = new Vector3(gridPosition.x, 0.5f, gridPosition.y);
        Debug.Log($"Moved player to {gridPosition} at frame {Time.frameCount}");
        ClearHighlights();
    }

    private void ClearHighlights()
    {
        foreach (Tile tile in highlightedTiles)
        {
            tile.ClearHighlight();
        }

        highlightedTiles.Clear();
    }

    /// <summary>
    /// Force trigger the initialization event (for debugging or special cases)
    /// </summary>
    public void ForceInitializationEvent()
    {
        if (!IsInitialized)
        {
            Debug.LogWarning($"ForceInitializationEvent called but PlayerPawn is not initialized at frame {Time.frameCount}");
            return;
        }

        Debug.Log($"Force triggering OnInitialized event at frame {Time.frameCount}");
        OnInitialized?.Invoke(this);
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only debug method
    /// </summary>
    [ContextMenu("Debug PlayerPawn State")]
    private void DebugPlayerState()
    {
        Debug.Log($"=== PLAYERPAWN DEBUG (Frame {Time.frameCount}) ===");
        Debug.Log($"Is Initialized: {IsInitialized}");
        Debug.Log($"Grid Position: {gridPosition}");
        Debug.Log($"World Position: {transform.position}");
        Debug.Log($"Grid Manager: {(grid != null ? grid.name : "NULL")}");
        Debug.Log($"Highlighted Tiles: {highlightedTiles.Count}");
        Debug.Log("=================================");
    }
#endif
}