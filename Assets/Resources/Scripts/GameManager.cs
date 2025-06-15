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
    public int playerCardsPlayedThisGame = 0;  // tracks how many cards the player has played this game


    // Track unique wins
    public bool[] rowUsed = new bool[3];
    public bool[] colUsed = new bool[3];
    public bool[] diagUsed = new bool[2];

    public PostGameXPPanel xpPanel; // Assign in Inspector
    public PlayerProfile profile;   // Optional direct access if not via ProfileManager


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
        if (AudioManager.instance != null)
            StartCoroutine(AudioManager.instance.FadeOutMusic(0.5f));

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
        playerCardsPlayedThisGame = 0;

        // Reset XPTracker state
        if (XPTracker.instance != null)
        {
            XPTracker.instance.playerWon = false;
            XPTracker.instance.totalTurns = 0;
            XPTracker.instance.cardsPlayed = 0;
            XPTracker.instance.playerRoundsWon = 0;
            XPTracker.instance.opponentRoundsWon = 0;
            XPTracker.instance.wasDown0to2 = false;
            XPTracker.instance.lineBlockOccurred = false;
            XPTracker.instance.signatureCardPlayed = false;
            XPTracker.instance.evolutionCardPlayed = false;
        }
        else
        {
            Debug.LogWarning("⚠️ XPTracker.instance is null — make sure it's in the scene.");
        }

        // ✅ Set signature card based on selected deck/character
        if (ProfileManager.instance != null && ProfileManager.instance.currentProfile != null)
        {
            string deck = ProfileManager.instance.currentProfile.lastDeckPlayed;

            Dictionary<string, string> signatureCards = new Dictionary<string, string>
        {
            { "Mr. Wax", "Ultimate Red Seal" },
            { "Nekomata", "Cat Trifecta" },
            { "Xu Taishi", "Bonsai Beast Lvl 3" }
        };

            if (signatureCards.ContainsKey(deck))
            {
                ProfileManager.instance.currentProfile.signatureCardName = signatureCards[deck];
                Debug.Log($"[GameManager] Signature card set to: {signatureCards[deck]}");
            }
            else
            {
                Debug.LogWarning($"[GameManager] No signature card defined for deck: {deck}");
            }
        }
    }




    public void EndTurn()
    {
        if (!gameActive) return;
        TurnManager.instance.EndTurn();
    }

  public GameObject postGamePopupPrefab; // Assign in Inspector (the popup UI with Restart, Home, etc.)

    public void CheckForWin()
    {
        // 1) Clone old win-tracking arrays
        var oldRows = (bool[])rowUsed.Clone();
        var oldCols = (bool[])colUsed.Clone();
        var oldDiags = (bool[])diagUsed.Clone();

        // 2) Check for any new completed lines
        int newLines = WinChecker.instance.CheckWinCondition(GridManager.instance.GetGrid());
        if (newLines <= 0)
            return;

        // 3) Play round-win audio
        if (audioSource != null && roundWinClip != null)
            audioSource.PlayOneShot(roundWinClip);

        // 4) Award line wins
        int winner = TurnManager.instance.GetCurrentPlayer();
        if (winner == 1)
        {
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

            if (roundsWonP1 == 3 && roundsWonP2 == 2 && XPTracker.instance != null)
                XPTracker.instance.wasDown0to2 = true;
        }
        else if (winner == 2)
        {
            roundsWonP2 += newLines;
            UpdateRoundsUI();
        }

        // 5) Show round-win text
        if (gameStatusText != null)
        {
            gameStatusText.gameObject.SetActive(true);
            gameStatusText.text = $"Player {winner} wins {newLines} line(s)!";
        }

        // 6) Game over?
        if (roundsWonP1 >= totalRoundsToWin || roundsWonP2 >= totalRoundsToWin)
        {
            if (audioSource != null && gameWinClip != null)
                audioSource.PlayOneShot(gameWinClip);

            if (gameStatusText != null)
            {
                gameStatusText.gameObject.SetActive(true);
                gameStatusText.text = $"Player {winner} Wins Game";
            }

            // ✅ Guard XP logic to avoid crash
            if (XPTracker.instance != null)
            {
                bool playerWon = (winner == 1);
                XPTracker.instance.playerWon = playerWon;
                XPTracker.instance.playerRoundsWon = roundsWonP1;
                XPTracker.instance.opponentRoundsWon = roundsWonP2;

                List<XPReward> rewards = XPTracker.instance.EvaluateXP();
                int totalXP = 0;
                foreach (var r in rewards) totalXP += r.XP;

                if (ProfileManager.instance?.currentProfile != null)
                {
                    ProfileManager.instance.RecordGameResult(
                        ProfileManager.instance.currentProfile.lastDeckPlayed,
                        playerWon,
                        playerCardsPlayedThisGame
                    );
                    ProfileManager.instance.currentProfile.AddXP(totalXP);
                }

                // Set splash background
                var deck = ProfileManager.instance?.currentProfile?.lastDeckPlayed ?? "";
                XPResultDataHolder.SplashBackgroundType splash = XPResultDataHolder.SplashBackgroundType.MrWax;

                if (deck == "Mr. Wax")
                {
                    splash = XPResultDataHolder.SplashBackgroundType.MrWax;
                    if (XPTracker.instance.signatureCardPlayed)
                        splash = XPResultDataHolder.SplashBackgroundType.MrWaxWithRedSeal;
                }
                else if (deck == "Kyoko")
                {
                    splash = XPResultDataHolder.SplashBackgroundType.Kyoko;
                    if (XPTracker.instance.signatureCardPlayed)
                        splash = XPResultDataHolder.SplashBackgroundType.KyokoWithWhiskerKing;
                }

                if (XPResultDataHolder.instance != null)
                    XPResultDataHolder.instance.Set(rewards, splash);
            }
            else
            {
                Debug.LogWarning("⚠️ XPTracker is missing — skipping XP logic.");
            }

            // ✅ Always show the popup
            playerCardsPlayedThisGame = 0;
            ShowPostGamePopup();
        }
        else
        {
            Invoke("ClearWinAnnouncement", 2f);
        }
    }






    private void ShowPostGamePopup()
    {
        GameObject canvas = GameObject.Find("OverlayCanvas");
        if (canvas != null && postGamePopupPrefab != null)
        {
            Instantiate(postGamePopupPrefab, canvas.transform);
        }
        else
        {
            Debug.LogError("❌ Missing OverlayCanvas or PostGamePopupPrefab.");
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

    public void RestartGame()
    {
        StartCoroutine(RestartRoutine());
    }

    private IEnumerator RestartRoutine()
    {
        // Load the scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);

        // Wait one frame so the scene can finish loading
        yield return null;

        // Reset the turn to ensure Player 1 (and AI) behave correctly
        TurnManager.instance.ResetTurn();
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
