using UnityEngine;
using TMPro;

public class PlayerZones : MonoBehaviour
{
    public TextMeshProUGUI deckCountText; // Assign a UI Text element in the Inspector
    public Transform graveContainer; // UI container for player's grave cards

    private int deckCount;

    //Deck Zone

    public void UpdateDeckCount(int count)
    {
        deckCount = count;
        if (deckCountText != null)
            deckCountText.text = $"Deck: {deckCount}";
    }

    //Grave Zone

    public void AddCardToGrave(GameObject cardObj)
    {
        if (graveContainer == null)
        {
            Debug.LogError("Player Missing graveContainer.");
            return;
        }
        // Re-parent the card to the grave container.
        cardObj.transform.SetParent(graveContainer, false);
        // Remove any drag functionality.
        CardDragHandler dragHandler = cardObj.GetComponent<CardDragHandler>();
        if (dragHandler != null)
            Destroy(dragHandler);
        // Reset its position relative to the grave container.
        RectTransform rt = cardObj.GetComponent<RectTransform>();
        if (rt != null)
            rt.anchoredPosition = Vector2.zero;
        else
            cardObj.transform.localPosition = Vector3.zero;

        Debug.Log($"Card moved to grave: {cardObj.name}");
    }
}

