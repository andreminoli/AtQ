using UnityEngine;
using System.Collections;
using System.Collections.Generic;

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

    void OnEnable()
    {
        PlayerPawn.OnInitialized += HandlePlayerInitialized;
        DeckManager.OnHandUpdated += HandleHandUpdated;
        DeckManager.OnHandCleared += HandleHandCleared;

        Debug.Log($"🔗 CardManager subscribed to events at frame {Time.frameCount}");
    }

    void OnDisable()
    {
        PlayerPawn.OnInitialized -= HandlePlayerInitialized;
        DeckManager.OnHandUpdated -= HandleHandUpdated;
        DeckManager.OnHandCleared -= HandleHandCleared;

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

        Debug.Log($"🃏 Updating hand display with {newHand.Count} cards at frame {Time.frameCount}");
        StartCoroutine(UpdateHandDisplayRoutine(newHand));
    }

    void HandleHandCleared()
    {
        Debug.Log($"🧹 Hand cleared by DeckManager at frame {Time.frameCount}");
        ClearAllCardUIs();
    }

    IEnumerator UpdateHandDisplayRoutine(List<MoveCard> cards)
    {
        Debug.Log($"🎬 Starting hand display update routine at frame {Time.frameCount}");

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

#if UNITY_EDITOR
    [ContextMenu("Debug Card Manager State")]
    void DebugCardManagerState()
    {
        Debug.Log($"=== CARD MANAGER DEBUG (Frame {Time.frameCount}) ===");
        Debug.Log($"Card Spawn Started: {cardSpawnStarted}");
        Debug.Log($"Player Reference: {(player != null ? player.name : "NULL")}");
        Debug.Log($"Player Initialized: {(player != null ? player.IsInitialized.ToString() : "N/A")}");
        Debug.Log($"Spawned Card Objects: {spawnedCardObjects.Count}");
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
        Debug.Log("===========================");
    }
#endif
}