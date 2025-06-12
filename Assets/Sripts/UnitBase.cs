using UnityEngine;

public abstract class UnitBase : MonoBehaviour
{
    [Header("Unit Base Settings")]
    [SerializeField] protected string unitName = "Unit";
    [SerializeField] protected int health = 1;
    [SerializeField] protected bool isAlive = true;

    public enum UnitType { King, Player, Enemy, Obstacle }
    [SerializeField] protected UnitType unitType = UnitType.King;

    // Grid position property - no backing field serialization
    private Vector2Int gridPosition;
    public virtual Vector2Int GridPosition
    {
        get => gridPosition;
        set => gridPosition = value;
    }

    protected virtual void Start() { }

    public string GetUnitName() => unitName;
    public UnitType GetUnitType() => unitType;
    public int GetHealth() => health;
    public bool IsAlive() => isAlive && health > 0;

    public virtual void TakeDamage(int damage)
    {
        if (!isAlive) return;
        health -= damage;
        if (health <= 0)
        {
            health = 0;
            Die();
        }
    }

    protected virtual void Die()
    {
        isAlive = false;
        Debug.Log($"{unitName} has died!");
    }

    public virtual void Heal(int healAmount)
    {
        if (!isAlive) return;
        health += healAmount;
        Debug.Log($"{unitName} healed for {healAmount} health");
    }

    public virtual void Initialize(string name, int startHealth, UnitType type)
    {
        unitName = name;
        health = startHealth;
        unitType = type;
        isAlive = true;
    }

    public virtual void OnTurnStart() { }
    public virtual void OnTurnEnd() { }
    public virtual void OnInteractWith(UnitBase otherUnit) { }
}