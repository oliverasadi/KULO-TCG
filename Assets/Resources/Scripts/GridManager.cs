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
    // - If cell is empty, enforce turn rule.
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
            // Check if the occupant belongs to the opponent.
            CardHandler occupantHandler = gridObjects[x, y].GetComponent<CardHandler>();
            if (occupantHandler != null)
            {
                if ((currentPlayer == 1 && occupantHandler.isAI) ||
                    (currentPlayer == 2 && !occupantHandler.isAI))
                {
                    // Replacement move: allow if new card's power is greater or equal.
                    bool allowed = card.power >= grid[x, y].power;
                    Debug.Log($"[GridManager] Replacement move at ({x},{y}): occupant power = {grid[x, y].power}, new card power = {card.power}. Allowed: {allowed}");
                    return allowed;
                }
            }
            // If occupant is on the same side, disallow.
            return false;
        }
    }

    /// <summary>
    /// Places a card into the specified cell.
    /// If the cell is occupied by an opponent's creature:
    ///   - If your card's power is greater, it replaces the opponent's card.
    ///   - If your card's power is equal, both cards are destroyed.
    /// Replacement moves count as your creature play for the turn.
    /// </summary>
    public bool PlaceExistingCard(int x, int y, GameObject cardObj, CardSO cardData, Transform cellParent)
    {
        Debug.Log($"[GridManager] Attempting to place {cardData.cardName} at ({x},{y}) under {cellParent.name}. Category: {cardData.category}");

        // ---- SACRIFICE REQUIREMENTS CHECK ----
        if (cardData.requiresSacrifice && cardData.sacrificeRequirements != null && cardData.sacrificeRequirements.Count > 0)
        {
            foreach (var req in cardData.sacrificeRequirements)
            {
                int foundCount = 0;
                // Loop over the entire grid to count matching cards.
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (grid[i, j] != null)
                        {
                            bool match = req.matchByCreatureType
                                ? (grid[i, j].creatureType == req.requiredCardName)
                                : (grid[i, j].cardName == req.requiredCardName);
                            if (match)
                                foundCount++;
                        }
                    }
                }
                Debug.Log($"[Sacrifice Check] For {req.requiredCardName}: found {foundCount}, need {req.count}.");
                if (foundCount < req.count)
                {
                    Debug.Log($"Cannot place {cardData.cardName}: Sacrifice requirement not met for {req.requiredCardName} (need {req.count}, found {foundCount}).");
                    return false;
                }
            }
            // Perform sacrifices.
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
                                RemoveCard(i, j, false);
                                sacrificed++;
                            }
                        }
                    }
                }
            }
        }
        // ---- END OF SACRIFICE CHECK ----

        // Replacement move if the target cell is occupied.
        if (grid[x, y] != null)
        {
            // If occupant's power is greater, disallow the move.
            if (grid[x, y].power > cardData.power)
            {
                Debug.Log($"Cannot replace {grid[x, y].cardName} at ({x},{y}) because its power ({grid[x, y].power}) is higher than {cardData.cardName}'s power ({cardData.power}).");
                return false;
            }
            // If the powers are equal, destroy both.
            else if (grid[x, y].power == cardData.power)
            {
                Debug.Log($"Same power at ({x},{y}). Destroying both {grid[x, y].cardName} and {cardData.cardName}.");
                // Remove the opponent's card.
                CardHandler occupantHandler = gridObjects[x, y].GetComponent<CardHandler>();
                bool occupantIsAI = (occupantHandler != null) ? occupantHandler.isAI : false;
                RemoveCard(x, y, occupantIsAI);
                // Also send the new card to the grave.
                if (TurnManager.instance.GetCurrentPlayer() == 1)
                {
                    if (GraveZone.instance != null)
                        GraveZone.instance.AddCardToGrave(cardObj);
                }
                else
                {
                    if (AIGraveZone.instance != null)
                        AIGraveZone.instance.AddCardToGrave(cardObj);
                }
                // Register the creature play.
                TurnManager.instance.RegisterCardPlay(cardData);
                return true;
            }
            // Otherwise, if your card's power is higher, replace normally.
            else
            {
                Debug.Log($"[GridManager] Replacing {grid[x, y].cardName} at ({x},{y}) with {cardData.cardName}.");
                CardHandler occupantHandler = gridObjects[x, y].GetComponent<CardHandler>();
                bool occupantIsAI = (occupantHandler != null) ? occupantHandler.isAI : false;
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

        // Register this play as your creature play (counts as your one creature for the turn).
        TurnManager.instance.RegisterCardPlay(cardData);
        if (audioSource != null && placeCardSound != null)
            audioSource.PlayOneShot(placeCardSound);

        Debug.Log($"[GridManager] Placed {cardData.cardName} at ({x},{y}).");

        // Trigger a semi-transparent highlight for non-spell cards.
        if (cardData.category != CardSO.CardCategory.Spell)
        {
            Color baseColor = (TurnManager.instance.GetCurrentPlayer() == 1) ? Color.green : Color.red;
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

        // For Spell cards, schedule removal so they go to the grave.
        if (cardData.category == CardSO.CardCategory.Spell)
        {
            bool isAI = (TurnManager.instance.GetCurrentPlayer() == 2);
            Debug.Log($"[GridManager] Starting removal coroutine for Spell: {cardData.cardName}");
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
            Debug.Log($"[GridManager] {card.cardName} is no longer at ({x},{y}) at removal time.");
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
                Debug.LogWarning($"[GridManager] Could not find grid cell 'GridCell_{x}_{y}' in the hierarchy.");
            }

            if (isAI)
            {
                if (AIGraveZone.instance != null)
                    AIGraveZone.instance.AddCardToGrave(cardObj);
                else
                    Debug.LogError("AIGraveZone instance is null!");
            }
            else
            {
                if (GraveZone.instance != null)
                    GraveZone.instance.AddCardToGrave(cardObj);
                else
                    Debug.LogError("GraveZone instance is null!");
            }

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

    // New public method for accessing gridObjects
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
}
