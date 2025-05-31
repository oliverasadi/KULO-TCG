using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PreviewLineController : MonoBehaviour
{
    public static PreviewLineController Instance;

    [SerializeField] private Image lineImage;

    private RectTransform lineRect;
    private RectTransform cardRect;
    private RectTransform cellRect;
    private Coroutine revealRoutine;

    void Awake()
    {
        Instance = this;

        if (lineImage == null)
        {
            Debug.LogError("❌ PreviewLineController: Line Image not assigned!");
            return;
        }

        lineRect = lineImage.GetComponent<RectTransform>();
        lineImage.gameObject.SetActive(false); // Start hidden
    }

    public void ShowLine(RectTransform card, RectTransform cell, string validity = "valid")
    {
        Debug.Log("▶ Showing preview line");

        cardRect = card;
        cellRect = cell;

        if (lineImage != null)
        {
            switch (validity)
            {
                case "valid": lineImage.color = Color.green; break;
                case "blocked": lineImage.color = Color.red; break;
                case "disabled": lineImage.color = Color.grey; break;
                default: lineImage.color = Color.cyan; break;
            }

            lineImage.color = new Color(lineImage.color.r, lineImage.color.g, lineImage.color.b, 1f);
        }

        gameObject.SetActive(true);
        lineImage.gameObject.SetActive(true);

        if (revealRoutine != null)
            StopCoroutine(revealRoutine);

        revealRoutine = StartCoroutine(AnimateReveal());

        if (TryGetComponent<PreviewLinePulse>(out var pulser))
            pulser.StartPulse();
    }

    public void HideLine()
    {
        if (revealRoutine != null)
            StopCoroutine(revealRoutine);

        lineImage.gameObject.SetActive(false);
        cardRect = cellRect = null;
    }

    private IEnumerator AnimateReveal()
    {
        if (cardRect == null || cellRect == null || lineRect == null)
            yield break;

        Vector3 startWorld = GetWorldCenter(cardRect);
        Vector3 endWorld = GetWorldCenter(cellRect);

        Vector2 startScreen = RectTransformUtility.WorldToScreenPoint(null, startWorld);
        Vector2 endScreen = RectTransformUtility.WorldToScreenPoint(null, endWorld);

        RectTransform canvasRect = lineRect.parent as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, startScreen, null, out Vector2 startLocal);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, endScreen, null, out Vector2 endLocal);

        Vector2 direction = endLocal - startLocal;
        float length = direction.magnitude;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // Anchor and rotate
        lineRect.anchoredPosition = startLocal;
        lineRect.localRotation = Quaternion.Euler(0, 0, angle);

        float t = 0f;
        float duration = 0.15f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float easedLength = Mathf.SmoothStep(0f, length, t);
            lineRect.sizeDelta = new Vector2(easedLength, 4f);
            yield return null;
        }

        lineRect.sizeDelta = new Vector2(length, 4f);
    }

    private Vector3 GetWorldCenter(RectTransform rt)
    {
        Vector3[] corners = new Vector3[4];
        rt.GetWorldCorners(corners);
        return (corners[0] + corners[2]) * 0.5f;
    }
}
