using System.Collections.Generic;

[System.Serializable]
public class PlayerProfile
{
    public string playerName = "New Player";       // Username
    public int totalWins = 0;                      // Total games won
    public int totalGames = 0;                     // Total games played

    public Dictionary<string, int> deckUsage = new(); // e.g., "Waxy Baby" -> 5 games

    public string selectedAvatar = "avatar_default"; // ✅ CORRECT
    public List<string> unlockedAvatars = new();   // Unlockable avatars

    public string selectedTitle = "Novice";        // Display title
    public List<string> unlockedTitles = new();    // Unlockable titles

    public int totalCardsPlayed = 0;               // Tracks how many cards were played
    public string lastDeckPlayed = "";             // The last deck the player used

    public string avatarImageName = "avatar_default"; // Used to load sprite from Resources/Avatars/
}
