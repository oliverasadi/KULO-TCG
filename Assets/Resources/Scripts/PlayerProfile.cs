using System.Collections.Generic;

[System.Serializable]
public class PlayerProfile
{
    public string playerName = "New Player";      // Username
    public int totalWins = 0;                     // Total games won
    public int totalGames = 0;                    // Total games played

    public Dictionary<string, int> deckUsage = new(); // e.g., "Waxy Baby" -> 5 games

    public string selectedAvatar = "default";
    public List<string> unlockedAvatars = new();  // Unlockables
    public string selectedTitle = "Novice";
    public List<string> unlockedTitles = new();

    public int totalCardsPlayed = 0;              // Optional: track usage stats
}
