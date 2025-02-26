using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GridDropZone : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public Vector2Int gridPosition;
    public Sprite highlightImage;
    public Sprite normalImage;
    public Image thisImage;
    public bool isOccupied;

    public void OnDrop(PointerEventData eventData)
    {
        CardDragHandler draggedCard = eventData.pointerDrag.GetComponent<CardDragHandler>();

        if (draggedCard != null)
        {
            // Attempt to drop the card at this grid position.
            draggedCard.CheckDroppedCard(gridPosition, transform, out bool _isOccupied);
            isOccupied = _isOccupied;
            HideHighlights();

            // If the drop was successful, trigger the overlay preview.
            if (isOccupied)
            {
                CardPreviewManager.Instance.ShowCardPreview(draggedCard.cardHandler.GetCardData());
            }
        }
    }

    public void ShowHighlight()
    {
        if (!isOccupied)
        {
            thisImage.sprite = highlightImage;
        }
    }

    public void HideHighlights()
    {
        thisImage.sprite = normalImage;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (GridManager.instance.isHoldingCard)
        {
            ShowHighlight();
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideHighlights();
    }
}
