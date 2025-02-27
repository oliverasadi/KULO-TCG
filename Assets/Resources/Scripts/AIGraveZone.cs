using UnityEngine;

public class AIGraveZone : MonoBehaviour
{
    public static AIGraveZone instance;
    public Transform graveContainer; // UI container for AI's grave cards

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
            Debug.LogError("AIGraveZone: Missing graveContainer.");
            return;
        }
        // Re-parent the card to the AI grave container.
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

        Debug.Log($"Card moved to AI grave: {cardObj.name}");
    }
}
