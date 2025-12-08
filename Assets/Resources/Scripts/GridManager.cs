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
    public AudioClip destroyCardSound;


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
            bool needsCreature = cardData.requiresTargetCreature;
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

            // 3) Cell is empty and this spell does NOT require a creature → normal play check
            return TurnManager.instance.CanPlayCard(cardData);
        }

        // ─── CREATURE (AND NON‐SPELL) LOGIC ────────────────────────────────────

        // ✅ Magnificent Garden restriction applies BEFORE any placement logic
        if (RestrictHighPowerPlacementEffect.IsRestrictedCell(x, y, cardData))
        {
            Debug.Log($"[CanPlaceCard] Cell ({x},{y}) locked by Magnificent Garden. Cannot place {cardData.cardName}.");
            return false;
        }

        if (grid[x, y] == null)
        {
            // Empty cell → just check if the player can actually play this creature
            return TurnManager.instance.CanPlayCard(cardData);
        }
        else
        {
            // Occupied by some card → only allow replacement if it’s an opponent’s creature with ≤ power
            CardUI occupantUI = gridObjects[x, y].GetComponent<CardUI>();
            if (occupantUI != null)
            {
                bool occupantIsOpponent =
                    (currentPlayer == 1 && occupantUI.GetComponent<CardHandler>().isAI) ||
                    (currentPlayer == 2 && !occupantUI.GetComponent<CardHandler>().isAI);

                if (occupantIsOpponent)
                {
                    int occupantEffectivePower = occupantUI.CalculateEffectivePower();
                    bool powerOK = newCardEffectivePower > occupantEffectivePower || Mathf.Approximately(newCardEffectivePower, occupantEffectivePower);

                    bool canAfford = TurnManager.instance.CanPlayCard(cardData);
                    bool allowed = (powerOK && canAfford);

                    Debug.Log($"[GridManager] Replacement at ({x},{y}): occupant power = {occupantEffectivePower}, " +
                              $"new card power = {newCardEffectivePower}, powerOK = {powerOK}, canAfford = {canAfford}, allowed = {allowed}");

                    return allowed;
                }
            }
            // Either there was no CardUI, or it was your own card, or it was a non-creature: cannot place here
            return false;
        }
    }



    public bool MoveCardOnBoard(int oldX, int oldY, int newX, int newY, GameObject cardObj)
    {
        // 0) Safety: don't move into an occupied cell
        if (grid[newX, newY] != null)
        {
            Debug.LogWarning("❌ Cannot move to occupied cell!");
            return false;
        }

        var cardData = grid[oldX, oldY];
        if (cardData == null)
        {
            Debug.LogError("❌ No card to move from the source cell!");
            return false;
        }

        // (1) Clear old slot
        grid[oldX, oldY] = null;
        gridObjects[oldX, oldY] = null;
        ResetCellVisual(oldX, oldY);

        // (1.5) Also clear highlight on old cell
        GameObject oldCell = GameObject.Find($"GridCell_{oldX}_{oldY}");
        if (oldCell != null)
        {
            var oldHighlighter = oldCell.GetComponent<GridCellHighlighter>();
            if (oldHighlighter != null)
            {
                oldHighlighter.ResetHighlight();
                oldHighlighter.ClearStoredPersistentHighlight();
            }

            // Also mark the old drop zone as empty
            var oldDz = oldCell.GetComponent<GridDropZone>();
            if (oldDz != null)
                oldDz.isOccupied = false;
        }

        // (2) Assign to new slot
        grid[newX, newY] = cardData;
        gridObjects[newX, newY] = cardObj;

        // (3) Move UI
        GameObject newCell = GameObject.Find($"GridCell_{newX}_{newY}");
        if (newCell != null)
        {
            cardObj.transform.SetParent(newCell.transform, false);
            RectTransform rt = cardObj.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
            }

            // (3a) Mark the new drop zone as occupied ✅
            var dz = newCell.GetComponent<GridDropZone>();
            if (dz != null)
                dz.isOccupied = true;

            // (4) Flash highlight on new cell
            var highlighter = newCell.GetComponent<GridCellHighlighter>();
            if (highlighter != null)
            {
                var handler = cardObj.GetComponent<CardHandler>();
                bool isAI = handler != null && handler.isAI;
                Color baseColor = isAI ? Color.red : Color.green;
                Color flashColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.5f);
                highlighter.FlashHighlight(flashColor);
            }
        }

        // (5) Ensure card still shows it's on the field
        var ui = cardObj.GetComponent<CardUI>();
        if (ui != null)
            ui.isOnField = true;

        Debug.Log($"[GridManager] Moved {cardData.cardName} from ({oldX},{oldY}) to ({newX},{newY})");

        // (6) Re-evaluate any conditional synergies (if you use them) ✅
        UpdateMutualConditionalEffects();

        // (7) Check for a win after the move ✅
        if (cardData.category == CardSO.CardCategory.Creature && !HasSelfDestructEffect(cardData))
        {
            GameManager.instance.CheckForWin();
        }

        return true;
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

        // (0) CHECK SUMMON RESTRICTIONS
        if (cardObj.TryGetComponent<CardUI>(out var cardUI))
        {
            if (cardUI.cardData.effects != null)
            {
                foreach (var effect in cardUI.cardData.effects)
                {
                    if (!effect.CanBeSummoned(cardUI))
                    {
                        Debug.Log($"❌ Cannot summon {cardUI.cardData.cardName} due to summon restriction in {effect.GetType().Name}.");

                        // 🔴 Show floating feedback (if available)
                        if (FloatingTextManager.instance != null)
                        {
                            FloatingTextManager.instance.ShowFloatingTextWorld(
                                "Field must be empty!",
                                cardObj.transform.position + new Vector3(0, 100f, 0),
                                Color.red
                            );
                        }

                        return false;
                    }
                }
            }
        }


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

    // Check the occupant’s power versus the new card’s power
    float occupantPower = gridObjects[x, y].GetComponent<CardUI>().CalculateEffectivePower();
    float newPower = cardObj.GetComponent<CardUI>().CalculateEffectivePower();

    // If occupant is stronger, you can’t replace
    if (occupantPower > newPower)
    {
        Debug.Log($"Cannot replace {grid[x, y].cardName} at ({x},{y}) - occupant power ({occupantPower}) > new card power ({newPower}).");
        return false;
    }
    // If equal power, both die
    else if (Mathf.Approximately(occupantPower, newPower))
    {
        if (cardData.category == CardSO.CardCategory.Spell)
        {
            Debug.Log($"[GridManager] Spell {cardData.cardName} cannot destroy Creature {grid[x, y].cardName} on equal power.");
            return false;
        }

        Debug.Log($"Equal power at ({x},{y}). Destroying both occupant and new card.");

        // 🔊 Play destroy SFX
        if (audioSource != null && destroyCardSound != null)
            audioSource.PlayOneShot(destroyCardSound);

        var occHandler = gridObjects[x, y].GetComponent<CardHandler>();
        RemoveCard(x, y, occHandler != null && occHandler.isAI);

        // Send the new card to graveyard instead of placing
        var newHandler = cardObj.GetComponent<CardHandler>();
        newHandler?.cardOwner?.zones.AddCardToGrave(cardObj);

        TurnManager.instance.RegisterCardPlay(cardData);
        if (cardData.baseOrEvo != CardSO.BaseOrEvo.Evolution)
            ResetCellVisual(x, y);

        return true;
    }
    // Occupant is weaker: replace, but only if player can afford to play
    else
    {
        bool canAfford = TurnManager.instance.CanPlayCard(cardData);
        if (!canAfford)
        {
            Debug.Log($"Cannot replace {grid[x, y].cardName} at ({x},{y}) because player cannot pay for {cardData.cardName}.");
            return false;
        }

        Debug.Log($"Replacing occupant {grid[x, y].cardName} at ({x},{y}) with {cardData.cardName}.");

        // 🔊 Play destroy SFX
        if (audioSource != null && destroyCardSound != null)
            audioSource.PlayOneShot(destroyCardSound);

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
        // (10) PROCESS EFFECTS IMMEDIATELY
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
                    else if (inlineEffect.effectType == CardEffectData.EffectType.MultipleTargetPowerBoost)
                    {
                        Debug.Log("Creating a runtime instance of MultipleTargetPowerBoostEffect for target selection...");
                        var boostEffect = ScriptableObject.CreateInstance<MultipleTargetPowerBoostEffect>();

                        // ✨ Copy values from the inline data
                        boostEffect.powerIncrease = inlineEffect.powerChange;
                        boostEffect.maxTargets = inlineEffect.maxTargets;   // <<< IMPORTANT

                        if (handler != null && handler.isAI)
                        {
                            var aiTargets = new List<CardUI>();
                            for (int gx = 0; gx < 3; gx++)
                                for (int gy = 0; gy < 3; gy++)
                                {
                                    var mCard = gridObjects[gx, gy];
                                    var mH = mCard?.GetComponent<CardHandler>();
                                    var mUI = mCard?.GetComponent<CardUI>();
                                    if (mH != null && mUI != null && mH.isAI)
                                    {
                                        aiTargets.Add(mUI);
                                        if (aiTargets.Count >= boostEffect.maxTargets) break; // optional safety
                                    }
                                }
                            boostEffect.targetCards = aiTargets;
                            boostEffect.ApplyEffect(cardUIComp);
                            Debug.Log($"[AI PowerBoost] Applied effect to {aiTargets.Count} targets.");
                        }
                        else if (TargetSelectionManager.Instance != null)
                        {
                            TargetSelectionManager.Instance.StartTargetSelection(boostEffect);
                            Debug.Log($"Please click on up to {boostEffect.maxTargets} target cards on the board for the boost effect.");
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
    // ────────────────────────────────────────────────────────────────────────────
    // New two-parameter overload: explicitly pass in the CardUI being summoned.
    // ────────────────────────────────────────────────────────────────────────────
    // Add these fields at the top of GridManager (just below existing fields)
    [SerializeField] private GameObject cellSelectionCancelButtonPrefab;
    private GameObject cellSelectionCancelButtonInstance;


    // ────────────────────────────────────────────────────────────────────────────
    // Two-parameter overload: shows valid cells and spawns a Cancel button.
    // ────────────────────────────────────────────────────────────────────────────
    public void EnableCellSelectionMode(CardUI newCardUI, System.Action<int, int> cellSelectedCallback)
    {
        int currentPlayer = TurnManager.instance.GetCurrentPlayer();

        // Loop through all grid cells (3×3)
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                // Only highlight if CanPlaceCard allows it for this newCardUI
                if (CanPlaceCard(x, y, newCardUI))
                {
                    GameObject cellObj = GameObject.Find($"GridCell_{x}_{y}");
                    if (cellObj == null) continue;

                    // Highlight the cell
                    var highlighter = cellObj.GetComponent<GridCellHighlighter>();
                    if (highlighter != null)
                    {
                        highlighter.ClearStoredPersistentHighlight();
                        highlighter.SetPersistentHighlight(new Color(1f, 1f, 0f, 0.5f));
                        highlighter.isSacrificeHighlight = true;
                        if (!cellSelectionCells.Contains(cellObj))
                            cellSelectionCells.Add(cellObj);
                    }

                    // Ensure it has a Button so clicks register
                    var btn = cellObj.GetComponent<Button>() ?? cellObj.AddComponent<Button>();
                    btn.onClick.RemoveAllListeners();

                    int cx = x, cy = y;
                    btn.onClick.AddListener(() =>
                    {
                        DisableCellSelectionMode();
                        cellSelectedCallback?.Invoke(cx, cy);
                    });
                    btn.enabled = true;
                }
            }
        }

        // ──────────────── Spawn a “Cancel” button ────────────────
        if (cellSelectionCancelButtonPrefab != null)
        {
            GameObject overlay = GameObject.Find("OverlayCanvas");
            if (overlay != null)
            {
                // Instantiate under OverlayCanvas
                cellSelectionCancelButtonInstance = Instantiate(
                    cellSelectionCancelButtonPrefab,
                    overlay.transform
                );

                // Position it (e.g., top‐center, 20px below top)
                var rt = cellSelectionCancelButtonInstance.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(1f, 1f);
                    rt.anchorMax = new Vector2(1f, 1f);
                    rt.pivot = new Vector2(1f, 1f);
                    rt.anchoredPosition = new Vector2(-300f, -300f);
                }

                // Hook its onClick to cancel selection mode
                var cancelBtn = cellSelectionCancelButtonInstance.GetComponent<Button>();
                if (cancelBtn != null)
                {
                    cancelBtn.onClick.RemoveAllListeners();
                    cancelBtn.onClick.AddListener(() =>
                    {
                        DisableCellSelectionMode();
                    });
                }
                else
                {
                    Debug.LogWarning("cellSelectionCancelButtonPrefab is missing a Button component.");
                }
            }
            else
            {
                Debug.LogWarning("EnableCellSelectionMode: 'OverlayCanvas' not found.");
            }
        }
        else
        {
            Debug.LogWarning("EnableCellSelectionMode: cellSelectionCancelButtonPrefab is not assigned.");
        }
    }


    // ────────────────────────────────────────────────────────────────────────────
    // Legacy one-parameter overload: fetch CardUI from SacrificeManager if available.
    // This preserves existing calls that only passed a callback.
    // ────────────────────────────────────────────────────────────────────────────
    public void EnableCellSelectionMode(System.Action<int, int> cellSelectedCallback)
    {
        CardUI evolvingCardUI = null;

        if (SacrificeManager.instance != null && SacrificeManager.instance.CurrentEvolutionCard != null)
        {
            evolvingCardUI = SacrificeManager.instance.CurrentEvolutionCard.GetComponent<CardUI>();
        }

        if (evolvingCardUI != null)
        {
            // Delegate to the two-parameter version
            EnableCellSelectionMode(evolvingCardUI, cellSelectedCallback);
        }
        else
        {
            Debug.LogError("EnableCellSelectionMode was called without a CardUI and no CurrentEvolutionCard was found.");
        }
    }


    // ────────────────────────────────────────────────────────────────────────────
    // Update DisableCellSelectionMode to also remove the Cancel button
    // ────────────────────────────────────────────────────────────────────────────
    public void DisableCellSelectionMode()
    {
        // Disable all cell buttons and clear their highlights
        foreach (GameObject cellObj in cellSelectionCells)
        {
            if (cellObj != null)
            {
                // Remove any click listeners and disable the button
                Button btn = cellObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.enabled = false;
                }

                // Restore or reset the highlight on each cell
                GridCellHighlighter highlighter = cellObj.GetComponent<GridCellHighlighter>();
                if (highlighter != null)
                {
                    if (highlighter.HasStoredPersistentHighlight)
                        highlighter.RestoreHighlight();
                    else
                        highlighter.ResetHighlight();

                    highlighter.isSacrificeHighlight = false;
                }
            }
        }
        cellSelectionCells.Clear();

        // Destroy the Cancel button if it exists
        if (cellSelectionCancelButtonInstance != null)
        {
            Destroy(cellSelectionCancelButtonInstance);
            cellSelectionCancelButtonInstance = null;
        }
    }


    // Arm click selection only for the provided cells.
    // Assumes the visuals (highlights) were already set by the caller.
    public void EnableClickSelectionForCells(List<Vector2Int> cells, System.Action<int, int> cellSelectedCallback)
    {
        if (cells == null || cells.Count == 0) return;

        foreach (var c in cells)
        {
            GameObject cellObj = GameObject.Find($"GridCell_{c.x}_{c.y}");
            if (cellObj == null) continue;

            // Track for later cleanup in DisableCellSelectionMode()
            if (!cellSelectionCells.Contains(cellObj))
                cellSelectionCells.Add(cellObj);

            // Mark as “selection” so DisableCellSelectionMode knows to restore/reset after
            var highlighter = cellObj.GetComponent<GridCellHighlighter>();
            if (highlighter != null)
                highlighter.isSacrificeHighlight = true;

            // Ensure it can receive clicks
            var btn = cellObj.GetComponent<UnityEngine.UI.Button>() ?? cellObj.AddComponent<UnityEngine.UI.Button>();
            btn.onClick.RemoveAllListeners();
            int sx = c.x, sy = c.y;
            btn.onClick.AddListener(() =>
            {
                DisableCellSelectionMode();          // removes listeners + restores highlights
                cellSelectedCallback?.Invoke(sx, sy);
            });
            btn.enabled = true;
        }

        // Spawn the same cancel button used by the other selection flow
        // (we reuse your existing Cancel logic & placement)
        if (cellSelectionCancelButtonPrefab != null)
        {
            GameObject overlay = GameObject.Find("OverlayCanvas");
            if (overlay != null)
            {
                cellSelectionCancelButtonInstance = Instantiate(cellSelectionCancelButtonPrefab, overlay.transform);
                var rt = cellSelectionCancelButtonInstance.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
                    rt.anchoredPosition = new Vector2(-300f, -300f);
                }
                var cancelBtn = cellSelectionCancelButtonInstance.GetComponent<UnityEngine.UI.Button>();
                if (cancelBtn != null)
                {
                    cancelBtn.onClick.RemoveAllListeners();
                    cancelBtn.onClick.AddListener(() => { DisableCellSelectionMode(); });
                }
            }
            else
            {
                Debug.LogWarning("EnableClickSelectionForCells: 'OverlayCanvas' not found.");
            }
        }
        else
        {
            Debug.LogWarning("EnableClickSelectionForCells: cancel prefab not assigned.");
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
            // 1) Clear the drop-zone occupancy so OnPointerEnter will highlight again
            var dz = cellObj.GetComponent<GridDropZone>();
            if (dz != null)
            {
                dz.isOccupied = false;
                dz.HideHighlights();  // ensure it’s showing the normalImage
            }

            // 2) Reset any outline/highlight background
            GridCellHighlighter highlighter = cellObj.GetComponent<GridCellHighlighter>();
            if (highlighter != null)
            {
                // Don’t reset evolution cards’ persistent color
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
            CardUI occupantUI = gridObjects[x, y].GetComponent<CardUI>();
            CardHandler occupantHandler = gridObjects[x, y].GetComponent<CardHandler>();
            if (occupantUI == null || occupantHandler == null)
            {
                Debug.LogError("PerformEvolutionAtCoords: Missing CardUI or CardHandler on the occupant.");
                return;
            }

            if (occupantHandler.isAI)
            {
                float occupantEffectivePower = occupantUI.CalculateEffectivePower();
                float evoEffectivePower = evoCard.GetComponent<CardUI>().CalculateEffectivePower();

                Debug.Log($"[PerformEvolutionAtCoords] Occupant effective power: {occupantEffectivePower}, Evolution effective power: {evoEffectivePower}");

                if (evoEffectivePower > occupantEffectivePower)
                {
                    Debug.Log($"[PerformEvolutionAtCoords] Removing opponent's card {grid[x, y].cardName} from cell ({x},{y}) because evolution power is higher.");
                    RemoveCard(x, y, true);
                }
                else
                {
                    Debug.Log($"[PerformEvolutionAtCoords] Cannot place {evoCard.cardData.cardName} at ({x},{y}) because the AI occupant's power is equal or higher.");
                    return;
                }
            }
            else
            {
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
            dz.isOccupied = true;
        else
            Debug.LogWarning("PerformEvolutionAtCoords: No GridDropZone on target cell.");

        TurnManager.instance.RegisterCardPlay(evoCard.cardData);

        // ✅ Apply effects + set isOnField for effect tracking
        CardUI cardUI = evoCard.GetComponent<CardUI>();
        if (cardUI != null)
        {
            cardUI.isOnField = true;

            if (cardUI.cardData.effects != null)
            {
                foreach (CardEffect effect in cardUI.cardData.effects)
                {
                    Debug.Log($"[Evolution Effect] Applying {effect.GetType().Name} to {cardUI.cardData.cardName}");
                    effect.ApplyEffect(cardUI);
                }
            }
        }

        // Floating text (power display)
        if (FloatingTextManager.instance != null)
        {
            GameObject floatingText = Instantiate(
                FloatingTextManager.instance.floatingTextPrefab,
                evoCard.transform.position,
                Quaternion.identity,
                evoCard.transform);
            floatingText.transform.localPosition = new Vector3(0, 50f, 0);

            TextMeshProUGUI tmp = floatingText.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = "Power: " + cardUI.CalculateEffectivePower();

            FloatingText ftScript = floatingText.GetComponent<FloatingText>();
            if (ftScript != null)
                ftScript.sourceCard = evoCard.gameObject;
        }

        Debug.Log($"[GridManager] Evolution complete: {evoCard.cardData.cardName} placed at ({x},{y}).");

        // Highlight the cell
        GridCellHighlighter highlighter = cellObj.GetComponent<GridCellHighlighter>();
        if (highlighter != null)
        {
            Color evoColor = new Color(0f, 1f, 0f, 0.5f);

            highlighter.SetPersistentHighlight(evoColor);
        }

        // ✅ Let GameManager handle win condition detection and resolution
        GameManager.instance.CheckForWin();
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
                        Debug.Log($"[CheckReplacementEffects] {cardUI.cardData.cardName} current turnDelay: {inlineEffect.turnDelay}");

                        if (inlineEffect.turnDelay > 0)
                        {
                            inlineEffect.turnDelay--;
                            Debug.Log($"[CheckReplacementEffects] {cardUI.cardData.cardName} decremented turnDelay to {inlineEffect.turnDelay}");
                        }

                        if (inlineEffect.turnDelay <= 0)
                        {
                            int ownerPlayer = cardUI.GetComponent<CardHandler>()?.cardOwner?.playerNumber ?? -1;
                            int currentTurnPlayer = TurnManager.instance.GetCurrentPlayer();

                            if (ownerPlayer == currentTurnPlayer)
                            {
                                if (ownerPlayer == TurnManager.instance.localPlayerNumber)
                                {
                                    ShowInlineReplacementPrompt(cardUI, i, j, inlineEffect); // ✅ Human player: show prompt
                                }
                                else
                                {
                                    Debug.Log($"[CheckReplacementEffects] Auto-replacing for AI: {cardUI.cardData.cardName}");
                                    ExecuteReplacementInline(cardUI, i, j); // ✅ AI: execute replacement directly
                                }

                                // Prevent re-triggering on future turns
                                inlineEffect.turnDelay = -999;
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
        // 🚫 If we've already played a creature this turn, don't even show the prompt.
        if (TurnManager.instance != null && TurnManager.instance.CreaturePlayed)
        {
            Debug.Log("[ShowInlineReplacementPrompt] Creature already played this turn – not showing replacement prompt.");
            return;
        }
        // Must belong to local player
        CardHandler cardHandler = sourceCardUI.GetComponent<CardHandler>();
        if (cardHandler == null) { Debug.LogError("[ShowInlineReplacementPrompt] CardHandler missing."); return; }
        PlayerManager owner = cardHandler.cardOwner;
        if (owner == null || owner.playerNumber != TurnManager.instance.localPlayerNumber)
        {
            Debug.Log("[ShowInlineReplacementPrompt] Not local player's card; skipping UI.");
            return;
        }

        if (inlineEffect.promptPrefab == null)
        {
            Debug.LogError($"[ShowInlineReplacementPrompt] No promptPrefab on {sourceCardUI.cardData.cardName}");
            return;
        }

        GameObject overlayCanvas = GameObject.Find("OverlayCanvas");
        if (overlayCanvas == null)
        {
            Debug.LogError("[ShowInlineReplacementPrompt] OverlayCanvas not found!");
            return;
        }

        // 🔹 Panels that already exist BEFORE we spawn this one
        SummonChoiceUI[] existingPanels = overlayCanvas.GetComponentsInChildren<SummonChoiceUI>(true);
        int index = existingPanels.Length;   // 0 = first panel, 1 = second, etc.

        // Instantiate the prefab
        GameObject panelInstance = Instantiate(inlineEffect.promptPrefab, overlayCanvas.transform);

        // ── Case 1: SummonChoiceUI (Koi-style panel with card art)
        var choiceUI = panelInstance.GetComponent<SummonChoiceUI>();
        if (choiceUI != null)
        {
            string targetName = inlineEffect.replacementCardName;
            CardSO replacement = DeckManager.instance.FindCardByName(targetName);
            var options = new List<CardSO>();
            if (replacement != null) options.Add(replacement);

            string desc = $"Evolve {sourceCardUI.cardData.cardName} into {targetName}?";

            // 👉 Small offset so panels are stacked/fanned
            Vector2 offset = new Vector2(index * 40f, -index * 40f);

            choiceUI.Show(
                options,
                chosen =>
                {
                    if (chosen != null)
                    {
                        // Do the evolution
                        ExecuteReplacementInline(sourceCardUI, gridX, gridY);

                        // 🔥 After a successful choice, close ALL remaining evolution panels
                        var allPanels = overlayCanvas.GetComponentsInChildren<SummonChoiceUI>(true);
                        foreach (var p in allPanels)
                        {
                            if (p != null)
                            {
                                // slide them out nicely; empty callback is fine
                                p.HideWithSlideOutAndThen(() => { });
                            }
                        }
                    }
                    else
                    {
                        Debug.Log($"[Inline Replacement] Cancelled for {sourceCardUI.cardData.cardName}");
                    }
                },
                desc,
                offset
            );

            // 🔥 Focus logic:
            // dim all old panels, then focus the new one
            foreach (var panel in existingPanels)
            {
                if (panel != null)
                    panel.SetFocused(false);
            }
            choiceUI.SetFocused(true);

            return;
        }

        // ── Case 2: fallback to text-only ReplaceEffectPrompt
        var prompt = panelInstance.GetComponent<ReplaceEffectPrompt>();
        if (prompt == null)
        {
            Debug.LogError("[ShowInlineReplacementPrompt] Prefab has neither SummonChoiceUI nor ReplaceEffectPrompt!");
            Destroy(panelInstance);
            return;
        }

        prompt.Initialize(sourceCardUI.cardData.cardName, sourceCardUI.cardData.effectDescription);
        prompt.OnResponse.AddListener(accepted =>
        {
            prompt.OnResponse.RemoveAllListeners();
            Destroy(panelInstance);

            if (accepted) ExecuteReplacementInline(sourceCardUI, gridX, gridY);
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
            // 🔹 NEW: respect "1 creature per turn" rule for replacements too.
            if (replacementCard.category == CardSO.CardCategory.Creature &&
                TurnManager.instance != null &&
                TurnManager.instance.CreaturePlayed)
            {
                Debug.Log("[ExecuteReplacementInline] A creature has already been played this turn – replacement cancelled.");
                return;
            }

            // Get the owner from the source card's CardHandler.
            PlayerManager owner = sourceCardUI.GetComponent<CardHandler>()?.cardOwner;
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


