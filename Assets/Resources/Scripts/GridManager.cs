using System.Collections;
using UnityEngine;
using TMPro;

public class GridManager : MonoBehaviour
{
    public static GridManager instance;

    private CardSO[,] grid = new CardSO[3, 3];
    private GameObject[,] gridObjects = new GameObject[3, 3];

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

        // (1) SACRIFICE REQUIREMENTS – (unchanged)
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
                                Debug.Log($"Sacrificing {grid[i, j].cardName} at ({i},{j}) for {cardData.cardName}.");
                                RemoveCard(i, j, false);
                                sacrificed++;
                            }
                        }
                    }
                }
            }
        }

        // (2) OCCUPANT REPLACEMENT LOGIC (unchanged)
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

        // (3) RE-PARENT & CENTER THE CARD
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

        // (4) UPDATE GRID REFERENCES & OWNERSHIP
        grid[x, y] = cardData;
        gridObjects[x, y] = cardObj;

        bool newOwnerIsAI2 = (TurnManager.instance.GetCurrentPlayer() == 2);
        Debug.Log($"[GridManager] Forcing ownership for {cardData.cardName}: isAI = {newOwnerIsAI2}");
        CardHandler handler = cardObj.GetComponent<CardHandler>();
        if (handler != null)
            handler.isAI = newOwnerIsAI2;

        TurnManager.instance.RegisterCardPlay(cardData);
        if (audioSource != null && placeCardSound != null)
            audioSource.PlayOneShot(placeCardSound);

        Debug.Log($"[GridManager] Placed {cardData.cardName} at ({x},{y}).");

        // (5) VISUAL / TEXT / SPELL REMOVAL

        // (a) Floating Text Display using effective power.
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

            // Assign sourceCard so the FloatingText script can live-update.
            FloatingText ft = floatingText.GetComponent<FloatingText>();
            if (ft != null)
            {
                ft.sourceCard = cardObj;
            }
        }

        // (b) Color Highlight for non-Spell cards.
        if (cardData.category != CardSO.CardCategory.Spell)
        {
            Color baseColor;
            if (cardData.baseOrEvo == CardSO.BaseOrEvo.Evolution)
                baseColor = Color.green;
            else
                baseColor = newOwnerIsAI2 ? Color.red : Color.green;
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

        // (c) If it's a Spell, queue up removal.
        if (cardData.category == CardSO.CardCategory.Spell)
        {
            bool isAI = (TurnManager.instance.GetCurrentPlayer() == 2);
            Debug.Log($"[GridManager] Removing spell {cardData.cardName} soon.");
            StartCoroutine(RemoveSpellAfterDelay(x, y, cardData, isAI));
        }
        else
        {
            Debug.Log($"[GridManager] {cardData.cardName} remains on the grid.");
        }

        // (A) -- PROCESS EFFECTS IMMEDIATELY --
        CardUI cardUIComp = cardObj.GetComponent<CardUI>();
        if (cardUIComp != null)
        {
            // (1) Asset-based Effects.
            if (cardUIComp.cardData.effects != null)
            {
                foreach (CardEffect effect in cardUIComp.cardData.effects)
                {
                    Debug.Log($"Applying asset-based effect on {cardUIComp.cardData.cardName}");
                    effect.ApplyEffect(cardUIComp);
                    if (grid[x, y] == null)
                    {
                        return true;
                    }
                }
            }
            // (2) Inline Effects.
            if (cardUIComp.cardData.inlineEffects != null)
            {
                foreach (var inlineEffect in cardUIComp.cardData.inlineEffects)
                {
                    Debug.Log($"Processing inline effect for {cardUIComp.cardData.cardName} with type {inlineEffect.effectType}");
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
                    // Removed one-time ConditionalPowerBoost processing here.
                }
            }
        }

        // (B) -- WIN CONDITION CHECK (only for non-Spell cards, and only if the card is still present,
        // and if it does NOT have a self-destruct effect like X1 Damiano).
        if (cardData.category != CardSO.CardCategory.Spell && grid[x, y] == cardData && !HasSelfDestructEffect(cardData))
        {
            Debug.Log("[GridManager] Checking for win condition now...");
            GameManager.instance.CheckForWin();
        }

        return true;
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

            grid[x, y] = null;
            gridObjects[x, y] = null;

            ResetCellVisual(x, y);

            PlayerManager co = cardObj.GetComponent<CardHandler>().cardOwner;
            if (co != null)
            {
                co.zones.AddCardToGrave(cardObj);
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
}
