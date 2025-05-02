using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;              // For TextMeshProUGUI
using UnityEngine.UI;     // For Button, Image
using UnityEngine.EventSystems; // For detecting UI clicks
using System.Collections;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    private bool gameActive = true;
    private int roundsWonP1 = 0;
    private int roundsWonP2 = 0;
    public int totalRoundsToWin = 3;

    // Track unique wins
    public bool[] rowUsed = new bool[3];
    public bool[] colUsed = new bool[3];
    public bool[] diagUsed = new bool[2];

    [Header("UI Elements")]
    public TextMeshProUGUI roundsWonTextP1;
    public TextMeshProUGUI roundsWonTextP2;
    public TextMeshProUGUI gameStatusText;

    // Round buttons
    public Button round1Button;
    public Button round2Button;
    public Button round3Button;

    // Store default button colors
    private Color defaultColor1;
    private Color defaultColor2;
    private Color defaultColor3;

    [Header("Audio Clips")]
    public AudioSource audioSource;
    public AudioClip roundWinClip;
    public AudioClip gameWinClip;

    // Data for each player's winning line
    private class RoundWinInfo { public Vector2Int[] cells; }
    private List<RoundWinInfo> playerRoundWins = new List<RoundWinInfo>();

    // Coroutine handle for pulsing
    private Coroutine pulseRoutine;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Reset state
        ResetUniqueWins();
        playerRoundWins.Clear();
        CancelPulse();
        UpdateRoundsUI();

        // Capture default button colors
        defaultColor1 = round1Button.image.color;
        defaultColor2 = round2Button.image.color;
        defaultColor3 = round3Button.image.color;

        // Disable all round buttons
        ResetRoundButtonsUI();

        // Hide status text initially
        if (gameStatusText != null)
            gameStatusText.gameObject.SetActive(false);

        // Hook up button clicks
        round1Button.onClick.AddListener(() => OnRoundButtonClicked(1));
        round2Button.onClick.AddListener(() => OnRoundButtonClicked(2));
        round3Button.onClick.AddListener(() => OnRoundButtonClicked(3));

        StartGame();
    }

    void Update()
    {
        // Stop pulsing if click outside buttons
        if (pulseRoutine != null && Input.GetMouseButtonDown(0))
        {
            bool overUI = EventSystem.current.IsPointerOverGameObject();
            if (!overUI || !IsClickOnRoundButton())
                CancelPulse();
        }
    }

    bool IsClickOnRoundButton()
    {
        var go = EventSystem.current.currentSelectedGameObject;
        return go == round1Button.gameObject || go == round2Button.gameObject || go == round3Button.gameObject;
    }

    public void StartGame()
    {
        Debug.Log("Game Started!");
        TurnManager.instance.StartTurn();
    }

    public void EndTurn()
    {
        if (!gameActive) return;
        TurnManager.instance.EndTurn();
    }

    public void CheckForWin()
    {
        var oldRows = (bool[])rowUsed.Clone();
        var oldCols = (bool[])colUsed.Clone();
        var oldDiags = (bool[])diagUsed.Clone();

        int newLines = WinChecker.instance.CheckWinCondition(GridManager.instance.GetGrid());
        if (newLines <= 0) return;

        // Play round win audio
        if (audioSource != null && roundWinClip != null)
            audioSource.PlayOneShot(roundWinClip);

        int winner = TurnManager.instance.GetCurrentPlayer();

        if (winner == 1)
        {
            // Record player wins
            for (int i = 0; i < 3; i++)
            {
                if (!oldRows[i] && rowUsed[i]) AddPlayerWin(i, Axis.Row);
                if (!oldCols[i] && colUsed[i]) AddPlayerWin(i, Axis.Col);
            }
            if (!oldDiags[0] && diagUsed[0]) AddPlayerWin(0, Axis.MainDiag);
            if (!oldDiags[1] && diagUsed[1]) AddPlayerWin(1, Axis.AntiDiag);

            roundsWonP1 += newLines;
            UpdateRoundsUI();
            RefreshRoundButtons();
        }
        else if (winner == 2)
        {
            // AI only updates text
            roundsWonP2 += newLines;
            UpdateRoundsUI();
        }

        // Display status
        if (gameStatusText != null)
        {
            gameStatusText.gameObject.SetActive(true);
            gameStatusText.text = $"Player {winner} wins {newLines} line(s)!";
        }

        // Check for overall game win
        if (roundsWonP1 >= totalRoundsToWin || roundsWonP2 >= totalRoundsToWin)
        {
            // Play the game-win sound
            if (audioSource != null && gameWinClip != null)
                audioSource.PlayOneShot(gameWinClip);

            // Show "Player X Wins Game"
            if (gameStatusText != null)
            {
                gameStatusText.gameObject.SetActive(true);
                gameStatusText.text = $"Player {winner} Wins Game";
            }

            Invoke("RestartGame", 3f);
        }
        else
        {
            Invoke("ClearWinAnnouncement", 2f);
        }
    }

    private void AddPlayerWin(int index, Axis axis)
    {
        Vector2Int[] cells = axis switch
        {
            Axis.Row => new[] { new Vector2Int(index, 0), new Vector2Int(index, 1), new Vector2Int(index, 2) },
            Axis.Col => new[] { new Vector2Int(0, index), new Vector2Int(1, index), new Vector2Int(2, index) },
            Axis.MainDiag => new[] { new Vector2Int(0, 0), new Vector2Int(1, 1), new Vector2Int(2, 2) },
            _ => new[] { new Vector2Int(0, 2), new Vector2Int(1, 1), new Vector2Int(2, 0) },
        };

        playerRoundWins.Add(new RoundWinInfo { cells = cells });

        // Apply persistent green highlight
        Color green = new Color(0f, 1f, 0f, 0.5f);
        foreach (var c in cells)
        {
            var cellObj = GameObject.Find($"GridCell_{c.x}_{c.y}");
            cellObj?.GetComponent<GridCellHighlighter>()?.SetPersistentHighlight(green);
        }
    }

    private void ResetRoundButtonsUI()
    {
        round1Button.image.color = defaultColor1; round1Button.interactable = false;
        round2Button.image.color = defaultColor2; round2Button.interactable = false;
        round3Button.image.color = defaultColor3; round3Button.interactable = false;
    }

    private void RefreshRoundButtons()
    {
        ResetRoundButtonsUI();
        int wins = playerRoundWins.Count;
        Debug.Log($"[GameManager] RefreshRoundButtons — wins = {wins}");
        if (wins >= 1)
        {
            Debug.Log("Enabling Round 1");
            round1Button.image.color = Color.green;
            round1Button.interactable = true;
        }
        if (wins >= 2)
        {
            Debug.Log("Enabling Round 2");
            round2Button.image.color = Color.green;
            round2Button.interactable = true;
        }
        if (wins >= 3)
        {
            Debug.Log("Enabling Round 3");
            round3Button.image.color = Color.green;
            round3Button.interactable = true;
        }
    }

    public void OnRoundButtonClicked(int uiIndex)
    {
        CancelPulse();
        if (uiIndex <= playerRoundWins.Count)
            pulseRoutine = StartCoroutine(PulseWinningCells(playerRoundWins[uiIndex - 1], uiIndex));
    }

    private IEnumerator PulseWinningCells(RoundWinInfo info, int uiIndex)
    {
        Button btn = uiIndex == 1 ? round1Button : uiIndex == 2 ? round2Button : round3Button;
        Color blue = new Color(0f, 0f, 1f, 0.5f);

        btn.image.color = blue;
        foreach (var c in info.cells)
            GameObject.Find($"GridCell_{c.x}_{c.y}")
                ?.GetComponent<GridCellHighlighter>()
                ?.FlashHighlight(blue);

        yield return new WaitForSeconds(1f);

        foreach (var c in info.cells)
            GameObject.Find($"GridCell_{c.x}_{c.y}")
                ?.GetComponent<GridCellHighlighter>()
                ?.RestoreHighlight();

        btn.image.color = Color.green;
        pulseRoutine = null;
    }

    private void CancelPulse()
    {
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;
        }
    }

    private void ClearWinAnnouncement()
    {
        if (gameStatusText != null)
            gameStatusText.gameObject.SetActive(false);
    }

    void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void UpdateRoundsUI()
    {
        if (roundsWonTextP1 != null)
            roundsWonTextP1.text = "Player 1 Rounds: " + roundsWonP1;
        if (roundsWonTextP2 != null)
            roundsWonTextP2.text = "Player 2 Rounds: " + roundsWonP2;
    }

    private void ResetUniqueWins()
    {
        for (int i = 0; i < 3; i++) rowUsed[i] = colUsed[i] = false;
        diagUsed[0] = diagUsed[1] = false;
    }

    private enum Axis { Row, Col, MainDiag, AntiDiag }
}
