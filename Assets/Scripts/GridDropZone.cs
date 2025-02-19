using UnityEngine;
using UnityEngine.EventSystems;

public class GridDropZone : MonoBehaviour, IDropHandler
{
    public Vector2Int gridPosition; // The position of this slot on the board

    public void OnDrop(PointerEventData eventData)
    {
        CardDragHandler draggedCard = eventData.pointerDrag.GetComponent<CardDragHandler>();

        if (draggedCard != null)
        {
            Debug.Log($"Card placed at {gridPosition.x}, {gridPosition.y}");
            draggedCard.transform.SetParent(transform); // Place the card in the slot
            draggedCard.transform.localPosition = Vector3.zero; // Snap into place
        }
    }
}