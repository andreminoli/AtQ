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

    [Header("Hold/Select UI (Optional)")]
    public Button holdButton;
    public Image holdIndicator;
    public Image selectedIndicator;

    private MoveCard card;
    private PlayerPawn player;
    private bool isHeld = false;
    private bool isSelected = false;

    public MoveCard GetCard() => card;
    public bool IsHeld => isHeld;
    public bool IsSelected => isSelected;

    public void Setup(MoveCard cardData, PlayerPawn playerRef)
    {
        card = cardData;
        player = playerRef;

        if (card == null)
        {
            Debug.LogError("Setup failed: cardData is null.");
            return;
        }

        // Validate PlayerPawn has GridManager
        if (player != null && !player.IsInitialized)
        {
            Debug.LogWarning($"[CardUI] PlayerPawn is not properly initialized for card {card.cardName}");
        }

        // 🔍 Check essential UI fields only (not artworkImage)
        if (cardNameText == null || descriptionText == null || focusText == null || playButton == null)
        {
            Debug.LogError("[CardUI] One or more critical UI references are missing!");
            Debug.Log($"cardNameText: {(cardNameText != null ? cardNameText.name : "NULL")}");
            Debug.Log($"descriptionText: {(descriptionText != null ? descriptionText.name : "NULL")}");
            Debug.Log($"focusText: {(focusText != null ? focusText.name : "NULL")}");
            Debug.Log($"playButton: {(playButton != null ? playButton.name : "NULL")}");
            return;
        }

        // ✅ Apply card data to text fields
        cardNameText.text = card.cardName;
        descriptionText.text = card.description;
        focusText.text = $"Focus = {card.focusCost}";

        // 🖼 Handle artwork (optional)
        if (artworkImage != null)
        {
            if (card.artwork != null)
            {
                artworkImage.sprite = card.artwork;
                artworkImage.enabled = true;
            }
            else
            {
                artworkImage.enabled = false;
                Debug.LogWarning($"Card {card.cardName} has no artwork assigned.");
            }
        }

        // 🎯 Set up play button safely
        playButton.interactable = false;
        playButton.onClick.RemoveAllListeners();
        playButton.onClick.AddListener(OnPlayClick);
        playButton.interactable = true;

        // 📌 Set up hold button if available
        if (holdButton != null)
        {
            holdButton.onClick.RemoveAllListeners();
            holdButton.onClick.AddListener(OnHoldClick);

            // Check if this card is already held
            if (DeckManager.Instance != null)
            {
                isHeld = DeckManager.Instance.IsCardHeld(card);
                UpdateHoldIndicator();
            }
        }

        // Check if this card is currently selected
        if (CardSelectionManager.Instance != null && CardSelectionManager.Instance.selectedCard == card)
        {
            SetSelected(true);
        }

        // 🧩 Optional: Set LayoutElement fallback size
        LayoutElement layout = GetComponent<LayoutElement>();
        if (layout != null)
        {
            if (layout.preferredWidth <= 0) layout.preferredWidth = 120;
            if (layout.preferredHeight <= 0) layout.preferredHeight = 200;
        }

        // ✅ Debug info
        RectTransform rt = GetComponent<RectTransform>();
        Debug.Log($"Card setup complete: {card.cardName}, sprite: {(card.artwork != null ? card.artwork.name : "None")}, size: {rt.sizeDelta}, scale: {rt.localScale}");
    }

    private void OnPlayClick()
    {
        Debug.Log($"🎮 Clicked play on card: {card?.cardName}");

        if (player == null || card == null)
        {
            Debug.LogWarning("Cannot select card — Player or Card is null.");
            return;
        }

        if (DeckManager.Instance == null)
        {
            Debug.LogWarning("Cannot select card — DeckManager not found.");
            return;
        }

        if (CardSelectionManager.Instance == null)
        {
            Debug.LogWarning("Cannot select card — CardSelectionManager not found.");
            return;
        }

        // Check if card is still in hand (UI might be outdated)
        if (!DeckManager.Instance.CurrentHand.Contains(card))
        {
            Debug.LogWarning($"Card {card.cardName} is no longer in hand - refreshing UI");
            return;
        }

        // If this card is already selected, don't reselect
        if (CardSelectionManager.Instance.selectedCard == card)
        {
            Debug.Log($"Card {card.cardName} is already selected");
            return;
        }

        // Select this card (this will deselect any previously selected card)
        CardSelectionManager.Instance.SelectCard(card);

        // Show movement preview through PlayerPawn (don't consume card yet)
        player.PreviewCardMoves(card);
    }

    private void OnHoldClick()
    {
        if (card == null || DeckManager.Instance == null)
        {
            Debug.LogWarning("Cannot toggle hold — Card or DeckManager is null.");
            return;
        }

        if (isHeld)
        {
            // Release the hold
            DeckManager.Instance.ReleaseHeldCard(card);
            isHeld = false;
            Debug.Log($"🔓 Released hold on card: {card.cardName}");
        }
        else
        {
            // Hold the card
            DeckManager.Instance.HoldCard(card);
            isHeld = true;
            Debug.Log($"📌 Holding card for next turn: {card.cardName}");
        }

        UpdateHoldIndicator();
    }

    private void UpdateHoldIndicator()
    {
        if (holdIndicator != null)
        {
            holdIndicator.gameObject.SetActive(isHeld);
        }

        if (holdButton != null)
        {
            // Update button text/appearance based on hold state
            TMP_Text buttonText = holdButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = isHeld ? "Release" : "Hold";
            }
        }
    }

    /// <summary>
    /// Update the selected state (for card selection mechanics)
    /// </summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectedIndicator != null)
        {
            selectedIndicator.gameObject.SetActive(selected);
        }

        // Update visual appearance
        if (playButton != null)
        {
            // Change button appearance based on selection
            ColorBlock colors = playButton.colors;
            colors.normalColor = selected ? Color.yellow : Color.white;
            playButton.colors = colors;
        }

        // You could also update the card's appearance here
        // e.g., change border color, scale, etc.
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            // Slightly highlight selected cards
            canvasGroup.alpha = selected ? 1f : 0.8f;
        }
    }

    /// <summary>
    /// Clear selection and any visual highlights
    /// </summary>
    public void CancelSelection()
    {
        SetSelected(false);

        // Clear any movement previews through PlayerPawn
        if (player != null)
        {
            player.ClearMovementPreview();
        }
    }

    /// <summary>
    /// Set card interactability
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (playButton != null)
        {
            playButton.interactable = interactable;
        }

        if (holdButton != null)
        {
            holdButton.interactable = interactable;
        }

        // Visual feedback for non-interactable state
        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = interactable ? (isSelected ? 1f : 0.8f) : 0.4f;
        }
    }

    /// <summary>
    /// Get card cost for UI logic
    /// </summary>
    public int GetFocusCost()
    {
        return card?.focusCost ?? 0;
    }

    /// <summary>
    /// Check if this card can be played (override for custom logic)
    /// </summary>
    public virtual bool CanPlay()
    {
        if (card == null || player == null) return false;

        // Check if card is still in hand
        if (DeckManager.Instance != null && !DeckManager.Instance.CurrentHand.Contains(card))
        {
            return false;
        }

        // Add any custom play conditions here
        // e.g., focus cost checks, turn restrictions, etc.

        return true;
    }

    /// <summary>
    /// Update the card's visual state based on game conditions
    /// </summary>
    public void UpdateCardState()
    {
        bool canPlay = CanPlay();
        SetInteractable(canPlay);

        // Update focus cost color if player doesn't have enough focus
        if (focusText != null && card != null)
        {
            // This would require a focus/resource system
            // For now, just ensure the text is correct
            focusText.text = $"Focus = {card.focusCost}";
        }
    }

    private void OnDestroy()
    {
        // Clean up button listeners to prevent memory leaks
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
        }

        if (holdButton != null)
        {
            holdButton.onClick.RemoveAllListeners();
        }

        // If this card was selected, clear the selection
        if (isSelected && CardSelectionManager.Instance != null && CardSelectionManager.Instance.selectedCard == card)
        {
            CardSelectionManager.Instance.ClearSelection();
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only debug method
    /// </summary>
    [ContextMenu("Debug Card UI State")]
    void DebugCardUIState()
    {
        Debug.Log("=== CARD UI DEBUG ===");
        Debug.Log($"Card: {(card != null ? card.cardName : "NULL")}");
        Debug.Log($"Player: {(player != null ? player.name : "NULL")}");
        Debug.Log($"Is Held: {isHeld}");
        Debug.Log($"Is Selected: {isSelected}");
        Debug.Log($"Can Play: {CanPlay()}");
        Debug.Log($"Focus Cost: {GetFocusCost()}");
        Debug.Log("=====================");
    }
#endif
}