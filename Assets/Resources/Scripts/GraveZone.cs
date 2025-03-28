using UnityEngine;

public class GraveZone : MonoBehaviour
{
    public static GraveZone instance;
    public Transform graveContainer; // UI container for player's grave cards

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public void AddCardToGrave(GameObject cardObj)
    {
        if (graveContainer == null)
        {
            Debug.LogError("GraveZone: Missing graveContainer.");
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

        // Remove any floating text children from the card.
        // Use GetComponentsInChildren with the 'true' flag to include inactive objects.
        FloatingText[] floatingTexts = cardObj.GetComponentsInChildren<FloatingText>(true);
        foreach (FloatingText ft in floatingTexts)
        {
            Destroy(ft.gameObject);
        }

        // Log the card's power by pulling it from its CardSO.
        CardUI cardUI = cardObj.GetComponent<CardUI>();
        if (cardUI != null && cardUI.cardData != null)
        {
            Debug.Log($"Card moved to grave: {cardObj.name} with power {cardUI.cardData.power}");
        }
        else
        {
            Debug.Log($"Card moved to grave: {cardObj.name} (cardData missing)");
        }
    }
}
