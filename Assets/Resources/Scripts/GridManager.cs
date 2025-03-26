using System.Collections;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager instance;

    private CardSO[,] grid = new CardSO[3, 3];
    private GameObject[,] gridObjects = new GameObject[3, 3];

    [Header("Evolution Splash")]
    public GameObject evolutionSplashPrefab; // <-- Add THIS line here

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip placeCardSound;
    public AudioClip removeCardSound;

    public bool isHoldingCard;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    public bool IsValidDropPosition(Vector2Int dropPosition, out int x, out int y)
    {
        x = dropPosition.x;
        y = dropPosition.y;
        return true;
    }

    public bool CanPlaceCard(int x, int y, CardSO card)
    {
        int currentPlayer = TurnManager.instance.GetCurrentPlayer();

        if (grid[x, y] == null)
        {
            return TurnManager.instance.CanPlayCard(card);
        }
        else
        {
            CardHandler occupantHandler = gridObjects[x, y].GetComponent<CardHandler>();
            if (occupantHandler != null)
            {
                bool occupantIsOpponent =
                    (currentPlayer == 1 && occupantHandler.isAI) ||
                    (currentPlayer == 2 && !occupantHandler.isAI);

                if (occupantIsOpponent)
                {
                    bool allowed = (card.power >= grid[x, y].power);
                    Debug.Log($"[GridManager] Replacement at ({x},{y}): occupant power = {grid[x, y].power}, new card power = {card.power}, allowed = {allowed}");
                    return allowed;
                }
            }
            return false;
        }
    }
    public bool PlaceExistingCard(int x, int y, GameObject cardObj, CardSO cardData, Transform cellParent)
    {
        Debug.Log($"[GridManager] Attempting to place {cardData.cardName} at ({x},{y}). Category: {cardData.category}");

        int currentPlayer = TurnManager.instance.GetCurrentPlayer();
        string baseCardName = null;

        // ------------------
        // (1) SACRIFICE REQUIREMENTS
        // ------------------
        if (cardData.requiresSacrifice && cardData.sacrificeRequirements != null && cardData.sacrificeRequirements.Count > 0)
        {
            foreach (var req in cardData.sacrificeRequirements)
            {
                int foundCount = 0;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (grid[i, j] != null)
                        {
                            bool match = req.matchByCreatureType
                                ? (grid[i, j].creatureType == req.requiredCardName)
                                : (grid[i, j].cardName == req.requiredCardName);
                            CardHandler occupantCH = gridObjects[i, j].GetComponent<CardHandler>();
                            if (match && occupantCH != null && occupantCH.cardOwner.playerNumber == currentPlayer)
                            {
                                foundCount++;
                            }
                        }
                    }
                }
                Debug.Log($"[Sacrifice Check] For {req.requiredCardName}: found {foundCount}, need {req.count}.");
                if (foundCount < req.count)
                {
                    Debug.Log($"Cannot place {cardData.cardName}: requirement not met (need {req.count}, found {foundCount}).");
                    return false;
                }
            }

            foreach (var req in cardData.sacrificeRequirements)
            {
                int sacrificed = 0;
                for (int i = 0; i < 3 && sacrificed < req.count; i++)
                {
                    for (int j = 0; j < 3 && sacrificed < req.count; j++)
                    {
                        if (grid[i, j] != null)
                        {
                            bool match = req.matchByCreatureType
                                ? (grid[i, j].creatureType == req.requiredCardName)
                                : (grid[i, j].cardName == req.requiredCardName);
                            CardHandler occupantCH = gridObjects[i, j].GetComponent<CardHandler>();
                            if (match && occupantCH != null && occupantCH.cardOwner.playerNumber == currentPlayer)
                            {
                                // Capture the first sacrificed card as the 'base'
                                if (baseCardName == null)
                                    baseCardName = grid[i, j].cardName;

                                Debug.Log($"Sacrificing {grid[i, j].cardName} at ({i},{j}) for {cardData.cardName}.");
                                RemoveCard(i, j, false);
                                sacrificed++;
                            }
                        }
                    }
                }
            }
        }

        // ------------------
        // (2) OCCUPANT REPLACEMENT LOGIC
        // ------------------
        if (grid[x, y] != null)
        {
            float occupantEffectivePower = gridObjects[x, y].GetComponent<CardUI>().CalculateEffectivePower();
            float newCardEffectivePower = cardObj.GetComponent<CardUI>().CalculateEffectivePower();

            if (occupantEffectivePower > newCardEffectivePower)
            {
                Debug.Log($"Cannot replace {grid[x, y].cardName} at ({x},{y}) - occupant effective power ({occupantEffectivePower}) is higher than new card effective power ({newCardEffectivePower}).");
                return false;
            }
            else if (Mathf.Approximately(occupantEffectivePower, newCardEffectivePower))
            {
                Debug.Log($"Equal effective power at ({x},{y}). Destroying both occupant and new card.");
                CardHandler occupantHandler = gridObjects[x, y].GetComponent<CardHandler>();
                bool occupantIsAI = (occupantHandler != null && occupantHandler.isAI);
                RemoveCard(x, y, occupantIsAI);

                CardHandler newCardHandler = cardObj.GetComponent<CardHandler>();
                if (newCardHandler != null && newCardHandler.cardOwner != null)
                {
                    newCardHandler.cardOwner.zones.AddCardToGrave(cardObj);
                }
                TurnManager.instance.RegisterCardPlay(cardData);
                if (cardData.baseOrEvo != CardSO.BaseOrEvo.Evolution)
                {
                    ResetCellVisual(x, y);
                }
                return true;
            }
            else
            {
                Debug.Log($"[GridManager] Replacing occupant {grid[x, y].cardName} at ({x},{y}) with {cardData.cardName} (new effective power: {newCardEffectivePower} > occupant effective power: {occupantEffectivePower}).");
                CardHandler occupantHandler = gridObjects[x, y].GetComponent<CardHandler>();
                bool occupantIsAI = (occupantHandler != null && occupantHandler.isAI);
                RemoveCard(x, y, occupantIsAI);
            }
        }

        // ------------------
        // (3) RE-PARENT & CENTER THE CARD
        // ------------------
        cardObj.transform.SetParent(cellParent, false);
        RectTransform rt = cardObj.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }
        else
        {
            cardObj.transform.localPosition = Vector3.zero;
        }

        // ------------------
        // (4) UPDATE GRID REFERENCES & OWNERSHIP
        // ------------------
        grid[x, y] = cardData;
        gridObjects[x, y] = cardObj;

        // Mark this card as on-field
        CardUI ui = cardObj.GetComponent<CardUI>();
        if (ui != null) ui.isOnField = true;

        // Force ownership: isAI or not
        CardHandler handler = cardObj.GetComponent<CardHandler>();
        bool isAICard = false;
        if (handler != null)
        {
            isAICard = (handler.cardOwner != null && handler.cardOwner.playerType == PlayerManager.PlayerTypes.AI);
            Debug.Log($"[GridManager] Forcing ownership for {cardData.cardName}: isAI = {isAICard}");
            handler.isAI = isAICard;
        }

        TurnManager.instance.RegisterCardPlay(cardData);

        if (audioSource != null && placeCardSound != null)
            audioSource.PlayOneShot(placeCardSound);

        Debug.Log($"[GridManager] Placed {cardData.cardName} at ({x},{y}).");

        // ------------------
        // (5) EVOLUTION SPLASH (if applicable)
        // ------------------
        if (cardData.baseOrEvo == CardSO.BaseOrEvo.Evolution)
        {
            ShowEvolutionSplash((baseCardName ?? "Base"), cardData.cardName);
        }

        // ------------------
        // (6) FLOATING TEXT & HIGHLIGHTING
        // ------------------
        if (FloatingTextManager.instance != null)
        {
            GameObject floatingText = Instantiate(
                FloatingTextManager.instance.floatingTextPrefab,
                cardObj.transform.position,
                Quaternion.identity,
                cardObj.transform
            );
            floatingText.transform.localPosition = new Vector3(0, 50f, 0);

            TextMeshProUGUI tmp = floatingText.GetComponent<TextMeshProUGUI>();
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            if (tmp != null && cardUI != null)
                tmp.text = "Power: " + cardUI.CalculateEffectivePower();

            FloatingText ft = floatingText.GetComponent<FloatingText>();
            if (ft != null)
            {
                ft.sourceCard = cardObj;
            }
        }

        // Color highlight for non-Spell cards
        if (cardData.category != CardSO.CardCategory.Spell)
        {
            Color baseColor;
            if (cardData.baseOrEvo == CardSO.BaseOrEvo.Evolution)
                baseColor = Color.green;
            else
                baseColor = isAICard ? Color.red : Color.green;

            Color flashColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.100f);
            GameObject cellObj = GameObject.Find($"GridCell_{x}_{y}");
            if (cellObj != null)
            {
                GridCellHighlighter highlighter = cellObj.GetComponent<GridCellHighlighter>();
                if (highlighter != null)
                {
                    if (cardData.baseOrEvo == CardSO.BaseOrEvo.Evolution)
                    {
                        highlighter.SetPersistentHighlight(flashColor);
                    }
                    else
                    {
                        highlighter.FlashHighlight(flashColor);
                    }
                }
            }
        }

        // If it's a Spell, remove it shortly
        if (cardData.category == CardSO.CardCategory.Spell)
        {
            bool isAI = (handler != null && handler.isAI);
            Debug.Log($"[GridManager] Removing spell {cardData.cardName} soon.");
            StartCoroutine(RemoveSpellAfterDelay(x, y, cardData, isAI));
        }
        else
        {
            Debug.Log($"[GridManager] {cardData.cardName} remains on the grid.");
        }

        // ------------------
        // (7) WIN CONDITION CHECK
        // ------------------
        if (cardData.category != CardSO.CardCategory.Spell && !HasSelfDestructEffect(cardData))
        {
            Debug.Log("[GridManager] Checking for win condition now...");
            GameManager.instance.CheckForWin();
        }

        // ------------------
        // (8) PROCESS EFFECTS IMMEDIATELY
        // ------------------
        CardUI cardUIComp = cardObj.GetComponent<CardUI>();
        if (cardUIComp != null)
        {
            // (a) Asset-based effects
            if (cardUIComp.cardData.effects != null)
            {
                foreach (CardEffect effect in cardUIComp.cardData.effects)
                {
                    Debug.Log($"[GridManager] Applying asset-based effect on {cardUIComp.cardData.cardName}: {effect.GetType().Name}");
                    effect.ApplyEffect(cardUIComp);

                    // (optional) track them in activeInlineEffects if you prefer
                    // cardUIComp.activeInlineEffects.Add(effect);
                }
            }

            // (b) Inline Effects
            if (cardUIComp.cardData.inlineEffects != null)
            {
                foreach (var inlineEffect in cardUIComp.cardData.inlineEffects)
                {
                    Debug.Log($"[GridManager] Processing inline effect for {cardUIComp.cardData.cardName} with type {inlineEffect.effectType}");

                    // 1) MutualConditionalPowerBoostEffect
                    if (inlineEffect.effectType == CardEffectData.EffectType.MutualConditionalPowerBoostEffect)
                    {
                        Debug.Log("Creating a runtime instance of MutualConditionalPowerBoostEffect for inline synergy...");
                        MutualConditionalPowerBoostEffect synergyEffect = ScriptableObject.CreateInstance<MutualConditionalPowerBoostEffect>();

                        synergyEffect.boostAmount = inlineEffect.powerChange;
                        synergyEffect.requiredCardNames = inlineEffect.requiredCreatureNames.ToArray();

                        synergyEffect.ApplyEffect(cardUIComp);
                        cardUIComp.activeInlineEffects.Add(synergyEffect);
                    }
                    // 2) DrawOnSummon
                    else if (inlineEffect.effectType == CardEffectData.EffectType.DrawOnSummon)
                    {
                        Debug.Log("Applying DrawOnSummon effect...");
                        if (TurnManager.currentPlayerManager != null)
                        {
                            for (int i = 0; i < inlineEffect.cardsToDraw; i++)
                            {
                                TurnManager.currentPlayerManager.DrawCard();
                            }
                        }
                    }
                    // 3) ConditionalPowerBoostEffect
                    else if (inlineEffect.effectType == CardEffectData.EffectType.ConditionalPowerBoost)
                    {
                        Debug.Log("Creating a runtime instance of ConditionalPowerBoostEffect for inline synergy...");
                        ConditionalPowerBoostEffect synergyEffect = ScriptableObject.CreateInstance<ConditionalPowerBoostEffect>();

                        synergyEffect.boostAmount = inlineEffect.powerChange;
                        synergyEffect.requiredCardNames = inlineEffect.requiredCreatureNames.ToArray();

                        synergyEffect.ApplyEffect(cardUIComp);
                        cardUIComp.activeInlineEffects.Add(synergyEffect);
                    }
                    // 4) MultipleTargetPowerBoostEffect
                    else if (inlineEffect.effectType == CardEffectData.EffectType.MultipleTargetPowerBoost)
                    {
                        Debug.Log("Creating a runtime instance of MultipleTargetPowerBoostEffect for target selection...");
                        MultipleTargetPowerBoostEffect boostEffect = ScriptableObject.CreateInstance<MultipleTargetPowerBoostEffect>();

                        // AI auto-selects targets
                        if (isAICard)
                        {
                            List<CardUI> aiTargets = new List<CardUI>();
                            for (int gx = 0; gx < 3; gx++)
                            {
                                for (int gy = 0; gy < 3; gy++)
                                {
                                    GameObject maybeCard = gridObjects[gx, gy];
                                    if (maybeCard == null) continue;

                                    CardHandler maybeHandler = maybeCard.GetComponent<CardHandler>();
                                    CardUI maybeUI = maybeCard.GetComponent<CardUI>();

                                    if (maybeHandler != null && maybeUI != null && maybeHandler.isAI)
                                    {
                                        aiTargets.Add(maybeUI);
                                        if (aiTargets.Count >= 3) break;
                                    }
                                }
                                if (aiTargets.Count >= 3) break;
                            }
                            boostEffect.targetCards = aiTargets;
                            boostEffect.ApplyEffect(cardUIComp);
                            Debug.Log($"[AI PowerBoost] Applied effect to {aiTargets.Count} targets.");
                        }
                        // Local player picks targets interactively
                        else
                        {
                            if (TargetSelectionManager.Instance != null)
                            {
                                TargetSelectionManager.Instance.StartTargetSelection(boostEffect);
                                Debug.Log("Please click on up to 3 target cards on the board for the boost effect.");
                            }
                            else
                            {
                                Debug.LogWarning("TargetSelectionManager instance not found!");
                            }
                        }
                    }
                    // 5) AdjustPowerAdjacentEffect (NEW EXAMPLE)
                    else if (inlineEffect.effectType == CardEffectData.EffectType.AdjustPowerAdjacent)
                    {
                        Debug.Log("Creating a runtime instance of AdjustPowerAdjacentEffect for adjacency synergy...");

                        // 1) Create a brand-new instance so we don't use the asset's inspector values
                        AdjustPowerAdjacentEffect adjacencyEffect = ScriptableObject.CreateInstance<AdjustPowerAdjacentEffect>();

                        // 2) Override the effect’s fields with the inline data

                        // - powerChangeAmount
                        adjacencyEffect.powerChangeAmount = inlineEffect.powerChangeAmount;

                        // - powerChangeType (Increase vs Decrease)
                        //   assuming inlineEffect.powerChangeType is an enum like "Increase" or "Decrease"
                        adjacencyEffect.powerChangeType =
                            (inlineEffect.powerChangeType == CardEffectData.PowerChangeType.Decrease)
                                ? AdjustPowerAdjacentEffect.PowerChangeType.Decrease
                                : AdjustPowerAdjacentEffect.PowerChangeType.Increase;

                        // - targetPositions (list of AdjacentPosition)
                        adjacencyEffect.targetPositions = new List<AdjustPowerAdjacentEffect.AdjacentPosition>();
                        foreach (var pos in inlineEffect.targetPositions)
                        {
                            // cast from your CardEffectData’s adjacency enum to the effect’s adjacency enum
                            adjacencyEffect.targetPositions.Add((AdjustPowerAdjacentEffect.AdjacentPosition)pos);
                        }

                        // 3) Apply the effect & track it
                        adjacencyEffect.ApplyEffect(cardUIComp);
                        cardUIComp.activeInlineEffects.Add(adjacencyEffect);
                    }
                }
            }
        }

        return true;
    }













    // -------------------------------------------------------------------------
    // [NEW HELPER] ShowEvolutionSplash
    // Called if baseCardName != null and this is an Evolution card.
    // -------------------------------------------------------------------------
    public void ShowEvolutionSplash(string baseName, string evoName)
    {
        Debug.Log($"[GridManager] ShowEvolutionSplash called with baseName='{baseName}', evoName='{evoName}'");

        if (evolutionSplashPrefab == null)
        {
            Debug.LogWarning("No evolutionSplashPrefab assigned in GridManager!");
            return;
        }

        // Try to find the overlay canvas by name
        GameObject overlayCanvas = GameObject.Find("OverlayCanvas");
        if (overlayCanvas == null)
        {
            Debug.LogWarning("OverlayCanvas not found. Make sure you have one named OverlayCanvas in the scene.");
            return;
        }

        GameObject splashObj = Instantiate(evolutionSplashPrefab, overlayCanvas.transform);
        EvolutionSplashUI splashUI = splashObj.GetComponent<EvolutionSplashUI>();
        if (splashUI != null)
        {
            splashUI.Setup(baseName, evoName);
        }
        else
        {
            Debug.LogWarning("EvolutionSplashPrefab is missing an EvolutionSplashUI component!");
        }
    }

    private bool HasSelfDestructEffect(CardSO cardData)
    {
        if (cardData.effects != null)
        {
            foreach (CardEffect effect in cardData.effects)
            {
                if (effect is X1DamianoEffect)
                    return true;
            }
        }
        return false;
    }

    private IEnumerator RemoveSpellAfterDelay(int x, int y, CardSO card, bool isAI)
    {
        yield return new WaitForSeconds(1f);
        if (grid[x, y] == card)
        {
            Debug.Log($"[GridManager] Removing spell {card.cardName} from ({x},{y}) after delay.");
            RemoveCard(x, y, isAI);
        }
        else
        {
            Debug.Log($"[GridManager] {card.cardName} is no longer at ({x},{y}) by removal time.");
        }
    }

    public void RemoveCard(int x, int y, bool isAI = false)
    {
        if (grid[x, y] != null)
        {
            CardSO removedCard = grid[x, y];
            GameObject cardObj = gridObjects[x, y];

            Debug.Log($"[GridManager] Removing {removedCard.cardName} at ({x},{y}).");

            FloatingText[] floatingTexts = cardObj.GetComponentsInChildren<FloatingText>(true);
            foreach (FloatingText ft in floatingTexts)
            {
                Destroy(ft.gameObject);
            }

            // NEW CODE: remove inline & asset-based effects
            CardUI occupantUI = cardObj.GetComponent<CardUI>();
            if (occupantUI != null)
            {
                // 1. remove any active inline synergy effects
                if (occupantUI.activeInlineEffects != null)
                {
                    foreach (CardEffect eff in occupantUI.activeInlineEffects)
                    {
                        eff.RemoveEffect(occupantUI);  // Ensure we clean up any active effects
                    }
                    occupantUI.activeInlineEffects.Clear();
                }

                // 2. remove any asset-based effects
                if (occupantUI.cardData.effects != null)
                {
                    foreach (CardEffect eff in occupantUI.cardData.effects)
                    {
                        eff.RemoveEffect(occupantUI);  // Ensure asset-based effects are cleaned up
                    }
                }
            }

            grid[x, y] = null;
            gridObjects[x, y] = null;

            ResetCellVisual(x, y);

            PlayerManager pm = cardObj.GetComponent<CardHandler>().cardOwner;
            if (pm != null)
            {
                pm.zones.AddCardToGrave(cardObj);
            }
            else
            {
                Debug.LogError("Zones instance is null!");
            }

            if (audioSource != null && removeCardSound != null)
                audioSource.PlayOneShot(removeCardSound);

            Debug.Log($"[GridManager] Moved {removedCard.cardName} to {(isAI ? "AI" : "player")} grave.");
        }
    }




    public void RemoveCardWithoutWinCheck(GameObject cardObj)
    {
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                if (gridObjects[x, y] == cardObj)
                {
                    CardSO removedCard = grid[x, y];
                    grid[x, y] = null;
                    gridObjects[x, y] = null;

                    ResetCellVisual(x, y);

                    // NEW CODE: remove inline & asset-based effects here too
                    CardUI occupantUI = cardObj.GetComponent<CardUI>();
                    if (occupantUI != null)
                    {
                        if (occupantUI.activeInlineEffects != null)
                        {
                            foreach (CardEffect eff in occupantUI.activeInlineEffects)
                            {
                                eff.RemoveEffect(occupantUI);
                            }
                            occupantUI.activeInlineEffects.Clear();
                        }

                        if (occupantUI.cardData.effects != null)
                        {
                            foreach (CardEffect eff in occupantUI.cardData.effects)
                            {
                                eff.RemoveEffect(occupantUI);
                            }
                        }
                    }

                    CardHandler handler = cardObj.GetComponent<CardHandler>();
                    if (handler != null && handler.cardOwner != null && handler.cardOwner.zones != null)
                    {
                        handler.cardOwner.zones.AddCardToGrave(cardObj);
                    }
                    else
                    {
                        Debug.LogError("RemoveCardWithoutWinCheck: Zones instance is null!");
                    }

                    if (audioSource != null && removeCardSound != null)
                        audioSource.PlayOneShot(removeCardSound);

                    Debug.Log($"[GridManager] Removed {removedCard.cardName} from ({x},{y}) without triggering win check.");
                    return;
                }
            }
        }
        Debug.LogWarning("[GridManager] RemoveCardWithoutWinCheck: Card not found in grid.");
    }

    public void GrabCard()
    {
        isHoldingCard = true;
    }

    public void ReleaseCard()
    {
        isHoldingCard = false;
    }

    public CardSO[,] GetGrid()
    {
        return grid;
    }

    public GameObject[,] GetGridObjects()
    {
        return gridObjects;
    }

    public void ResetGrid()
    {
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (grid[x, y] != null)
                {
                    GameObject occupantObj = gridObjects[x, y];
                    bool occupantIsAI = false;
                    if (occupantObj != null)
                    {
                        CardHandler occHandler = occupantObj.GetComponent<CardHandler>();
                        if (occHandler != null)
                            occupantIsAI = occHandler.isAI;
                    }
                    RemoveCard(x, y, occupantIsAI);
                }
            }
        }
        Debug.Log("[GridManager] Grid Reset!");
    }

    public void ResetCellVisual(int x, int y)
    {
        GameObject cellObj = GameObject.Find($"GridCell_{x}_{y}");
        if (cellObj != null)
        {
            GridCellHighlighter highlighter = cellObj.GetComponent<GridCellHighlighter>();
            if (highlighter != null)
            {
                if (grid[x, y] != null && grid[x, y].baseOrEvo == CardSO.BaseOrEvo.Evolution)
                {
                    Debug.Log($"[GridManager] Not resetting highlight for evolution card at ({x},{y}).");
                }
                else
                {
                    highlighter.ResetHighlight();
                    Debug.Log($"[GridManager] Reset visual for cell ({x},{y}).");
                }
            }
        }
    }

    // ------------------------------------------------
    // Methods for Sacrifice/Evolution placeholders
    // ------------------------------------------------

    public void HighlightEligibleSacrifices(CardUI evoCard)
    {
        if (evoCard == null || evoCard.cardData == null || evoCard.cardData.sacrificeRequirements == null)
        {
            Debug.LogError("HighlightEligibleSacrifices: Invalid evoCard or missing sacrifice requirements.");
            return;
        }
        int currentPlayer = TurnManager.instance.GetCurrentPlayer();
        Debug.Log($"[GridManager] Highlighting valid sacrifices for evolving {evoCard.cardData.cardName}");
        foreach (var req in evoCard.cardData.sacrificeRequirements)
        {
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (grid[x, y] != null && gridObjects[x, y] != null)
                    {
                        CardHandler ch = gridObjects[x, y].GetComponent<CardHandler>();
                        if (ch != null && ch.cardOwner.playerNumber == currentPlayer)
                        {
                            bool match = req.matchByCreatureType
                                ? (grid[x, y].creatureType == req.requiredCardName)
                                : (grid[x, y].cardName == req.requiredCardName);
                            if (match)
                            {
                                ch.ShowSacrificeHighlight();
                                Debug.Log($"[Sacrifice Highlight] {grid[x, y].cardName} is a valid sacrifice.");
                            }
                        }
                    }
                }
            }
        }
    }

    public void ClearSacrificeHighlights()
    {
        Debug.Log("[GridManager] ClearSacrificeHighlights called.");
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (gridObjects[x, y] != null)
                {
                    CardHandler ch = gridObjects[x, y].GetComponent<CardHandler>();
                    if (ch != null)
                    {
                        ch.HideSacrificeHighlight();
                    }
                }
            }
        }
    }

    public void RemoveSacrificeCard(GameObject card)
    {
        Debug.Log("[GridManager] RemoveSacrificeCard called for " + card.name);
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (gridObjects[x, y] == card)
                {
                    RemoveCard(x, y, false);
                    return;
                }
            }
        }
    }

    public void PerformEvolution(CardUI evoCard, GameObject firstSacrifice)
    {
        // Example placeholder for multi-sac evolution.
    }

    public void PerformEvolutionAtCoords(CardUI evoCard, int x, int y)
    {
        GameObject cellObj = GameObject.Find($"GridCell_{x}_{y}");
        if (cellObj == null)
        {
            Debug.LogError($"PerformEvolutionAtCoords: Could not find GridCell_{x}_{y}");
            return;
        }
        Debug.Log($"PerformEvolutionAtCoords: Placing {evoCard.cardData.cardName} at GridCell_{x}_{y}");
        evoCard.transform.SetParent(cellObj.transform, false);
        RectTransform rt = evoCard.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }
        else
        {
            evoCard.transform.localPosition = Vector3.zero;
        }
        grid[x, y] = evoCard.cardData;
        gridObjects[x, y] = evoCard.gameObject;
        GridDropZone dz = cellObj.GetComponent<GridDropZone>();
        if (dz != null)
        {
            dz.isOccupied = true;
        }
        else
        {
            Debug.LogWarning("PerformEvolutionAtCoords: No GridDropZone on target cell.");
        }
        TurnManager.instance.RegisterCardPlay(evoCard.cardData);
        if (FloatingTextManager.instance != null)
        {
            GameObject floatingText = Instantiate(
                FloatingTextManager.instance.floatingTextPrefab,
                evoCard.gameObject.transform.position,
                Quaternion.identity,
                evoCard.gameObject.transform);
            floatingText.transform.localPosition = new Vector3(0, 50f, 0);
            TextMeshProUGUI tmp = floatingText.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = "Power: " + evoCard.GetComponent<CardUI>().CalculateEffectivePower();
            }
        }
        Debug.Log($"[GridManager] Evolution complete: {evoCard.cardData.cardName} placed at ({x},{y}).");
        GridCellHighlighter highlighter = cellObj.GetComponent<GridCellHighlighter>();
        if (highlighter != null)
        {
            Color evoColor = new Color(0f, 1f, 0f, 0.2f);
            highlighter.SetPersistentHighlight(evoColor);
        }
    }

    public void PlaceEvolutionCard(CardUI evoCard, Vector2 targetPos)
    {
        Debug.Log("[GridManager] PlaceEvolutionCard called for " + evoCard.cardData.cardName + " at " + targetPos);
        // Implementation as needed...
    }

    // ------------------------------------------------
    // Inline ReplaceAfterOpponentTurn Effect Methods
    // ------------------------------------------------

    // This method should be called at the end of every turn (e.g., from TurnManager.EndTurn()).

    // ------------------------------------------------
    // Inline ReplaceAfterOpponentTurn Effect Methods
    // ------------------------------------------------

    // This method should be called at the end of every turn (for example, from TurnManager.EndTurn()).
    // ------------------------------------------------
    // Inline ReplaceAfterOpponentTurn Effect Methods
    // ------------------------------------------------

    // This method should be called at the end of every turn (e.g., from TurnManager.EndTurn()).
    public void CheckReplacementEffects()
    {
        // Loop through every cell in the grid.
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                if (grid[i, j] != null)
                {
                    GameObject cardObj = gridObjects[i, j];
                    CardUI cardUI = cardObj.GetComponent<CardUI>();
                    if (cardUI == null)
                        continue;

                    // Use runtime inline effects if available, otherwise fall back to the asset inline effects.
                    var inlineEffects = (cardUI.runtimeInlineEffects != null && cardUI.runtimeInlineEffects.Count > 0)
                        ? cardUI.runtimeInlineEffects
                        : cardUI.cardData.inlineEffects;

                    // Process each inline effect on this card.
                    foreach (var inlineEffect in inlineEffects)
                    {
                        if (inlineEffect.effectType == CardEffectData.EffectType.ReplaceAfterOpponentTurn)
                        {
                            Debug.Log($"[CheckReplacementEffects] {cardUI.cardData.cardName} current turnDelay: {inlineEffect.turnDelay}");

                            // Decrement the turnDelay if greater than 0.
                            if (inlineEffect.turnDelay > 0)
                            {
                                inlineEffect.turnDelay--;
                                Debug.Log($"[CheckReplacementEffects] {cardUI.cardData.cardName} decremented turnDelay to {inlineEffect.turnDelay}");
                            }

                            // When turnDelay is 0 or less, trigger the prompt.
                            if (inlineEffect.turnDelay <= 0)
                            {
                                ShowInlineReplacementPrompt(cardUI, i, j, inlineEffect);
                            }
                        }
                    }
                }
            }
        }
    }

    private void ShowInlineReplacementPrompt(CardUI sourceCardUI, int gridX, int gridY, CardEffectData inlineEffect)
    {
        // Check if the card belongs to the local player.
        CardHandler cardHandler = sourceCardUI.GetComponent<CardHandler>();
        if (cardHandler == null)
        {
            Debug.LogError("[ShowInlineReplacementPrompt] CardHandler missing on cardUI.");
            return;
        }
        PlayerManager owner = cardHandler.cardOwner;
        if (owner == null || owner.playerNumber != TurnManager.instance.localPlayerNumber)
        {
            Debug.Log("[ShowInlineReplacementPrompt] Not showing prompt because the card does not belong to the local player.");
            return;
        }

        // Check that the inline effect has a prompt prefab assigned.
        if (inlineEffect.promptPrefab == null)
        {
            Debug.LogError($"[ShowInlineReplacementPrompt] No promptPrefab assigned for card {sourceCardUI.cardData.cardName}");
            return;
        }

        // Find the canvas named "OverlayCanvas" in the scene.
        GameObject overlayCanvas = GameObject.Find("OverlayCanvas");
        if (overlayCanvas == null)
        {
            Debug.LogError("[ShowInlineReplacementPrompt] OverlayCanvas not found in the scene!");
            return;
        }

        // Instantiate the prompt as a child of OverlayCanvas.
        GameObject promptInstance = Instantiate(inlineEffect.promptPrefab, overlayCanvas.transform);
        ReplaceEffectPrompt prompt = promptInstance.GetComponent<ReplaceEffectPrompt>();
        if (prompt == null)
        {
            Debug.LogError("[ShowInlineReplacementPrompt] The prompt prefab is missing a ReplaceEffectPrompt component!");
            Destroy(promptInstance);
            return;
        }

        // Initialize the prompt with the card's name and effect description.
        prompt.Initialize(sourceCardUI.cardData.cardName, sourceCardUI.cardData.effectDescription);

        // Add a one-time listener to the prompt's OnResponse event.
        prompt.OnResponse.AddListener((bool accepted) =>
        {
            // Remove all listeners and destroy the prompt immediately.
            prompt.OnResponse.RemoveAllListeners();
            Destroy(promptInstance);

            if (accepted)
            {
                ExecuteReplacementInline(sourceCardUI, gridX, gridY);
            }
            else
            {
                Debug.Log($"[Inline Replacement] Replacement declined for {sourceCardUI.cardData.cardName}");
            }
        });
    }





    /// <summary>
    /// Executes the inline replacement effect by removing the source card from the grid,
    /// instantiating a replacement card, and placing it into the same grid cell.
    /// </summary>
    private void ExecuteReplacementInline(CardUI sourceCardUI, int gridX, int gridY)
    {
        string replacementName = sourceCardUI.inlineReplacementCardName;
        bool blockAdditional = sourceCardUI.inlineBlockAdditionalPlays;
        Debug.Log($"[ExecuteReplacementInline] Attempting to replace {sourceCardUI.cardData.cardName} with {replacementName} at cell ({gridX},{gridY})");

        CardSO replacementCard = DeckManager.instance.FindCardByName(replacementName);
        if (replacementCard != null)
        {
            // Remove the source occupant first.
            RemoveCard(gridX, gridY, false);

            // Pass the original card's owner to the replacement.
            PlayerManager owner = sourceCardUI.GetComponent<CardHandler>().cardOwner;
            if (owner == null)
            {
                Debug.LogError($"[ExecuteReplacementInline] Source card '{sourceCardUI.cardData.cardName}' has no owner!");
            }
            GameObject newCardObj = InstantiateReplacementCardInline(replacementCard, owner);

            // Find the cell transform.
            GameObject cellObj = GameObject.Find($"GridCell_{gridX}_{gridY}");
            if (cellObj == null)
            {
                Debug.LogError($"[ExecuteReplacementInline] Grid cell 'GridCell_{gridX}_{gridY}' not found.");
                return;
            }
            Transform cellTransform = cellObj.transform;

            // Place the replacement card.
            PlaceReplacementCard(gridX, gridY, newCardObj, replacementCard, cellTransform);

            // If the replacement card is an Evo, show the splash.
            if (replacementCard.baseOrEvo == CardSO.BaseOrEvo.Evolution)
            {
                Debug.Log($"[ExecuteReplacementInline] Attempting ShowEvolutionSplash with oldCardName='{sourceCardUI.cardData.cardName}' and new evo='{replacementCard.cardName}'");
                GridManager.instance.ShowEvolutionSplash(sourceCardUI.cardData.cardName, replacementCard.cardName);
            }

            Debug.Log($"DEBUG: blockAdditional = {blockAdditional} for {sourceCardUI.cardData.cardName} [inline]");

            if (blockAdditional)
            {
                Debug.Log("Replacing & blocking next turn [inline]!");
                TurnManager.instance.BlockAdditionalCardPlays();
            }
            Debug.Log($"[Inline Replacement] Successfully replaced card at ({gridX},{gridY}) with {replacementName}.");
        }
        else
        {
            Debug.LogError($"[ExecuteReplacementInline] Replacement card not found: {replacementName}");
        }
    }



    /// <summary>
    /// Instantiates a replacement card object for inline replacement.
    /// </summary>
    private GameObject InstantiateReplacementCardInline(CardSO replacementCard, PlayerManager owner)
    {
        GameObject cardPrefab = DeckManager.instance.cardPrefab;
        GameObject newCardObj = Instantiate(cardPrefab);
        CardHandler handler = newCardObj.GetComponent<CardHandler>();
        if (handler != null)
        {
            handler.SetCard(replacementCard, false, false);
            handler.cardOwner = owner;
            Debug.Log($"[InstantiateReplacementCardInline] New replacement card '{replacementCard.cardName}' owner set to: {owner?.playerNumber}");
        }
        else
        {
            Debug.LogError("[InstantiateReplacementCardInline] CardHandler not found on the new card object.");
        }
        return newCardObj;
    }


    /// <summary>
    /// Places a replacement card into the grid cell, bypassing sacrifice checks.
    /// </summary>
    public bool PlaceReplacementCard(int x, int y, GameObject cardObj, CardSO cardData, Transform cellParent)
    {
        // Re-parent & center the card.
        cardObj.transform.SetParent(cellParent, false);
        RectTransform rt = cardObj.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
        }
        else
        {
            cardObj.transform.localPosition = Vector3.zero;
        }

        // Update grid references.
        grid[x, y] = cardData;
        gridObjects[x, y] = cardObj;

        // Updated Ownership Assignment:
        CardHandler handler = cardObj.GetComponent<CardHandler>();
        bool isAICard = false;
        if (handler != null)
        {
            isAICard = (handler.cardOwner != null && handler.cardOwner.playerType == PlayerManager.PlayerTypes.AI);
            Debug.Log($"[PlaceReplacementCard] Forcing ownership for {cardData.cardName}: isAI = {isAICard}, owner = {handler.cardOwner?.playerNumber}");
            handler.isAI = isAICard;
        }

        // Register the card play and optionally play a sound.
        TurnManager.instance.RegisterCardPlay(cardData);
        if (audioSource != null && placeCardSound != null)
            audioSource.PlayOneShot(placeCardSound);

        Debug.Log($"[PlaceReplacementCard] Placed {cardData.cardName} at ({x},{y}).");

        // Floating Text Display.
        if (FloatingTextManager.instance != null)
        {
            GameObject floatingText = Instantiate(
                FloatingTextManager.instance.floatingTextPrefab,
                cardObj.transform.position,
                Quaternion.identity,
                cardObj.transform
            );
            floatingText.transform.localPosition = new Vector3(0, 50f, 0);
            TextMeshProUGUI tmp = floatingText.GetComponent<TextMeshProUGUI>();
            CardUI cardUI = cardObj.GetComponent<CardUI>();
            if (tmp != null && cardUI != null)
                tmp.text = "Power: " + cardUI.CalculateEffectivePower();

            FloatingText ft = floatingText.GetComponent<FloatingText>();
            if (ft != null)
            {
                ft.sourceCard = cardObj;
            }
        }

        // Highlight the cell for non-Spell cards.
        if (cardData.category != CardSO.CardCategory.Spell)
        {
            Color baseColor;
            if (cardData.baseOrEvo == CardSO.BaseOrEvo.Evolution)
                baseColor = Color.green;
            else
                baseColor = isAICard ? Color.red : Color.green;
            Color flashColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.100f);
            GameObject cellObj = GameObject.Find($"GridCell_{x}_{y}");
            if (cellObj != null)
            {
                GridCellHighlighter highlighter = cellObj.GetComponent<GridCellHighlighter>();
                if (highlighter != null)
                {
                    if (cardData.baseOrEvo == CardSO.BaseOrEvo.Evolution)
                        highlighter.SetPersistentHighlight(flashColor);
                    else
                        highlighter.FlashHighlight(flashColor);
                }
            }
        }

        // For Spell cards, queue removal.
        if (cardData.category == CardSO.CardCategory.Spell)
        {
            Debug.Log($"[PlaceReplacementCard] Removing spell {cardData.cardName} soon.");
            StartCoroutine(RemoveSpellAfterDelay(x, y, cardData, isAICard));
        }
        else
        {
            Debug.Log($"[PlaceReplacementCard] {cardData.cardName} remains on the grid.");
        }

        // Process inline & asset-based effects.
        CardUI cardUIComp = cardObj.GetComponent<CardUI>();
        if (cardUIComp != null)
        {
            // (1) Asset-based Effects.
            if (cardUIComp.cardData.effects != null)
            {
                foreach (CardEffect effect in cardUIComp.cardData.effects)
                {
                    Debug.Log($"[PlaceReplacementCard] Applying asset-based effect on {cardUIComp.cardData.cardName}: {effect.GetType().Name}");
                    effect.ApplyEffect(cardUIComp);
                    if (grid[x, y] == null)
                        return true;
                }
            }

            // (2) Inline Effects.
            if (cardUIComp.cardData.inlineEffects != null)
            {
                if (cardUIComp.activeInlineEffects == null)
                {
                    cardUIComp.activeInlineEffects = new System.Collections.Generic.List<CardEffect>();
                }

                foreach (var inlineEffect in cardUIComp.cardData.inlineEffects)
                {
                    Debug.Log($"[PlaceReplacementCard] Processing inline effect for {cardUIComp.cardData.cardName} with type {inlineEffect.effectType}");

                    // Example: DrawOnSummon effect.
                    if (inlineEffect.effectType == CardEffectData.EffectType.DrawOnSummon)
                    {
                        if (TurnManager.currentPlayerManager != null)
                        {
                            for (int i = 0; i < inlineEffect.cardsToDraw; i++)
                            {
                                TurnManager.currentPlayerManager.DrawCard();
                            }
                        }
                    }
                    // Handle ConditionalPowerBoost effect.
                    if (inlineEffect.effectType == CardEffectData.EffectType.ConditionalPowerBoost)
                    {
                        Debug.Log("Creating a runtime instance of ConditionalPowerBoostEffect for inline synergy...");
                        ConditionalPowerBoostEffect synergyEffect = ScriptableObject.CreateInstance<ConditionalPowerBoostEffect>();

                        synergyEffect.boostAmount = inlineEffect.powerChange;
                        synergyEffect.requiredCardNames = inlineEffect.requiredCreatureNames.ToArray();

                        synergyEffect.ApplyEffect(cardUIComp);
                        cardUIComp.activeInlineEffects.Add(synergyEffect);
                    }
                    // Handle MutualConditionalPowerBoostEffect.
                    else if (inlineEffect.effectType == CardEffectData.EffectType.MutualConditionalPowerBoostEffect)
                    {
                        Debug.Log("Creating a runtime instance of MutualConditionalPowerBoostEffect for inline synergy...");
                        MutualConditionalPowerBoostEffect synergyEffect = ScriptableObject.CreateInstance<MutualConditionalPowerBoostEffect>();

                        synergyEffect.boostAmount = inlineEffect.powerChange;
                        synergyEffect.requiredCardNames = inlineEffect.requiredCreatureNames.ToArray();

                        synergyEffect.ApplyEffect(cardUIComp);
                        cardUIComp.activeInlineEffects.Add(synergyEffect);
                    }
                }
            }
        }

        // For non-Spell cards, check for an immediate win.
        if (cardData.category != CardSO.CardCategory.Spell && grid[x, y] == cardData && !HasSelfDestructEffect(cardData))
            if (cardData.category != CardSO.CardCategory.Spell && grid[x, y] == cardData && !HasSelfDestructEffect(cardData))
            {
                Debug.Log("[PlaceReplacementCard] Checking for win condition now...");

                // 🔍 Log the column you think should win (e.g., column 1)
                for (int i = 0; i < 3; i++)
                {
                    GameObject obj = GridManager.instance.GetGridObjects()[i, 1];
                    CardHandler ch = obj?.GetComponent<CardHandler>();
                    var cardName = GridManager.instance.GetGrid()[i, 1]?.cardName;
                    Debug.Log($"[DEBUG] Column 1 - cell ({i},1): {cardName}, Owner: {ch?.cardOwner?.playerNumber}, isAI: {ch?.isAI}");
                }

                GameManager.instance.CheckForWin();
            }

        return true;
    }
}
