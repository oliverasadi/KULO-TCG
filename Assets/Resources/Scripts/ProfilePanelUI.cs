using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class ProfilePanelUI : MonoBehaviour
{
    [Header("Text Display")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI winStatsText;
    public TextMeshProUGUI deckStatsText;
    public TextMeshProUGUI cardsPlayedText;
    public TextMeshProUGUI lastDeckText;
    public TextMeshProUGUI levelText;   // New: to display Level
    public TextMeshProUGUI xpText;      // New: to display XP

    [Header("Edit Features")]
    public TMP_InputField nameInputField;
    public Button saveNameButton;
    public Button closeButton;

    [Header("Selection Dropdowns")]
    public TMP_Dropdown avatarDropdown;
    public TMP_Dropdown titleDropdown;

    [Header("Avatar Display")]
    public Image avatarImageDisplay;
    public GameObject avatarPopupPanel; // 👈 drag your AvatarPopupPanel here

    private void Start()
    {
        if (saveNameButton != null)
            saveNameButton.onClick.AddListener(SaveName);

        if (closeButton != null)
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));

        if (avatarDropdown != null)
            avatarDropdown.onValueChanged.AddListener(SetAvatar);

        if (titleDropdown != null)
            titleDropdown.onValueChanged.AddListener(SetTitle);

        // 🔍 Optional debug test for dev
        Sprite test = Resources.Load<Sprite>("Avatars/avatar_default");
        Debug.Log("Avatar Sprite Test: " + (test != null ? "✅ Found!" : "❌ Not Found!"));

        StartCoroutine(DelayedRefresh());
    }

    private IEnumerator DelayedRefresh()
    {
        yield return null;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (ProfileManager.instance == null || ProfileManager.instance.currentProfile == null)
        {
            Debug.LogWarning("❌ ProfileManager or currentProfile is null in RefreshUI().");
            return;
        }

        var p = ProfileManager.instance.currentProfile;

        if (nameText != null)
            nameText.text = $"Player Name: {p.playerName}";
        if (nameInputField != null)
            nameInputField.text = p.playerName;

        if (winStatsText != null)
            winStatsText.text = $"Wins: {p.totalWins} / {p.totalGames} games";

        string mostUsedDeck = "None";
        int maxPlays = 0;
        foreach (var entry in p.deckUsage)
        {
            if (entry.Value > maxPlays)
            {
                mostUsedDeck = entry.Key;
                maxPlays = entry.Value;
            }
        }

        if (deckStatsText != null)
            deckStatsText.text = $"Most Played Deck: {mostUsedDeck}";

        if (cardsPlayedText != null)
            cardsPlayedText.text = $"Cards Played: {p.totalCardsPlayed}";

        if (lastDeckText != null)
            lastDeckText.text = $"Last Used Deck: {p.lastDeckPlayed}";

        if (titleDropdown != null)
        {
            titleDropdown.ClearOptions();
            titleDropdown.AddOptions(p.unlockedTitles);
            int titleIndex = p.unlockedTitles.IndexOf(p.selectedTitle);
            titleDropdown.value = titleIndex >= 0 ? titleIndex : 0;
        }

        UpdateAvatarImage(p.selectedAvatar);

        // ─── NEW LEVEL & XP DISPLAY ────────────────────────────────────────────
        if (levelText != null)
            levelText.text = $"Level: {p.currentLevel}";
        if (xpText != null)
            xpText.text = $"XP: {p.totalXP} / {p.currentLevel * 100}";
    }


    private void SaveName()
    {
        if (ProfileManager.instance == null || ProfileManager.instance.currentProfile == null)
        {
            Debug.LogWarning("❌ ProfileManager or currentProfile is null in SaveName().");
            return;
        }

        var p = ProfileManager.instance.currentProfile;
        p.playerName = nameInputField.text;
        ProfileManager.instance.SaveProfile();
        RefreshUI();

        Debug.Log("✅ Save button clicked!");
        if (ToastManager.instance != null)
            ToastManager.instance.ShowToast("✔️ Name Saved!");
        else
            Debug.LogWarning("❌ ToastManager.instance is null!");
    }

    private void SetAvatar(int index)
    {
        var p = ProfileManager.instance.currentProfile;
        if (index >= 0 && index < avatarDropdown.options.Count)
        {
            string selected = avatarDropdown.options[index].text;
            Debug.Log($"🧠 Avatar selected via dropdown: '{selected}'");

            p.selectedAvatar = selected;
            ProfileManager.instance.SaveProfile();
            UpdateAvatarImage(p.selectedAvatar);
        }
    }

    private void SetTitle(int index)
    {
        var p = ProfileManager.instance.currentProfile;
        if (index >= 0 && index < titleDropdown.options.Count)
        {
            p.selectedTitle = titleDropdown.options[index].text;
            ProfileManager.instance.SaveProfile();
        }
    }

    private void UpdateAvatarImage(string avatarKey)
    {
        if (avatarImageDisplay == null) return;

        string path = $"Avatars/{avatarKey}";
        Sprite loaded = Resources.Load<Sprite>(path);
        if (loaded != null)
        {
            avatarImageDisplay.sprite = loaded;
        }
        else
        {
            Debug.LogWarning($"⚠️ Could not load avatar sprite at path: Resources/Avatars/{avatarKey}");
        }
    }

    // 🔓 Avatar popup handling

    public void OpenAvatarPopup()
    {
        if (avatarPopupPanel != null)
            avatarPopupPanel.SetActive(true);
    }

    public void CloseAvatarPopup()
    {
        if (avatarPopupPanel != null)
            avatarPopupPanel.SetActive(false);
    }
}
