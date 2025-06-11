using UnityEngine;

public class UnitBase : MonoBehaviour
{
    public Vector2Int gridPosition;
    public int baseHP = 1;
    public int armor = 0;

    public int TotalHP => baseHP + armor;

    public virtual void TakeDamage(int amount)
    {
        int remaining = TotalHP - amount;

        if (remaining <= 0)
        {
            Debug.Log($"{gameObject.name} defeated.");
            Destroy(gameObject); // simulate capture
        }
        else
        {
            if (armor > 0)
            {
                int absorbed = Mathf.Min(armor, amount);
                armor -= absorbed;
                amount -= absorbed;
            }

            baseHP -= amount;
            Debug.Log($"{gameObject.name} took damage. HP: {baseHP}, Armor: {armor}");
        }
    }
}
