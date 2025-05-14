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

    [Header("Edit Features")]
    public TMP_InputField nameInputField;
    public Button saveNameButton;
    public Button closeButton;

    private void Start()
    {
        // Hook up save and close buttons
        if (saveNameButton != null)
            saveNameButton.onClick.AddListener(SaveName);

        if (closeButton != null)
            closeButton.onClick.AddListener(() => gameObject.SetActive(false));

        StartCoroutine(DelayedRefresh());
    }

    private IEnumerator DelayedRefresh()
    {
        // Wait one frame to ensure ProfileManager loads
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
}
