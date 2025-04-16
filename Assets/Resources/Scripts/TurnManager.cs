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

    // The synergy-subscription event fired when a card is played or removed
    private event Action<CardSO> _onCardPlayed; // keep private to avoid direct invocation

    // Provide a public accessor for subscription
    public event Action<CardSO> OnCardPlayed
    {
        add { _onCardPlayed += value; }
        remove { _onCardPlayed -= value; }
    }

    // Additional internal flag to block any more plays for the current turn
    private bool noAdditionalPlays = false;

    // If we want to block the entire *next* turn, call this
    private bool blockNextTurn = false;

    // Expose creaturePlayed via a public property.
    public bool CreaturePlayed
    {
        get { return creaturePlayed; }
    }

    // Turn Splash reference
    [Header("Turn Splash")]
    public GameObject turnSplashPrefab;

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
        GridManager.instance.PrintGridState(); // debug

        currentPlayerManager = SelectPlayerManager();
        if (currentPlayerManager != null)
        {
            Debug.Log($"[TurnManager] Player {currentPlayer} has {currentPlayerManager.cardHandlers.Count} card(s) in hand.");
        }
        else
        {
            Debug.LogError("❌ PlayerManager not found!");
        }

        // Show splash
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

        // ✅ ✅ Apply power loss (e.g., Mango Lango) NOW
        GridManager.instance.CheckReplacementEffects();

        if (currentPlayerManager != null && drawCard)
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

    // Called whenever a card is successfully placed / replaced
    public void RegisterCardPlay(CardSO card)
    {
        if (card.category == CardSO.CardCategory.Creature)
            creaturePlayed = true;
        if (card.category == CardSO.CardCategory.Spell)
            spellPlayed = true;

        // Fire event
        FireOnCardPlayed(card);
    }

    // The new public method to safely invoke the event
    public void FireOnCardPlayed(CardSO occupant)
    {
        // This is where the actual invocation happens
        _onCardPlayed?.Invoke(occupant);
    }

    public void PlayerEndTurn()
    {
        EndTurn();
    }

    public void EndTurn()
    {
        // Show turn end splash
        string splashMessage = (currentPlayer == localPlayerNumber) ? "Your Turn End" : "CPU Turn End";
        ShowTurnSplash(splashMessage);

        int endingPlayer = currentPlayer;
        PlayerManager endingPM = SelectPlayerManager();
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

        GridManager.instance.PrintGridState(); // debug

        StartCoroutine(WaitForDiscardThenEndTurn());
    }

    private IEnumerator WaitForDiscardThenEndTurn()
    {
        yield return new WaitUntil(() => HandDiscardManager.Instance == null || !HandDiscardManager.Instance.isDiscarding);
        StartCoroutine(EndTurnRoutine());
    }

    private IEnumerator EndTurnRoutine()
    {
        yield return new WaitForSeconds(1.5f);
        currentPlayer = (currentPlayer == 1) ? 2 : 1;
        Debug.Log($"🔄 Turn ended. Now Player {currentPlayer}'s turn.");

        StartTurn();

    }

    public void ResetTurn()
    {
        currentPlayer = 1;
        Debug.Log("🔄 Turn Reset: Player 1 starts the new round!");
        skipDrawOnTurnStart = true;
        StartTurn();
    }

    public int GetCurrentPlayer() => currentPlayer;

    public void BlockAdditionalCardPlays()
    {
        noAdditionalPlays = true;
        creaturePlayed = true;
        spellPlayed = true;
        Debug.Log("Additional card plays blocked for this turn.");
    }

    public void BlockPlaysNextTurn()
    {
        Debug.Log("Scheduling a block for next turn!");
        blockNextTurn = true;
    }

    public void ShowTurnSplash(string message)
    {
        Debug.Log($"[TurnManager] ShowTurnSplash with message: '{message}'");

        if (turnSplashPrefab == null)
        {
            Debug.LogWarning("No turnSplashPrefab assigned in TurnManager!");
            return;
        }

        GameObject overlayCanvas = GameObject.Find("OverlayCanvas");
        if (overlayCanvas == null)
        {
            Debug.LogWarning("OverlayCanvas not found!");
            return;
        }

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
}
