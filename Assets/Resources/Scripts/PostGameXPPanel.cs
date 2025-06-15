using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PostGameXPPanel : MonoBehaviour
{
    public Transform xpRewardContainer;
    public GameObject xpRewardItemPrefab;
    public TextMeshProUGUI totalXPText;

    public void DisplayResults(List<XPReward> rewards)
    {
        foreach (Transform child in xpRewardContainer) Destroy(child.gameObject);

        int total = 0;
        foreach (var reward in rewards)
        {
            total += reward.XP;
            GameObject item = Instantiate(xpRewardItemPrefab, xpRewardContainer);
            item.GetComponentInChildren<TextMeshProUGUI>().text = $"{reward.Name} +{reward.XP} XP";
        }
        totalXPText.text = $"Total XP: {total}";
    }
}
