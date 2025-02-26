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

        // Get the card data from the dragged card.
        CardSO selectedCard = draggedCard.cardHandler.GetCardData();

        // --- Check for Sacrifice Requirements ---
        if (selectedCard.requiresSacrifice &&
            selectedCard.sacrificeRequirements != null &&
            selectedCard.sacrificeRequirements.Count > 0)
        {
            CardSO[,] currentField = GridManager.instance.GetGrid();

            // For each requirement in the list, make sure the field has enough matching cards.
            foreach (var req in selectedCard.sacrificeRequirements)
            {
                int foundCount = 0;
                // Count cards on the field that satisfy this requirement.
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (currentField[i, j] != null)
                        {
                            bool match = req.matchByCreatureType
                                ? (currentField[i, j].creatureType == req.requiredCardName)
                                : (currentField[i, j].cardName == req.requiredCardName);

                            if (match)
                                foundCount++;
                        }
                    }
                }
                // If not enough matches, reject the play.
                if (foundCount < req.count)
                {
                    Debug.Log($"Cannot summon {selectedCard.cardName}. " +
                              $"Requirement not met for '{req.requiredCardName}' (need {req.count}, found {foundCount}).");
                    draggedCard.ResetCardPosition();
                    return;
                }
            }
        }

        // --- Attempt to Drop the Card ---
        draggedCard.CheckDroppedCard(gridPosition, transform, out bool _isOccupied);
        isOccupied = _isOccupied;
        HideHighlights();

        // If the drop was successful, trigger the overlay preview.
        if (isOccupied)
        {
            CardPreviewManager.Instance.ShowCardPreview(selectedCard);
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
