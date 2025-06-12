using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("Turn Settings")]
    [SerializeField] private bool autoProcessTurns = false;
    [SerializeField] private float turnDelay = 1f;
    [SerializeField] private bool debugMode = true;

    [Header("Events")]
    public UnityEvent OnPlayerTurnStart;
    public UnityEvent OnPlayerTurnEnd;
    public UnityEvent OnKingTurnStart;
    public UnityEvent OnKingTurnEnd;
    public UnityEvent OnLevelComplete;

    public enum TurnState
    {
        PlayerTurn,
        KingTurn,
        LevelComplete
    }

    [SerializeField] private TurnState currentState = TurnState.PlayerTurn;
    private int turnCount = 0;

    private GridManager gridManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (debugMode)
            Debug.Log("TurnManager: Instance set during Awake.");
    }

    private void Start()
    {
        gridManager = Object.FindAnyObjectByType<GridManager>();
        if (gridManager == null)
        {
            Debug.LogError("TurnManager: No GridManager found!");
        }

        StartPlayerTurn();
    }

    private void Update()
    {
        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame && currentState == TurnState.PlayerTurn)
            {
                EndPlayerTurn();
            }

            if (Keyboard.current.rKey.wasPressedThisFrame)
            {
                ResetLevel();
            }
        }
    }

    public void StartPlayerTurn()
    {
        if (currentState == TurnState.LevelComplete) return;

        currentState = TurnState.PlayerTurn;

        if (debugMode)
        {
            Debug.Log($"=== PLAYER TURN {turnCount + 1} ===");
        }

        OnPlayerTurnStart?.Invoke();

        // Refresh Hand at Start of Player Turn
        if (DeckManager.Instance != null)
        {
            if (debugMode)
            {
                Debug.Log($"Refreshing hand at start of turn {turnCount + 1}");
            }

            // Clear non-held cards, preserving held ones
            DeckManager.Instance.ClearHand();

            // Draw up to full hand size, including held cards
            DeckManager.Instance.DrawHand();
        }
        else if (debugMode)
        {
            Debug.LogWarning("DeckManager not found - cannot refresh hand");
        }
    }

    public void EndPlayerTurn()
    {
        if (currentState != TurnState.PlayerTurn) return;

        if (debugMode) Debug.Log("Player turn ended");

        OnPlayerTurnEnd?.Invoke();
        StartKingTurn();
    }

    private void StartKingTurn()
    {
        currentState = TurnState.KingTurn;

        if (debugMode) Debug.Log("=== KING TURN ===");

        OnKingTurnStart?.Invoke();

        if (autoProcessTurns)
        {
            Invoke(nameof(ProcessKingTurn), turnDelay);
        }
        else
        {
            ProcessKingTurn();
        }
    }

    private void ProcessKingTurn()
    {
        if (gridManager == null)
        {
            Debug.LogError("No GridManager assigned!");
            return;
        }

        bool kingMoved = gridManager.ProcessKingTurn();
        Vector2Int kingPos = gridManager.GetKing().GridPosition;

        if (debugMode)
        {
            Debug.Log(kingMoved
                ? $"King moved to {kingPos}"
                : $"King blocked at {kingPos}");
        }

        if (gridManager.IsLevelComplete())
        {
            CompleteLevel();
            return;
        }

        EndKingTurn();
    }

    private void EndKingTurn()
    {
        if (debugMode) Debug.Log("King turn ended");

        OnKingTurnEnd?.Invoke();
        turnCount++;
        StartPlayerTurn();
    }

    private void CompleteLevel()
    {
        currentState = TurnState.LevelComplete;

        if (debugMode) Debug.Log("LEVEL COMPLETE!");

        OnLevelComplete?.Invoke();
    }

    public void ResetLevel()
    {
        if (debugMode) Debug.Log("Resetting level...");
        turnCount = 0;
        currentState = TurnState.PlayerTurn;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public TurnState GetCurrentState() => currentState;
    public int GetTurnCount() => turnCount;
    public bool IsPlayerTurn() => currentState == TurnState.PlayerTurn;
    public bool IsLevelComplete() => currentState == TurnState.LevelComplete;

    private void OnGUI()
    {
        if (!debugMode) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, 150));
        GUILayout.Label($"Turn: {turnCount + 1}");
        GUILayout.Label($"State: {currentState}");

        if (gridManager != null && gridManager.GetKing() != null)
        {
            Vector2Int pos = gridManager.GetKing().GridPosition;
            GUILayout.Label($"King: {pos}");
        }

        GUILayout.Space(10);
        GUILayout.Label("SPACE - End Player Turn");
        GUILayout.Label("R - Reset Level");
        GUILayout.EndArea();
    }
}