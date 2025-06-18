using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

public class ButtonHoverScaler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private RectTransform rectTransform;
    private Tween scaleTween;

    [SerializeField] private float scaleUp = 1.1f;
    [SerializeField] private float duration = 0.2f;

    private bool hasPlayedHover = false;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        scaleTween?.Kill();
        scaleTween = rectTransform.DOScale(scaleUp, duration)
            .SetEase(Ease.OutBack);

        // Play hover sound once per hover entry
        if (!hasPlayedHover)
        {
            AudioManager.instance?.PlayButtonHoverSound();
            hasPlayedHover = true;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        scaleTween?.Kill();
        scaleTween = rectTransform.DOScale(1f, duration)
            .SetEase(Ease.OutQuad);

        hasPlayedHover = false;
    }
}
