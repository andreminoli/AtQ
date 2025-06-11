using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CardManager : MonoBehaviour
{
    [Header("Card Setup")]
    public Transform cardParent;
    public GameObject cardUIPrefab;

    [Header("Legacy - Will be removed")]
    [SerializeField] private List<MoveCard> cardsToSpawn = new();

    private PlayerPawn player;
    private bool cardSpawnStarted = false;
    private List<GameObject> spawnedCardObjects = new();
    private List<MoveCard> currentDisplayedCards = new(); // Track what's currently displayed

    void OnEnable()
    {
        PlayerPawn.OnInitialized += HandlePlayerInitialized;
        DeckManager.OnHandUpdated += HandleHandUpdated;
        DeckManager.OnHandCleared += HandleHandCleared;

        // Subscribe to selection events to avoid unnecessary redraws
        CardSelectionManager.OnCardSelected += HandleCardSelected;
        CardSelectionManager.OnCardDeselected += HandleCardDeselected;

        Debug.Log($"🔗 CardManager subscribed to events at frame {Time.frameCount}");
    }

    void OnDisable()
    {
        PlayerPawn.OnInitialized -= HandlePlayerInitialized;
        DeckManager.OnHandUpdated -= HandleHandUpdated;
        DeckManager.OnHandCleared -= HandleHandCleared;
        CardSelectionManager.OnCardSelected -= HandleCardSelected;
        CardSelectionManager.OnCardDeselected -= HandleCardDeselected;

        Debug.Log($"🔌 CardManager unsubscribed from events at frame {Time.frameCount}");
    }

    void Start()
    {
        Debug.Log($"📦 CardManager Start() called at frame {Time.frameCount}");
        StartCoroutine(DelayedStartInitialization());
    }

    IEnumerator DelayedStartInitialization()
    {
        Debug.Log($"⏳ Starting delayed initialization at frame {Time.frameCount}");

        // Wait for systems like DeckManager to fully initialize
        yield return null;
        yield return null;
        yield return new WaitForEndOfFrame();

        if (cardParent == null || cardUIPrefab == null)
        {
            Debug.LogError("❌ CardManager is missing cardParent or cardUIPrefab references.");
            yield break;
        }

        if (cardsToSpawn.Count > 0 && DeckManager.Instance != null)
        {
            Debug.Log($"🔄 Found {cardsToSpawn.Count} legacy cards - migrating to DeckManager at frame {Time.frameCount}");
            MigrateLegacyCards();
        }

        player = FindAnyObjectByType<PlayerPawn>();
        if (player != null && player.IsInitialized && !cardSpawnStarted)
        {
            Debug.Log($"📦 Late PlayerPawn found — triggering HandlePlayerInitialized at frame {Time.frameCount}");
            HandlePlayerInitialized(player);
        }
    }

    void HandlePlayerInitialized(PlayerPawn playerRef)
    {
        if (cardSpawnStarted)
        {
            Debug.Log($"⏭️ Card spawn already started, skipping HandlePlayerInitialized at frame {Time.frameCount}");
            return;
        }

        if (DeckManager.Instance == null)
        {
            Debug.LogWarning($"🚫 DeckManager is null during HandlePlayerInitialized — skipping.");
            return;
        }

        Debug.Log($"🕐 CardManager initialized with PlayerPawn at frame {Time.frameCount}");
        player = playerRef;
        cardSpawnStarted = true;

        if (DeckManager.Instance.HasCards)
        {
            Debug.Log($"📋 DeckManager already has {DeckManager.Instance.CurrentHand.Count} cards — displaying them at frame {Time.frameCount}");
            HandleHandUpdated(DeckManager.Instance.CurrentHand);
        }
        else
        {
            Debug.Log($"📭 DeckManager has no cards yet, waiting for hand update at frame {Time.frameCount}");
        }
    }

    void MigrateLegacyCards()
    {
        foreach (MoveCard card in cardsToSpawn)
        {
            if (card != null)
            {
                DeckManager.Instance.AddCardToPool(card);
            }
        }

        cardsToSpawn.Clear();
        Debug.Log($"✅ Legacy card migration complete at frame {Time.frameCount}");
    }

    void HandleHandUpdated(List<MoveCard> newHand)
    {
        if (!cardSpawnStarted || player == null)
        {
            Debug.LogWarning($"⚠️ CardManager not ready to display hand — waiting for player initialization at frame {Time.frameCount}");
            return;
        }

        // Check if the hand actually changed to avoid unnecessary UI rebuilds
        if (AreHandsEqual(currentDisplayedCards, newHand))
        {
            Debug.Log($"⚪ Hand content unchanged ({newHand.Count} cards) - skipping UI rebuild at frame {Time.frameCount}");
            return;
        }

        Debug.Log($"🃏 Hand content changed - updating display with {newHand.Count} cards at frame {Time.frameCount}");
        StartCoroutine(UpdateHandDisplayRoutine(newHand));
    }

    void HandleHandCleared()
    {
        Debug.Log($"🧹 Hand cleared by DeckManager at frame {Time.frameCount}");
        ClearAllCardUIs();
        currentDisplayedCards.Clear();
    }

    void HandleCardSelected(MoveCard selectedCard)
    {
        Debug.Log($"🎯 Card selected: {selectedCard?.cardName} - no UI rebuild needed at frame {Time.frameCount}");
        // No need to rebuild UI - CardSelectionManager handles the visual updates
    }

    void HandleCardDeselected(MoveCard deselectedCard)
    {
        Debug.Log($"⚪ Card deselected: {deselectedCard?.cardName} - no UI rebuild needed at frame {Time.frameCount}");
        // No need to rebuild UI - CardSelectionManager handles the visual updates
    }

    /// <summary>
    /// Check if two hand lists contain the same cards in the same order
    /// </summary>
    bool AreHandsEqual(List<MoveCard> hand1, List<MoveCard> hand2)
    {
        if (hand1.Count != hand2.Count) return false;

        for (int i = 0; i < hand1.Count; i++)
        {
            if (hand1[i] != hand2[i]) return false;
        }

        return true;
    }

    IEnumerator UpdateHandDisplayRoutine(List<MoveCard> cards)
    {
        Debug.Log($"🎬 Starting hand display update routine at frame {Time.frameCount}");

        // Store the new hand configuration
        currentDisplayedCards = new List<MoveCard>(cards);

        ClearAllCardUIs();

        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        Debug.Log($"🎭 Beginning card UI spawning at frame {Time.frameCount}");

        int successfulSpawns = 0;
        foreach (MoveCard card in cards)
        {
            if (card == null)
            {
                Debug.LogWarning($"⚠️ Null card in hand — skipping at frame {Time.frameCount}");
                continue;
            }

            GameObject cardGO = Instantiate(cardUIPrefab, cardParent);
            CardUI cardUI = cardGO.GetComponent<CardUI>();

            if (cardUI != null)
            {
                cardUI.Setup(card, player);
                spawnedCardObjects.Add(cardGO);
                successfulSpawns++;

                Debug.Log($"✅ Card '{card.cardName}' spawned successfully at frame {Time.frameCount}");
                Debug.Log($"↳ Parent: {cardParent.name} | Pos: {cardGO.transform.localPosition} | Scale: {cardGO.transform.localScale}");
            }
            else
            {
                Debug.LogError($"❌ Spawned card prefab missing CardUI component at frame {Time.frameCount}");
                Destroy(cardGO);
            }

            yield return null;
        }

        Debug.Log($"🎴 Hand display updated — {successfulSpawns}/{cards.Count} card UIs spawned at frame {Time.frameCount}");

        // Notify CardSelectionManager to update selections after UI rebuild
        if (CardSelectionManager.Instance != null)
        {
            CardSelectionManager.Instance.OnHandUpdated();
        }
    }

    void ClearAllCardUIs()
    {
        int clearedCount = 0;
        foreach (GameObject cardObj in spawnedCardObjects)
        {
            if (cardObj != null)
            {
                Destroy(cardObj);
                clearedCount++;
            }
        }

        spawnedCardObjects.Clear();
        Debug.Log($"🗑️ Cleared {clearedCount} card UI objects at frame {Time.frameCount}");
    }

    public void RefreshHandDisplay()
    {
        Debug.Log($"🔄 Force refreshing hand display at frame {Time.frameCount}");

        if (DeckManager.Instance != null)
        {
            // Force a rebuild by clearing current displayed cards
            currentDisplayedCards.Clear();
            HandleHandUpdated(DeckManager.Instance.CurrentHand);
        }
        else
        {
            Debug.LogWarning("❌ Cannot refresh hand display — DeckManager not found.");
        }
    }

    public int GetDisplayedCardCount() => spawnedCardObjects.Count;

    public bool IsCardDisplayed(MoveCard card)
    {
        foreach (GameObject cardObj in spawnedCardObjects)
        {
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            if (cardUI != null && cardUI.GetCard() == card)
                return true;
        }
        return false;
    }

    public void ForceHandUpdate()
    {
        if (cardSpawnStarted && player != null && DeckManager.Instance != null && DeckManager.Instance.HasCards)
        {
            Debug.Log($"🔧 Force triggering hand update at frame {Time.frameCount}");
            currentDisplayedCards.Clear(); // Force rebuild
            HandleHandUpdated(DeckManager.Instance.CurrentHand);
        }
        else
        {
            Debug.LogWarning($"⚠️ Cannot force hand update — systems not ready at frame {Time.frameCount}");
            Debug.LogWarning($"    Card Spawn Started: {cardSpawnStarted}");
            Debug.LogWarning($"    Player: {(player != null ? player.name : "NULL")}");
            Debug.LogWarning($"    DeckManager: {(DeckManager.Instance != null ? "Found" : "NULL")}");
            Debug.LogWarning($"    Has Cards: {(DeckManager.Instance?.HasCards ?? false)}");
        }
    }

    /// <summary>
    /// Get all currently spawned CardUI components
    /// </summary>
    public List<CardUI> GetAllCardUIs()
    {
        List<CardUI> cardUIs = new List<CardUI>();
        foreach (GameObject cardObj in spawnedCardObjects)
        {
            if (cardObj != null)
            {
                CardUI cardUI = cardObj.GetComponent<CardUI>();
                if (cardUI != null)
                {
                    cardUIs.Add(cardUI);
                }
            }
        }
        return cardUIs;
    }

    /// <summary>
    /// Update the interactability of all cards based on game state
    /// </summary>
    public void UpdateAllCardsInteractability()
    {
        foreach (CardUI cardUI in GetAllCardUIs())
        {
            if (cardUI != null)
            {
                cardUI.UpdateCardState();
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Debug Card Manager State")]
    void DebugCardManagerState()
    {
        Debug.Log($"=== CARD MANAGER DEBUG (Frame {Time.frameCount}) ===");
        Debug.Log($"Card Spawn Started: {cardSpawnStarted}");
        Debug.Log($"Player Reference: {(player != null ? player.name : "NULL")}");
        Debug.Log($"Player Initialized: {(player != null ? player.IsInitialized.ToString() : "N/A")}");
        Debug.Log($"Spawned Card Objects: {spawnedCardObjects.Count}");
        Debug.Log($"Currently Displayed Cards: {currentDisplayedCards.Count}");
        Debug.Log($"Legacy Cards to Spawn: {cardsToSpawn.Count}");

        if (DeckManager.Instance != null)
        {
            Debug.Log($"DeckManager Hand Size: {DeckManager.Instance.CurrentHand.Count}");
            Debug.Log($"DeckManager Has Cards: {DeckManager.Instance.HasCards}");
        }
        else
        {
            Debug.Log("DeckManager: NOT FOUND");
        }

        Debug.Log($"Card Parent: {(cardParent != null ? cardParent.name : "NULL")}");
        Debug.Log($"Card UI Prefab: {(cardUIPrefab != null ? cardUIPrefab.name : "NULL")}");

        // Display current hand vs displayed cards
        Debug.Log("=== HAND COMPARISON ===");
        if (DeckManager.Instance != null)
        {
            var currentHand = DeckManager.Instance.CurrentHand;
            Debug.Log($"DeckManager Hand: [{string.Join(", ", currentHand.Where(c => c != null).Select(c => c.cardName))}]");
        }
        Debug.Log($"Displayed Cards: [{string.Join(", ", currentDisplayedCards.Where(c => c != null).Select(c => c.cardName))}]");

        Debug.Log("===========================");
    }

    [ContextMenu("Force Rebuild UI")]
    void ForceRebuildUI()
    {
        Debug.Log("🔧 Force rebuilding card UI from editor");
        RefreshHandDisplay();
    }
#endif
}