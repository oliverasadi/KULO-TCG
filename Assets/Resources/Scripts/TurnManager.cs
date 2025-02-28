using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    public static TurnManager instance;
    public Button endTurnButton; // Assign in Inspector
    private int currentPlayer = 1; // 1 = Player, 2 = AI
    private bool creaturePlayed = false;
    private bool spellPlayed = false;
    private DeckManager deckManager; // Reference to DeckManager

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
        deckManager = FindObjectOfType<DeckManager>(); // Ensure DeckManager is referenced
        StartTurn();
    }

    public void StartTurn()
    {
        Debug.Log($"🕒 Player {currentPlayer}'s turn starts.");
        creaturePlayed = false;
        spellPlayed = false;

        if (currentPlayer == 1) // Player's turn, draw a card
        {
            if (deckManager != null)
            {
                deckManager.DrawCard();
            }
            else
            {
                Debug.LogError("❌ DeckManager not found! Make sure it's in the scene.");
            }
        }
        else if (currentPlayer == 2) // AI's turn
        {
            AIController.instance.AITakeTurn();
        }
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
