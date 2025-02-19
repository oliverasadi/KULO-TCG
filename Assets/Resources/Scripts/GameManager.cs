using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    private bool gameActive = true; // Track if the game is ongoing
    private int roundsWonP1 = 0;
    private int roundsWonP2 = 0;
    private int totalRoundsToWin = 3; // First to 3 unique rows wins

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
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

    public void CheckForWin()
    {
        if (WinChecker.instance.CheckWinCondition(GridManager.instance.GetGrid()))
        {
            Debug.Log("Player " + TurnManager.instance.GetCurrentPlayer() + " won this round!");

            if (TurnManager.instance.GetCurrentPlayer() == 1) roundsWonP1++;
            else roundsWonP2++;

            if (roundsWonP1 >= totalRoundsToWin || roundsWonP2 >= totalRoundsToWin)
            {
                Debug.Log("Player " + TurnManager.instance.GetCurrentPlayer() + " wins the game!");
                Invoke("RestartGame", 3f); // Restart full game after 3 seconds
            }
            else
            {
                Debug.Log("Starting Next Round...");
                Invoke("StartNewRound", 2f); // Start next round after 2 seconds
            }
        }
    }

    void StartNewRound()
    {
        GridManager.instance.ResetGrid(); // Keeps winning cards on field
        TurnManager.instance.ResetTurn();
    }

    void RestartGame()
    {
        Debug.Log("Restarting Entire Game...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

}
