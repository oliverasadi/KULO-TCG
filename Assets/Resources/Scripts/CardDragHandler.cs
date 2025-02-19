using UnityEngine;
using UnityEngine.EventSystems;

public class CardDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector3 originalPosition;
    private Transform originalParent;

    private CardSO cardData;
    private bool isDragging = false;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
        cardData = GetComponent<CardHandler>().GetCardData();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (cardData == null)
        {
            Debug.LogError("❌ Card data is missing in CardHandler!");
            return;
        }

        isDragging = true;
        originalPosition = rectTransform.position;
        originalParent = transform.parent;
        transform.SetParent(transform.root); // Move to top level UI to avoid masking issues

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.7f; // Make it slightly transparent when dragging

        GridManager.instance.ShowGridHighlights(); // ✅ Show available drop areas
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

        if (GridManager.instance.IsValidDropPosition(Input.mousePosition, out int x, out int y))
        {
            // ✅ Attempt to place the card on the grid
            GridManager.instance.PlaceCard(x, y, cardData);
            GridManager.instance.HideGridHighlightAt(x, y); // ✅ Hide highlight at placed position

            Destroy(gameObject); // Remove from hand after placement
        }
        else
        {
            // ❌ Invalid placement, return to original position
            rectTransform.position = originalPosition;
            transform.SetParent(originalParent);
        }

        GridManager.instance.HideGridHighlights(); // ✅ Hide all highlights
    }
}
