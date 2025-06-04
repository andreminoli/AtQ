using UnityEngine;
using System.Collections;

/// <summary>
/// CardManager is responsible for spawning and initializing a single card UI element in the scene.
/// It waits for PlayerPawn to be present before proceeding, since the card needs a valid player reference.
/// </summary>
public class CardManager : MonoBehaviour
{
    // The parent RectTransform where card UI elements will be instantiated (e.g., a panel inside the Canvas)
    public RectTransform cardParent;

    // The card UI prefab to instantiate (should include the CardUI component and proper layout setup)
    public GameObject cardUIPrefab;

    // A reference to the card data (ScriptableObject) that defines this card's name, sprite, cost, etc.
    public MoveCard cardToSpawn;

    /// <summary>
    /// Unity's Start method, implemented as a coroutine to wait for dependencies (PlayerPawn).
    /// </summary>
    private IEnumerator Start()
    {
        Debug.Log("CardManager: Start");

        // 🔄 DEFERRED EXECUTION:
        // GridManager spawns PlayerPawn at runtime, but Unity calls Start() in hierarchy order.
        // To ensure the player is available, we wait here until it exists.
        yield return new WaitUntil(() => FindAnyObjectByType<PlayerPawn>() != null);

        // 🔍 SAFETY CHECK:
        // We check again in case the player was unexpectedly destroyed or never spawned.
        PlayerPawn player = FindAnyObjectByType<PlayerPawn>();
        if (player == null)
        {
            Debug.LogError("No PlayerPawn found after waiting.");
            yield break; // Abort early — cannot continue without player reference.
        }

        // 🔐 VALIDATION:
        // Ensure that all necessary references are set before continuing.
        if (!cardUIPrefab || !cardParent || !cardToSpawn)
        {
            Debug.LogError("Missing reference(s) on CardManager.");
            yield break; // Prevent null reference exceptions during instantiation/setup.
        }

        // 🛠️ INSTANTIATION:
        // Create the UI card as a child of the specified panel (cardParent) so it's rendered in the canvas hierarchy.
        GameObject cardGO = Instantiate(cardUIPrefab, cardParent);

        // 🎯 LAYOUT SETUP:
        // Manually configure RectTransform to center the card on the panel.
        // These values override any prefab anchor defaults and ensure a consistent position.
        RectTransform rt = cardGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f); // Anchor to middle of parent
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);     // Pivot from center
        rt.anchoredPosition = Vector2.zero;     // Position at center
        rt.localScale = Vector3.one;            // Ensure uniform scale
        rt.sizeDelta = new Vector2(300, 550);   // Optional: force a standard card size (can be parameterized later)

        Debug.Log("Spawned card at " + rt.anchoredPosition);

        // ⚙️ DATA BINDING:
        // Feed the card's data and player reference into the CardUI so it can display and react accordingly.
        CardUI cardUI = cardGO.GetComponent<CardUI>();
        if (cardUI != null)
        {
            cardUI.Setup(cardToSpawn, player);
        }
        else
        {
            // 🔴 CRITICAL ERROR:
            // If this happens, the prefab is not set up correctly (missing CardUI component).
            Debug.LogError("CardUI component not found on card prefab.");
        }
    }
}
