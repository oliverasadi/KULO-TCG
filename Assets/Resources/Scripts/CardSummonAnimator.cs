using DG.Tweening;
using UnityEngine;

public static class CardSummonAnimator
{
    /// <summary>
    /// Animates a card from its current spot into the target cell.
    /// Uses world-space DOMove so we avoid any RectTransform anchoring issues.
    /// </summary>
    public static void AnimateCardToCell(CardUI cardUI, Transform cellTransform, System.Action onComplete)
    {
        // 1) Grab the card's RectTransform
        RectTransform cardRect = cardUI.GetComponent<RectTransform>();

        // 2) Find OverlayCanvas in the scene
        GameObject overlayCanvas = GameObject.Find("OverlayCanvas");
        if (overlayCanvas == null)
        {
            Debug.LogWarning("OverlayCanvas not found – skipping animation.");
            onComplete?.Invoke();
            return;
        }

        // 3) Reparent to overlay, KEEPING world position so it doesn't jump
        cardRect.SetParent(overlayCanvas.transform, worldPositionStays: true);

        // 4) Compute cell's world position (center of its RectTransform)
        Vector3 targetWorldPos = cellTransform.position;

        // 5) Tween the card's world position into the cell
        float duration = 0.5f;
        cardRect.DOMove(targetWorldPos, duration)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                // 6) Once done, parent it under the cell and center it in local
                cardRect.SetParent(cellTransform, worldPositionStays: false);
                cardRect.anchorMin = cardRect.anchorMax = cardRect.pivot = new Vector2(0.5f, 0.5f);
                cardRect.anchoredPosition = Vector2.zero;

                // 7) Finally invoke the game‐logic callback
                onComplete?.Invoke();
            });
    }
}
