using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;

public class CardInfoPanel : MonoBehaviour, IPointerClickHandler
{
    public CanvasGroup canvasGroup;         // Controls visibility & interactivity.
    public Image cardImage;
    public TextMeshProUGUI cardNameText;
    public TextMeshProUGUI cardDescriptionText;
    public TextMeshProUGUI cardPowerText;     // Displays the card's power.
    public TextMeshProUGUI extraDetailsText;  // Displays extra details for creature cards.

    // Tracks if the panel is visible and which card is currently displayed.
    private bool isVisible = false;
    private CardSO currentCard = null;
    public float fadeDuration = 0.3f;         // Duration for fade in/out.

    // Reference to the card image pulsing animation coroutine.
    private Coroutine cardImageAnimationCoroutine;

    void Start()
    {
        // Hide the panel initially while keeping the GameObject active.
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            Debug.LogError("CardInfoPanel: CanvasGroup is not assigned.");
        }
    }

    void Update()
    {
        // Hide the panel when Escape is pressed.
        if (Input.GetKeyDown(KeyCode.Escape) && isVisible)
        {
            HidePanel();
        }
    }

    public void ShowCardInfo(CardSO card)
    {
        if (card == null)
        {
            Debug.LogError("CardInfoPanel: No card data provided.");
            return;
        }

        // Remember which card is being displayed.
        currentCard = card;

        // Update UI elements.
        if (cardImage != null && card.cardImage != null)
            cardImage.sprite = card.cardImage;
        if (cardNameText != null)
            cardNameText.text = card.cardName;
        if (cardDescriptionText != null)
            cardDescriptionText.text = card.effectDescription;
        if (cardPowerText != null)
            cardPowerText.text = card.power.ToString();  // Display the power value.
        if (extraDetailsText != null)
        {
            // Show extra details only for creature cards.
            if (card.category == CardSO.CardCategory.Creature)
                extraDetailsText.text = card.extraDetails;
            else
                extraDetailsText.text = ""; // Optionally clear for non-creatures.
        }

        // Animate the panel's fade-in.
        if (canvasGroup != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroup(0f, 1f, fadeDuration));
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        isVisible = true;

        // Start the card image pulsing animation.
        if (cardImage != null)
        {
            cardImageAnimationCoroutine = StartCoroutine(AnimateCardImage());
        }
    }

    public void HidePanel()
    {
        if (canvasGroup != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroup(canvasGroup.alpha, 0f, fadeDuration));
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        isVisible = false;
        currentCard = null;

        // Stop the card image animation and reset its scale.
        if (cardImageAnimationCoroutine != null)
        {
            StopCoroutine(cardImageAnimationCoroutine);
            cardImageAnimationCoroutine = null;
            if (cardImage != null)
            {
                cardImage.transform.localScale = Vector3.one; // Reset scale.
            }
        }
    }

    // Coroutine to fade the CanvasGroup's alpha value.
    private IEnumerator FadeCanvasGroup(float start, float end, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, end, elapsed / duration);
            yield return null;
        }
        canvasGroup.alpha = end;
    }

    // Coroutine to animate the card image with a subtle pulsing effect.
    private IEnumerator AnimateCardImage()
    {
        Vector3 originalScale = cardImage.transform.localScale;
        float amplitude = 0.05f; // Adjust for pulsing magnitude.
        float speed = 2f;        // Adjust for pulsing speed.

        while (isVisible)
        {
            float scaleFactor = 1f + amplitude * Mathf.Sin(Time.time * speed);
            cardImage.transform.localScale = originalScale * scaleFactor;
            yield return null;
        }
        // Reset scale when done.
        cardImage.transform.localScale = originalScale;
    }

    // Implement IPointerClickHandler so that a right-click on the panel hides it.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            HidePanel();
        }
    }

    // Read-only properties so other scripts can check the panel's state.
    public bool IsVisible => isVisible;
    public CardSO CurrentCard => currentCard;
}
