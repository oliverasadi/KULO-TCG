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
            DontDestroyOnLoad(gameObject); // So it persists across scenes
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
            SaveProfile();
        }
    }

    public void RecordGameResult(string deckName, bool won)
    {
        currentProfile.totalGames++;
        if (won) currentProfile.totalWins++;

        if (!currentProfile.deckUsage.ContainsKey(deckName))
            currentProfile.deckUsage[deckName] = 0;
        currentProfile.deckUsage[deckName]++;

        CheckForUnlocks();
        SaveProfile();
    }

    private void CheckForUnlocks()
    {
        if (currentProfile.totalWins >= 10 && !currentProfile.unlockedTitles.Contains("Champion"))
        {
            currentProfile.unlockedTitles.Add("Champion");
            Debug.Log("🏆 Title unlocked: Champion");
        }

        if (currentProfile.totalGames >= 20 && !currentProfile.unlockedAvatars.Contains("GoldFrame"))
        {
            currentProfile.unlockedAvatars.Add("GoldFrame");
            Debug.Log("🌟 Avatar unlocked: Gold Frame");
        }
    }
}
