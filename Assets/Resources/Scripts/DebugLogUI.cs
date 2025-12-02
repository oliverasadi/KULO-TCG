using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text;

public class DebugLogUI : MonoBehaviour
{
    public static DebugLogUI Instance;

    [Header("UI References")]
    public TextMeshProUGUI logText;
    public ScrollRect scrollRect;
    public GameObject rootPanel;          // 👈 assign Panel here
    public int maxLines = 200;

    [Header("Behaviour")]
    public bool startHiddenInBuild = true;
    public KeyCode toggleKey = KeyCode.D;

    private StringBuilder _builder = new StringBuilder();
    private int _lineCount = 0;

    // how close to the bottom counts as “at bottom” (0 = bottom, 1 = top)
    private const float BottomThreshold = 0.01f;

    RectTransform contentRect;   // parent of LogText
    RectTransform logTextRect;

    void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (rootPanel == null && transform.childCount > 0)
            rootPanel = transform.GetChild(0).gameObject;

        if (startHiddenInBuild && !Debug.isDebugBuild && rootPanel != null)
            rootPanel.SetActive(false);

        if (logText == null)
        {
            Debug.LogError("[DebugLogUI] logText is not assigned!");
        }

        if (logText != null)
        {
            logTextRect = logText.rectTransform;
            contentRect = logTextRect.parent as RectTransform;
        }

        Application.logMessageReceived += HandleLog;
    }

    void OnDestroy()
    {
        Application.logMessageReceived -= HandleLog;
        if (Instance == this)
            Instance = null;
    }

    void Update()
    {
        // Toggle panel visibility (no extra Debug.Log here to avoid log spam)
        if (Input.GetKeyDown(toggleKey) && rootPanel != null)
        {
            bool newState = !rootPanel.activeSelf;
            rootPanel.SetActive(newState);
        }
    }

    void HandleLog(string condition, string stackTrace, LogType type)
    {
        // Still good to keep this so we don't re-log TMP warnings themselves
        if (condition.Contains("The character with Unicode value"))
            return;

        // (Optional) ignore plain Debug.Log to keep log light
        // if (type == LogType.Log)
        //     return;

        string prefix = type switch
        {
            LogType.Warning => "<color=#FFCC00>[WARN]</color> ",
            LogType.Error => "<color=#FF6666>[ERROR]</color> ",
            LogType.Exception => "<color=#FF3333>[EXC]</color> ",
            _ => ""
        };

        // 🔧 Strip emojis / unsupported glyphs before they reach LogText
        string safe = Sanitize(condition);

        AppendLine(prefix + safe);
    }


    public void AppendLine(string message)
    {
        if (logText == null) return;

        // 1️⃣ were we at the bottom before updating?
        bool wasAtBottom = false;
        if (scrollRect != null)
        {
            float pos = scrollRect.verticalNormalizedPosition; // 0 = bottom, 1 = top
            wasAtBottom = pos <= BottomThreshold;
        }

        // 2️⃣ update text buffer
        _builder.AppendLine(message);
        _lineCount++;

        if (_lineCount > maxLines)
        {
            string[] lines = _builder.ToString().Split('\n');
            int start = Mathf.Max(0, lines.Length - maxLines - 1);

            _builder.Clear();
            for (int i = start; i < lines.Length; i++)
            {
                if (!string.IsNullOrEmpty(lines[i]))
                    _builder.AppendLine(lines[i]);
            }

            _lineCount = maxLines;
        }

        logText.text = _builder.ToString();

        // 3️⃣ manually resize content height based on text preferred height
        if (contentRect != null)
        {
            float preferred = logText.preferredHeight;
            float padding = 20f; // top+bottom margin
            contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, preferred + padding);
        }

        // 4️⃣ snap to bottom ONLY if we were already at bottom
        if (scrollRect != null && wasAtBottom)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f; // bottom
        }
    }

    public void ClearLog()
    {
        _builder.Clear();
        _lineCount = 0;
        if (logText != null)
            logText.text = "";

        if (contentRect != null)
            contentRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, 0f);
    }

    // Remove characters that our TMP font can't handle (e.g. emojis)
    string Sanitize(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var sb = new StringBuilder(input.Length);
        foreach (char c in input)
        {
            // Keep basic ASCII only (feel free to loosen this if you want)
            if (c <= 127)
                sb.Append(c);
            // else skip the character (this removes emojis / fancy symbols)
        }
        return sb.ToString();
    }

}
