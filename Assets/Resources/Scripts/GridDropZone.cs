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
        if (draggedCard == null) return;

        // Let the CardDragHandler handle placing the card onto this cell
        draggedCard.CheckDroppedCard(gridPosition, transform, out bool _isOccupied);
        isOccupied = _isOccupied;
        HideHighlights();
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
            CardDragHandler draggingCard = eventData.pointerDrag?.GetComponent<CardDragHandler>();
            if (draggingCard != null && draggingCard.cardHandler.cardData.category == CardSO.CardCategory.Spell)
            {
                // If dragging a spell, allow highlight on valid targets (even if occupied)
                CardSO spellData = draggingCard.cardHandler.cardData;
                bool needsCreature = spellData.requiresTargetCreature;
                int currentPlayer = TurnManager.instance.GetCurrentPlayer();

                if (!isOccupied)
                {
                    // Highlight empty cell only if spell *does not* require a creature target
                    if (!needsCreature)
                        thisImage.sprite = highlightImage;
                }
                else
                {
                    // Cell is occupied; highlight if spell allows targeting a creature here
                    if (!needsCreature)
                    {
                        // Spell can be cast on any cell (empty or occupied)
                        thisImage.sprite = highlightImage;
                    }
                    else
                    {
                        // Spell requires a creature – highlight only if the occupant is the current player’s creature
                        // (Pamyu Poo, for example, should target your own creature)
                        if (GridManager.instance.IsOwnedByPlayer(gridPosition.x, gridPosition.y, currentPlayer))
                            thisImage.sprite = highlightImage;
                    }
                }
            }
            else
            {
                // Non-spell cards: only highlight if cell is free (original behavior)
                if (!isOccupied)
                    thisImage.sprite = highlightImage;
            }
        }
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        HideHighlights();
    }
}
