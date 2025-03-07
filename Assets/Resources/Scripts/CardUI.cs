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
    public CardSO cardData;                   // The card's ScriptableObject data
    private bool isFaceDown = false;
    public bool isInDeck = false;             // Track if the card is in deck

    // New field: indicates if this card is already on the field.
    public bool isOnField = false;

    [Header("Card Info Popup")]
    public CardInfoPanel cardInfoPanel;       // Reference to the pop-up panel's controller

    // --- New Fields for Summoning ---
    public GameObject summonMenuPrefab;       // Assign a prefab for the summon menu (should be under the Canvas)
    public Vector2 summonMenuOffset = new Vector2(0, 20f); // Offset for positioning the summon menu

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
        // Prevent interactions if this card belongs to the AI.
        CardHandler handler = GetComponent<CardHandler>();
        if (handler != null && handler.isAI)
        {
            // Optionally log that this card cannot be interacted with.
            Debug.Log("Attempted interaction on AI card ignored.");
            return;
        }

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
            // If a SummonMenu is already open, close it (toggle behavior).
            if (SummonMenu.currentMenu != null)
            {
                if (SummonMenu.currentMenu.cardUI == this)
                {
                    Destroy(SummonMenu.currentMenu.gameObject);
                    return;
                }
                else
                {
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

        // Determine if the card is on the field based on its parent.
        // Assuming grid cells are named with "GridCell" in their name.
        if (transform.parent != null && transform.parent.name.Contains("GridCell"))
        {
            isOnField = true;
        }
        else
        {
            isOnField = false;
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
        menuInstance.transform.SetAsLastSibling(); // Ensure it's on top.

        // Position the menu near the card.
        RectTransform cardRect = GetComponent<RectTransform>();
        RectTransform menuRect = menuInstance.GetComponent<RectTransform>();
        if (menuRect != null && cardRect != null)
        {
            Vector2 cardScreenPos = RectTransformUtility.WorldToScreenPoint(null, cardRect.position);
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                cardScreenPos,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
                out localPoint
            );
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
