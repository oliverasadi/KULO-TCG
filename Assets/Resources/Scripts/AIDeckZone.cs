using UnityEngine;
using TMPro;

public class AIDeckZone : MonoBehaviour
{
    public static AIDeckZone instance;
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
            deckCountText.text = $"AI Deck: {deckCount}";
    }
}
