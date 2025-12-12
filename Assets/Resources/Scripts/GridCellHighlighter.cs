using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GridCellHighlighter : MonoBehaviour
{
    [Header("Visuals")]
    public Outline outlineComponent;
    public GameObject highlightObject;

    [Header("Flash")]
    public float highlightDuration = 0.5f;

    // Used by selection flows (sacrifice / choose-cell etc)
    public bool isSacrificeHighlight = false;

    // Stored "previous state" so we can restore properly after flash/selection
    // (this is really "hasStoredState", not "had a highlight")
    private bool hasStoredState = false;
    private bool storedOutlineEnabled = false;
    private bool storedHighlightEnabled = false;
    private Color storedOutlineColor;
    private Color storedHighlightColor;

    // Defaults
    private Color defaultOutlineColor;
    private Color defaultHighlightColor;

    private Image _highlightImage;

    private void Awake()
    {
        if (outlineComponent != null)
        {
            defaultOutlineColor = outlineComponent.effectColor;
            outlineComponent.enabled = false;
        }

        if (highlightObject != null)
        {
            _highlightImage = highlightObject.GetComponent<Image>();
            if (_highlightImage != null)
                defaultHighlightColor = _highlightImage.color;

            highlightObject.SetActive(false);
        }
    }

    public void ClearStoredPersistentHighlight()
    {
        hasStoredState = false;
        storedOutlineEnabled = false;
        storedHighlightEnabled = false;
    }

    public void FlashHighlight(Color flashColor)
    {
        // Important: kill any previous flash so it can't restore later
        StopAllCoroutines();
        StartCoroutine(HighlightRoutine(flashColor));
    }

    private IEnumerator HighlightRoutine(Color flashColor)
    {
        if (outlineComponent != null)
        {
            outlineComponent.effectColor = flashColor;
            outlineComponent.enabled = true;
        }

        if (highlightObject != null)
        {
            if (_highlightImage != null)
                _highlightImage.color = flashColor;

            highlightObject.SetActive(true);
        }

        yield return new WaitForSeconds(highlightDuration);

        RestoreHighlight();
    }

    public void ResetHighlight()
    {
        // Important: stop flashes so they can't re-apply colour after we clear
        StopAllCoroutines();

        if (outlineComponent != null)
        {
            outlineComponent.enabled = false;
            outlineComponent.effectColor = defaultOutlineColor;
        }

        if (highlightObject != null)
        {
            if (_highlightImage != null)
                _highlightImage.color = defaultHighlightColor;

            highlightObject.SetActive(false);
        }

        isSacrificeHighlight = false;
        ClearStoredPersistentHighlight();
    }

    public void SetPersistentHighlight(Color persistentColor)
    {
        // Capture current state ONCE so we can restore later
        // (even if the current state is "no highlight")
        if (!hasStoredState)
        {
            storedOutlineEnabled = (outlineComponent != null && outlineComponent.enabled);
            storedHighlightEnabled = (highlightObject != null && highlightObject.activeSelf);

            storedOutlineColor = outlineComponent != null ? outlineComponent.effectColor : defaultOutlineColor;
            storedHighlightColor = _highlightImage != null ? _highlightImage.color : defaultHighlightColor;

            hasStoredState = true;
        }

        if (outlineComponent != null)
        {
            outlineComponent.effectColor = persistentColor;
            outlineComponent.enabled = true;
        }

        if (highlightObject != null)
        {
            if (_highlightImage != null)
                _highlightImage.color = persistentColor;

            highlightObject.SetActive(true);
        }

        isSacrificeHighlight = true;
    }

    public void RestoreHighlight()
    {
        StopAllCoroutines();

        if (!hasStoredState)
        {
            ResetHighlight();
            return;
        }

        if (outlineComponent != null)
        {
            outlineComponent.effectColor = storedOutlineColor;
            outlineComponent.enabled = storedOutlineEnabled;
        }

        if (highlightObject != null)
        {
            if (_highlightImage != null)
                _highlightImage.color = storedHighlightColor;

            highlightObject.SetActive(storedHighlightEnabled);
        }

        isSacrificeHighlight = false;
    }

    // Strong “make it definitely clean” call (used when a cell becomes empty)
    public void HardReset()
    {
        ResetHighlight();
    }

    public bool HasStoredPersistentHighlight => hasStoredState;
}
