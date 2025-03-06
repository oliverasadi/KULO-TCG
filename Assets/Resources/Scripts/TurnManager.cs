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

    // Expose creaturePlayed via a public property.
    public bool CreaturePlayed
    {
        get { return creaturePlayed; }
    }

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        endTurnButton.onClick.AddListener(PlayerEndTurn); // Link button to EndTurn
        StartTurn(); // Start the first turn (card draw will occur)
    }

    PlayerManager SelectPlayerManager()
    {
        return (currentPlayer == 1) ? playerManager1 : playerManager2;
    }

    public void StartTurn(bool drawCard = true)
    {
        Debug.Log($"🕒 Player {currentPlayer}'s turn starts.");
        creaturePlayed = false;
        spellPlayed = false;

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
                    // Skip drawing a card on turn start due to round reset.
                    skipDrawOnTurnStart = false; // Reset flag for future turns.
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
    }

    public void PlayerEndTurn()
    {
        EndTurn(); // End turn; new turn logic will run.
    }

    public void EndTurn()
    {
        currentPlayer = (currentPlayer == 1) ? 2 : 1;
        Debug.Log($"🔄 Turn ended. Now Player {currentPlayer}'s turn.");
        StartTurn();
    }

    public void ResetTurn()
    {
        currentPlayer = 1; // Reset turn to Player 1 at the start of a new round
        Debug.Log("🔄 Turn Reset: Player 1 starts the new round!");
        skipDrawOnTurnStart = true; // Set flag to skip drawing a card immediately after round reset
        StartTurn();
    }

    public int GetCurrentPlayer()
    {
        return currentPlayer;
    }
}
