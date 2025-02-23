using UnityEngine;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    public static TurnManager instance;
    public Button endTurnButton; // Assign in Inspector
    private int currentPlayer = 1; // 1 = Player, 2 = AI
    private bool creaturePlayed = false;
    private bool spellPlayed = false;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        endTurnButton.onClick.AddListener(EndTurn); // Link the button
    }

    public void StartTurn()
    {
        Debug.Log($"🕒 Player {currentPlayer}'s turn starts.");
        creaturePlayed = false;
        spellPlayed = false;

        if (currentPlayer == 2) // ✅ If it's the AI's turn, let AI play
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

    public void EndTurn()
    {
        currentPlayer = (currentPlayer == 1) ? 2 : 1;
        Debug.Log($"🔄 Turn ended. Now Player {currentPlayer}'s turn.");
        StartTurn();
    }

    public void ResetTurn()
    {
        currentPlayer = 1; // ✅ Reset turn to Player 1 at the start of a new round
        Debug.Log("🔄 Turn Reset: Player 1 starts the new round!");
        StartTurn();
    }

    public int GetCurrentPlayer()
    {
        return currentPlayer;
    }
}