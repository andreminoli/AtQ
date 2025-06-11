using UnityEngine;
using System.Collections.Generic;
using System;

public class PlayerPawn : MonoBehaviour
{
    public Vector2Int gridPosition;
    private GridManager grid;
    private List<Tile> highlightedTiles = new();
    private MoveCard currentPreviewCard; // Track which card is being previewed

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

    /// <summary>
    /// Preview card moves without consuming the card (new method)
    /// </summary>
    public void PreviewCardMoves(MoveCard card)
    {
        Debug.Log($"Previewing moves for card: {card.cardName} at frame {Time.frameCount}");

        if (grid == null)
        {
            Debug.LogError($"PreviewCardMoves failed: GridManager reference is null at frame {Time.frameCount}.");
            return;
        }

        // Clear any existing highlights
        ClearMovementPreview();

        // Store reference to current preview card
        currentPreviewCard = card;

        // Show possible moves
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
                        tile.Highlight(Color.cyan, OnTileClickedForMove);
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
                        tile.Highlight(Color.green, OnTileClickedForMove);
                        highlightedTiles.Add(tile);
                    }
                }
            }
        }

        Debug.Log($"Highlighted {highlightedTiles.Count} tiles for card {card.cardName}");
    }

    /// <summary>
    /// Legacy method - now calls PreviewCardMoves for backward compatibility
    /// </summary>
    public void TryUseCard(MoveCard card)
    {
        Debug.Log($"TryUseCard (legacy) called for: {card.cardName} - delegating to PreviewCardMoves");
        PreviewCardMoves(card);
    }

    /// <summary>
    /// Handle tile click when previewing moves - this is where we actually consume the card
    /// </summary>
    private void OnTileClickedForMove(Tile tile)
    {
        Debug.Log($"Tile clicked for move: {tile.gridPosition} at frame {Time.frameCount}");

        if (currentPreviewCard == null)
        {
            Debug.LogWarning("Tile clicked but no card is being previewed!");
            return;
        }

        if (DeckManager.Instance == null)
        {
            Debug.LogWarning("Cannot execute move - DeckManager not found.");
            ClearMovementPreview();
            return;
        }

        // NOW we consume the card from the hand
        if (DeckManager.Instance.TryUseCard(currentPreviewCard))
        {
            // Execute the move
            ExecuteMoveTo(tile.gridPosition);
            Debug.Log($"Successfully used card {currentPreviewCard.cardName} to move to {tile.gridPosition}");
        }
        else
        {
            Debug.LogWarning($"Failed to use card {currentPreviewCard.cardName} - not in current hand");
        }

        // Clear the preview
        ClearMovementPreview();
    }

    /// <summary>
    /// Legacy tile click handler - kept for backward compatibility
    /// </summary>
    private void OnTileClicked(Tile tile)
    {
        Debug.Log($"Legacy OnTileClicked called - delegating to OnTileClickedForMove");
        OnTileClickedForMove(tile);
    }

    /// <summary>
    /// Execute the actual movement to a position
    /// </summary>
    private void ExecuteMoveTo(Vector2Int targetPosition)
    {
        gridPosition = targetPosition;
        transform.position = new Vector3(gridPosition.x, 0.5f, gridPosition.y);
        Debug.Log($"Moved player to {gridPosition} at frame {Time.frameCount}");
    }

    /// <summary>
    /// Clear movement preview highlights
    /// </summary>
    public void ClearMovementPreview()
    {
        ClearHighlights();
        currentPreviewCard = null;
        Debug.Log($"Cleared movement preview at frame {Time.frameCount}");
    }

    /// <summary>
    /// Clear tile highlights (internal method)
    /// </summary>
    private void ClearHighlights()
    {
        foreach (Tile tile in highlightedTiles)
        {
            if (tile != null)
            {
                tile.ClearHighlight();
            }
        }

        highlightedTiles.Clear();
    }

    /// <summary>
    /// Get the currently previewed card
    /// </summary>
    public MoveCard GetCurrentPreviewCard()
    {
        return currentPreviewCard;
    }

    /// <summary>
    /// Check if player is currently previewing moves
    /// </summary>
    public bool IsPreviewingMoves()
    {
        return currentPreviewCard != null && highlightedTiles.Count > 0;
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
        Debug.Log($"Current Preview Card: {(currentPreviewCard != null ? currentPreviewCard.cardName : "NULL")}");
        Debug.Log($"Is Previewing Moves: {IsPreviewingMoves()}");
        Debug.Log("=================================");
    }
#endif
}