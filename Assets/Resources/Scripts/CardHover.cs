using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class UIOnHoverEvent : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Vector3 originalScale = Vector3.one;
    public float scaleFactor = 1.2f;
    public float animationSpeed = 0.2f; // Speed of the transition

    void Start()
    {
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(originalScale * scaleFactor));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StopAllCoroutines();
        StartCoroutine(ScaleTo(originalScale));
    }

    IEnumerator ScaleTo(Vector3 targetScale)
    {
        float time = 0;
        Vector3 startScale = transform.localScale;

        while (time < animationSpeed)
        {
            transform.localScale = Vector3.Lerp(startScale, targetScale, time / animationSpeed);
            time += Time.deltaTime;
            yield return null;
        }
        transform.localScale = targetScale;
    }

    // ✅ Add this so other scripts can reset the baseline scale after placement
    public void ForceResetOriginalScale(Vector3 newOriginal)
    {
        originalScale = newOriginal;
        transform.localScale = newOriginal;
    }
}
