using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    [Header("UI References")]
    public Image artworkImage;
    public TMP_Text cardNameText;
    public TMP_Text descriptionText;
    public TMP_Text focusText;
    public Button playButton;

    private MoveCard card;
    private PlayerPawn player;

    public void Setup(MoveCard cardData, PlayerPawn playerRef)
    {
        card = cardData;
        player = playerRef;

        // Defensive null checks
        if (card == null)
        {
            Debug.LogError("Setup failed: cardData is null.");
            return;
        }

        if (artworkImage == null || cardNameText == null || descriptionText == null || focusText == null || playButton == null)
        {
            Debug.LogError("CardUI is missing one or more UI references!");
            return;
        }

        // Assign UI text from ScriptableObject
        cardNameText.text = card.cardName;
        descriptionText.text = card.description;
        focusText.text = $"Focus = {card.focusCost}";

        // Check and assign artwork safely
        if (card.artwork == null)
        {
            Debug.LogWarning($"Card {card.cardName} has no artwork assigned!");
            artworkImage.enabled = false; // Prevents invisible null sprite
        }
        else
        {
            artworkImage.sprite = card.artwork;
            artworkImage.enabled = true;
        }

        Debug.Log($"Card setup complete: {card.cardName}, sprite: {card.artwork?.name ?? "NULL"}");

        // Button behavior
        playButton.onClick.RemoveAllListeners();
        playButton.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        Debug.Log($"Clicked card: {card?.cardName}");
        if (player != null && card != null)
        {
            player.TryUseCard(card);
        }
        else
        {
            Debug.LogWarning("Cannot play card — Player or Card is null.");
        }
    }
}
