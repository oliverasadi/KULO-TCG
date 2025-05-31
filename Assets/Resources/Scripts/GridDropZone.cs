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
        // No drag-based drop supported in this version
        HideHighlights();

        if (PreviewLineController.Instance != null)
            PreviewLineController.Instance.HideLine();
    }

    public void ShowHighlight()
    {
        thisImage.sprite = highlightImage;
    }

    public void HideHighlights()
    {
        thisImage.sprite = normalImage;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // ✅ Only respond when actively selecting a cell (Summon mode)
        if (!GridManager.instance.isCellSelectionMode)
            return;

        // ✅ Get the selected card from GridManager
        CardUI cardUI = GridManager.instance.selectedCardUI;
        if (cardUI == null || cardUI.cardData == null)
            return;

        // ❌ Do not allow selection if occupied
        if (isOccupied)
            return;

        // 🔷 Highlight cell
        thisImage.sprite = highlightImage;

        // 🔵 Show preview line
        if (PreviewLineController.Instance != null)
        {
            RectTransform cardRect = cardUI.GetComponent<RectTransform>();
            RectTransform cellRect = GetComponent<RectTransform>();
            PreviewLineController.Instance.ShowLine(cardRect, cellRect);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!GridManager.instance.isCellSelectionMode)
            return;

        thisImage.sprite = normalImage;

        if (PreviewLineController.Instance != null)
            PreviewLineController.Instance.HideLine();
    }
}