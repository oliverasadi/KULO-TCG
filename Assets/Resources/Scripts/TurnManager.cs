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
    
    public PlayerManager playerManager1; // Reference to PlayerManagerForPLayer1
    public PlayerManager playerManager2; // Reference to PlayerManagerForPLayer1

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
        StartTurn(); //Don't double draw on first turn
    }
    
    PlayerManager SelectPlayerManager()
    {
        if (currentPlayer == 1) return playerManager1;
        else return playerManager2;
    }

    public void StartTurn(bool drawCard = true)
    {
        Debug.Log($"🕒 Player {currentPlayer}'s turn starts.");
        creaturePlayed = false;
        spellPlayed = false;

        currentPlayerManager = SelectPlayerManager();
        
        if (currentPlayerManager != null && drawCard)
        {
            currentPlayerManager.DrawCard();
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
        EndTurn(); // Player will now only draw at the start of their turn
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
        StartTurn();
    }

    public int GetCurrentPlayer()
    {
        return currentPlayer;
    }
}
