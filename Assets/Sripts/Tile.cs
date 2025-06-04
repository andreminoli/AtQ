using UnityEngine;

public class Tile : MonoBehaviour
{
    public Vector2Int gridPosition;

    public void Init(Vector2Int pos)
    {
        gridPosition = pos;
        name = $"Tile_{pos.x}_{pos.y}";
    }
}