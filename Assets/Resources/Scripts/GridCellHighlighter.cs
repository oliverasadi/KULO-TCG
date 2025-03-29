using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GridCellHighlighter : MonoBehaviour
{
    // Outline component for border glow.
    public Outline outlineComponent;
    // Child object for a highlight background.
    public GameObject highlightObject;
    // Duration for the temporary highlight effect.
    public float highlightDuration = 0.5f;

    // Field to mark the cell as a sacrifice highlight.
    public bool isSacrificeHighlight = false;

    // Fields to store a pre-existing persistent highlight.
    private bool hadPersistentHighlight = false;
    private Color storedOutlineColor;
    private Color storedHighlightColor;

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
            Image img = highlightObject.GetComponent<Image>();
            if (img != null)
                defaultHighlightColor = img.color;
            highlightObject.SetActive(false);
        }
    }

    /// <summary>
    /// Temporarily flashes the highlight with the given color.
    /// After the duration, it restores to the previously stored persistent state if available.
    /// </summary>
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

        // Instead of resetting completely, restore any persistent highlight.
        RestoreHighlight();
    }

    /// <summary>
    /// Immediately resets the cell's visuals to their default state.
    /// </summary>
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
        isSacrificeHighlight = false;
        hadPersistentHighlight = false;
    }

    /// <summary>
    /// Sets a persistent highlight on the cell with the specified color.
    /// This highlight will remain until you manually clear or restore it.
    /// </summary>
    public void SetPersistentHighlight(Color persistentColor)
    {
        // Before applying the new persistent highlight, store the current state if not already stored.
        if (!hadPersistentHighlight)
        {
            if (outlineComponent != null && outlineComponent.enabled)
            {
                storedOutlineColor = outlineComponent.effectColor;
            }
            if (highlightObject != null && highlightObject.activeSelf)
            {
                Image img = highlightObject.GetComponent<Image>();
                if (img != null)
                {
                    storedHighlightColor = img.color;
                }
            }
            hadPersistentHighlight = true;
        }

        if (outlineComponent != null)
        {
            outlineComponent.effectColor = persistentColor;
            outlineComponent.enabled = true;
        }
        if (highlightObject != null)
        {
            Image img = highlightObject.GetComponent<Image>();
            if (img != null)
            {
                img.color = persistentColor;
            }
            highlightObject.SetActive(true);
        }
        isSacrificeHighlight = true;
    }

    /// <summary>
    /// Restores the highlight to the previously stored persistent state,
    /// or resets to default if none was stored.
    /// </summary>
    public void RestoreHighlight()
    {
        if (hadPersistentHighlight)
        {
            if (outlineComponent != null)
            {
                outlineComponent.effectColor = storedOutlineColor;
                outlineComponent.enabled = true;
            }
            if (highlightObject != null)
            {
                highlightObject.SetActive(true);
                Image img = highlightObject.GetComponent<Image>();
                if (img != null)
                {
                    img.color = storedHighlightColor;
                }
            }
        }
        else
        {
            ResetHighlight();
        }
    }

    // Expose whether this cell had a persistent highlight before temporary selection.
    public bool HasStoredPersistentHighlight
    {
        get { return hadPersistentHighlight; }
    }
}
