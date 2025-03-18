using UnityEngine;
using TMPro;
using UnityEngine.UI; // only if you are using Unity UI, not necessary for TMP

public class EvoAnnouncementUI : MonoBehaviour
{
    public static EvoAnnouncementUI instance;

    [Header("UI References")]
    public CanvasGroup canvasGroup;        // The panel’s CanvasGroup
    public TextMeshProUGUI announcementText; // The text component

    [Header("Animation Settings")]
    public float fadeDuration = 1f;
    public float displayDuration = 2f; // How long text stays visible

    private bool isShowing = false;

    void Awake()
    {
        // Simple singleton approach
        if (instance == null) instance = this;
        else Destroy(gameObject);

        // Make sure the panel starts invisible
        canvasGroup.alpha = 0f;
    }

    public void ShowEvolutionAnnouncement(string baseName, string evoName)
    {
        // e.g. "CARD EVOLUTION - 'Base Cat' > 'Mega Cat'"
        string textToDisplay = $"CARD EVOLUTION!\n{baseName} → {evoName}";
        announcementText.text = textToDisplay;

        if (!isShowing)
            StartCoroutine(FadeRoutine());
    }

    private System.Collections.IEnumerator FadeRoutine()
    {
        isShowing = true;
        // Fade in
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Display for a while
        yield return new WaitForSeconds(displayDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;

        isShowing = false;
    }
}
