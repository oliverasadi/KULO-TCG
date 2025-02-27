using UnityEngine;
using UnityEngine.EventSystems;

public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private Transform originalParent;

    public CardHandler cardHandler;
    private bool isDragging = false;
    private bool isDroppedOnValidZone;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        cardHandler = GetComponent<CardHandler>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (cardHandler.cardData == null)
        {
            Debug.LogError("❌ Card data is missing in CardHandler!");
            return;
        }
        isDroppedOnValidZone = false;
        isDragging = true;
        originalPosition = rectTransform.position;
        originalParent = transform.parent;
        transform.SetParent(transform.root, false); // Move to top-level UI to avoid masking issues

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f; // Slightly transparent while dragging

        GridManager.instance.GrabCard();
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        rectTransform.position = Input.mousePosition;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        // If we're still under the original parent, the drop wasn't valid
        if (transform.parent == originalParent)
        {
            ResetCardPosition();
        }
        GridManager.instance.ReleaseCard();
    }

    public void CheckDroppedCard(Vector2Int position, Transform cellParent, out bool isOccupied)
    {
        isOccupied = false;
        CardSO cardData = cardHandler.GetCardData();

        // Check if the drop position is valid and the card can be placed
        if (GridManager.instance.IsValidDropPosition(position, out int x, out int y) &&
            GridManager.instance.CanPlaceCard(x, y, cardData))
        {
            Debug.Log($"[CardDragHandler] Attempting to place {cardData.cardName} at {x},{y}");
            // Re-parent this card to the actual cell using PlaceExistingCard
            bool placed = GridManager.instance.PlaceExistingCard(x, y, gameObject, cardData, cellParent);

            if (placed)
            {
                isOccupied = true;
                isDroppedOnValidZone = true;
                CardPreviewManager.Instance.ShowCardPreview(cardData);
            }
            else
            {
                ResetCardPosition();
            }
        }
        else
        {
            ResetCardPosition();
        }
    }

    public void ResetCardPosition()
    {
        rectTransform.position = originalPosition;
        transform.SetParent(originalParent, false);
    }
}
