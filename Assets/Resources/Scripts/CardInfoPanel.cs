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
    public TextMeshProUGUI cardPowerText;     // New field for displaying the power value.

    // Tracks if the panel is visible and which card is currently displayed.
    private bool isVisible = false;
    private CardSO currentCard = null;
    public float fadeDuration = 0.3f;         // Animation duration for fade in/out.

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
            cardPowerText.text = card.power.ToString(); // Display the power value.

        // Animate the panel's fade-in.
        if (canvasGroup != null)
        {
            StopAllCoroutines();
            StartCoroutine(FadeCanvasGroup(0f, 1f, fadeDuration));
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        isVisible = true;
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
