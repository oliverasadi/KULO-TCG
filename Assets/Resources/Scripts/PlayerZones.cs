using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class PlayerZones : MonoBehaviour
{
    public TextMeshProUGUI deckCountText; // Assign a UI Text element in the Inspector
    public Transform graveContainer;      // UI container for player's grave cards

    private int deckCount;

    // ✅ Internal graveyard tracking
    private List<GameObject> graveyard = new List<GameObject>();

    // Updates the deck count UI.
    public void UpdateDeckCount(int count)
    {
        deckCount = count;
        if (deckCountText != null)
            deckCountText.text = $"Deck: {deckCount}";
    }

    // Moves a card to the grave zone.
    public void AddCardToGrave(GameObject cardObj)
    {
        if (graveContainer == null)
        {
            Debug.LogError("PlayerZones: Missing graveContainer.");
            return;
        }

        // Remove any floating text objects attached to the card.
        FloatingText[] floatingTexts = cardObj.GetComponentsInChildren<FloatingText>(true);
        foreach (FloatingText ft in floatingTexts)
        {
            Destroy(ft.gameObject);
        }

        // Re-parent the card to the grave container.
        cardObj.transform.SetParent(graveContainer, false);

        // Remove any drag functionality.
        CardDragHandler dragHandler = cardObj.GetComponent<CardDragHandler>();
        if (dragHandler != null)
            Destroy(dragHandler);

        // Reset the card's position.
        RectTransform rt = cardObj.GetComponent<RectTransform>();
        if (rt != null)
            rt.anchoredPosition = Vector2.zero;
        else
            cardObj.transform.localPosition = Vector3.zero;

        // ✅ Mark the card as in the graveyard
        CardUI ui = cardObj.GetComponent<CardUI>();
        if (ui != null)
        {
            ui.isInGraveyard = true;
        }

        // ✅ Track it internally
        if (!graveyard.Contains(cardObj))
        {
            graveyard.Add(cardObj);
        }

        Debug.Log($"Card moved to grave: {cardObj.name}");
    }

    // ✅ Check if a card is in the graveyard
    public bool IsInGraveyard(GameObject cardObj)
    {
        return graveyard.Contains(cardObj);
    }

    // ✅ Get all graveyard cards
    public List<GameObject> GetGraveyardCards()
    {
        return graveyard;
    }
}
