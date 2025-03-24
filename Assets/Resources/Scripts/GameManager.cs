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
    public bool roundWinDetected = false; // Add this field at the top of your GameManager


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
    // Now, we handle multiple lines in one call.

    public void CheckForWin()
    {
        Debug.Log("[GameManager] Checking win condition...");
        // WinChecker returns how many new winning lines were formed.
        int newlyFormedLines = WinChecker.instance.CheckWinCondition(GridManager.instance.GetGrid());
        if (newlyFormedLines > 0)
        {
            int winningPlayer = TurnManager.instance.GetCurrentPlayer();
            Debug.Log($"[GameManager] {newlyFormedLines} new winning line(s) formed! Player {winningPlayer} scores.");

            // Play round win sound.
            if (audioSource != null && roundWinClip != null)
            {
                audioSource.PlayOneShot(roundWinClip);
            }

            // Increment win count for each new line.
            for (int i = 0; i < newlyFormedLines; i++)
            {
                if (winningPlayer == 1)
                    roundsWonP1++;
                else
                    roundsWonP2++;
            }
            UpdateRoundsUI();

            // Record that a win has been detected.
            roundWinDetected = true;

            // Update UI to indicate the win.
            if (gameStatusText != null)
            {
                gameStatusText.gameObject.SetActive(true);
                gameStatusText.text = $"Player {winningPlayer} wins {newlyFormedLines} line(s)!";
            }

            // Check if someone reached totalRoundsToWin.
            if (roundsWonP1 >= totalRoundsToWin || roundsWonP2 >= totalRoundsToWin)
            {
                Debug.Log($"[GameManager] Player {winningPlayer} wins the game!");
                if (gameStatusText != null)
                {
                    gameStatusText.gameObject.SetActive(true);
                    gameStatusText.text = $"Player {winningPlayer} wins the game!";
                }
                if (audioSource != null && gameWinClip != null)
                {
                    audioSource.PlayOneShot(gameWinClip);
                }
                Invoke("RestartGame", 3f);
            }
            else
            {
                // Clear the win announcement after 2 seconds.
                Invoke("ClearWinAnnouncement", 2f);
            }
        }
        else
        {
            Debug.Log("[GameManager] No win condition met yet.");
        }
    }

    private void ClearWinAnnouncement()
    {
        if (gameStatusText != null)
        {
            gameStatusText.gameObject.SetActive(false);
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
