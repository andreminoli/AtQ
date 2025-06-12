using UnityEngine;
using System.Collections.Generic;
using System;

public class PlayerPawn : UnitBase
{
    private GridManager grid;
    private List<Tile> highlightedTiles = new();
    private MoveCard currentPreviewCard;

    public static event Action<PlayerPawn> OnInitialized;
    public bool IsInitialized { get; private set; }

    public GridManager GridManager => grid;

    protected override void Start()
    {
        base.Start();

        if (string.IsNullOrEmpty(unitName))
        {
            unitName = "Player";
        }
        unitType = UnitType.Player;

        Debug.Log($"PlayerPawn Start() called at frame {Time.frameCount}");
    }

    public void Init(Vector2Int startPos, GridManager gridManager)
    {
        if (IsInitialized)
        {
            Debug.LogWarning($"PlayerPawn.Init() called but already initialized at frame {Time.frameCount}");
            return;
        }

        grid = gridManager;
        GridPosition = startPos;
        transform.position = new Vector3(startPos.x, 0.5f, startPos.y);

        Initialize("Player", 10, UnitType.Player);

        Debug.Log($"PlayerPawn initializing at {startPos} with GridManager: {grid != null} at frame {Time.frameCount}");

        IsInitialized = true;

        Debug.Log($"PlayerPawn initialization complete - triggering OnInitialized event at frame {Time.frameCount}");

        OnInitialized?.Invoke(this);

        Debug.Log($"PlayerPawn OnInitialized event triggered at frame {Time.frameCount}");
    }

    public void PreviewCardMoves(MoveCard card)
    {
        Debug.Log($"Previewing moves for card: {card.cardName} at frame {Time.frameCount}");

        if (grid == null)
        {
            Debug.LogError($"PreviewCardMoves failed: GridManager reference is null at frame {Time.frameCount}.");
            return;
        }

        ClearMovementPreview();

        currentPreviewCard = card;

        if (card.isSliding)
        {
            foreach (Vector2Int dir in card.slideDirections)
            {
                Vector2Int testPos = GridPosition + dir;
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
                Vector2Int testPos = GridPosition + offset;
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

    public void TryUseCard(MoveCard card)
    {
        Debug.Log($"TryUseCard (legacy) called for: {card.cardName} - delegating to PreviewCardMoves");
        PreviewCardMoves(card);
    }

    private void OnTileClickedForMove(Tile tile)
    {
        Debug.Log($"Tile clicked for move: {tile.GridPosition} at frame {Time.frameCount}");

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

        if (DeckManager.Instance.TryUseCard(currentPreviewCard))
        {
            ExecuteMoveTo(tile.GridPosition);
            Debug.Log($"Successfully used card {currentPreviewCard.cardName} to move to {tile.GridPosition}");
        }
        else
        {
            Debug.LogWarning($"Failed to use card {currentPreviewCard.cardName} - not in current hand");
        }

        ClearMovementPreview();
    }

    private void OnTileClicked(Tile tile)
    {
        Debug.Log($"Legacy OnTileClicked called - delegating to OnTileClickedForMove");
        OnTileClickedForMove(tile);
    }

    private void ExecuteMoveTo(Vector2Int targetPosition)
    {
        if (grid != null)
        {
            grid.UpdateUnitPosition(this, GridPosition, targetPosition);
        }

        GridPosition = targetPosition;
        transform.position = new Vector3(GridPosition.x, 0.5f, GridPosition.y);
        Debug.Log($"Moved player to {GridPosition} at frame {Time.frameCount}");
    }

    public void ClearMovementPreview()
    {
        ClearHighlights();
        currentPreviewCard = null;
        Debug.Log($"Cleared movement preview at frame {Time.frameCount}");
    }

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

    public MoveCard GetCurrentPreviewCard()
    {
        return currentPreviewCard;
    }

    public bool IsPreviewingMoves()
    {
        return currentPreviewCard != null && highlightedTiles.Count > 0;
    }

    public bool IsReadyForCardOperations()
    {
        return IsInitialized && grid != null && GridPosition != Vector2Int.zero;
    }

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

    protected override void Die()
    {
        base.Die();
        Debug.Log("Player has died! Game Over!");
    }

#if UNITY_EDITOR
    [ContextMenu("Debug PlayerPawn State")]
    private void DebugPlayerState()
    {
        Debug.Log($"=== PLAYERPAWN DEBUG (Frame {Time.frameCount}) ===");
        Debug.Log($"Is Initialized: {IsInitialized}");
        Debug.Log($"Grid Position: {GridPosition}");
        Debug.Log($"World Position: {transform.position}");
        Debug.Log($"Grid Manager: {(grid != null ? grid.name : "NULL")}");
        Debug.Log($"Ready for Card Operations: {IsReadyForCardOperations()}");
        Debug.Log($"Health: {GetHealth()}");
        Debug.Log($"Is Alive: {IsAlive()}");
        Debug.Log($"Unit Type: {GetUnitType()}");
        Debug.Log($"Highlighted Tiles: {highlightedTiles.Count}");
        Debug.Log($"Current Preview Card: {(currentPreviewCard != null ? currentPreviewCard.cardName : "NULL")}");
        Debug.Log($"Is Previewing Moves: {IsPreviewingMoves()}");
        Debug.Log("=================================");
    }
#endif
}