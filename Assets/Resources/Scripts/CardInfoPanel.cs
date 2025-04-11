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

    // Store the CardUI that is currently displayed.
    private CardUI currentCardUI = null;
    public float fadeDuration = 0.3f;         // Duration for fade in/out.

    // Expose the current CardUI so that other scripts (like FloatingText) can verify which card is on display.
    public CardUI CurrentCardUI
    {
        get { return currentCardUI; }
    }

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
        if (Input.GetKeyDown(KeyCode.Escape) && canvasGroup.alpha > 0)
        {
            HidePanel();
        }
    }

    // ShowCardInfo now accepts a CardUI reference.
    public void ShowCardInfo(CardUI cardUI)
    {
        if (cardUI == null)
        {
            Debug.LogError("CardInfoPanel: No CardUI provided.");
            return;
        }

        // Set the current card UI.
        currentCardUI = cardUI;

        // Update UI elements using the dynamic (runtime) values.
        if (cardImage != null && cardUI.cardData.cardImage != null)
            cardImage.sprite = cardUI.cardData.cardImage;
        if (cardNameText != null)
            cardNameText.text = cardUI.cardData.cardName;
        if (cardDescriptionText != null)
            cardDescriptionText.text = cardUI.cardData.effectDescription;
        if (cardPowerText != null)
            cardPowerText.text = cardUI.currentPower.ToString();  // Show the effective power.
        if (extraDetailsText != null)
        {
            extraDetailsText.text = (cardUI.cardData.category == CardSO.CardCategory.Creature)
                ? cardUI.cardData.extraDetails
                : "";
        }

        // Animate the panel's fade-in.
        if (canvasGroup != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroup(0f, 1f, fadeDuration));
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

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
        currentCardUI = null;

        // Stop the card image animation and reset its scale.
        if (cardImageAnimationCoroutine != null)
        {
            StopCoroutine(cardImageAnimationCoroutine);
            cardImageAnimationCoroutine = null;
            if (cardImage != null)
            {
                cardImage.transform.localScale = Vector3.one;
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
        while (canvasGroup.alpha > 0)
        {
            float scaleFactor = 1f + amplitude * Mathf.Sin(Time.time * speed);
            cardImage.transform.localScale = originalScale * scaleFactor;
            yield return null;
        }
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

    // Method to update the power text in the Card Info Panel.
    public void UpdatePowerDisplay()
    {
        Debug.Log("CardInfoPanel: Updating power text.");
        if (currentCardUI != null && cardPowerText != null)
        {
            cardPowerText.text = currentCardUI.currentPower.ToString();
        }
        else
        {
            Debug.LogError("CardInfoPanel: currentCardUI or cardPowerText is null.");
        }
    }
}
