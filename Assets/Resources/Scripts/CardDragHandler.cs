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
        if (cardHandler.cardData== null)
        {
            Debug.LogError("❌ Card data is missing in CardHandler!");
            return;
        }
        isDroppedOnValidZone = false;
        isDragging = true;
        originalPosition = rectTransform.position;
        originalParent = transform.parent;
        transform.SetParent(transform.root); // Move to top level UI to avoid masking issues

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f; // Make it slightly transparent when dragging

        GridManager.instance.GrabCard(); // ✅ Show available drop areas
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
            ResetCardPosition(); // Go back if not on a valid drop zone
        }

        GridManager.instance.ReleaseCard();

    }

    public void CheckDroppedCard(Vector2Int position, Transform parent, out bool isOccupied)
    {
        isOccupied = false;
        if (GridManager.instance.IsValidDropPosition(position, out int x, out int y)
            && GridManager.instance.CanPlaceCard(x, y, cardHandler.GetCardData()))
        {
            Debug.Log("VALID POSITION");
            GridManager.instance.PlaceCard(x, y, cardHandler.GetCardData());

            transform.SetParent(parent);
            transform.localPosition = Vector3.zero;
            isOccupied = true;
            isDroppedOnValidZone = true;
        }
        else
        {
            ResetCardPosition();
        }
        GridManager.instance.ReleaseCard();

    }

    private void ResetCardPosition()
    {
        rectTransform.position = originalPosition;
        transform.SetParent(originalParent);
    }
}
