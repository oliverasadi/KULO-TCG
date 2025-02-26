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
        transform.SetParent(transform.root); // Move to top-level UI to avoid masking issues

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f; // Make it slightly transparent while dragging

        GridManager.instance.GrabCard(); // Show available drop areas
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
        if (!isDroppedOnValidZone)
        {
            ResetCardPosition(); // Revert position if not dropped on a valid zone
        }
        GridManager.instance.ReleaseCard();
    }

    public void CheckDroppedCard(Vector2Int position, Transform parent, out bool isOccupied)
    {
        isOccupied = false;
        // Check if the drop position is valid and the card can be placed (including evolution/sacrifice checks)
        if (GridManager.instance.IsValidDropPosition(position, out int x, out int y) &&
            GridManager.instance.CanPlaceCard(x, y, cardHandler.GetCardData()))
        {
            Debug.Log("VALID POSITION");
            // Attempt to place the card; this call will handle sacrifice requirements if needed.
            GridManager.instance.PlaceCard(x, y, cardHandler.GetCardData());

            // Verify if placement was successful by checking the grid.
            CardSO[,] grid = GridManager.instance.GetGrid();
            if (grid[x, y] == cardHandler.GetCardData())
            {
                // Placement successful
                transform.SetParent(parent);
                transform.localPosition = Vector3.zero;
                isOccupied = true;
                isDroppedOnValidZone = true;

                // Trigger the overlay preview for the played card.
                CardPreviewManager.Instance.ShowCardPreview(cardHandler.GetCardData());
            }
            else
            {
                // Placement failed (e.g. sacrifice requirements were not met)
                ResetCardPosition();
            }
        }
        else
        {
            ResetCardPosition();
        }
        GridManager.instance.ReleaseCard();
    }

    public void ResetCardPosition()
    {
        rectTransform.position = originalPosition;
        transform.SetParent(originalParent);
    }
}
