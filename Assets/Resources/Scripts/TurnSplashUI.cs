using UnityEngine;
using TMPro;
using DG.Tweening;

public class TurnSplashUI : MonoBehaviour
{
    public TextMeshProUGUI splashText;
    public float moveDuration = 0.5f;   // Slide speed
    public float holdDuration = 1f;     // Time to stay in center
    public Ease easeIn = Ease.OutCubic; // Ease for sliding in
    public Ease easeOut = Ease.InCubic; // Ease for sliding out

    private RectTransform rectTransform;
    private Vector3 offRight;
    private Vector3 center;
    private Vector3 offLeft;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // Call this to start the splash animation
    public void Setup(string message)
    {
        if (splashText != null)
            splashText.text = message;

        RectTransform canvasRect = rectTransform.parent as RectTransform;
        float canvasWidth = canvasRect.rect.width;

        center = Vector3.zero;
        offRight = new Vector3(canvasWidth, 0f, 0f);
        offLeft = new Vector3(-canvasWidth, 0f, 0f);

        // Start off-screen right
        rectTransform.anchoredPosition = offRight;

        // Build DOTween sequence
        Sequence seq = DOTween.Sequence();
        seq.Append(rectTransform.DOAnchorPos(center, moveDuration).SetEase(easeIn));
        seq.AppendInterval(holdDuration);
        seq.Append(rectTransform.DOAnchorPos(offLeft, moveDuration).SetEase(easeOut));
        seq.OnComplete(() => Destroy(gameObject));
    }
}
