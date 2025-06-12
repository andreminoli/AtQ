using UnityEngine;
using System.Collections.Generic;
using System;

public class DeckManager : MonoBehaviour
{
    [Header("Deck Configuration")]
    [SerializeField] private List<MoveCard> deckPool = new();
    [SerializeField] private int handSize = 4;

    [Header("Runtime State")]
    [SerializeField] private List<MoveCard> currentHand = new();
    [SerializeField] private List<MoveCard> heldCards = new();

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    // Events for hand management
    public static event Action<List<MoveCard>> OnHandUpdated;
    public static event Action OnHandCleared;

    // Singleton pattern for easy access
    public static DeckManager Instance { get; private set; }

    // Public properties
    public List<MoveCard> CurrentHand => new List<MoveCard>(currentHand);
    public List<MoveCard> HeldCards => new List<MoveCard>(heldCards);
    public int HandSize => handSize;
    public bool HasCards => currentHand.Count > 0;

    // Track initialization state
    private bool isInitialized = false;

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Initialize deck if empty (for testing/fallback)
        if (deckPool.Count == 0)
        {
            InitializeSampleDeck();
        }

        isInitialized = true;
        DebugLog($"DeckManager initialized at frame {Time.frameCount}");
    }

    private void OnEnable()
    {
        // Subscribe to player initialization event
        PlayerPawn.OnInitialized += OnPlayerInitialized;
        DebugLog($"DeckManager subscribed to PlayerPawn events at frame {Time.frameCount}");
    }

    private void OnDisable()
    {
        // Unsubscribe from events
        PlayerPawn.OnInitialized -= OnPlayerInitialized;
        DebugLog($"DeckManager unsubscribed from PlayerPawn events at frame {Time.frameCount}");
    }

    private void Start()
    {
        DebugLog($"DeckManager Start() called at frame {Time.frameCount}");

        // Check if player is already initialized (fallback scenario)
        PlayerPawn existingPlayer = FindAnyObjectByType<PlayerPawn>();
        if (existingPlayer != null && existingPlayer.IsInitialized)
        {
            DebugLog($"Found already-initialized PlayerPawn in Start() at frame {Time.frameCount}");
            OnPlayerInitialized(existingPlayer);
        }
    }

    /// <summary>
    /// Called when PlayerPawn is initialized - DO NOT draw initial hand here
    /// Let TurnManager handle all hand drawing to avoid double draws
    /// </summary>
    private void OnPlayerInitialized(PlayerPawn player)
    {
        DebugLog($"Player initialized detected - DeckManager ready at frame {Time.frameCount}");

        // REMOVED: Initial hand drawing - TurnManager will handle this
        // StartCoroutine(DelayedInitialHandDraw()); // <-- This was causing double draw

        // Just log that we're ready to serve cards when requested
        DebugLog($"DeckManager ready to serve cards when TurnManager requests them at frame {Time.frameCount}");
    }

    /// <summary>
    /// Initialize a sample deck for testing purposes
    /// </summary>
    private void InitializeSampleDeck()
    {
        DebugLog($"No deck pool assigned - creating sample deck at frame {Time.frameCount}");

        // This would typically be populated by loading from ScriptableObjects
        // For now, we'll leave it empty and log a warning
        // You should assign your MoveCard ScriptableObjects in the inspector

        Debug.LogWarning("DeckManager: Please assign MoveCard assets to the deckPool in the inspector!");
    }

    /// <summary>
    /// Draw a new hand of cards
    /// </summary>
    public void DrawHand()
    {
        DebugLog($"🎯 Drawing hand of {currentHand.Count} cards at frame {Time.frameCount}...");

        // Clear current hand
        currentHand.Clear();

        // Add any held cards first
        if (heldCards.Count > 0)
        {
            currentHand.AddRange(heldCards);
            DebugLog($"Added {heldCards.Count} held cards to hand at frame {Time.frameCount}");
            heldCards.Clear(); // Clear held cards after adding them
        }

        // Fill remaining slots with random draws from deck pool
        int cardsNeeded = handSize - currentHand.Count;

        if (deckPool.Count == 0)
        {
            Debug.LogError($"Cannot draw cards - deck pool is empty! Frame: {Time.frameCount}");
            return;
        }

        for (int i = 0; i < cardsNeeded; i++)
        {
            MoveCard drawnCard = DrawRandomCard();
            if (drawnCard != null)
            {
                currentHand.Add(drawnCard);
                DebugLog($"Drew card: {drawnCard.cardName} at frame {Time.frameCount}");
            }
        }

        DebugLog($"Hand drawn - {currentHand.Count} cards total at frame {Time.frameCount}");

        // Broadcast hand update
        DebugLog($"Broadcasting hand update at frame {Time.frameCount}");
        OnHandUpdated?.Invoke(CurrentHand);
    }

    /// <summary>
    /// Draw a random card from the deck pool (with replacement)
    /// </summary>
    private MoveCard DrawRandomCard()
    {
        if (deckPool.Count == 0) return null;

        int randomIndex = UnityEngine.Random.Range(0, deckPool.Count);
        return deckPool[randomIndex];
    }

    /// <summary>
    /// Use a card from the current hand
    /// </summary>
    public bool TryUseCard(MoveCard card)
    {
        if (!currentHand.Contains(card))
        {
            DebugLog($"Cannot use card {card.cardName} - not in current hand at frame {Time.frameCount}");
            return false;
        }

        DebugLog($"Using card: {card.cardName} at frame {Time.frameCount}");

        // Mark card as used (if it has usage tracking)
        if (card != null)
        {
            card.hasBeenUsed = true;
        }

        // Remove from current hand
        currentHand.Remove(card);

        // Notify CardSelectionManager that this card was used
        if (CardSelectionManager.Instance != null)
        {
            CardSelectionManager.Instance.OnCardUsed(card);
        }

        // Broadcast hand update
        DebugLog($"Broadcasting hand update after card use at frame {Time.frameCount}");
        OnHandUpdated?.Invoke(CurrentHand);

        return true;
    }

    /// <summary>
    /// Hold a card for the next hand (survives cleanup)
    /// </summary>
    public void HoldCard(MoveCard card)
    {
        if (!currentHand.Contains(card))
        {
            DebugLog($"Cannot hold card {card.cardName} - not in current hand at frame {Time.frameCount}");
            return;
        }

        if (!heldCards.Contains(card))
        {
            heldCards.Add(card);
            DebugLog($"Holding card for next turn: {card.cardName} at frame {Time.frameCount}");
        }
    }

    /// <summary>
    /// Release a held card back to normal discard
    /// </summary>
    public void ReleaseHeldCard(MoveCard card)
    {
        if (heldCards.Remove(card))
        {
            DebugLog($"Released held card: {card.cardName} at frame {Time.frameCount}");
        }
    }

    /// <summary>
    /// Check if a card is currently held
    /// </summary>
    public bool IsCardHeld(MoveCard card)
    {
        return heldCards.Contains(card);
    }

    /// <summary>
    /// Clear the current hand (end of turn cleanup)
    /// </summary>
    public void ClearHand()
    {
        DebugLog($"Clearing current hand at frame {Time.frameCount}...");

        // Don't clear held cards - they survive cleanup
        List<MoveCard> cardsToRemove = new List<MoveCard>();

        foreach (MoveCard card in currentHand)
        {
            if (!heldCards.Contains(card))
            {
                cardsToRemove.Add(card);
            }
        }

        foreach (MoveCard card in cardsToRemove)
        {
            currentHand.Remove(card);
        }

        DebugLog($"Removed {cardsToRemove.Count} cards, {currentHand.Count} cards remain (held) at frame {Time.frameCount}");

        // Broadcast hand cleared
        OnHandCleared?.Invoke();
        OnHandUpdated?.Invoke(CurrentHand);
    }

    /// <summary>
    /// Add a card to the deck pool
    /// </summary>
    public void AddCardToPool(MoveCard card, int quantity = 1)
    {
        for (int i = 0; i < quantity; i++)
        {
            deckPool.Add(card);
        }
        DebugLog($"Added {quantity}x {card.cardName} to deck pool at frame {Time.frameCount}");
    }

    /// <summary>
    /// Remove a card from the deck pool
    /// </summary>
    public void RemoveCardFromPool(MoveCard card, int quantity = 1)
    {
        for (int i = 0; i < quantity && deckPool.Contains(card); i++)
        {
            deckPool.Remove(card);
        }
        DebugLog($"Removed {quantity}x {card.cardName} from deck pool at frame {Time.frameCount}");
    }

    /// <summary>
    /// Get deck composition for debugging
    /// </summary>
    public Dictionary<string, int> GetDeckComposition()
    {
        Dictionary<string, int> composition = new Dictionary<string, int>();

        foreach (MoveCard card in deckPool)
        {
            if (card != null)
            {
                string cardName = card.cardName;
                if (composition.ContainsKey(cardName))
                    composition[cardName]++;
                else
                    composition[cardName] = 1;
            }
        }

        return composition;
    }

    /// <summary>
    /// Debug logging helper
    /// </summary>
    private void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[DeckManager] {message}");
        }
    }

    /// <summary>
    /// Reset the deck manager state
    /// </summary>
    public void ResetDeck()
    {
        currentHand.Clear();
        heldCards.Clear();

        // Reset all cards' used flags
        foreach (MoveCard card in deckPool)
        {
            if (card != null)
            {
                card.hasBeenUsed = false;
            }
        }

        DebugLog($"Deck manager reset at frame {Time.frameCount}");
        OnHandCleared?.Invoke();
    }

    /// <summary>
    /// Force redraw hand (useful for debugging or special cases)
    /// </summary>
    public void ForceRedrawHand()
    {
        DebugLog($"Force redrawing hand at frame {Time.frameCount}");
        DrawHand();
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editor-only method to debug deck state
    /// </summary>
    [ContextMenu("Debug Deck State")]
    private void DebugDeckState()
    {
        Debug.Log($"=== DECK MANAGER DEBUG (Frame {Time.frameCount}) ===");
        Debug.Log($"Is Initialized: {isInitialized}");

        Debug.Log($"Deck Pool ({deckPool.Count} cards):");
        var composition = GetDeckComposition();
        foreach (var kvp in composition)
        {
            Debug.Log($"  {kvp.Value}x {kvp.Key}");
        }

        Debug.Log($"Current Hand ({currentHand.Count} cards):");
        for (int i = 0; i < currentHand.Count; i++)
        {
            Debug.Log($"  [{i}] {currentHand[i]?.cardName ?? "NULL"}");
        }

        Debug.Log($"Held Cards ({heldCards.Count} cards):");
        for (int i = 0; i < heldCards.Count; i++)
        {
            Debug.Log($"  [{i}] {heldCards[i]?.cardName ?? "NULL"}");
        }

        Debug.Log("=========================");
    }
#endif
}