using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;

public class CardUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Elements")]
    public Image cardArtImage;                // Displays the card image
    public TextMeshProUGUI cardNameText;        // Displays the card name

    public Sprite cardBackSprite;             // Card back sprite (assign in Prefab Inspector)
    public CardSO cardData;                  // The card's ScriptableObject data
    private bool isFaceDown = false;
    public bool isInDeck = false;             // Track if the card is in deck

    [Header("Card Info Popup")]
    public CardInfoPanel cardInfoPanel;       // Reference to the pop-up panel's controller

    // --- New Fields for Summoning ---
    public GameObject summonMenuPrefab;       // Assign a prefab for the summon menu (under a Canvas)

    // Optionally store additional info for evolution summoning (like target cell indices)
    // public int targetCellX, targetCellY;

    void Start()
    {
        LoadCardBack();
        // Fallback: if no CardInfoPanel is assigned in the Inspector, try to find one in the scene.
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

    // This method is called to assign and update the card data.
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

    // IPointerClickHandler implementation.
    // Right-click shows card info, left-click opens the summon menu.
    public void OnPointerClick(PointerEventData eventData)
    {
        if (cardData == null)
        {
            Debug.LogError($"CardUI: cardData is null on {gameObject.name}. Ensure SetCardData is called.");
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (cardInfoPanel != null)
                cardInfoPanel.ShowCardInfo(cardData);
        }
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            // Open Summon Menu for the card in hand.
            ShowSummonMenu();
        }
    }
    // Add this field near the top of your CardUI class:
    public Vector2 summonMenuOffset = new Vector2(0, 20f);

    private void ShowSummonMenu()
    {
        if (summonMenuPrefab == null)
        {
            Debug.LogError("Summon Menu Prefab is not assigned in CardUI.");
            return;
        }

        // Find the Canvas in the scene.
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in the scene!");
            return;
        }

        // Instantiate the SummonMenu as a child of the Canvas.
        GameObject menuInstance = Instantiate(summonMenuPrefab, canvas.transform);

        // Get the RectTransform of the card and the menu.
        RectTransform cardRect = GetComponent<RectTransform>();
        RectTransform menuRect = menuInstance.GetComponent<RectTransform>();
        if (menuRect != null && cardRect != null)
        {
            // Convert the card's world position to screen point.
            Vector2 cardScreenPos = RectTransformUtility.WorldToScreenPoint(null, cardRect.position);

            // Convert screen point to local point in the Canvas.
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                cardScreenPos,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out localPoint);

            // Add the inspector-adjustable offset.
            localPoint += summonMenuOffset;

            menuRect.anchoredPosition = localPoint;
        }
        else
        {
            Debug.LogWarning("Missing RectTransform on card or menu.");
        }

        // Initialize the SummonMenu with this CardUI reference.
        SummonMenu summonMenu = menuInstance.GetComponent<SummonMenu>();
        if (summonMenu != null)
        {
            summonMenu.Initialize(this);
        }
        else
        {
            Debug.LogError("SummonMenu component is missing on the instantiated menu prefab.");
        }
    }



    // Update method: if selected and space is pressed, show info (kept from original)
    void Update()
    {
        if (isSelected && Input.GetKeyDown(KeyCode.Space))
        {
            if (cardInfoPanel != null)
                cardInfoPanel.ShowCardInfo(cardData);
        }
    }

    // Flag indicating if this card is selected (e.g., via hover logic)
    public bool isSelected = false;
}
