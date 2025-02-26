using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardPreviewManager : MonoBehaviour
{
    public static CardPreviewManager Instance;

    public CanvasGroup previewCanvasGroup;
    public Image previewCardImage;
    public TextMeshProUGUI previewCardNameText;
    public TextMeshProUGUI previewCardDescriptionText;
    public TextMeshProUGUI previewCardPowerText;
    public TextMeshProUGUI previewExtraDetailsText;

    public float previewDisplayDuration = 1.0f; // How long to display the card preview.
    public float previewFadeDuration = 0.5f;      // Duration for fade-out.

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Hide the preview panel when the game starts.
        if (previewCanvasGroup != null)
        {
            previewCanvasGroup.alpha = 0f;
            previewCanvasGroup.interactable = false;
            previewCanvasGroup.blocksRaycasts = false;
        }
    }

    public void ShowCardPreview(CardSO card)
    {
        if (card == null)
        {
            Debug.LogError("CardPreviewManager: No card provided!");
            return;
        }

        // Update the preview UI elements.
        if (previewCardImage != null && card.cardImage != null)
            previewCardImage.sprite = card.cardImage;
        if (previewCardNameText != null)
            previewCardNameText.text = card.cardName;
        if (previewCardDescriptionText != null)
            previewCardDescriptionText.text = card.effectDescription;
        if (previewCardPowerText != null)
            previewCardPowerText.text = card.power.ToString();
        if (previewExtraDetailsText != null)
        {
            if (card.category == CardSO.CardCategory.Creature)
                previewExtraDetailsText.text = card.extraDetails;
            else
                previewExtraDetailsText.text = "";
        }

        // Make the preview panel visible.
        previewCanvasGroup.alpha = 1f;
        previewCanvasGroup.interactable = true;
        previewCanvasGroup.blocksRaycasts = true;

        // Stop any previous fade routines and start the fade-out coroutine.
        StopAllCoroutines();
        StartCoroutine(PreviewRoutine());
    }
    
    private IEnumerator PreviewRoutine()
    {
        yield return new WaitForSeconds(previewDisplayDuration);
        float elapsed = 0f;
        while (elapsed < previewFadeDuration)
        {
            elapsed += Time.deltaTime;
            previewCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / previewFadeDuration);
            yield return null;
        }
        previewCanvasGroup.alpha = 0f;
        previewCanvasGroup.interactable = false;
        previewCanvasGroup.blocksRaycasts = false;
    }
}
