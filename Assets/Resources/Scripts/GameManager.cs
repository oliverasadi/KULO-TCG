using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;              // For TextMeshProUGUI
using UnityEngine.UI;     // For Button, Image
using UnityEngine.EventSystems; // For detecting UI clicks
using System.Collections;
using System.Collections.Generic;
using DG.Tweening; // DOTween

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

    // DOTween sequence handle for the win message (so we can kill/replace cleanly)
    private Sequence winMsgSeq;

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
        pulseRoutine = null;          // just clear the handle
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
    { "WaxyBaby",       "Ultimate Red Seal" },   // Mr. Wax
    { "CatAdventures",  "Cat Trifecta" },        // Nekomata
    { "SuzhouDeck",     "Bonsai Beast Lvl 3" }   // Xu Taishi
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

        // 5) Show round-win text (animated)
        ShowWinMessageAnimated($"Player {winner} wins {newLines} line(s)!", false);

        // 6) Game over?
        if (roundsWonP1 >= totalRoundsToWin || roundsWonP2 >= totalRoundsToWin)
        {
            if (audioSource != null && gameWinClip != null)
                audioSource.PlayOneShot(gameWinClip);

            // Final win message (stay in center — no slide-out)
            ShowWinMessageAnimated($"Player {winner} Wins Game", true);

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
                else if (deck == "Nekomata")
                {
                    splash = XPResultDataHolder.SplashBackgroundType.Nekomata;
                    if (XPTracker.instance.signatureCardPlayed)
                        splash = XPResultDataHolder.SplashBackgroundType.NekomataWithCatTriFecta;
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
            // We let the animation handle hiding on its own (no Invoke/Clear needed).
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
    // Re-apply persistent green highlight for all stored winning lines.
    // This is a safety net in case a pulse was cancelled mid-way.
    private void ReapplyAllRoundHighlights()
    {
        Color green = new Color(0f, 1f, 0f, 0.5f);

        foreach (var info in playerRoundWins)
        {
            foreach (var c in info.cells)
            {
                var cellObj = GameObject.Find($"GridCell_{c.x}_{c.y}");
                var highlighter = cellObj ? cellObj.GetComponent<GridCellHighlighter>() : null;
                if (highlighter != null)
                {
                    highlighter.SetPersistentHighlight(green);
                }
            }
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
        {
            GameObject.Find($"GridCell_{c.x}_{c.y}")
                ?.GetComponent<GridCellHighlighter>()
                ?.FlashHighlight(blue);
        }

        yield return new WaitForSeconds(1f);

        foreach (var c in info.cells)
        {
            GameObject.Find($"GridCell_{c.x}_{c.y}")
                ?.GetComponent<GridCellHighlighter>()
                ?.RestoreHighlight();
        }

        // Make sure ALL round win highlights are back in their correct state
        ReapplyAllRoundHighlights();

        btn.image.color = Color.green;
        pulseRoutine = null;
    }


    private void CancelPulse()
    {
        if (pulseRoutine != null)
        {
            StopCoroutine(pulseRoutine);
            pulseRoutine = null;

            // Safety: restore all stored win highlights and button colours
            ReapplyAllRoundHighlights();
            RefreshRoundButtons();
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

    // ─────────────────────────────────────────────────────────────────────
    // DOTween animation for win message (right → center → left) + POP
    // If isFinal == true: it flies in and stays centered (no fly-out).
    // ─────────────────────────────────────────────────────────────────────
    public void ShowWinMessageAnimated(
        string message,
        bool isFinal,
        float inDuration = 0.5f,
        float holdDuration = 1f,
        float outDuration = 0.5f)
    {
        if (gameStatusText == null) return;

        var rect = gameStatusText.rectTransform;

        // --- Force full-screen, center anchored rect so it can't clip ---
        rect.anchorMin = Vector2.zero;       // (0,0)
        rect.anchorMax = Vector2.one;        // (1,1)
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.zero;       // fill parent
        rect.anchoredPosition = Vector2.zero;

        // --- TMP sizing so it doesn't get gigantic/cropped ---
        gameStatusText.enableAutoSizing = true;
        gameStatusText.fontSizeMin = 36;   // tweak to taste
        gameStatusText.fontSizeMax = 96;   // <- cap so it won’t be huge
        gameStatusText.enableWordWrapping = false;
        gameStatusText.overflowMode = TMPro.TextOverflowModes.Overflow;
        gameStatusText.alignment = TextAlignmentOptions.Center;
        gameStatusText.margin = new Vector4(40, 0, 40, 0); // left/right breathing room

        // Resolve canvas width (prefer parent rect over Screen.width)
        float canvasWidth = Screen.width;
        if (rect.parent is RectTransform parentRect && parentRect.rect.width > 0f)
            canvasWidth = parentRect.rect.width;

        Vector2 startPos = new Vector2(canvasWidth * 1.1f, 0f);   // off-screen right
        Vector2 centerPos = Vector2.zero;                         // center
        Vector2 endPos = new Vector2(-canvasWidth * 1.1f, 0f);  // off-screen left

        // Kill any old sequence
        if (winMsgSeq != null && winMsgSeq.IsActive())
            winMsgSeq.Kill();

        // Initial state
        rect.anchoredPosition = startPos;
        rect.localScale = Vector3.one;
        gameStatusText.color = Color.white; // reset after any previous color tween
        gameStatusText.text = message;
        gameStatusText.gameObject.SetActive(true);

        // Build sequence: slide in -> pop -> gold flash -> hold -> (slide out)
        winMsgSeq = DOTween.Sequence()
            .Append(rect.DOAnchorPos(centerPos, inDuration).SetEase(Ease.OutCubic))
            .Append(rect.DOPunchScale(Vector3.one * 0.18f, 0.22f, vibrato: 6, elasticity: 0.75f))
            .Join(gameStatusText.DOColor(new Color(1f, 0.92f, 0.2f), 0.12f))  // gold flash
            .Append(gameStatusText.DOColor(Color.white, 0.25f))
            .AppendInterval(holdDuration);

        if (!isFinal)
        {
            winMsgSeq.Append(rect.DOAnchorPos(endPos, outDuration).SetEase(Ease.InCubic))
                     .OnComplete(() => gameStatusText.gameObject.SetActive(false));
        }
        else
        {
            // gentle idle pulse if it's the final win
            winMsgSeq.Append(rect.DOScale(1.05f, 0.6f).SetLoops(-1, LoopType.Yoyo));
        }

        // Optional: play pop SFX right as it lands
        if (audioSource != null && roundWinClip != null)
            winMsgSeq.Insert(inDuration, DOVirtual.DelayedCall(0f, () => audioSource.PlayOneShot(roundWinClip)));
    }
}