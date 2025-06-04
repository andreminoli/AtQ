using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MoveCard", menuName = "AtQ/MoveCard")]
public class MoveCard : ScriptableObject
{
    [Header("Card Info")]
    public string cardName;
    [TextArea] public string description;
    public Sprite artwork;

    [Header("Cost")]
    public int focusCost = 1; // 🧘 Value between 1–5 for now

    [Header("Movement Rules")]
    public List<Vector2Int> moveOffsets;       // Legal move directions
    public List<Vector2Int> captureOffsets;    // Legal capture directions

    [Header("Special Rules")]
    public bool isFirstMoveBonus = false;
    public int bonusMoveDistance = 0;

    [HideInInspector] public bool hasBeenUsed = false; // Tracked at runtime
}
