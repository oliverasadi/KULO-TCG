using System;
using System.Collections;
using System.Collections.Generic;
using Mirror.Examples.CCU;
using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    public static TurnManager instance;
    public static PlayerManager currentPlayerManager;

    public Button endTurnButton; // Assign in Inspector
    public int currentPlayer = 1; // 1 = Player, 2 = AI
    public bool creaturePlayed = false;
    public bool spellPlayed = false;

    public PlayerManager playerManager1; // Reference to PlayerManager for Player 1
    public PlayerManager playerManager2; // Reference to PlayerManager for Player 2

    // Flag to control card draw on turn start.
    private bool skipDrawOnTurnStart = false;

    // Assume the local player is player 1.
    public int localPlayerNumber = 1;

    // Event to be fired at the end of an opponent's turn.
    public event Action OnOpponentTurnEnd;

    // NEW: Event fired when a card is played
    public event Action<CardSO> OnCardPlayed;

    // Additional internal flag to block any more plays for the current turn
    private bool noAdditionalPlays = false;

    // NEW: If we want to block the entire *next* turn, call this
    private bool blockNextTurn = false;

    // Expose creaturePlayed via a public property.
    public bool CreaturePlayed
    {
        get { return creaturePlayed; }
    }

    // ---------------------------
    // NEW: Turn Splash reference
    [Header("Turn Splash")]
    public GameObject turnSplashPrefab;
    // ---------------------------

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        endTurnButton.onClick.AddListener(PlayerEndTurn);
        StartTurn(); // Start the first turn
    }

    PlayerManager SelectPlayerManager()
    {
        return (currentPlayer == 1) ? playerManager1 : playerManager2;
    }

    public void StartTurn(bool drawCard = true)
    {
        Debug.Log($"🕒 Player {currentPlayer}'s turn starts.");

        // Get the current player's manager.
        currentPlayerManager = SelectPlayerManager();
        if (currentPlayerManager != null)
        {
            // Log the current hand count.
            Debug.Log($"[TurnManager] Player {currentPlayer} has {currentPlayerManager.cardHandlers.Count} card(s) in hand.");
        }
        else
        {
            Debug.LogError("❌ PlayerManager not found! Make sure it's in the scene.");
        }

        // Show turn start splash.
        string splashMessage = (currentPlayer == localPlayerNumber) ? "Your Turn Start" : "CPU Turn Start";
        ShowTurnSplash(splashMessage);

        if (blockNextTurn)
        {
            noAdditionalPlays = true;
            creaturePlayed = true;
            spellPlayed = true;
            Debug.Log("Blocking all plays this turn from last turn's effect!");
            blockNextTurn = false;
        }
        else
        {
            noAdditionalPlays = false;
            creaturePlayed = false;
            spellPlayed = false;
        }

        if (currentPlayerManager != null)
        {
            if (drawCard)
            {
                if (!skipDrawOnTurnStart)
                {
                    currentPlayerManager.DrawCard();
                }
                else
                {
                    skipDrawOnTurnStart = false;
                }
            }
        }

        currentPlayerManager.pc.StartTurn();
    }


    public bool CanPlayCard(CardSO card)
    {
        if (noAdditionalPlays)
        {
            Debug.Log("❌ Additional plays are blocked for this turn!");
            return false;
        }

        if (card.category == CardSO.CardCategory.Creature && creaturePlayed)
        {
            Debug.Log("❌ You already played a Creature this turn!");
            return false;
        }

        if (card.category == CardSO.CardCategory.Spell && spellPlayed)
        {
            Debug.Log("❌ You already played a Spell this turn!");
            return false;
        }

        return true;
    }

    public void RegisterCardPlay(CardSO card)
    {
        if (card.category == CardSO.CardCategory.Creature)
            creaturePlayed = true;
        if (card.category == CardSO.CardCategory.Spell)
            spellPlayed = true;

        // Fire the OnCardPlayed event
        OnCardPlayed?.Invoke(card);
    }

    public void PlayerEndTurn()
    {
        EndTurn();
    }

    public void EndTurn()
    {
        // Show turn end splash.
        string splashMessage = (currentPlayer == localPlayerNumber) ? "Your Turn End" : "CPU Turn End";
        ShowTurnSplash(splashMessage);

        int endingPlayer = currentPlayer;
        PlayerManager endingPM = SelectPlayerManager(); // Get the PlayerManager for the ending player.
        if (endingPM != null)
        {
            if (endingPlayer == localPlayerNumber)
            {
                endingPM.EnforceHandLimitWithPrompt();
            }
            else
            {
                endingPM.EnforceHandLimit(); // For AI, discard automatically.
            }
        }

        if (endingPlayer != localPlayerNumber)
        {
            OnOpponentTurnEnd?.Invoke();
        }

        // Wait until discard mode is finished before switching turns.
        StartCoroutine(WaitForDiscardThenEndTurn());
    }


    private IEnumerator WaitForDiscardThenEndTurn()
    {
        // Wait until discard mode is complete.
        yield return new WaitUntil(() => HandDiscardManager.Instance == null || !HandDiscardManager.Instance.isDiscarding);
        // Then continue with the end turn routine.
        StartCoroutine(EndTurnRoutine());
    }



    private IEnumerator EndTurnRoutine()
    {
        // Wait a bit for the end-turn splash to play out.
        yield return new WaitForSeconds(1.5f);

        // Switch players.
        currentPlayer = (currentPlayer == 1) ? 2 : 1;
        Debug.Log($"🔄 Turn ended. Now Player {currentPlayer}'s turn.");

        StartTurn();

        // If the new current player is the local player, check inline replacements.
        if (currentPlayer == localPlayerNumber)
        {
            GridManager.instance.CheckReplacementEffects();
        }
    }


    public void ResetTurn()
    {
        currentPlayer = 1; // Reset to Player 1 at new round start
        Debug.Log("🔄 Turn Reset: Player 1 starts the new round!");
        skipDrawOnTurnStart = true;
        StartTurn();
    }

    public int GetCurrentPlayer()
    {
        return currentPlayer;
    }

    // Blocks additional card plays for the remainder of this turn.
    public void BlockAdditionalCardPlays()
    {
        noAdditionalPlays = true;
        creaturePlayed = true;
        spellPlayed = true;
        Debug.Log("Additional card plays blocked for this turn.");
    }

    // NEW: Block the entire next turn.
    public void BlockPlaysNextTurn()
    {
        Debug.Log("Scheduling a block for next turn!");
        blockNextTurn = true;
    }

    // -------------------------------
    // Helper method to spawn a turn splash.
    public void ShowTurnSplash(string message)
    {
        Debug.Log($"[TurnManager] ShowTurnSplash called with message: '{message}'");

        if (turnSplashPrefab == null)
        {
            Debug.LogWarning("No turnSplashPrefab assigned in TurnManager!");
            return;
        }

        // Find the OverlayCanvas. Ensure your canvas is named exactly "OverlayCanvas".
        GameObject overlayCanvas = GameObject.Find("OverlayCanvas");
        if (overlayCanvas == null)
        {
            Debug.LogWarning("OverlayCanvas not found. Make sure you have one named OverlayCanvas in the scene.");
            return;
        }

        // Instantiate the splash prefab as a child of the overlay canvas.
        GameObject splashObj = Instantiate(turnSplashPrefab, overlayCanvas.transform);
        TurnSplashUI splashUI = splashObj.GetComponent<TurnSplashUI>();
        if (splashUI != null)
        {
            splashUI.Setup(message);
        }
        else
        {
            Debug.LogWarning("TurnSplashPrefab is missing a TurnSplashUI component!");
        }
    }
    // -------------------------------

    // ... rest of your TurnManager code ...
}
