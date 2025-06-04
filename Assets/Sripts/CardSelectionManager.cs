using UnityEngine;

public class CardSelectionManager : MonoBehaviour
{
    public static CardSelectionManager Instance;

    public MoveCard selectedCard;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    public void SelectCard(MoveCard card)
    {
        selectedCard = card;
        Debug.Log("Card selected: " + card.cardName);
    }
}