using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

public class SelectableGraveCard : MonoBehaviour, IPointerClickHandler
{
    private GameObject realCard;
    private Outline outline;
    private Coroutine pulseRoutine;

    public void Setup(GameObject originalCard)
    {
        realCard = originalCard;

        // ✅ Add yellow glow outline
        outline = gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(Color.yellow.r, Color.yellow.g, Color.yellow.b, 1f);
        outline.effectDistance = new Vector2(7f, 7f);
        outline.useGraphicAlpha = false; // ensures alpha doesn't block raycasts

        // ✅ Make sure raycast works properly
        var image = GetComponent<Image>();
        if (image != null)
            image.raycastTarget = true;

        // ✅ Start pulsing
        pulseRoutine = StartCoroutine(PulseOutline());
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (realCard != null && GraveyardSelectionManager.Instance != null)
        {
            Debug.Log($"✅ Graveyard selection active. Passing {realCard.name} to selector.");
            GraveyardSelectionManager.Instance.SelectCard(realCard);
        }
    }

    private IEnumerator PulseOutline()
    {
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime;

            // Glow: alpha and thickness pulse
            float alpha = 0.8f + 0.2f * Mathf.Sin(t * 3f);
            float thickness = 6f + 2f * Mathf.Sin(t * 3f);

            outline.effectColor = new Color(Color.yellow.r, Color.yellow.g, Color.yellow.b, alpha);
            outline.effectDistance = new Vector2(thickness, thickness);

            yield return null;
        }
    }

    private void OnDestroy()
    {
        if (pulseRoutine != null)
            StopCoroutine(pulseRoutine);
        if (outline != null)
            Destroy(outline);
    }
}
