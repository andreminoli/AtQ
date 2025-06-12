using UnityEngine;

public class EnemyUnit : UnitBase
{
    public void ApplyKnockback(Vector2Int direction, GridManager grid)
    {
        Vector2Int newPos = GridPosition + direction;

        if (grid.IsInBounds(newPos) && grid.GetTileAt(newPos) != null)
        {
            Debug.Log($"{name} knocked back from {GridPosition} to {newPos}");

            GridPosition = newPos;
            transform.position = new Vector3(newPos.x, 0.5f, newPos.y);
        }
        else
        {
            Debug.Log($"{name} cannot be knocked back — blocked!");
            TakeDamage(1); // bonus damage if stuck
        }
    }
}