using UnityEngine;
using System;

public class CardSelectionManager : MonoBehaviour
{
    public static CardSelectionManager Instance;

    [Header("Current Selection")]
    public MoveCard selectedCard;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Events for selection changes
    public static event Action<MoveCard> OnCardSelected;
    public static event Action<MoveCard> OnCardDeselected;
    public static event Action OnSelectionCleared;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        DebugLog($"🎯 CardSelectionManager initialized at frame {Time.frameCount}");
    }

    /// <summary>
    /// Select a new card, deselecting any previously selected card
    /// </summary>
    public void SelectCard(MoveCard card)
    {
        if (card == null)
        {
            DebugLog("❌ Attempted to select null card");
            return;
        }

        // If same card is already selected, do nothing
        if (selectedCard == card)
        {
            DebugLog($"⏭️ Card {card.cardName} is already selected");
            return;
        }

        // Deselect current card if any
        if (selectedCard != null)
        {
            DebugLog($"🔄 Deselecting previous card: {selectedCard.cardName}");
            DeselectCurrentCard();
        }

        // Select new card
        selectedCard = card;
        DebugLog($"✅ Card selected: {card.cardName}");

        // Update UI for all card UIs
        UpdateAllCardUISelections();

        // Broadcast selection event
        OnCardSelected?.Invoke(card);
    }

    /// <summary>
    /// Deselect the currently selected card
    /// </summary>
    public void DeselectCurrentCard()
    {
        if (selectedCard == null)
        {
            DebugLog("⚠️ No card to deselect");
            return;
        }

        MoveCard previousCard = selectedCard;
        selectedCard = null;

        DebugLog($"❌ Deselected card: {previousCard.cardName}");

        // Clear any movement previews
        PlayerPawn player = FindAnyObjectByType<PlayerPawn>();
        if (player != null)
        {
            player.ClearMovementPreview();
        }

        // Update UI for all card UIs
        UpdateAllCardUISelections();

        // Broadcast deselection event
        OnCardDeselected?.Invoke(previousCard);
    }

    /// <summary>
    /// Clear selection completely
    /// </summary>
    public void ClearSelection()
    {
        if (selectedCard != null)
        {
            DeselectCurrentCard();
        }

        DebugLog("🧹 Selection cleared");
        OnSelectionCleared?.Invoke();
    }

    /// <summary>
    /// Check if a specific card is currently selected
    /// </summary>
    public bool IsCardSelected(MoveCard card)
    {
        return selectedCard != null && selectedCard == card;
    }

    /// <summary>
    /// Get the currently selected card
    /// </summary>
    public MoveCard GetSelectedCard()
    {
        return selectedCard;
    }

    /// <summary>
    /// Check if any card is currently selected
    /// </summary>
    public bool HasSelection()
    {
        return selectedCard != null;
    }

    /// <summary>
    /// Update all CardUI components to reflect current selection state
    /// </summary>
    private void UpdateAllCardUISelections()
    {
        // Find all CardUI components in the scene
        CardUI[] allCardUIs = FindObjectsByType<CardUI>(FindObjectsSortMode.None);

        foreach (CardUI cardUI in allCardUIs)
        {
            if (cardUI == null || cardUI.GetCard() == null) continue;

            bool shouldBeSelected = (selectedCard != null && cardUI.GetCard() == selectedCard);

            // Update selection state if it differs
            if (cardUI.IsSelected != shouldBeSelected)
            {
                if (shouldBeSelected)
                {
                    cardUI.SetSelected(true);
                    DebugLog($"🎯 Updated CardUI for {cardUI.GetCard().cardName} - SELECTED");
                }
                else
                {
                    cardUI.SetSelected(false);
                    DebugLog($"⚪ Updated CardUI for {cardUI.GetCard().cardName} - DESELECTED");
                }
            }
        }
    }

    /// <summary>
    /// Handle when a card is used/consumed from the hand
    /// </summary>
    public void OnCardUsed(MoveCard usedCard)
    {
        if (selectedCard == usedCard)
        {
            DebugLog($"🎮 Selected card {usedCard.cardName} was used - clearing selection");
            selectedCard = null; // Don't trigger deselection events since card is gone
            OnSelectionCleared?.Invoke();
        }
    }

    /// <summary>
    /// Handle when the hand is updated (cards added/removed)
    /// </summary>
    public void OnHandUpdated()
    {
        // Check if selected card is still in hand
        if (selectedCard != null && DeckManager.Instance != null)
        {
            if (!DeckManager.Instance.CurrentHand.Contains(selectedCard))
            {
                DebugLog($"🗑️ Selected card {selectedCard.cardName} no longer in hand - clearing selection");
                selectedCard = null;
                OnSelectionCleared?.Invoke();
            }
        }

        // Update all card UI selections
        UpdateAllCardUISelections();
    }

    /// <summary>
    /// Debug logging helper
    /// </summary>
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[CardSelectionManager] {message}");
        }
    }

    /// <summary>
    /// Subscribe to relevant events
    /// </summary>
    private void OnEnable()
    {
        // Subscribe to DeckManager events to handle card removal
        DeckManager.OnHandUpdated += OnHandUpdatedEvent;
        DeckManager.OnHandCleared += OnHandClearedEvent;

        DebugLog($"🔗 Subscribed to DeckManager events at frame {Time.frameCount}");
    }

    /// <summary>
    /// Unsubscribe from events
    /// </summary>
    private void OnDisable()
    {
        DeckManager.OnHandUpdated -= OnHandUpdatedEvent;
        DeckManager.OnHandCleared -= OnHandClearedEvent;

        DebugLog($"🔌 Unsubscribed from DeckManager events at frame {Time.frameCount}");
    }

    /// <summary>
    /// Handle DeckManager hand updated event
    /// </summary>
    private void OnHandUpdatedEvent(System.Collections.Generic.List<MoveCard> newHand)
    {
        OnHandUpdated();
    }

    /// <summary>
    /// Handle DeckManager hand cleared event
    /// </summary>
    private void OnHandClearedEvent()
    {
        DebugLog("🧹 Hand cleared by DeckManager - clearing selection");
        ClearSelection();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only debug method
    /// </summary>
    [ContextMenu("Debug Selection State")]
    private void DebugSelectionState()
    {
        Debug.Log($"=== CARD SELECTION MANAGER DEBUG (Frame {Time.frameCount}) ===");
        Debug.Log($"Selected Card: {(selectedCard != null ? selectedCard.cardName : "NULL")}");
        Debug.Log($"Has Selection: {HasSelection()}");

        // Count CardUI components and their selection states
        CardUI[] allCardUIs = FindObjectsByType<CardUI>(FindObjectsSortMode.None);
        Debug.Log($"Total CardUI Components: {allCardUIs.Length}");

        int selectedCount = 0;
        foreach (CardUI cardUI in allCardUIs)
        {
            if (cardUI != null && cardUI.IsSelected)
            {
                selectedCount++;
                Debug.Log($"  SELECTED: {cardUI.GetCard()?.cardName ?? "NULL CARD"}");
            }
        }

        Debug.Log($"CardUIs showing as selected: {selectedCount}");
        Debug.Log("====================================");
    }

    /// <summary>
    /// Force refresh all card UI selections (for debugging)
    /// </summary>
    [ContextMenu("Force Refresh Card UI Selections")]
    private void ForceRefreshSelections()
    {
        DebugLog("🔄 Force refreshing all card UI selections");
        UpdateAllCardUISelections();
    }
#endif
}