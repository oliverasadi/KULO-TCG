using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class CardUI : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Elements")]
    public Image cardArtImage;
    public TextMeshProUGUI cardNameText;
    public Sprite cardBackSprite;
    public CardSO cardData;
    private bool isFaceDown = false;
    public bool isInDeck = false;
    public bool isOnField = false;
    public bool effectsAppliedInHand = false;


    [Header("Card Info Popup")]
    public CardInfoPanel cardInfoPanel;

    // Summon Menu
    public GameObject summonMenuPrefab;
    public Vector2 summonMenuOffset = new Vector2(0, 20f);

    // Runtime fields
    public int currentPower;
    public int temporaryBoost = 0;
    public bool isInGraveyard = false;
    public bool isCloneInGraveyardPanel = false; // NEW

    public GameObject powerChangeBadgeInstance;  // runtime badge
    public GameObject powerChangeBadgePrefab;    // assign this in Inspector


    // Runtime replacement effect fields (for inline ReplaceAfterOpponentTurn)
    public List<CardEffectData> runtimeInlineEffects;
    public int replacementTurnDelay = -1; // -1 means no effect by default.
    public string inlineReplacementCardName = "";
    public bool inlineBlockAdditionalPlays = false;

    // NEW: store dynamically created inline effects (e.g. ConditionalPowerBoost) 
    public List<CardEffect> activeInlineEffects; // used so we can remove them on card removal

    private bool hasEffectsApplied = false; // Track if effects have been applied

    // NEW: Flag to indicate this card is selected as a sacrifice.
    public bool isSacrificeSelected = false;
    private Vector3 originalScale;
    private Coroutine hoverCoroutine; // For pulsating hover effect

    void Awake()
    {
        originalScale = transform.localScale;
    }

    void Start()
    {
        LoadCardBack();

        if (cardInfoPanel == null)
        {
            cardInfoPanel = FindObjectOfType<CardInfoPanel>();
            Debug.Log($"CardInfoPanel found: {cardInfoPanel != null}");  // Check if it's correctly assigned
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
        currentPower = card.power;

        if (!hasEffectsApplied)
        {
            hasEffectsApplied = true;
        }

        if (cardNameText != null)
            cardNameText.text = cardData.cardName;
        else

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

    public void UpdatePower(int newPower)
    {
        if (currentPower != newPower)  // Only update if the power is actually different
        {
            currentPower = newPower;  // Update the internal power value
            Debug.Log($"Power updated: {currentPower}");

            UpdatePowerDisplay();  // Update the power display in the Card Info Panel
        }
    }


    public void UpdatePowerDisplay()
    {
        if (cardNameText != null)
            cardNameText.text = $"{cardData.cardName} ({currentPower})";
    }

    public void ApplyInlineEffects()
    {
        if (cardData.inlineEffects != null)
        {
            foreach (var effect in cardData.inlineEffects)
            {
                // Effects are handled elsewhere.
            }
        }
    }

    public int CalculateEffectivePower()
    {
        // Start with the card's base power from its CardSO.
        int effectivePower = cardData.power;

        // Only proceed if cardData and its inlineEffects are valid.
        if (cardData == null || cardData.inlineEffects == null)
            return effectivePower;

        // Obtain the current state of the grid.
        CardSO[,] gridArray = GridManager.instance.GetGrid();
        GameObject[,] gridObjs = GridManager.instance.GetGridObjects();

        // Process each inline effect for ConditionalPowerBoost.
        foreach (var effect in cardData.inlineEffects)
        {
            if (effect.effectType == CardEffectData.EffectType.ConditionalPowerBoost)
            {
                int synergyCount = 0;

                // Count matching cards on the board if specific names are required.
                if (effect.requiredCreatureNames != null && effect.requiredCreatureNames.Count > 0)
                {
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
                                if (!string.IsNullOrEmpty(reqName) && gridArray[i, j].cardName == reqName)
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
                else
                {
                    // Default: count every creature on the field except this one.
                    for (int i = 0; i < gridArray.GetLength(0); i++)
                    {
                        for (int j = 0; j < gridArray.GetLength(1); j++)
                        {
                            if (gridArray[i, j] == null)
                                continue;
                            if (gridObjs[i, j] == this.gameObject)
                                continue;
                            if (gridArray[i, j].category == CardSO.CardCategory.Creature)
                                synergyCount++;
                        }
                    }
                }
                // If one or more matching cards were found, add the boost.
                if (synergyCount > 0)
                {
                    effectivePower += effect.powerChange * synergyCount;
                }
            }
        }

        // Finally, add any aggregated modifiers stored in temporaryBoost.
        // (Other effects should update temporaryBoost when applied or removed.)
        effectivePower += temporaryBoost;

        return Mathf.Max(0, effectivePower);
    }



    public void UpdatePowerIndicator()
    {
        if (!isOnField)
        {
            if (powerChangeBadgeInstance != null)
                Destroy(powerChangeBadgeInstance);
            return;
        }

        int basePower = cardData.power;
        int effectivePower = CalculateEffectivePower();

        if (effectivePower > basePower)
            ShowPowerChangeBadge(true);
        else if (effectivePower < basePower)
            ShowPowerChangeBadge(false);
        else if (powerChangeBadgeInstance != null)
            Destroy(powerChangeBadgeInstance);
    }

    public void ShowPowerChangeBadge(bool isIncrease)
    {
        if (powerChangeBadgePrefab == null)
        {
            Debug.LogWarning("⚠️ powerChangeBadgePrefab not assigned.");
            return;
        }

        if (powerChangeBadgeInstance == null)
        {
            powerChangeBadgeInstance = Instantiate(powerChangeBadgePrefab, transform);
            powerChangeBadgeInstance.transform.localPosition = Vector3.zero;
            powerChangeBadgeInstance.transform.SetAsLastSibling();
        }

        var text = powerChangeBadgeInstance.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
            text.text = isIncrease ? "↑" : "↓";
    }

    public void SetFaceDown()
    {
        isFaceDown = true;
        if (cardArtImage != null && cardBackSprite != null)
            cardArtImage.sprite = cardBackSprite;
        else
            Debug.LogError($"CardUI: Unable to set face down for {gameObject.name}. Check cardArtImage and cardBackSprite assignments.");
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

        // Target selection mode
        if (TargetSelectionManager.Instance != null && TargetSelectionManager.Instance.IsSelectingTargets)
        {
            TargetSelectionManager.Instance.AddTarget(this);
            return;
        }

        // Sacrifice selection mode
        if (SacrificeManager.instance != null && SacrificeManager.instance.isSelectingSacrifices)
        {
            if (SacrificeManager.instance.IsValidSacrifice(this))
            {
                SacrificeManager.instance.SelectSacrifice(gameObject);
                return;
            }
        }

        // ───────────────────────────────────────────────────────────────
        // RIGHT-CLICK BEHAVIOR
        // ───────────────────────────────────────────────────────────────
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // If this is a graveyard viewer clone, ignore right-click
            if (isCloneInGraveyardPanel)
            {
                if (GraveyardSelectionManager.Instance != null)
                {
                    Debug.Log($"👁️ GraveyardSelectionManager.IsSelecting: {GraveyardSelectionManager.Instance.IsSelecting}");

                    if (GraveyardSelectionManager.Instance.IsSelecting)
                    {
                        Debug.Log($"✅ Forwarding click to selection manager: {cardData.cardName}");
                        GraveyardSelectionManager.Instance.SelectCard(gameObject);
                        return;
                    }
                }

                Debug.Log($"🪦 Left-click ignored inside Graveyard Panel: {cardData.cardName}");
                return;
            }



            // If this is a real card in graveyard zone, open the full panel
            if (isInGraveyard)
            {
                CardHandler cardHandler = GetComponent<CardHandler>();
                if (cardHandler != null && cardHandler.cardOwner?.zones != null)
                {
                    Debug.Log($"🪦 Right-clicked graveyard card: {cardData.cardName}");
                    UIManager.Instance?.ShowGraveyardPanel(cardHandler.cardOwner.zones.GetGraveyardCards());
                    return;
                }
            }

            // Otherwise, show card info panel
            if (cardInfoPanel != null)
                cardInfoPanel.ShowCardInfo(this);
        }

        // ───────────────────────────────────────────────────────────────
        // LEFT-CLICK BEHAVIOR
        // ───────────────────────────────────────────────────────────────
        else if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (isCloneInGraveyardPanel)
            {
                if (GraveyardSelectionManager.Instance != null && GraveyardSelectionManager.Instance.IsSelecting)
                {
                    Debug.Log($"✅ Graveyard selection active. Passing {cardData.cardName} to selector.");
                    GraveyardSelectionManager.Instance.SelectCard(gameObject);
                    return;
                }

                Debug.Log($"🪦 Left-click ignored inside Graveyard Panel: {cardData.cardName}");
                return;
            }

            ShowSummonMenu();
        }
    }





    // Implement pointer events – these now log debug info but do not control the persistent effect.
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isSacrificeSelected)
        {
            Debug.Log($"[CardUI] OnPointerEnter: {cardData.cardName} is hovered (sacrifice selected).");
            // If the persistent hover is not running, start it.
            if (hoverCoroutine == null)
            {
                ApplySacrificeHoverEffect();
            }
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isSacrificeSelected)
        {
            Debug.Log($"[CardUI] OnPointerExit: {cardData.cardName} hover ended.");
            // We no longer stop the persistent hover here so it continues.
            // If you want to stop the effect on exit, you can call ResetSacrificeHoverEffect().
        }
    }

    // Public method to start the persistent pulsating hover effect.
    public void ApplySacrificeHoverEffect()
    {
        if (!isSacrificeSelected) return;
        if (hoverCoroutine == null)
        {
            Debug.Log($"[CardUI] Starting persistent pulsate effect for {cardData.cardName}");
            hoverCoroutine = StartCoroutine(Pulsate());
        }
    }

    // Public method to stop the pulsating hover effect and reset the scale.
    public void ResetSacrificeHoverEffect()
    {
        if (hoverCoroutine != null)
        {
            StopCoroutine(hoverCoroutine);
            hoverCoroutine = null;
        }
        transform.localScale = originalScale;
        isSacrificeSelected = false;
    }

    private IEnumerator Pulsate()
    {
        float timer = 0f;
        while (true)
        {
            timer += Time.deltaTime;
            // Oscillate scale factor between 1.0 and 1.1 using PingPong.
            float scaleFactor = 1f + 0.1f * Mathf.PingPong(timer * 2f, 1f);
            transform.localScale = originalScale * scaleFactor;
            yield return null;
        }
    }

    private void ShowSummonMenu()
    {
        if (summonMenuPrefab == null)
        {
            Debug.LogError("❌ Summon Menu Prefab is not assigned in CardUI.");
            return;
        }

        isOnField = (transform.parent != null && transform.parent.name.Contains("GridCell"));

        // ✅ Find the overlay canvas to parent the menu to
        GameObject overlayCanvasObj = GameObject.Find("OverlayCanvas");
        if (overlayCanvasObj == null)
        {
            Debug.LogError("❌ 'OverlayCanvas' not found in the scene!");
            return;
        }

        Canvas canvas = overlayCanvasObj.GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("❌ 'OverlayCanvas' found but it has no Canvas component!");
            return;
        }

        // ✅ Instantiate the summon menu prefab as a child of the overlay canvas
        GameObject menuInstance = Instantiate(summonMenuPrefab, overlayCanvasObj.transform);
        menuInstance.transform.SetAsLastSibling(); // ensure it's on top

        // ✅ Position it near the card
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
                out localPoint);

            localPoint += summonMenuOffset;
            menuRect.anchoredPosition = localPoint;
        }
        else
        {
            Debug.LogWarning("⚠️ Missing RectTransform on card or menu.");
        }

        // ✅ Initialize the menu logic
        SummonMenu summonMenu = menuInstance.GetComponent<SummonMenu>();
        if (summonMenu != null)
        {
            summonMenu.Initialize(this);
        }
        else
        {
            Debug.LogError("❌ SummonMenu component is missing on the instantiated menu prefab.");
        }
    }




    void Update()
    {
        if (cardData == null) return;

        int newEffectivePower = CalculateEffectivePower();
        if (newEffectivePower != currentPower)
        {
            currentPower = newEffectivePower;
            UpdatePowerDisplay();
            UpdatePowerIndicator(); // ✅ Shows ↑ or ↓ badge when power changes
        }

        if (isSelected && Input.GetKeyDown(KeyCode.Space))
        {
            if (cardInfoPanel != null)
                cardInfoPanel.ShowCardInfo(this);
        }
    }



    public bool isSelected = false;
}