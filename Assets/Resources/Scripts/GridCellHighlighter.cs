using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GridCellHighlighter : MonoBehaviour
{
    // Outline component for border glow.
    public Outline outlineComponent;
    // Child object for a highlight background.
    public GameObject highlightObject;
    // Duration for the highlight effect.
    public float highlightDuration = 0.5f;

    // Cached default values.
    private Color defaultOutlineColor;
    private Color defaultHighlightColor;

    void Awake()
    {
        if (outlineComponent != null)
        {
            defaultOutlineColor = outlineComponent.effectColor;
            outlineComponent.enabled = false;
        }
        if (highlightObject != null)
        {
            // If the highlight object has an Image, store its default color.
            Image img = highlightObject.GetComponent<Image>();
            if (img != null)
                defaultHighlightColor = img.color;
            highlightObject.SetActive(false);
        }
    }

    // Flash the highlight with the given color.
    public void FlashHighlight(Color flashColor)
    {
        StartCoroutine(HighlightRoutine(flashColor));
    }

    private IEnumerator HighlightRoutine(Color flashColor)
    {
        // Set outline effect color.
        if (outlineComponent != null)
        {
            outlineComponent.effectColor = flashColor;
            outlineComponent.enabled = true;
        }

        // Set the child highlight image color.
        if (highlightObject != null)
        {
            Image img = highlightObject.GetComponent<Image>();
            if (img != null)
            {
                img.color = flashColor;
            }
            highlightObject.SetActive(true);
        }

        yield return new WaitForSeconds(highlightDuration);

        // Revert changes.
        ResetHighlight();
    }

    // Immediately resets the cell's visuals to their default state.
    public void ResetHighlight()
    {
        if (outlineComponent != null)
        {
            outlineComponent.enabled = false;
            outlineComponent.effectColor = defaultOutlineColor;
        }
        if (highlightObject != null)
        {
            highlightObject.SetActive(false);
            Image img = highlightObject.GetComponent<Image>();
            if (img != null)
            {
                img.color = defaultHighlightColor;
            }
        }
    }
}
