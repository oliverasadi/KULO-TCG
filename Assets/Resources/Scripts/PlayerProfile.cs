using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PlayerProfile
{
    public string playerName = "New Player";       // Username
    public int totalWins = 0;                      // Total games won
    public int totalGames = 0;                     // Total games played

    public int currentLevel = 1;                   // Player's current level
    public int totalXP = 0;                        // Total accumulated XP

    public Dictionary<string, int> deckUsage = new(); // e.g., "Waxy Baby" -> 5 games

    public string selectedAvatar = "avatar_default";
    public List<string> unlockedAvatars = new();

    public string selectedTitle = "Novice";
    public List<string> unlockedTitles = new();

    public int totalCardsPlayed = 0;
    public string lastDeckPlayed = "";
    public string signatureCardName = "Ultimate Red Seal"; // 👈 New!

    public static string selectedCharacterName;

    public string avatarImageName = "avatar_default";

    // XP required to level up formula (can be customized)
    public int GetXPForNextLevel()
    {
        return 100 + (currentLevel - 1) * 50; // Example: 100, 150, 200, ...
    }

    // Call this after a match to add XP and level up if needed
    public void AddXP(int amount)
    {
        totalXP += amount;
        Debug.Log($"[XP] Gained {amount} XP. Total now: {totalXP}");

        while (totalXP >= GetXPForNextLevel())
        {
            totalXP -= GetXPForNextLevel();
            currentLevel++;
            Debug.Log($"🎉 Leveled up! New level: {currentLevel}");

            // Optional: Add title/avatars on level-up
            // if (currentLevel == 5) unlockedTitles.Add("Rising Star");
            // if (currentLevel == 10) unlockedAvatars.Add("avatar_glowup");
        }
    }
}
