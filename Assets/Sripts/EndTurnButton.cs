using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class EndTurnButton : MonoBehaviour
{
    [SerializeField] private Button endTurnBtn;
    [SerializeField] private Text buttonText; // Optional: for showing loading state
    private bool isInitialized = false;

    private void Awake()
    {
        // Ensure button reference is set
        if (endTurnBtn == null)
            endTurnBtn = GetComponent<Button>();

        if (endTurnBtn != null)
        {
            endTurnBtn.onClick.AddListener(OnEndTurnPressed);
        }
        else
        {
            Debug.LogError("EndTurnButton: Button reference is missing!");
        }
    }

    private void OnEnable()
    {
        if (!isInitialized)
        {
            StartCoroutine(WaitForTurnManager());
        }
        else
        {
            // If already initialized, just sync the button state
            SyncButtonState();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromTurnManager();
    }

    private void OnDestroy()
    {
        UnsubscribeFromTurnManager();
    }

    private IEnumerator WaitForTurnManager()
    {
        Debug.Log("🔄 EndTurnButton: Waiting for TurnManager to initialize...");

        // Show loading state
        SetLoadingState(true);

        // Wait indefinitely until TurnManager.Instance is available and enabled
        while (TurnManager.Instance == null || !TurnManager.Instance.enabled)
        {
            yield return null;
        }

        Debug.Log("✅ EndTurnButton: TurnManager found! Initializing button...");

        // Subscribe to TurnManager events
        SubscribeToTurnManager();

        // Set initial button state based on current turn state
        SyncButtonState();

        // Clear loading state
        SetLoadingState(false);

        isInitialized = true;

        Debug.Log("✅ EndTurnButton: Initialization complete.");
    }

    private void SubscribeToTurnManager()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart.AddListener(EnableButton);
            TurnManager.Instance.OnPlayerTurnEnd.AddListener(DisableButton);
            TurnManager.Instance.OnKingTurnStart.AddListener(DisableButton);
            TurnManager.Instance.OnLevelComplete.AddListener(DisableButton);

            Debug.Log("🔗 EndTurnButton: Subscribed to TurnManager events.");
        }
    }

    private void UnsubscribeFromTurnManager()
    {
        if (TurnManager.Instance != null)
        {
            TurnManager.Instance.OnPlayerTurnStart.RemoveListener(EnableButton);
            TurnManager.Instance.OnPlayerTurnEnd.RemoveListener(DisableButton);
            TurnManager.Instance.OnKingTurnStart.RemoveListener(DisableButton);
            TurnManager.Instance.OnLevelComplete.RemoveListener(DisableButton);

            Debug.Log("🔌 EndTurnButton: Unsubscribed from TurnManager events.");
        }
    }

    private void SyncButtonState()
    {
        if (TurnManager.Instance != null)
        {
            // Use the actual method from your TurnManager
            if (TurnManager.Instance.IsPlayerTurn())
            {
                EnableButton();
            }
            else
            {
                DisableButton();
            }
        }
        else
        {
            DisableButton();
        }
    }

    private void OnEndTurnPressed()
    {
        if (TurnManager.Instance != null && endTurnBtn.interactable)
        {
            Debug.Log("🎯 EndTurnButton: Player ended turn.");
            TurnManager.Instance.EndPlayerTurn();
        }
        else
        {
            Debug.LogWarning("❌ EndTurnButton: Cannot end turn - TurnManager unavailable or button not interactable.");
        }
    }

    private void EnableButton()
    {
        if (endTurnBtn != null)
        {
            endTurnBtn.interactable = true;
            Debug.Log("🟢 EndTurnButton: Button enabled (Player Turn).");
        }
    }

    private void DisableButton()
    {
        if (endTurnBtn != null)
        {
            endTurnBtn.interactable = false;
            Debug.Log("🔴 EndTurnButton: Button disabled.");
        }
    }

    private void SetLoadingState(bool isLoading)
    {
        if (endTurnBtn != null)
        {
            endTurnBtn.interactable = !isLoading;
        }

        // Optional: Update button text to show loading state
        if (buttonText != null)
        {
            if (isLoading)
            {
                buttonText.text = "Loading...";
            }
            else
            {
                buttonText.text = "End Turn";
            }
        }

        if (isLoading)
        {
            Debug.Log("⏳ EndTurnButton: Showing loading state...");
        }
    }
}