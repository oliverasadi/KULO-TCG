using System.Collections;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class GridManager : MonoBehaviour
{
    public static GridManager instance;

    private CardSO[,] grid = new CardSO[3, 3];
    private GameObject[,] gridObjects = new GameObject[3, 3];
    public List<GameObject> cellSelectionCells = new List<GameObject>();


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

    public bool CanPlaceCard(int x, int y, CardUI newCardUI)
    {
        CardSO cardData = newCardUI.cardData;
        int currentPlayer = TurnManager.instance.GetCurrentPlayer();
        int newCardEffectivePower = newCardUI.CalculateEffectivePower();

        // ─── SPELL PLACEMENT RULES ─────────────────────────────────────────────
        if (cardData.category == CardSO.CardCategory.Spell)
        {
            bool needsCreature = cardData.requiresTargetCreature;      // new flag on your CardSO
            bool cellHasCard = (grid[x, y] != null);

            // 1) If this spell must target a creature but the cell is empty, disallow
            if (!cellHasCard && needsCreature)
            {
                Debug.Log($"[CanPlaceCard] {cardData.cardName} requires a creature target, but cell ({x},{y}) is empty.");
                return false;
            }

            // 2) If the cell is occupied, ensure occupant is a Creature—and if the spell requires a creature, that it’s friendly
            if (cellHasCard)
            {
                CardSO occupantData = grid[x, y];
                if (occupantData.category != CardSO.CardCategory.Creature)
                {
                    Debug.Log($"[CanPlaceCard] {cardData.cardName} cannot target non‐creature at ({x},{y}).");
                    return false;
                }

                if (needsCreature)
                {
                    CardHandler occHandler = gridObjects[x, y].GetComponent<CardHandler>();
                    bool isFriendly = occHandler != null && occHandler.cardOwner.playerNumber == currentPlayer;
                    if (!isFriendly)
                    {
                        Debug.Log($"[CanPlaceCard] {cardData.cardName} must target your creature – cannot play on opponent’s card at ({x},{y}).");
                        return false;
                    }
                }

                // Spell is allowed here (occupied by creature, and targeting rules passed)
                return TurnManager.instance.CanPlayCard(cardData);
            }

            // 3) Cell is empty and this spell does *not* require a creature → normal play check
            return TurnManager.instance.CanPlayCard(cardData);
        }

        // ─── CREATURE (AND NON‐SPELL) LOGIC ────────────────────────────────────
        if (grid[x, y] == null)
        {
            // Empty cell → normal summon/play check
            return TurnManager.instance.CanPlayCard(cardData);
        }
        else
        {
            // Occupied by some card → only allow if it’s an opponent’s creature with ≤ power
            CardUI occupantUI = gridObjects[x, y].GetComponent<CardUI>();
            if (occupantUI != null)
            {
                bool occupantIsOpponent =
                    (currentPlayer == 1 && occupantUI.GetComponent<CardHandler>().isAI) ||
                    (currentPlayer == 2 && !occupantUI.GetComponent<CardHandler>().isAI);

                if (occupantIsOpponent)
                {
                    int occupantEffectivePower = occupantUI.CalculateEffectivePower();
                    bool allowed = (newCardEffectivePower >= occupantEffectivePower);
                    Debug.Log($"[GridManager] Replacement at ({x},{y}): occupant power = {occupantEffectivePower}, new card power = {newCardEffectivePower}, allowed = {allowed}");
                    return allowed;
                }
            }
            return false;
        }
    }


    public bool PlaceExistingCard(int x, int y, GameObject cardObj, CardSO cardData, Transform cellParent)
    {
        Debug.Log($"[GridManager] Attempting to place {cardData.cardName} at ({x},{y}). Category: {cardData.category}");

        // ─── SPELLS: EARLY EXIT ────────────────────────────────────────────────
        if (cardData.category == CardSO.CardCategory.Spell)
        {
            // 1) Overlay the spell on the cell
            cardObj.transform.SetParent(cellParent, false);
            var rtSpell = cardObj.GetComponent<RectTransform>();
            if (rtSpell != null)
            {
                rtSpell.anchorMin = rtSpell.anchorMax = rtSpell.pivot = new Vector2(0.5f, 0.5f);
                rtSpell.anchoredPosition = Vector2.zero;
            }
            cardObj.transform.SetAsLastSibling();

            // 2) Mark spell as on-field
            var spellUI = cardObj.GetComponent<CardUI>();
            var spellHandler = cardObj.GetComponent<CardHandler>();
            if (spellUI != null) spellUI.isOnField = true;
            if (spellHandler != null)
                spellHandler.isAI = (spellHandler.cardOwner?.playerType == PlayerManager.PlayerTypes.AI);

            // 3) Register the play & fire callbacks
            TurnManager.instance.RegisterCardPlay(cardData);
            TurnManager.instance.FireOnCardPlayed(cardData);
            Debug.Log($"[GridManager] Spell {cardData.cardName} played at ({x},{y}). Applying effects...");

            // 4) Determine target: creature under the spell (if required) or spellUI itself
            CardUI targetUI = null;
            if (cardData.requiresTargetCreature && grid[x, y] != null)
                targetUI = gridObjects[x, y]?.GetComponent<CardUI>();

            // 5) Apply each configured effect
            if (cardData.effects != null)
            {
                foreach (var effect in cardData.effects)
                {
                    if (targetUI != null)
                    {
                        Debug.Log($"[GridManager] Applying {effect.GetType().Name} from {cardData.cardName} to {targetUI.cardData.cardName}.");
                        effect.ApplyEffect(targetUI);
                    }
                    else if (spellUI != null)
                    {
                        Debug.Log($"[GridManager] Applying {effect.GetType().Name} from {cardData.cardName} (no direct target).");
                        effect.ApplyEffect(spellUI);
                    }
                }
            }

            // 6) Immediately send the spell itself to the graveyard
            if (spellHandler?.cardOwner != null)
            {
                spellHandler.cardOwner.zones.AddCardToGrave(cardObj);
            }
            else
            {
                Debug.LogWarning($"[GridManager] No owner for {cardData.cardName}; destroying object.");
                Destroy(cardObj);
            }

            Debug.Log($"[GridManager] {cardData.cardName} effect applied. No grid change.");
            return true;
        }

        // ─── NON‐SPELL CARDS: FALL THROUGH TO EXISTING LOGIC ───────────────────
        int currentPlayer = TurnManager.instance.GetCurrentPlayer();
        string baseCardName = null;

        // (1) SACRIFICE REQUIREMENTS
        if (cardData.requiresSacrifice && cardData.sacrificeRequirements != null && cardData.sacrificeRequirements.Count > 0)
        {
            PlayerManager pm = (currentPlayer == 1)
                ? TurnManager.instance.playerManager1
                : TurnManager.instance.playerManager2;

            // Verify requirements
            foreach (var req in cardData.sacrificeRequirements)
            {
                int foundOnField = 0;
                if (req.allowFromField)
                {
                    for (int i = 0; i < 3; i++)
                        for (int j = 0; j < 3; j++)
                            if (grid[i, j] != null)
                            {
                                bool match = req.matchByCreatureType
                                    ? grid[i, j].creatureType == req.requiredCardName
                                    : grid[i, j].cardName == req.requiredCardName;
                                var occ = gridObjects[i, j].GetComponent<CardHandler>();
                                if (match && occ != null && occ.cardOwner.playerNumber == currentPlayer)
                                    foundOnField++;
                            }
                }

                int foundInHand = 0;
                if (req.allowFromHand && pm != null)
                {
                    foreach (var handCard in pm.cardHandlers)
                    {
                        if (handCard?.cardData == null) continue;
                        bool match = req.matchByCreatureType
                            ? handCard.cardData.creatureType == req.requiredCardName
                            : handCard.cardData.cardName == req.requiredCardName;
                        if (match) foundInHand++;
                    }
                }

                int totalFound = foundOnField + foundInHand;
                Debug.Log($"[Sacrifice Check] For {req.requiredCardName}: onField={foundOnField}, inHand={foundInHand}, total={totalFound}, need={req.count}.");
                if (totalFound < req.count)
                {
                    Debug.Log($"Cannot place {cardData.cardName}: requirement not met (need {req.count}, found {totalFound}).");
                    return false;
                }
            }

            // Perform sacrifices
            foreach (var req in cardData.sacrificeRequirements)
            {
                int toSacrifice = req.count;
                if (req.allowFromField)
                {
                    for (int i = 0; i < 3 && toSacrifice > 0; i++)
                        for (int j = 0; j < 3 && toSacrifice > 0; j++)
                            if (grid[i, j] != null)
                            {
                                bool match = req.matchByCreatureType
                                    ? grid[i, j].creatureType == req.requiredCardName
                                    : grid[i, j].cardName == req.requiredCardName;
                                var occ = gridObjects[i, j].GetComponent<CardHandler>();
                                if (match && occ != null && occ.cardOwner.playerNumber == currentPlayer)
                                {
                                    if (baseCardName == null) baseCardName = grid[i, j].cardName;
                                    Debug.Log($"Sacrificing {grid[i, j].cardName} at ({i},{j}) for {cardData.cardName}.");
                                    RemoveCard(i, j, false);
                                    toSacrifice--;
                                }
                            }
                }

                if (req.allowFromHand && toSacrifice > 0 && pm != null)
                {
                    for (int h = pm.cardHandlers.Count - 1; h >= 0 && toSacrifice > 0; h--)
                    {
                        var handCard = pm.cardHandlers[h];
                        if (handCard?.cardData == null) continue;
                        bool match = req.matchByCreatureType
                            ? handCard.cardData.creatureType == req.requiredCardName
                            : handCard.cardData.cardName == req.requiredCardName;
                        if (match)
                        {
                            if (baseCardName == null) baseCardName = handCard.cardData.cardName;
                            Debug.Log($"Sacrificing {handCard.cardData.cardName} from hand for {cardData.cardName}.");
                            pm.zones.AddCardToGrave(handCard.gameObject);
                            pm.cardHandlers.RemoveAt(h);
                            toSacrifice--;
                        }
                    }
                }
            }
        }

        // (2) OCCUPANT REPLACEMENT LOGIC
        if (grid[x, y] != null)
        {
            // Prevent spells replacing creatures
            if (cardData.category == CardSO.CardCategory.Spell && grid[x, y].category == CardSO.CardCategory.Creature)
            {
                Debug.Log($"[GridManager] Spells have no power and cannot replace Creature cards. {cardData.cardName} cannot replace {grid[x, y].cardName}.");
                return false;
            }

            float occupantPower = gridObjects[x, y].GetComponent<CardUI>().CalculateEffectivePower();
            float newPower = cardObj.GetComponent<CardUI>().CalculateEffectivePower();

            if (occupantPower > newPower)
            {
                Debug.Log($"Cannot replace {grid[x, y].cardName} at ({x},{y}) - occupant power ({occupantPower}) > new card power ({newPower}).");
                return false;
            }
            else if (Mathf.Approximately(occupantPower, newPower))
            {
                if (cardData.category == CardSO.CardCategory.Spell)
                {
                    Debug.Log($"[GridManager] Spell {cardData.cardName} cannot destroy Creature {grid[x, y].cardName} on equal power.");
                    return false;
                }
                Debug.Log($"Equal power at ({x},{y}). Destroying both occupant and new card.");
                var occHandler = gridObjects[x, y].GetComponent<CardHandler>();
                RemoveCard(x, y, occHandler != null && occHandler.isAI);
                var newHandler = cardObj.GetComponent<CardHandler>();
                newHandler?.cardOwner?.zones.AddCardToGrave(cardObj);
                TurnManager.instance.RegisterCardPlay(cardData);
                if (cardData.baseOrEvo != CardSO.BaseOrEvo.Evolution)
                    ResetCellVisual(x, y);
                return true;
            }
            else
            {
                Debug.Log($"Replacing occupant {grid[x, y].cardName} at ({x},{y}) with {cardData.cardName}.");
                var occHandler = gridObjects[x, y].GetComponent<CardHandler>();
                RemoveCard(x, y, occHandler != null && occHandler.isAI);
            }
        }

        // (3) RE-PARENT & CENTER THE CARD
        cardObj.transform.SetParent(cellParent, false);
        var rt2 = cardObj.GetComponent<RectTransform>();
        if (rt2 != null)
        {
            rt2.anchorMin = rt2.anchorMax = rt2.pivot = new Vector2(0.5f, 0.5f);
            rt2.anchoredPosition = Vector2.zero;
        }
        else
        {
            cardObj.transform.localPosition = Vector3.zero;
        }

        // (4) UPDATE GRID & OWNERSHIP
        grid[x, y] = cardData;
        gridObjects[x, y] = cardObj;
        var uiComp = cardObj.GetComponent<CardUI>();
        if (uiComp != null) uiComp.isOnField = true;
        var handler = cardObj.GetComponent<CardHandler>();
        bool isAICard = (handler?.cardOwner?.playerType == PlayerManager.PlayerTypes.AI);
        if (handler != null) handler.isAI = isAICard;

        // (5) REGISTER & FIRE & SOUND
        TurnManager.instance.RegisterCardPlay(cardData);
        audioSource?.PlayOneShot(placeCardSound);
        TurnManager.instance.FireOnCardPlayed(cardData);

        // ------------------
        // (6) EVOLUTION SPLASH
        // ------------------
        if (cardData.baseOrEvo == CardSO.BaseOrEvo.Evolution)
            ShowEvolutionSplash((baseCardName ?? "Base"), cardData.cardName);

        // ------------------
        // (7) FLOATING TEXT & HIGHLIGHTING
        // ------------------
        if (FloatingTextManager.instance != null)
        {
            var floatingText = Instantiate(
                FloatingTextManager.instance.floatingTextPrefab,
                cardObj.transform.position,
                Quaternion.identity,
                cardObj.transform
            );
            floatingText.transform.localPosition = new Vector3(0, 50f, 0);
            var tmp = floatingText.GetComponent<TextMeshProUGUI>();
            if (tmp != null && uiComp != null)
                tmp.text = "Power: " + uiComp.CalculateEffectivePower();
            var ft = floatingText.GetComponent<FloatingText>();
            if (ft != null) ft.sourceCard = cardObj;
        }

        if (cardData.category != CardSO.CardCategory.Spell)
        {
            Color baseColor = isAICard ? Color.red : Color.green;
            Color flashColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.5f);
            var cellObj = GameObject.Find($"GridCell_{x}_{y}");
            cellObj?.GetComponent<GridCellHighlighter>()?.FlashHighlight(flashColor);
        }
        else
        {
            // schedule removal of spells played on empty cells
            StartCoroutine(RemoveSpellAfterDelay(x, y, cardData, isAICard));
        }

        // ------------------
        // (9) WIN CHECK
        // ------------------
        if (cardData.category != CardSO.CardCategory.Spell && !HasSelfDestructEffect(cardData))
            GameManager.instance.CheckForWin();

        // ------------------
        // (9) PROCESS EFFECTS IMMEDIATELY
        // ------------------
        var cardUIComp = cardObj.GetComponent<CardUI>();
        if (cardUIComp != null)
        {
            if (cardUIComp.cardData.effects != null)
                foreach (var effect in cardUIComp.cardData.effects)
                {
                    Debug.Log($"[GridManager] Applying {effect.GetType().Name} on {cardUIComp.cardData.cardName}");
                    effect.ApplyEffect(cardUIComp);
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
                        var synergyEffect = ScriptableObject.CreateInstance<MutualConditionalPowerBoostEffect>();
                        synergyEffect.boostAmount = inlineEffect.powerChange;
                        synergyEffect.requiredCardNames = inlineEffect.requiredCreatureNames.ToArray();
                        synergyEffect.requiredCreatureTypes = inlineEffect.requiredCreatureTypes.ToArray();
                        synergyEffect.searchOwner = (MutualConditionalPowerBoostEffect.SearchOwnerOption)(int)inlineEffect.searchOwner;
                        synergyEffect.ApplyEffect(cardUIComp);
                        cardUIComp.activeInlineEffects.Add(synergyEffect);
                    }
                    // 2) DrawOnSummon
                    else if (inlineEffect.effectType == CardEffectData.EffectType.DrawOnSummon)
                    {
                        Debug.Log("Applying DrawOnSummon effect...");
                        if (TurnManager.currentPlayerManager != null)
                            for (int i = 0; i < inlineEffect.cardsToDraw; i++)
                                TurnManager.currentPlayerManager.DrawCard();
                    }
                    // 3) ConditionalPowerBoostEffect
                    else if (inlineEffect.effectType == CardEffectData.EffectType.ConditionalPowerBoost)
                    {
                        Debug.Log("Creating a runtime instance of ConditionalPowerBoostEffect for inline synergy...");
                        var condEffect = ScriptableObject.CreateInstance<ConditionalPowerBoostEffect>();
                        condEffect.boostAmount = inlineEffect.powerChange;
                        condEffect.requiredCardNames = inlineEffect.requiredCreatureNames.ToArray();
                        condEffect.useCountMode = inlineEffect.useCountMode;
                        condEffect.ApplyEffect(cardUIComp);
                        cardUIComp.activeInlineEffects.Add(condEffect);
                    }
                    // 4) MultipleTargetPowerBoostEffect
                    else if (inlineEffect.effectType == CardEffectData.EffectType.MultipleTargetPowerBoost)
                    {
                        Debug.Log("Creating a runtime instance of MultipleTargetPowerBoostEffect for target selection...");
                        var boostEffect = ScriptableObject.CreateInstance<MultipleTargetPowerBoostEffect>();
                        if (handler != null && handler.isAI)
                        {
                            var aiTargets = new List<CardUI>();
                            for (int gx = 0; gx < 3; gx++) for (int gy = 0; gy < 3; gy++)
                                {
                                    var mCard = gridObjects[gx, gy];
                                    var mH = mCard?.GetComponent<CardHandler>();
                                    var mUI = mCard?.GetComponent<CardUI>();
                                    if (mH != null && mUI != null && mH.isAI)
                                    {
                                        aiTargets.Add(mUI);
                                        if (aiTargets.Count >= 3) break;
                                    }
                                }
                            boostEffect.targetCards = aiTargets;
                            boostEffect.ApplyEffect(cardUIComp);
                            Debug.Log($"[AI PowerBoost] Applied effect to {aiTargets.Count} targets.");
                        }
                        else if (TargetSelectionManager.Instance != null)
                        {
                            TargetSelectionManager.Instance.StartTargetSelection(boostEffect);
                            Debug.Log("Please click on up to 3 target cards on the board for the boost effect.");
                        }
                        else Debug.LogWarning("TargetSelectionManager instance not found!");
                    }
                    // 5) AdjustPowerAdjacentEffect
                    else if (inlineEffect.effectType == CardEffectData.EffectType.AdjustPowerAdjacent)
                    {
                        Debug.Log("Creating a runtime instance of AdjustPowerAdjacentEffect for adjacency synergy...");
                        var adjEffect = ScriptableObject.CreateInstance<AdjustPowerAdjacentEffect>();
                        adjEffect.powerChangeAmount = inlineEffect.powerChangeAmount;
                        adjEffect.powerChangeType = (inlineEffect.powerChangeType == CardEffectData.PowerChangeType.Decrease)
                            ? AdjustPowerAdjacentEffect.PowerChangeType.Decrease
                            : AdjustPowerAdjacentEffect.PowerChangeType.Increase;
                        adjEffect.ownerToAffect = inlineEffect.adjacencyOwnerToAffect;
                        adjEffect.targetPositions = new List<AdjustPowerAdjacentEffect.AdjacentPosition>();
                        foreach (var pos in inlineEffect.targetPositions)
                            adjEffect.targetPositions.Add((AdjustPowerAdjacentEffect.AdjacentPosition)pos);
                        adjEffect.ApplyEffect(cardUIComp);
                        cardUIComp.activeInlineEffects.Add(adjEffect);
                    }
                }
            }
        }

        UpdateMutualConditionalEffects();
        return true;
    }


    public void UpdateMutualConditionalEffects()
    {
        CardSO[,] field = GetGrid();
        GameObject[,] gridObjs = GetGridObjects();

        for (int x = 0; x < field.GetLength(0); x++)
        {
            for (int y = 0; y < field.GetLength(1); y++)
            {
                if (gridObjs[x, y] != null)
                {
                    CardUI ui = gridObjs[x, y].GetComponent<CardUI>();
                    if (ui != null && ui.activeInlineEffects != null)
                    {
                        foreach (var eff in ui.activeInlineEffects)
                        {
                            if (eff is MutualConditionalPowerBoostEffect mEffect)
                            {
                                mEffect.OnFieldChanged(null); // Trigger re-evaluation
                            }
                        }
                    }
                }
            }
        }
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
    public void EnableCellSelectionMode(System.Action<int, int> cellSelectedCallback)
    {
        int currentPlayer = TurnManager.instance.GetCurrentPlayer();
        int evolvingCardPower = 0;
        // Retrieve evolving card power from SacrificeManager using the public property.
        if (SacrificeManager.instance != null && SacrificeManager.instance.CurrentEvolutionCard != null)
        {
            evolvingCardPower = SacrificeManager.instance.CurrentEvolutionCard.GetComponent<CardUI>().CalculateEffectivePower();
        }

        // Loop through all grid cells (assuming a 3x3 grid)
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                // Allow selection if the cell is empty OR occupied by an opponent’s card that can be replaced.
                if (grid[x, y] == null ||
                    (grid[x, y] != null && !IsOwnedByPlayer(x, y, currentPlayer) && evolvingCardPower > grid[x, y].power))
                {
                    GameObject cellObj = GameObject.Find($"GridCell_{x}_{y}");
                    if (cellObj != null)
                    {
                        GridCellHighlighter highlighter = cellObj.GetComponent<GridCellHighlighter>();
                        if (highlighter != null)
                        {
                            // Clear any previously stored persistent state so we re–store the current highlight.
                            highlighter.ClearStoredPersistentHighlight();
                            // Apply the temporary yellow highlight.
                            highlighter.SetPersistentHighlight(new Color(1f, 1f, 0f, 0.5f));
                            highlighter.isSacrificeHighlight = true; // Mark it as a sacrifice highlight.

                            // Add this cellObj to the selection list so we can restore it later.
                            if (!cellSelectionCells.Contains(cellObj))
                            {
                                cellSelectionCells.Add(cellObj);
                            }
                        }
                        Button btn = cellObj.GetComponent<Button>();
                        if (btn == null)
                        {
                            btn = cellObj.AddComponent<Button>();
                        }
                        btn.onClick.RemoveAllListeners();
                        // Capture x and y in local variables.
                        int capturedX = x, capturedY = y;
                        btn.onClick.AddListener(() =>
                        {
                            // When a cell is clicked, disable selection immediately.
                            DisableCellSelectionMode();
                            if (cellSelectedCallback != null)
                            {
                                cellSelectedCallback(capturedX, capturedY);
                            }
                        });
                        // Enable the button in case it was disabled.
                        btn.enabled = true;
                    }
                }
            }
        }
    }










    public bool IsOwnedByPlayer(int x, int y, int playerNumber)
    {
        // If there's no card in the cell, it's not owned by anyone.
        if (gridObjects[x, y] == null)
            return false;

        // Try to get the CardHandler from the cell's GameObject.
        CardHandler handler = gridObjects[x, y].GetComponent<CardHandler>();
        // If no handler or card owner is found, assume not owned.
        if (handler == null || handler.cardOwner == null)
            return false;

        // Check if the card owner's player number matches the provided number.
        return handler.cardOwner.playerNumber == playerNumber;
    }





    public void RemoveCard(int x, int y, bool isAI = false)
    {
        if (grid[x, y] != null)
        {
            CardSO removedCard = grid[x, y];
            GameObject cardObj = gridObjects[x, y];

            Debug.Log($"[GridManager] Removing {removedCard.cardName} at ({x},{y}).");

            // Stop the sacrifice hover effect before removal.
            CardUI occupantUI = cardObj.GetComponent<CardUI>();
            if (occupantUI != null)
            {
                occupantUI.ResetSacrificeHoverEffect();
            }

            FloatingText[] floatingTexts = cardObj.GetComponentsInChildren<FloatingText>(true);
            foreach (FloatingText ft in floatingTexts)
            {
                Destroy(ft.gameObject);
            }

            // Remove inline & asset-based effects.
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

            // NEW LINE: Notify synergy effects that the board changed
            // If your synergy is subscribed to OnCardPlayed, it will re-check now.
            TurnManager.instance.FireOnCardPlayed(removedCard);
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
            // 1) Highlight from the field (grid)
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
                                Debug.Log($"[Sacrifice Highlight] {grid[x, y].cardName} is a valid sacrifice (on field).");
                            }
                        }
                    }
                }
            }

            // 2) Highlight from the hand if allowed by the requirement
            if (req.allowFromHand)
            {
                PlayerManager pm = (currentPlayer == 1)
                    ? TurnManager.instance.playerManager1
                    : TurnManager.instance.playerManager2;

                if (pm == null)
                {
                    Debug.LogWarning("HighlightEligibleSacrifices: PlayerManager is null.");
                    continue;
                }

                foreach (CardHandler handCard in pm.cardHandlers)
                {
                    if (handCard != null && handCard.cardData != null)
                    {
                        bool match = req.matchByCreatureType
                            ? (handCard.cardData.creatureType == req.requiredCardName)
                            : (handCard.cardData.cardName == req.requiredCardName);

                        // Ensure the card is actually in hand (i.e. not on the field)
                        CardUI handCardUI = handCard.GetComponent<CardUI>();
                        if (match && handCardUI != null && !handCardUI.isOnField)
                        {
                            handCard.ShowSacrificeHighlight();
                            Debug.Log($"[Sacrifice Highlight] {handCard.cardData.cardName} is a valid sacrifice (in hand).");
                        }
                    }
                }
            }
        }
    }



    public void ClearSacrificeHighlights()
    {
        Debug.Log("[GridManager] ClearSacrificeHighlights called.");

        // Only clear cells that were enabled for sacrifice selection.
        foreach (GameObject cellObj in cellSelectionCells)
        {
            if (cellObj != null)
            {
                GridCellHighlighter highlighter = cellObj.GetComponent<GridCellHighlighter>();
                if (highlighter != null)
                {
                    // If the cell had a persistent highlight before cell selection,
                    // restore it; otherwise, reset to default.
                    if (highlighter.HasStoredPersistentHighlight)
                    {
                        highlighter.RestoreHighlight();
                    }
                    else
                    {
                        highlighter.ResetHighlight();
                    }
                    highlighter.isSacrificeHighlight = false;
                }
            }
        }
        // Clear our list since we've handled these cells.
        cellSelectionCells.Clear();

        // Also clear sacrifice highlights from cards in hand.
        int currentPlayer = TurnManager.instance.GetCurrentPlayer();
        PlayerManager pm = (currentPlayer == 1)
            ? TurnManager.instance.playerManager1
            : TurnManager.instance.playerManager2;
        if (pm != null)
        {
            foreach (CardHandler handCard in pm.cardHandlers)
            {
                handCard.HideSacrificeHighlight();
            }
        }
    }


    public void DisableCellSelectionMode()
    {
        // Only disable buttons and clear highlights on cells that were marked.
        foreach (GameObject cellObj in cellSelectionCells)
        {
            if (cellObj != null)
            {
                Button btn = cellObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.enabled = false;
                }
                GridCellHighlighter highlighter = cellObj.GetComponent<GridCellHighlighter>();
                if (highlighter != null)
                {
                    // Only clear the highlight if the cell was marked for sacrifice selection.
                    if (highlighter.isSacrificeHighlight)
                    {
                        if (highlighter.HasStoredPersistentHighlight)
                        {
                            highlighter.RestoreHighlight();
                        }
                        else
                        {
                            highlighter.ResetHighlight();
                        }
                        highlighter.isSacrificeHighlight = false;
                    }
                }
            }
        }
        cellSelectionCells.Clear();
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

        // Check if the cell is occupied.
        if (grid[x, y] != null)
        {
            // Retrieve the CardUI and CardHandler for the occupant.
            CardUI occupantUI = gridObjects[x, y].GetComponent<CardUI>();
            CardHandler occupantHandler = gridObjects[x, y].GetComponent<CardHandler>();
            if (occupantUI == null || occupantHandler == null)
            {
                Debug.LogError("PerformEvolutionAtCoords: Missing CardUI or CardHandler on the occupant.");
                return;
            }

            // Only proceed with replacement if the occupant is an AI card.
            if (occupantHandler.isAI)
            {
                float occupantEffectivePower = occupantUI.CalculateEffectivePower();
                float evoEffectivePower = evoCard.GetComponent<CardUI>().CalculateEffectivePower();

                Debug.Log($"[PerformEvolutionAtCoords] Occupant effective power: {occupantEffectivePower}, Evolution effective power: {evoEffectivePower}");

                // Remove the occupant only if the evolution's effective power is greater.
                if (evoEffectivePower > occupantEffectivePower)
                {
                    Debug.Log($"[PerformEvolutionAtCoords] Removing opponent's card {grid[x, y].cardName} from cell ({x},{y}) because evolution power is higher.");
                    RemoveCard(x, y, true);
                }
                else
                {
                    Debug.Log($"[PerformEvolutionAtCoords] Cannot place {evoCard.cardData.cardName} at ({x},{y}) because the AI occupant's power is equal or higher.");
                    return; // Cancel the placement.
                }
            }
            else
            {
                // If the occupant is not an AI card, do not allow replacement.
                Debug.Log($"[PerformEvolutionAtCoords] Cell ({x},{y}) is occupied by a non-AI card. Evolution replacement is not allowed.");
                return;
            }
        }

        // Place the evolution card in the now free cell.
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

        // Optionally, display floating text showing the card's effective power.
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

        // Set the cell highlight to a persistent evolution color.
        GridCellHighlighter highlighter = cellObj.GetComponent<GridCellHighlighter>();
        if (highlighter != null)
        {
            Color evoColor = new Color(0f, 1f, 0f, 0.2f); // Green for evolution
            highlighter.SetPersistentHighlight(evoColor);
        }

        // Check for a win condition after placing the evolved card
        int newLines = WinChecker.instance.CheckWinCondition(GridManager.instance.GetGrid());
        if (newLines > 0)
        {
            Debug.Log($"[WinChecker] New winning lines formed: {newLines}");
            // Trigger win handling logic here (e.g., display win message or end the round)
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
        for (int i = 0; i < grid.GetLength(0); i++)
        {
            for (int j = 0; j < grid.GetLength(1); j++)
            {
                if (grid[i, j] == null) continue;

                GameObject cardObj = gridObjects[i, j];
                CardUI cardUI = cardObj.GetComponent<CardUI>();
                if (cardUI == null) continue;

                // Use runtime effects if available; otherwise fall back to the original ones.
                var inlineEffects = (cardUI.runtimeInlineEffects != null && cardUI.runtimeInlineEffects.Count > 0)
                    ? cardUI.runtimeInlineEffects
                    : cardUI.cardData.inlineEffects;

                foreach (var inlineEffect in inlineEffects)
                {
                    // 🔁 ReplaceAfterOpponentTurn logic
                    if (inlineEffect.effectType == CardEffectData.EffectType.ReplaceAfterOpponentTurn)
                    {
                        // Only run for local player's turn (so AI turns don’t show the prompt)
                        if (TurnManager.instance.GetCurrentPlayer() == TurnManager.instance.localPlayerNumber)
                        {
                            Debug.Log($"[CheckReplacementEffects] {cardUI.cardData.cardName} current turnDelay: {inlineEffect.turnDelay}");

                            if (inlineEffect.turnDelay > 0)
                            {
                                inlineEffect.turnDelay--;
                                Debug.Log($"[CheckReplacementEffects] {cardUI.cardData.cardName} decremented turnDelay to {inlineEffect.turnDelay}");
                            }

                            if (inlineEffect.turnDelay <= 0)
                            {
                                ShowInlineReplacementPrompt(cardUI, i, j, inlineEffect);
                            }
                        }
                    }

                    // 🔻 LosePowerEachTurn logic (via temporaryBoost)
                    if (inlineEffect.effectType == CardEffectData.EffectType.LosePowerEachTurn)
                    {
                        cardUI.temporaryBoost -= inlineEffect.powerChange;
                        Debug.Log($"[LosePowerEachTurn] {cardUI.cardData.cardName} gets -{inlineEffect.powerChange} temp boost. Now: {cardUI.temporaryBoost}");

                        // Update the card UI
                        cardUI.UpdatePowerDisplay();

                        // Update the info panel if this card is currently viewed
                        if (cardUI.cardInfoPanel != null && cardUI.cardInfoPanel.CurrentCardUI == cardUI)
                        {
                            cardUI.cardInfoPanel.UpdatePowerDisplay();
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
        string replacementName = sourceCardUI.cardData.inlineEffects[0].replacementCardName;
        bool blockAdditional = sourceCardUI.inlineBlockAdditionalPlays;
        Debug.Log($"[ExecuteReplacementInline] Attempting to replace {sourceCardUI.cardData.cardName} with {replacementName} at cell ({gridX},{gridY})");

        CardSO replacementCard = DeckManager.instance.FindCardByName(replacementName);
        if (replacementCard != null)
        {
            // Get the owner from the source card's CardHandler.
            PlayerManager owner = sourceCardUI.GetComponent<CardHandler>().cardOwner;
            if (owner == null)
            {
                Debug.LogError($"[ExecuteReplacementInline] Source card '{sourceCardUI.cardData.cardName}' has no owner!");
                return;
            }

            // Remove the replacement card from the owner's deck.
            if (owner.RemoveCardFromDeck(replacementCard))
            {
                Debug.Log($"[ExecuteReplacementInline] {replacementCard.cardName} removed from deck.");
            }
            else
            {
                Debug.LogWarning($"[ExecuteReplacementInline] {replacementCard.cardName} was not found in the deck.");
            }

            // Remove the source occupant from the grid.
            RemoveCard(gridX, gridY, false);

            // Instantiate the replacement card object.
            GameObject newCardObj = InstantiateReplacementCardInline(replacementCard, owner);

            // Find the grid cell's transform.
            GameObject cellObj = GameObject.Find($"GridCell_{gridX}_{gridY}");
            if (cellObj == null)
            {
                Debug.LogError($"[ExecuteReplacementInline] Grid cell 'GridCell_{gridX}_{gridY}' not found.");
                return;
            }
            Transform cellTransform = cellObj.transform;

            // Place the replacement card.
            PlaceReplacementCard(gridX, gridY, newCardObj, replacementCard, cellTransform);

            // For evolution cards, show the evolution splash.
            if (replacementCard.baseOrEvo == CardSO.BaseOrEvo.Evolution)
            {
                Debug.Log($"[ExecuteReplacementInline] Showing evolution splash with old '{sourceCardUI.cardData.cardName}' and new evo '{replacementCard.cardName}'");
                GridManager.instance.ShowEvolutionSplash(sourceCardUI.cardData.cardName, replacementCard.cardName);
            }

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
            // Use the isAICard flag for both evolution and non-evolution cards.
            Color baseColor = isAICard ? Color.red : Color.green;
            Color flashColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.5f);
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
    public void PrintGridState()
    {
        for (int x = 0; x < 3; x++)
        {
            string rowStatus = "";
            for (int y = 0; y < 3; y++)
            {
                if (grid[x, y] != null)
                {
                    rowStatus += $"[{grid[x, y].cardName}] ";
                }
                else
                {
                    rowStatus += "[Empty] ";
                }
            }
            Debug.Log($"Grid Row {x}: {rowStatus}");
        }
    }
}


