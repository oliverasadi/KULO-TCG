using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager instance;

    private int currentPlayer = 1; // 1 = Player 1, 2 = Player 2
    private bool creaturePlayed = false;
    private bool spellPlayed = false;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void StartTurn()
    {
        Debug.Log($"Player {currentPlayer}'s turn starts.");
        creaturePlayed = false;
        spellPlayed = false;
    }

    public int GetCurrentPlayer() => currentPlayer;

    public bool CanPlayCard(CardSO card)
    {
        if (card.category == CardSO.CardCategory.Creature && creaturePlayed)
        {
            Debug.Log("You have already played a Creature this turn!");
            return false;
        }

        if (card.category == CardSO.CardCategory.Spell && spellPlayed)
        {
            Debug.Log("You have already played a Spell this turn!");
            return false;
        }

        return true;
    }

    public void RegisterCardPlay(CardSO card)
    {
        if (card.category == CardSO.CardCategory.Creature) creaturePlayed = true;
        else if (card.category == CardSO.CardCategory.Spell) spellPlayed = true;
    }

    public void EndTurn()
    {
        currentPlayer = (currentPlayer == 1) ? 2 : 1;
        Debug.Log($"Turn ended. Now it's Player {currentPlayer}'s turn.");
        StartTurn(); // Start the next player's turn immediately
    }

    public void ResetTurn()
    {
        currentPlayer = 1; // Reset to Player 1 for a new game
        Debug.Log("New round starts. Player 1 goes first.");
        StartTurn();
    }
}
