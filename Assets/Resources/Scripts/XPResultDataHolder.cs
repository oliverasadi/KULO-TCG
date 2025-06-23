using System.Collections.Generic;
using UnityEngine;

public class XPResultDataHolder : MonoBehaviour
{
    public static XPResultDataHolder instance;

    public List<XPReward> rewards;
    public int totalXP;
    public bool leveledUp;
    public List<string> cardsPlayed = new List<string>();

    public List<MemoryCardData> memoryCardsToUnlock = new List<MemoryCardData>(); // ✅ New: memory unlock queue

    public SplashBackgroundType backgroundType = SplashBackgroundType.MrWax; // Default

    public enum SplashBackgroundType
    {
        MrWax,
        MrWaxWithRedSeal,
        Nekomata,
        NekomataWithCatTriFecta,
        // Add more as needed
    }

    void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[XPResultDataHolder] Singleton instance created and will persist.");
        }
    }

    public void Set(List<XPReward> rewardList, SplashBackgroundType splash)
    {
        rewards = rewardList;
        backgroundType = splash;

        totalXP = 0;
        foreach (var r in rewards)
            totalXP += r.XP;
    }

    public void Clear()
    {
        rewards = new List<XPReward>();
        totalXP = 0;
        backgroundType = SplashBackgroundType.MrWax;
        memoryCardsToUnlock.Clear(); // ✅ Clear unlocks when resetting
    }
}
