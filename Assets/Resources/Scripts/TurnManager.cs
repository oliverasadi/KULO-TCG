using System;
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

    // NEW: If we want to block the *next* turn from some effect, we can store it here
    private bool blockNextTurn = false;

    // Expose creaturePlayed via a public property.
    public bool CreaturePlayed
    {
        get { return creaturePlayed; }
    }

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

        if (blockNextTurn)
        {
            noAdditionalPlays = true;
            creaturePlayed = true;
            spellPlayed = true;
            // NEW debug line
            Debug.Log("Blocking all plays this turn from last turn's effect!");
            blockNextTurn = false;
        }
        else
        {
            noAdditionalPlays = false;
            creaturePlayed = false;
            spellPlayed = false;
        }

        currentPlayerManager = SelectPlayerManager();
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
        else
        {
            Debug.LogError("❌ PlayerManager not found! Make sure it's in the scene.");
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
        int endingPlayer = currentPlayer;
        // Switch players
        currentPlayer = (currentPlayer == 1) ? 2 : 1;
        Debug.Log($"🔄 Turn ended. Now Player {currentPlayer}'s turn.");

        // If the turn that ended was NOT the local player's, we can raise OnOpponentTurnEnd
        if (endingPlayer != localPlayerNumber)
        {
            OnOpponentTurnEnd?.Invoke();
        }

        StartTurn();

        // If the new current player *is* the local player, check inline replacements
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

    // Blocks additional card plays for the remainder of *this* turn
    public void BlockAdditionalCardPlays()
    {
        noAdditionalPlays = true;
        creaturePlayed = true;
        spellPlayed = true;
        Debug.Log("Additional card plays blocked for this turn.");
    }

    // NEW: If we want to block the entire *next* turn, call this
    public void BlockPlaysNextTurn()
    {
        Debug.Log("Scheduling a block for next turn!");
        blockNextTurn = true;
    }
}
