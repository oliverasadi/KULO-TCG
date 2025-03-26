using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CardUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI Elements")]
    public Image cardArtImage;
    public TextMeshProUGUI cardNameText;
    public Sprite cardBackSprite;
    public CardSO cardData;
    private bool isFaceDown = false;
    public bool isInDeck = false;
    public bool isOnField = false;

    [Header("Card Info Popup")]
    public CardInfoPanel cardInfoPanel;

    // Summon Menu
    public GameObject summonMenuPrefab;
    public Vector2 summonMenuOffset = new Vector2(0, 20f);

    // Runtime fields
    public int currentPower;
    public int temporaryBoost = 0;

    // Runtime replacement effect fields (for inline ReplaceAfterOpponentTurn)
    public List<CardEffectData> runtimeInlineEffects;
    public int replacementTurnDelay = -1; // -1 means no effect by default.
    public string inlineReplacementCardName = "";
    public bool inlineBlockAdditionalPlays = false;

    // NEW: store dynamically created inline effects (e.g. ConditionalPowerBoost) 
    public List<CardEffect> activeInlineEffects; // used so we can remove them on card removal

    private bool hasEffectsApplied = false; // Track if effects have been applied

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

    // SetCardData should not apply any effects, we just set data here.
    public void SetCardData(CardSO card, bool setFaceDown = false)
    {
        if (card == null)
        {
            Debug.LogError("❌ CardUI.SetCardData was given a null card!");
            return;
        }

        cardData = card;
        isInDeck = false;
        currentPower = card.power;

        // Do not apply inline effects here; this is handled elsewhere
        if (!hasEffectsApplied)
        {
            // Flag to prevent reapplying effects
            hasEffectsApplied = true; // This flag will ensure we do not apply effects multiple times during initialization
        }

        // Update the UI elements
        if (cardNameText != null)
            cardNameText.text = cardData.cardName;
        else
            Debug.LogError($"CardUI: cardNameText is not assigned on {gameObject.name}");

        if (cardArtImage != null)
        {
            if (setFaceDown)
                SetFaceDown();
            else if (cardData.cardImage != null)
                cardArtImage.sprite = cardData.cardImage;
            else
                Debug.LogError($"CardUI: Card art missing for {cardData.cardName}");
        }
        else
        {
            Debug.LogError($"CardUI: cardArtImage is not assigned on {gameObject.name}");
        }
    }

    // Method to update the card's power dynamically (this is called when the power changes, e.g., from AdjustPowerAdjacentEffect)
    public void UpdatePower(int newPower)
    {
        currentPower = newPower;
        UpdatePowerDisplay(); // Update the display when the power is changed
    }

    // Update the card power display
    public void UpdatePowerDisplay()
    {
        if (cardNameText != null)
        {
            cardNameText.text = $"{cardData.cardName} ({currentPower})"; // Update the UI to show power change
        }
        else
        {
            Debug.LogError("Card Name Text is not assigned!");
        }
    }

    // ApplyInlineEffects should not apply any effects here directly.
    // It only stores inline effects for future application elsewhere.
    public void ApplyInlineEffects()
    {
        // No direct effect application here, simply set it up for the relevant effect handler (MutualConditionalPowerBoostEffect).
        if (cardData.inlineEffects != null)
        {
            foreach (var effect in cardData.inlineEffects)
            {
                // No effect application here anymore
                // We used to apply effects directly in this method but no longer do that.
                // This part is now handled by the AdjustPowerAdjacentEffect class.
            }
        }
    }

    // Method for calculating effective power (only used for calculations, no effect application)
    public int CalculateEffectivePower()
    {
        int effectivePower = currentPower;

        // Inline effects logic should be handled in the effect classes, not here.
        foreach (var effect in cardData.inlineEffects)
        {
            // This part should be for power calculations, not effect applications
            if (effect.effectType == CardEffectData.EffectType.ConditionalPowerBoost)
            {
                int synergyCount = 0;
                if (effect.requiredCreatureNames != null && effect.requiredCreatureNames.Count > 0)
                {
                    CardSO[,] gridArray = GridManager.instance.GetGrid();
                    GameObject[,] gridObjs = GridManager.instance.GetGridObjects();
                    CardHandler myHandler = GetComponent<CardHandler>();

                    for (int i = 0; i < gridArray.GetLength(0); i++)
                    {
                        for (int j = 0; j < gridArray.GetLength(1); j++)
                        {
                            if (gridArray[i, j] == null)
                                continue;
                            if (gridObjs[i, j] == this.gameObject)
                                continue;

                            foreach (string reqName in effect.requiredCreatureNames)
                            {
                                if (gridArray[i, j].cardName == reqName)
                                {
                                    CardHandler candidate = gridObjs[i, j].GetComponent<CardHandler>();
                                    if (candidate != null && myHandler != null && candidate.cardOwner == myHandler.cardOwner)
                                    {
                                        synergyCount++;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                if (synergyCount > 0)
                {
                    effectivePower += effect.powerChange * synergyCount;
                }
            }
        }

        return effectivePower;
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

        // If we're in target selection mode, add this card as a target.
        if (TargetSelectionManager.Instance != null && TargetSelectionManager.Instance.IsSelectingTargets)
        {
            TargetSelectionManager.Instance.AddTarget(this);
            return;
        }

        // Right-click to open CardInfoPanel
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (cardInfoPanel != null)
                cardInfoPanel.ShowCardInfo(cardData);
        }
        // Left-click to open SummonMenu
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
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

        if (transform.parent != null && transform.parent.name.Contains("GridCell"))
        {
            isOnField = true;
        }
        else
        {
            isOnField = false;
        }

        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("No Canvas found in the scene!");
            return;
        }

        GameObject menuInstance = Instantiate(summonMenuPrefab, canvas.transform);
        menuInstance.transform.SetAsLastSibling();

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
