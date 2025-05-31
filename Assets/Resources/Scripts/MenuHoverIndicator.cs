using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class MenuHoverIndicator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject arrowIcon;
    private RectTransform arrowRect;
    private Coroutine pulseRoutine;

    private bool wasSelectedLastFrame = false;

    void Start()
    {
        if (arrowIcon != null)
            arrowRect = arrowIcon.GetComponent<RectTransform>();
    }

    void Update()
    {
        if (arrowIcon == null) return;

        bool isSelected = EventSystem.current.currentSelectedGameObject == gameObject;

        if (isSelected && !wasSelectedLastFrame)
        {
            arrowIcon.SetActive(true);

            if (pulseRoutine != null)
                StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseIn());
        }
        else if (!isSelected && wasSelectedLastFrame)
        {
            arrowIcon.SetActive(false);
        }

        wasSelectedLastFrame = isSelected;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (arrowIcon != null)
        {
            arrowIcon.SetActive(true);

            if (pulseRoutine != null)
                StopCoroutine(pulseRoutine);
            pulseRoutine = StartCoroutine(PulseIn());
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (arrowIcon != null)
        {
            arrowIcon.SetActive(false);
        }
    }

    private IEnumerator PulseIn()
    {
        float duration = 0.2f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.zero;
        Vector3 peakScale = Vector3.one * 1.05f;
        Vector3 endScale = Vector3.one;

        if (arrowRect != null)
            arrowRect.localScale = startScale;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float smoothT = Mathf.SmoothStep(0, 1, t);
            Vector3 currentScale = Vector3.Lerp(startScale, peakScale, smoothT);
            if (arrowRect != null)
                arrowRect.localScale = currentScale;
            yield return null;
        }

        if (arrowRect != null)
            arrowRect.localScale = endScale;
    }
}
