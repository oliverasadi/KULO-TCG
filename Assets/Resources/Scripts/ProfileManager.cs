using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class ProfileManager : MonoBehaviour
{
    public static ProfileManager instance;
    public PlayerProfile currentProfile;

    private string profilePath => Application.persistentDataPath + "/playerProfile.json";


    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);

            Debug.Log("🔍 Profile Save Path: " + Application.persistentDataPath); // 👈 Added this line

            LoadProfile();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SaveProfile()
    {
        string json = JsonUtility.ToJson(currentProfile, true);
        File.WriteAllText(profilePath, json);
        Debug.Log("✅ Profile saved.");
    }

    public void LoadProfile()
    {
        if (File.Exists(profilePath))
        {
            string json = File.ReadAllText(profilePath);
            currentProfile = JsonUtility.FromJson<PlayerProfile>(json);
            Debug.Log("✅ Profile loaded.");
        }
        else
        {
            currentProfile = new PlayerProfile();
            GrantDefaultUnlocks(); // ensure defaults like "Novice" and "default" exist
            SaveProfile();
        }
    }

    public void RecordGameResult(string deckName, bool won)
    {
        if (currentProfile == null) return;

        currentProfile.totalGames++;
        if (won) currentProfile.totalWins++;

        if (!currentProfile.deckUsage.ContainsKey(deckName))
            currentProfile.deckUsage[deckName] = 0;
        currentProfile.deckUsage[deckName]++;

        currentProfile.totalCardsPlayed += 5; // adjust based on actual logic
        currentProfile.lastDeckPlayed = deckName;

        CheckForUnlocks();
        SaveProfile();
    }

    private void CheckForUnlocks()
    {
        if (currentProfile.totalWins >= 10 && !currentProfile.unlockedTitles.Contains("Champion"))
        {
            currentProfile.unlockedTitles.Add("Champion");
            ShowUnlockToast("🏆 Title Unlocked: Champion");
        }

        if (currentProfile.totalGames >= 20 && !currentProfile.unlockedAvatars.Contains("GoldFrame"))
        {
            currentProfile.unlockedAvatars.Add("GoldFrame");
            ShowUnlockToast("🌟 Avatar Unlocked: Gold Frame");
        }
    }

    private void GrantDefaultUnlocks()
    {
        if (!currentProfile.unlockedAvatars.Contains("avatar_default"))
            currentProfile.unlockedAvatars.Add("avatar_default");

        if (!currentProfile.unlockedTitles.Contains("Novice"))
            currentProfile.unlockedTitles.Add("Novice");

        if (string.IsNullOrEmpty(currentProfile.selectedAvatar))
            currentProfile.selectedAvatar = "avatar_default";

        if (string.IsNullOrEmpty(currentProfile.selectedTitle))
            currentProfile.selectedTitle = "Novice";
    }

    private void ShowUnlockToast(string message)
    {
        if (ToastManager.instance != null)
            ToastManager.instance.ShowToast(message);
        else
            Debug.Log($"[Unlock] {message}");
    }
}
