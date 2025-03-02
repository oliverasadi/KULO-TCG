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
    public GameObject summonMenuPrefab;       // Assign a prefab for the summon menu (should be under the Canvas)

    void Start()
    {
        LoadCardBack();
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
            cardBackSprite = Resources.Load<Sprite>("CardArt/CardBack");
            if (cardBackSprite == null)
            {
                Debug.LogError("❌ Card back image not found! Ensure it's in Resources/CardArt/CardBack.png");
            }
        }
    }

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
            // Check if a SummonMenu is already open.
            if (SummonMenu.currentMenu != null)
            {
                // If the current open menu belongs to this card, toggle it off.
                if (SummonMenu.currentMenu.cardUI == this)
                {
                    Destroy(SummonMenu.currentMenu.gameObject);
                    return;
                }
                else
                {
                    // Otherwise, close the existing menu.
                    Destroy(SummonMenu.currentMenu.gameObject);
                }
            }
            ShowSummonMenu();
        }
    }

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

        // Move SummonMenu to the top of the hierarchy to ensure it's on top.
        menuInstance.transform.SetAsLastSibling();

        // Get the RectTransform of the card and the menu.
        RectTransform cardRect = GetComponent<RectTransform>();
        RectTransform menuRect = menuInstance.GetComponent<RectTransform>();
        if (menuRect != null && cardRect != null)
        {
            // Convert the card's world position to a screen point.
            Vector2 cardScreenPos = RectTransformUtility.WorldToScreenPoint(null, cardRect.position);

            // Convert the screen point to local point in the Canvas.
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                cardScreenPos,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out localPoint
            );

            // Add an inspector-adjustable offset (set in CardUI).
            localPoint += summonMenuOffset;
            menuRect.anchoredPosition = localPoint;
        }
        else
        {
            Debug.LogWarning("Missing RectTransform on card or menu.");
        }

        // Initialize the menu with this CardUI reference.
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


    // Public field to allow adjusting the summon menu offset in the Inspector.
    public Vector2 summonMenuOffset = new Vector2(0, 20f);

    void Update()
    {
        if (isSelected && Input.GetKeyDown(KeyCode.Space))
        {
            if (cardInfoPanel != null)
                cardInfoPanel.ShowCardInfo(cardData);
        }
    }

    public bool isSelected = false;
}
