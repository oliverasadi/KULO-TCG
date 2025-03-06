using System.Collections;
using UnityEngine;

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

    // Always allow drop; detailed validation occurs in CanPlaceCard.
    public bool IsValidDropPosition(Vector2Int dropPosition, out int x, out int y)
    {
        x = dropPosition.x;
        y = dropPosition.y;
        return true;
    }

    // Simplified logic:
    // - If cell is empty, enforce one creature/one spell rule.
    // - If occupied by an opponent's creature, allow replacement if new card's power is >= occupant's power.
    public bool CanPlaceCard(int x, int y, CardSO card)
    {
        int currentPlayer = TurnManager.instance.GetCurrentPlayer();

        if (grid[x, y] == null)
        {
            // Normal move: enforce one creature/one spell rule.
            return TurnManager.instance.CanPlayCard(card);
        }
        else
        {
            // Check occupant ownership.
            CardHandler occupantHandler = gridObjects[x, y].GetComponent<CardHandler>();
            if (occupantHandler != null)
            {
                if ((currentPlayer == 1 && occupantHandler.isAI) ||
                    (currentPlayer == 2 && !occupantHandler.isAI))
                {
                    // Replacement move: allow if new card's power is >= occupant's power.
                    bool allowed = card.power >= grid[x, y].power;
                    Debug.Log($"[GridManager] Replacement at ({x},{y}): occupant power = {grid[x, y].power}, new card power = {card.power}, allowed = {allowed}");
                    return allowed;
                }
            }
            // Occupant is on the same side, disallow.
            return false;
        }
    }

    public bool PlaceExistingCard(int x, int y, GameObject cardObj, CardSO cardData, Transform cellParent)
    {
        Debug.Log($"[GridManager] Attempting to place {cardData.cardName} at ({x},{y}) under {cellParent.name}. Category: {cardData.category}");

        // ---- SACRIFICE REQUIREMENTS CHECK ----
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
                            if (match) foundCount++;
                        }
                    }
                }
                Debug.Log($"[Sacrifice Check] For {req.requiredCardName}: found {foundCount}, need {req.count}.");
                if (foundCount < req.count)
                {
                    Debug.Log($"Cannot place {cardData.cardName}: requirement not met for {req.requiredCardName} (need {req.count}, found {foundCount}).");
                    return false;
                }
            }
            // At this point, sacrifice requirements are met.
            // TODO: Insert confirmation UI here.
            // If the player confirms the sacrifice, then perform the following:
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
                            if (match)
                            {
                                Debug.Log($"Sacrificing {grid[i, j].cardName} at ({i},{j}) for {cardData.cardName}.");
                                // Only sacrifice if it is the player's card.
                                CardHandler ch = gridObjects[i, j].GetComponent<CardHandler>();
                                if (ch != null && !ch.isAI)
                                {
                                    RemoveCard(i, j, false);
                                    sacrificed++;
                                }
                            }
                        }
                    }
                }
            }
        }
        // ---- END SACRIFICE CHECK ----

        // If occupant is present, handle replacement logic.
        if (grid[x, y] != null)
        {
            if (grid[x, y].power > cardData.power)
            {
                Debug.Log($"Cannot replace {grid[x, y].cardName} at ({x},{y}) - occupant power {grid[x, y].power} > {cardData.cardName}'s power ({cardData.power}).");
                return false;
            }
            else if (grid[x, y].power == cardData.power)
            {
                Debug.Log($"Equal power at ({x},{y}). Destroying both occupant and new card.");
                CardHandler occupantHandler = gridObjects[x, y].GetComponent<CardHandler>();
                bool occupantIsAI = occupantHandler != null && occupantHandler.isAI;
                RemoveCard(x, y, occupantIsAI);

                bool newCardIsAI = (TurnManager.instance.GetCurrentPlayer() == 2);
                CardHandler newCardHandler = cardObj.GetComponent<CardHandler>();
                if (newCardHandler != null) newCardHandler.isAI = newCardIsAI;

                if (newCardHandler.cardOwner.zones != null) newCardHandler.cardOwner.zones.AddCardToGrave(cardObj);

                TurnManager.instance.RegisterCardPlay(cardData);
                ResetCellVisual(x, y);
                return true;
            }
            else
            {
                Debug.Log($"[GridManager] Replacing occupant {grid[x, y].cardName} at ({x},{y}) with {cardData.cardName} (higher power).");
                CardHandler occupantHandler = gridObjects[x, y].GetComponent<CardHandler>();
                bool occupantIsAI = occupantHandler != null && occupantHandler.isAI;
                RemoveCard(x, y, occupantIsAI);
            }
        }

        // Re-parent and center the new card.
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

        // Force ownership for new card.
        bool newOwnerIsAI2 = (TurnManager.instance.GetCurrentPlayer() == 2);
        Debug.Log($"[GridManager] Forcing ownership for {cardData.cardName}: isAI = {newOwnerIsAI2}");
        CardHandler handler = cardObj.GetComponent<CardHandler>();
        if (handler != null) handler.isAI = newOwnerIsAI2;

        TurnManager.instance.RegisterCardPlay(cardData);
        if (audioSource != null && placeCardSound != null) audioSource.PlayOneShot(placeCardSound);

        Debug.Log($"[GridManager] Placed {cardData.cardName} at ({x},{y}).");

        if (cardData.category != CardSO.CardCategory.Spell)
        {
            Color baseColor = newOwnerIsAI2 ? Color.red : Color.green;
            Color flashColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.2f);
            GameObject cellObj = GameObject.Find($"GridCell_{x}_{y}");
            if (cellObj != null)
            {
                GridCellHighlighter highlighter = cellObj.GetComponent<GridCellHighlighter>();
                if (highlighter != null)
                {
                    highlighter.FlashHighlight(flashColor);
                }
            }
        }

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

        GameManager.instance.CheckForWin();
        return true;
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

            grid[x, y] = null;
            gridObjects[x, y] = null;

            GameObject cellObj = GameObject.Find($"GridCell_{x}_{y}");
            if (cellObj != null)
            {
                GridDropZone dz = cellObj.GetComponent<GridDropZone>();
                if (dz != null)
                {
                    dz.isOccupied = false;
                    Debug.Log($"[GridManager] Reset isOccupied for cell GridCell_{x}_{y}");
                }
            }
            else
            {
                Debug.LogWarning($"[GridManager] Could not find 'GridCell_{x}_{y}' in the hierarchy.");
            }

            PlayerManager co = cardObj.GetComponent<CardHandler>().cardOwner;
            if (co != null)
                co.zones.AddCardToGrave(cardObj);
            else
                Debug.LogError("Zones instance is null!");

            if (audioSource != null && removeCardSound != null)
                audioSource.PlayOneShot(removeCardSound);

            Debug.Log($"[GridManager] Moved {removedCard.cardName} to {(isAI ? "AI" : "player")} grave.");
        }
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

    private void ResetCellVisual(int x, int y)
    {
        GameObject cellObj = GameObject.Find($"GridCell_{x}_{y}");
        if (cellObj != null)
        {
            GridCellHighlighter highlighter = cellObj.GetComponent<GridCellHighlighter>();
            if (highlighter != null)
            {
                highlighter.ResetHighlight();
                Debug.Log($"[GridManager] Reset visual for cell ({x},{y}).");
            }
        }
    }

    // ------------------------------------------------
    // NEW METHODS for Sacrifice/Evolution placeholders
    // ------------------------------------------------

    /// <summary>
    /// Highlights valid sacrifice cards on the field.
    /// Only the player's cards (where CardHandler.isAI is false) are considered.
    /// </summary>
    public void HighlightEligibleSacrifices(CardUI evoCard)
    {
        if (evoCard == null || evoCard.cardData == null || evoCard.cardData.sacrificeRequirements == null)
        {
            Debug.LogError("HighlightEligibleSacrifices: Invalid evoCard or missing sacrifice requirements.");
            return;
        }

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

                        if (ch != null && !ch.isAI) // Only highlight player’s cards
                        {
                            bool match = req.matchByCreatureType
                                ? (grid[x, y].creatureType == req.requiredCardName)
                                : (grid[x, y].cardName == req.requiredCardName);

                            if (match)
                            {
                                ch.ShowSacrificeHighlight(); // Highlights only valid sacrifices
                                Debug.Log($"[Sacrifice Highlight] {grid[x, y].cardName} is a valid sacrifice.");
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Clears any highlights placed for sacrifice selection.
    /// </summary>
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

    /// <summary>
    /// Finds and removes the given sacrifice card from the board.
    /// Only considers player's cards.
    /// </summary>
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

    /// <summary>
    /// Places the evolved card at the position of the first sacrificed card (by reading the parent's transform).
    /// </summary>
    public void PerformEvolution(CardUI evoCard, GameObject firstSacrifice)
    {
        // The existing method that tries to parse the parent's name.
        // Left intact if you want to keep it for other flows.
        // For the new recommended approach, see PerformEvolutionAtCoords below.
        // ...
    }

    /// <summary>
    /// A more reliable method: places the evolved card at explicit (x,y) coordinates in the grid.
    /// </summary>
    public void PerformEvolutionAtCoords(CardUI evoCard, int x, int y)
    {
        // 1. Find the grid cell object
        GameObject cellObj = GameObject.Find($"GridCell_{x}_{y}");
        if (cellObj == null)
        {
            Debug.LogError($"PerformEvolutionAtCoords: Could not find GridCell_{x}_{y}");
            return;
        }
        Debug.Log($"PerformEvolutionAtCoords: Placing {evoCard.cardData.cardName} at GridCell_{x}_{y}");

        // 2. Re-parent the evolution card to that cell
        evoCard.transform.SetParent(cellObj.transform, false);

        // 3. Reset anchoring/position
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

        // 4. Update grid references
        grid[x, y] = evoCard.cardData;
        gridObjects[x, y] = evoCard.gameObject;

        // 5. Mark the cell as occupied
        GridDropZone dz = cellObj.GetComponent<GridDropZone>();
        if (dz != null)
        {
            dz.isOccupied = true;
        }
        else
        {
            Debug.LogWarning("PerformEvolutionAtCoords: No GridDropZone on target cell.");
        }

        // 6. Register the evolution as a played card
        TurnManager.instance.RegisterCardPlay(evoCard.cardData);

        Debug.Log($"[GridManager] Evolution complete: {evoCard.cardData.cardName} placed at ({x},{y}).");
    }

    /// <summary>
    /// Default evolution method placeholder.
    /// </summary>
    public void PlaceEvolutionCard(CardUI evoCard, Vector2 targetPos)
    {
        Debug.Log("[GridManager] PlaceEvolutionCard called for " + evoCard.cardData.cardName + " at " + targetPos);
        // For demonstration purposes, you might decide which cell to target based on targetPos.
        // In practice, you would capture that reference during sacrifice selection.
    }
}
