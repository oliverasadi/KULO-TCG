using System.Collections.Generic;
using UnityEngine;

public class XPTracker : MonoBehaviour
{
    public static XPTracker instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // Prevent duplicate
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject); // ✅ Persist between scenes
    }

    // Tracked data
    public bool playerWon;
    public int playerRoundsWon;
    public int opponentRoundsWon;
    public int totalTurns = 0;
    public int cardsPlayed = 0;
    public bool signatureCardPlayed = false;
    public bool evolutionCardPlayed = false;
    public bool lineBlockOccurred = false;
    public bool wasDown0to2 = false;

    public void OnCardPlayed(CardSO card)
    {
        cardsPlayed++;
        if (card.cardName == ProfileManager.instance.currentProfile.signatureCardName)
            signatureCardPlayed = true;
        if (card.baseOrEvo == CardSO.BaseOrEvo.Evolution)
            evolutionCardPlayed = true;
    }

    public void OnLineBlocked()
    {
        lineBlockOccurred = true;
        Debug.Log("🧱 XPTracker: Line block detected.");
    }

    public List<XPReward> EvaluateXP()
    {
        List<XPReward> rewards = new();

        if (playerWon)
        {
            rewards.Add(new XPReward("Champion’s Crown", 100));
            if (opponentRoundsWon == 0) rewards.Add(new XPReward("Untouched Legend", 25));
            if (wasDown0to2 && playerRoundsWon > opponentRoundsWon) rewards.Add(new XPReward("Against All Odds", 25));
            if (totalTurns < 10) rewards.Add(new XPReward("Swift Duelist", 15));
            if (evolutionCardPlayed) rewards.Add(new XPReward("Evolution Ascendant", 15));
        }

        if (cardsPlayed >= 21) rewards.Add(new XPReward("Master of the Draw", 21));
        if (signatureCardPlayed) rewards.Add(new XPReward("Seal the Deal", 30));
        if (lineBlockOccurred) rewards.Add(new XPReward("Line Breaker", 5));

        return rewards;
    }
}

public class XPReward
{
    public string Name;
    public int XP;
    public XPReward(string name, int xp) { Name = name; XP = xp; }
}
