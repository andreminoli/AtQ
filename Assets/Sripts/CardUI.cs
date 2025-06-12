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

        if (player == null)
        {
            Debug.LogError($"[CardUI] Setup failed: PlayerPawn is null for card {card.cardName}");
            return;
        }

        if (!player.IsInitialized)
        {
            Debug.LogWarning($"[CardUI] PlayerPawn is not properly initialized for card {card.cardName}");
        }

        if (player.GridManager == null)
        {
            Debug.LogWarning($"[CardUI] PlayerPawn lacks GridManager for card {card.cardName}");
        }

        if (cardNameText == null || descriptionText == null || focusText == null || playButton == null)
        {
            Debug.LogError("[CardUI] One or more critical UI references are missing!");
            Debug.Log($"cardNameText: {(cardNameText != null ? cardNameText.name : "NULL")}");
            Debug.Log($"descriptionText: {(descriptionText != null ? descriptionText.name : "NULL")}");
            Debug.Log($"focusText: {(focusText != null ? focusText.name : "NULL")}");
            Debug.Log($"playButton: {(playButton != null ? playButton.name : "NULL")}");
            return;
        }

        cardNameText.text = card.cardName;
        descriptionText.text = card.description;
        focusText.text = $"Focus = {card.focusCost}";

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

        playButton.interactable = false;
        playButton.onClick.RemoveAllListeners();
        playButton.onClick.AddListener(OnPlayClick);
        playButton.interactable = true;

        if (holdButton != null)
        {
            holdButton.onClick.RemoveAllListeners();
            holdButton.onClick.AddListener(OnHoldClick);

            if (DeckManager.Instance != null)
            {
                isHeld = DeckManager.Instance.IsCardHeld(card);
                UpdateHoldIndicator();
            }
        }

        if (CardSelectionManager.Instance != null && CardSelectionManager.Instance.selectedCard == card)
        {
            SetSelected(true);
        }

        LayoutElement layout = GetComponent<LayoutElement>();
        if (layout != null)
        {
            if (layout.preferredWidth <= 0) layout.preferredWidth = 120;
            if (layout.preferredHeight <= 0) layout.preferredHeight = 200;
        }

        RectTransform rt = GetComponent<RectTransform>();
        Debug.Log($"Card setup complete: {card.cardName}, sprite: {(card.artwork != null ? card.artwork.name : "None")}, size: {rt.sizeDelta}, scale: {rt.localScale}");
    }

    private void OnPlayClick()
    {
        Debug.Log($"Clicked play on card: {card?.cardName}");

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

        if (!DeckManager.Instance.CurrentHand.Contains(card))
        {
            Debug.LogWarning($"Card {card.cardName} is no longer in hand - refreshing UI");
            return;
        }

        if (CardSelectionManager.Instance.selectedCard == card)
        {
            Debug.Log($"Card {card.cardName} is already selected");
            return;
        }

        CardSelectionManager.Instance.SelectCard(card);

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
            DeckManager.Instance.ReleaseHeldCard(card);
            isHeld = false;
            Debug.Log($"Released hold on card: {card.cardName}");
        }
        else
        {
            DeckManager.Instance.HoldCard(card);
            isHeld = true;
            Debug.Log($"Holding card for next turn: {card.cardName}");
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
            TMP_Text buttonText = holdButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = isHeld ? "Release" : "Hold";
            }
        }
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;

        if (selectedIndicator != null)
        {
            selectedIndicator.gameObject.SetActive(selected);
        }

        if (playButton != null)
        {
            ColorBlock colors = playButton.colors;
            colors.normalColor = selected ? Color.yellow : Color.white;
            playButton.colors = colors;
        }

        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = selected ? 1f : 0.8f;
        }
    }

    public void CancelSelection()
    {
        SetSelected(false);

        if (player != null)
        {
            player.ClearMovementPreview();
        }
    }

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

        CanvasGroup canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = interactable ? (isSelected ? 1f : 0.8f) : 0.4f;
        }
    }

    public int GetFocusCost()
    {
        return card?.focusCost ?? 0;
    }

    public virtual bool CanPlay()
    {
        if (card == null || player == null) return false;

        if (DeckManager.Instance != null && !DeckManager.Instance.CurrentHand.Contains(card))
        {
            return false;
        }

        return true;
    }

    public void UpdateCardState()
    {
        bool canPlay = CanPlay();
        SetInteractable(canPlay);

        if (focusText != null && card != null)
        {
            focusText.text = $"Focus = {card.focusCost}";
        }
    }

    private void OnDestroy()
    {
        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
        }

        if (holdButton != null)
        {
            holdButton.onClick.RemoveAllListeners();
        }

        if (isSelected && CardSelectionManager.Instance != null && CardSelectionManager.Instance.selectedCard == card)
        {
            CardSelectionManager.Instance.ClearSelection();
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Debug Card UI State")]
    void DebugCardUIState()
    {
        Debug.Log("=== CARD UI DEBUG ===");
        Debug.Log($"Card: {(card != null ? card.cardName : "NULL")}");
        Debug.Log($"Player: {(player != null ? player.name : "NULL")}");
        Debug.Log($"Player Initialized: {(player != null ? player.IsInitialized.ToString() : "N/A")}");
        Debug.Log($"Player Grid Position: {(player != null ? player.GridPosition.ToString() : "N/A")}");
        Debug.Log($"Player World Position: {(player != null ? player.transform.position.ToString() : "N/A")}");
        Debug.Log($"Player GridManager: {(player?.GridManager != null ? player.GridManager.name : "NULL")}");
        Debug.Log($"Is Held: {isHeld}");
        Debug.Log($"Is Selected: {isSelected}");
        Debug.Log($"Can Play: {CanPlay()}");
        Debug.Log($"Focus Cost: {GetFocusCost()}");
        Debug.Log("=====================");
    }
#endif
}