using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Use this if you're using TextMeshProUGUI

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    private bool gameActive = true; // Track if the game is ongoing
    private int roundsWonP1 = 0;
    private int roundsWonP2 = 0;
    public int totalRoundsToWin = 3; // First to 3 rounds wins the game

    [Header("UI Elements")]
    public TextMeshProUGUI roundsWonTextP1;   // Assign in Inspector
    public TextMeshProUGUI roundsWonTextP2;   // Assign in Inspector
    public TextMeshProUGUI gameStatusText;    // To display messages like "Player 1 wins the round!"

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        UpdateRoundsUI();
        StartGame();
    }

    public void StartGame()
    {
        Debug.Log("Game Started!");
        TurnManager.instance.StartTurn(); // Begin first turn
    }

    public void EndTurn()
    {
        if (!gameActive) return;
        TurnManager.instance.EndTurn();
    }

    // Call this method after each move to check if the current grid meets the win condition.
    public void CheckForWin()
    {
        // Log the grid state for debugging (optional)
        Debug.Log("[GameManager] Checking win condition...");
        if (WinChecker.instance.CheckWinCondition(GridManager.instance.GetGrid()))
        {
            int winningPlayer = TurnManager.instance.GetCurrentPlayer();
            Debug.Log("[GameManager] Win condition met! Player " + winningPlayer + " wins the round.");

            if (winningPlayer == 1)
                roundsWonP1++;
            else
                roundsWonP2++;

            UpdateRoundsUI();

            // If a player has won enough rounds, the game is over.
            if (roundsWonP1 >= totalRoundsToWin || roundsWonP2 >= totalRoundsToWin)
            {
                Debug.Log("[GameManager] Player " + winningPlayer + " wins the game!");
                if (gameStatusText != null)
                    gameStatusText.text = "Player " + winningPlayer + " wins the game!";
                // Restart game after a delay
                Invoke("RestartGame", 3f);
            }
            else
            {
                if (gameStatusText != null)
                    gameStatusText.text = "Player " + winningPlayer + " wins the round!";
                // Do not reset the grid; just reset the turn.
                Invoke("StartNewRound", 2f);
            }
        }
        else
        {
            Debug.Log("[GameManager] No win condition met yet.");
        }
    }

    // Updated StartNewRound: Only resets the turn (grid remains intact).
    void StartNewRound()
    {
        TurnManager.instance.ResetTurn();
        if (gameStatusText != null)
            gameStatusText.text = "";
    }

    void RestartGame()
    {
        Debug.Log("Restarting Entire Game...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Update the UI elements that show rounds won by each player.
    void UpdateRoundsUI()
    {
        if (roundsWonTextP1 != null)
            roundsWonTextP1.text = "Player 1 Rounds: " + roundsWonP1;
        if (roundsWonTextP2 != null)
            roundsWonTextP2.text = "Player 2 Rounds: " + roundsWonP2;
    }
}
