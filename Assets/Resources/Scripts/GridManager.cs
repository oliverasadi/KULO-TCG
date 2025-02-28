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

    public bool IsValidDropPosition(Vector2Int dropPosition, out int x, out int y)
    {
        x = dropPosition.x;
        y = dropPosition.y;
        // True if there's no occupant in that cell.
        return grid[x, y] == null;
    }

    public bool CanPlaceCard(int x, int y, CardSO card)
    {
        // Get the current player's number (1 = player, 2 = AI)
        int currentPlayer = TurnManager.instance.GetCurrentPlayer();
        bool bypassTurnLimit = false;

        // If the cell is occupied, check if the occupant belongs to the opponent.
        if (grid[x, y] != null && gridObjects[x, y] != null)
        {
            CardHandler occupantHandler = gridObjects[x, y].GetComponent<CardHandler>();
            if (occupantHandler != null)
            {
                // If current player is 1 (human) and the occupant is from the AI,
                // or current player is 2 (AI) and the occupant is from the human,
                // then we bypass the one-per-turn rule.
                if ((currentPlayer == 1 && occupantHandler.isAI) ||
                    (currentPlayer == 2 && !occupantHandler.isAI))
                {
                    bypassTurnLimit = true;
                }
            }
        }

        // If we are not bypassing and TurnManager says we can't play the card, then disallow.
        if (!bypassTurnLimit && !TurnManager.instance.CanPlayCard(card))
        {
            Debug.Log($"❌ Cannot place {card.cardName}: One-per-turn rule.");
            return false;
        }

        // If the cell is empty, it's fine.
        if (grid[x, y] == null)
            return true;

        // Otherwise, allow placement only if the existing occupant's power is less than the new card's power.
        return grid[x, y].power < card.power;
    }

    /// <summary>
    /// Re-parents the existing card GameObject (from hand) into the specified cell.
    /// It first checks sacrifice requirements – if the card requires sacrifices and they are not met, placement fails.
    /// If requirements are met, it removes (sacrifices) the required cards before placing the new card.
    /// </summary>
    public bool PlaceExistingCard(int x, int y, GameObject cardObj, CardSO cardData, Transform cellParent)
    {
        Debug.Log($"[GridManager] Attempting to place {cardData.cardName} at {x},{y} under {cellParent.name}. Category: {cardData.category}");

        // ---- SACRIFICE REQUIREMENTS CHECK ----
        if (cardData.requiresSacrifice && cardData.sacrificeRequirements != null && cardData.sacrificeRequirements.Count > 0)
        {
            foreach (var req in cardData.sacrificeRequirements)
            {
                int foundCount = 0;
                // Count matching cards in the grid.
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
                if (foundCount < req.count)
                {
                    Debug.Log($"Cannot place {cardData.cardName}: Sacrifice requirement not met for {req.requiredCardName} (need {req.count}, found {foundCount}).");
                    return false;
                }
            }
            // Sacrifice the required cards.
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
                                // Assuming sacrifices are for the player; adjust isAI as needed.
                                RemoveCard(i, j, false);
                                sacrificed++;
                            }
                        }
                    }
                }
            }
        }
        // ---- END OF SACRIFICE CHECK ----

        // If an occupant is already present, remove it.
        if (grid[x, y] != null)
        {
            Debug.Log($"💥 Replacing {grid[x, y].cardName} at {x},{y}!");
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

        // Re-parent the card to the specified grid cell.
        cardObj.transform.SetParent(cellParent, false);

        // Center the card in the cell:
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

        // Mark the card as played.
        TurnManager.instance.RegisterCardPlay(cardData);
        if (audioSource != null && placeCardSound != null)
            audioSource.PlayOneShot(placeCardSound);

        Debug.Log($"✅ Placed {cardData.cardName} at {x},{y}. Category = {cardData.category}");

        // Only trigger the highlight effect if the card is not a Spell.
        if (cardData.category != CardSO.CardCategory.Spell)
        {
            // Determine the flash color: green for human, red for AI.
            Color baseColor = (TurnManager.instance.GetCurrentPlayer() == 1) ? Color.green : Color.red;
            // Set transparency to 20% by setting alpha to 0.2f.
            Color flashColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.2f);

            // Trigger the grid cell highlight effect.
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

        // If it's a Spell, schedule removal.
        if (cardData.category == CardSO.CardCategory.Spell)
        {
            bool isAI = (TurnManager.instance.GetCurrentPlayer() == 2);
            Debug.Log($"[GridManager] Starting removal coroutine for Spell: {cardData.cardName}");
            StartCoroutine(RemoveSpellAfterDelay(x, y, cardData, isAI));
        }
        else
        {
            Debug.Log($"[GridManager] {cardData.cardName} is not a spell; remains on the grid.");
        }

        GameManager.instance.CheckForWin();
        return true;
    }

    private IEnumerator RemoveSpellAfterDelay(int x, int y, CardSO card, bool isAI)
    {
        yield return new WaitForSeconds(1f);
        if (grid[x, y] == card)
        {
            Debug.Log($"[GridManager] Removing spell {card.cardName} from {x},{y} after delay.");
            RemoveCard(x, y, isAI);
        }
        else
        {
            Debug.Log($"[GridManager] {card.cardName} is no longer at {x},{y} at removal time.");
        }
    }

    public void RemoveCard(int x, int y, bool isAI = false)
    {
        if (grid[x, y] != null)
        {
            CardSO removedCard = grid[x, y];
            GameObject cardObj = gridObjects[x, y];

            Debug.Log($"🗑️ Removing {removedCard.cardName} at ({x},{y}).");

            grid[x, y] = null;
            gridObjects[x, y] = null;

            // Optionally, reset the corresponding grid cell's occupancy flag.
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

            // Re-parent the card to the correct grave container.
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

            Debug.Log($"Moved {removedCard.cardName} to {(isAI ? "AI" : "player")} grave.");
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
        Debug.Log("🔄 Grid Reset!");
    }
}
