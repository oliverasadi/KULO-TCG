using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

public class CardUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Elements")]
    public Image cardArtImage; // Displays the card image
    public TextMeshProUGUI cardNameText; // Displays the card name

    public Sprite cardBackSprite; // Holds the back of the card sprite (assign in Prefab Inspector)
    private CardSO cardData; // Stores the card's ScriptableObject data
    private bool isFaceDown = false;
    public bool isInDeck = false; // Track if card is in deck

    [Header("Card Info Popup")]
    // Reference to your pop-up panel's controller
    public CardInfoPanel cardInfoPanel;
    // Flag to indicate if this card is selected (set via your hover/selection logic)
    public bool isSelected = false;

    void Start()
    {
        LoadCardBack();
        // Fallback: if no CardInfoPanel assigned in the Inspector, try to find one in the scene.
        if (cardInfoPanel == null)
        {
            cardInfoPanel = FindObjectOfType<CardInfoPanel>();
            if (cardInfoPanel == null)
            {
                Debug.LogError("No CardInfoPanel found in the scene. Please add one and assign it.");
            }
        }
    }

    private void LoadCardBack()
    {
        if (cardBackSprite == null)
        {
            cardBackSprite = Resources.Load<Sprite>("CardArt/CardBack"); // Supports .jpeg and .jpg
            if (cardBackSprite == null)
            {
                Debug.LogError("❌ Card back image not found! Ensure it's in Resources/CardArt/CardBack.png");
            }
        }
    }

    // This method is called (typically by CardHandler) to assign and update the card data.
    public void SetCardData(CardSO card, bool setFaceDown = false)
    {
        if (card == null)
        {
            Debug.LogError("❌ CardUI.SetCardData was given a null card!");
            return;
        }

        cardData = card;
        isInDeck = false;

        Debug.Log($"✅ CardUI: Card data set for {gameObject.name} - {cardData.cardName}");
        if (cardNameText != null)
            cardNameText.text = cardData.cardName;
        else
            Debug.LogError($"CardUI: cardNameText is not assigned on {gameObject.name}");

        if (cardArtImage != null)
        {
            if (setFaceDown)
            {
                SetFaceDown();
            }
            else if (cardData.cardImage != null)
            {
                cardArtImage.sprite = cardData.cardImage;
            }
            else
            {
                Debug.LogError($"CardUI: Card art missing for {cardData.cardName}");
            }
        }
        else
        {
            Debug.LogError($"CardUI: cardArtImage is not assigned on {gameObject.name}");
        }
    }

    public void SetFaceDown()
    {
        isFaceDown = true;
        if (cardArtImage != null && cardBackSprite != null)
        {
            cardArtImage.sprite = cardBackSprite;
        }
        else
        {
            Debug.LogError($"CardUI: Unable to set face down for {gameObject.name}. Check cardArtImage and cardBackSprite assignments.");
        }
    }

    public void RevealCard()
    {
        if (isFaceDown)
        {
            isFaceDown = false;
            StartCoroutine(FlipCardAnimationWithRotation());
        }
        else if (cardArtImage != null && cardData != null && cardData.cardImage != null)
        {
            cardArtImage.sprite = cardData.cardImage;
        }
    }

    private IEnumerator FlipCardAnimationWithRotation()
    {
        float duration = 0.5f;
        float time = 0f;

        while (time < duration / 2)
        {
            float angle = Mathf.Lerp(0, 90, time / (duration / 2));
            transform.rotation = Quaternion.Euler(0, angle, 0);
            time += Time.deltaTime;
            yield return null;
        }

        if (cardArtImage != null && cardData != null && cardData.cardImage != null)
            cardArtImage.sprite = cardData.cardImage;

        yield return null;

        time = 0f;
        while (time < duration / 2)
        {
            float angle = Mathf.Lerp(90, 0, time / (duration / 2));
            transform.rotation = Quaternion.Euler(0, angle, 0);
            time += Time.deltaTime;
            yield return null;
        }

        isFaceDown = false;
    }

    // Detect right-clicks on the card to show its info.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (cardData == null)
            {
                Debug.LogError($"CardUI: cardData is null on {gameObject.name}. Ensure SetCardData is called.");
                return;
            }
            if (cardInfoPanel != null)
            {
                cardInfoPanel.ShowCardInfo(cardData);
            }
        }
    }

    void Update()
    {
        // If this card is selected and the spacebar is pressed, show its info.
        if (isSelected && Input.GetKeyDown(KeyCode.Space))
        {
            if (cardData == null)
            {
                Debug.LogError($"CardUI: cardData is null on {gameObject.name}. Ensure SetCardData is called.");
                return;
            }
            if (cardInfoPanel != null)
            {
                cardInfoPanel.ShowCardInfo(cardData);
            }
        }
    }
}
