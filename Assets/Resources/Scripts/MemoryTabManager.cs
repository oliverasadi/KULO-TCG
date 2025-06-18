using UnityEngine;

public class MemoryTabManager : MonoBehaviour
{
    public Transform gridParent; // should be your GridLayoutGroup
    public GameObject memoryCardPrefab;
    public MemoryCardData[] artCards;
    public MemoryCardData[] loreCards;
    public MemoryCardData[] cutsceneCards;

    public void LoadTab(MemoryCardData[] cards)
    {
        foreach (Transform child in gridParent)
            Destroy(child.gameObject); // clear old cards

        foreach (MemoryCardData card in cards)
        {
            GameObject newCard = Instantiate(memoryCardPrefab, gridParent);
            newCard.GetComponent<MemoryCardUI>().Setup(card);
        }
    }

    // 👇 These show up in Unity's Button OnClick dropdown
    public void LoadArtTab()
    {
        LoadTab(artCards);
    }

    public void LoadLoreTab()
    {
        LoadTab(loreCards);
    }

    public void LoadCutsceneTab()
    {
        LoadTab(cutsceneCards);
    }
}
