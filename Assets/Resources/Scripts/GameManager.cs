using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Using TextMeshProUGUI

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    private bool gameActive = true; // Track if the game is ongoing
    private int roundsWonP1 = 0;
    private int roundsWonP2 = 0;
    public int totalRoundsToWin = 3; // First to 3 unique wins wins the game

    // Unique win tracking arrays:
    public bool[] rowUsed = new bool[3];    // rows 0,1,2
    public bool[] colUsed = new bool[3];    // columns 0,1,2
    public bool[] diagUsed = new bool[2];   // diagUsed[0]: main diagonal, diagUsed[1]: anti-diagonal

    [Header("UI Elements")]
    public TextMeshProUGUI roundsWonTextP1;   // Assign in Inspector
    public TextMeshProUGUI roundsWonTextP2;   // Assign in Inspector
    public TextMeshProUGUI gameStatusText;    // For status messages (e.g., "Player 1 wins the round!")

    [Header("Audio Clips")]
    public AudioSource audioSource;      // Assign an AudioSource in the Inspector
    public AudioClip roundWinClip;       // Clip to play when a round is won
    public AudioClip gameWinClip;        // Clip to play when the game is won

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        ResetUniqueWins();
        UpdateRoundsUI();
        // Ensure status text is inactive at the start.
        if (gameStatusText != null)
            gameStatusText.gameObject.SetActive(false);
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

    // Call this method after each move to check if the board meets a win condition.
    public void CheckForWin()
    {
        Debug.Log("[GameManager] Checking win condition...");
        if (WinChecker.instance.CheckWinCondition(GridManager.instance.GetGrid()))
        {
            int winningPlayer = TurnManager.instance.GetCurrentPlayer();
            Debug.Log("[GameManager] Win condition met! Player " + winningPlayer + " wins the round.");

            // Play round win sound.
            if (audioSource != null && roundWinClip != null)
            {
                audioSource.PlayOneShot(roundWinClip);
            }

            // Mark the winning line as used.
            MarkWinningLine();

            // Increment the winner's round win count.
            if (winningPlayer == 1)
                roundsWonP1++;
            else
                roundsWonP2++;

            UpdateRoundsUI();

            if (roundsWonP1 >= totalRoundsToWin || roundsWonP2 >= totalRoundsToWin)
            {
                Debug.Log("[GameManager] Player " + winningPlayer + " wins the game!");
                if (gameStatusText != null)
                {
                    gameStatusText.gameObject.SetActive(true);
                    gameStatusText.text = "Player " + winningPlayer + " wins the game!";
                }
                // Play game win sound.
                if (audioSource != null && gameWinClip != null)
                {
                    audioSource.PlayOneShot(gameWinClip);
                }
                Invoke("RestartGame", 3f);
            }
            else
            {
                if (gameStatusText != null)
                {
                    gameStatusText.gameObject.SetActive(true);
                    gameStatusText.text = "Player " + winningPlayer + " wins the round!";
                }
                // For the next round, we do not reset the grid (winning cards remain), just reset the turn.
                Invoke("StartNewRound", 2f);
            }
        }
        else
        {
            Debug.Log("[GameManager] No win condition met yet.");
        }
    }

    // Marks the first available winning line (row, column, or diagonal) as used.
    // This is a simple approach – it checks rows first, then columns, then diagonals.
    private void MarkWinningLine()
    {
        CardSO[,] grid = GridManager.instance.GetGrid();
        // Check rows.
        for (int i = 0; i < 3; i++)
        {
            if (!rowUsed[i] && grid[i, 0] != null && grid[i, 1] != null && grid[i, 2] != null)
            {
                rowUsed[i] = true;
                Debug.Log($"Marking row {i} as used.");
                return;
            }
        }
        // Check columns.
        for (int j = 0; j < 3; j++)
        {
            if (!colUsed[j] && grid[0, j] != null && grid[1, j] != null && grid[2, j] != null)
            {
                colUsed[j] = true;
                Debug.Log($"Marking column {j} as used.");
                return;
            }
        }
        // Check main diagonal.
        if (!diagUsed[0] && grid[0, 0] != null && grid[1, 1] != null && grid[2, 2] != null)
        {
            diagUsed[0] = true;
            Debug.Log("Marking main diagonal as used.");
            return;
        }
        // Check anti-diagonal.
        if (!diagUsed[1] && grid[0, 2] != null && grid[1, 1] != null && grid[2, 0] != null)
        {
            diagUsed[1] = true;
            Debug.Log("Marking anti-diagonal as used.");
            return;
        }
    }

    // Resets only the turn (and leaves winning lines marked so they won't be reused).
    void StartNewRound()
    {
        TurnManager.instance.ResetTurn();
        if (gameStatusText != null)
        {
            gameStatusText.text = "";
            gameStatusText.gameObject.SetActive(false);
        }
    }

    void RestartGame()
    {
        Debug.Log("Restarting Entire Game...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void UpdateRoundsUI()
    {
        if (roundsWonTextP1 != null)
            roundsWonTextP1.text = "Player 1 Rounds: " + roundsWonP1;
        if (roundsWonTextP2 != null)
            roundsWonTextP2.text = "Player 2 Rounds: " + roundsWonP2;
    }

    // Reset unique win tracking arrays for a new game.
    private void ResetUniqueWins()
    {
        for (int i = 0; i < 3; i++)
        {
            rowUsed[i] = false;
            colUsed[i] = false;
        }
        diagUsed[0] = false;
        diagUsed[1] = false;
    }
}
