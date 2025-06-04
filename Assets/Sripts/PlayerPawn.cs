using UnityEngine;

public class PlayerPawn : MonoBehaviour
{
    public Vector2Int gridPosition;
    private GridManager grid;

    // Called from GridManager after instantiating
    public void Init(Vector2Int startPos, GridManager gridManager)
    {
        grid = gridManager;
        gridPosition = startPos;

        // Place this pawn on the board (Y = 0.5 so it sits above the tile)
        transform.position = new Vector3(startPos.x, 0.5f, startPos.y);
    }

    // This must be inside the class brackets!
    public void TryUseCard(MoveCard card)
    {
        Debug.Log($"Attempting to use card: {card.cardName}");
        // Eventually: Highlight legal tiles here
    }
}
