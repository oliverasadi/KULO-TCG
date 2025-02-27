using UnityEngine;
using TMPro;

public class DeckZone : MonoBehaviour
{
    public static DeckZone instance;
    public TextMeshProUGUI deckCountText; // Assign a UI Text element in the Inspector

    private int deckCount;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public void UpdateDeckCount(int count)
    {
        deckCount = count;
        if (deckCountText != null)
            deckCountText.text = $"Deck: {deckCount}";
    }
}
