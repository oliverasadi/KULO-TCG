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
            // Occupant is same side, disallow.
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
            // Perform the sacrifices.
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
        // ---- END SACRIFICE CHECK ----

        // If occupant is present, handle replacement logic.
        if (grid[x, y] != null)
        {
            if (grid[x, y].power > cardData.power)
            {
                Debug.Log($"Cannot replace {grid[x, y].cardName} at ({x},{y}) - occupant power {grid[x, y].power} > {cardData.power}.");
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

                // Move the new card to grave as well, since both are destroyed.
                if (newCardIsAI)
                {
                    if (AIGraveZone.instance != null) AIGraveZone.instance.AddCardToGrave(cardObj);
                }
                else
                {
                    if (GraveZone.instance != null) GraveZone.instance.AddCardToGrave(cardObj);
                }

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

        // Mark the card as played.
        TurnManager.instance.RegisterCardPlay(cardData);
        if (audioSource != null && placeCardSound != null) audioSource.PlayOneShot(placeCardSound);

        Debug.Log($"[GridManager] Placed {cardData.cardName} at ({x},{y}).");

        // Flash highlight if it's not a Spell.
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

        // If it's a Spell, schedule removal.
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
    /// E.g., loop over the player's creatures, check conditions, and visually mark them.
    /// </summary>
    public void HighlightEligibleSacrifices()
    {
        Debug.Log("[GridManager] HighlightEligibleSacrifices called. Implement logic to highlight valid sacrifice cards.");
        // Example:
        // for (int x=0; x<3; x++){
        //   for (int y=0; y<3; y++){
        //     if (grid[x,y] != null && gridObjects[x,y] != null){
        //       // If it's the player's creature, highlight it
        //       // Possibly check card type or name
        //       // Add an outline or color highlight
        //     }
        //   }
        // }
    }

    /// <summary>
    /// Removes a sacrifice card from the board (like a partial remove).
    /// Could be used by SacrificeManager.
    /// </summary>
    public void RemoveSacrificeCard(GameObject card)
    {
        Debug.Log("[GridManager] RemoveSacrificeCard called with " + card.name);
        // 1) Find the card's position in the grid
        // 2) Remove it (grid[x,y] = null, gridObjects[x,y] = null)
        // 3) Move it to the grave or destroy it
        // For example:
        // for(int x=0; x<3; x++){
        //   for(int y=0; y<3; y++){
        //     if(gridObjects[x,y] == card){
        //       RemoveCard(x,y,false); // if it's the player's card
        //       return;
        //     }
        //   }
        // }
    }

    /// <summary>
    /// Clears any highlights placed for sacrifice selection.
    /// E.g., remove outlines from previously highlighted cards.
    /// </summary>
    public void ClearSacrificeHighlights()
    {
        Debug.Log("[GridManager] ClearSacrificeHighlights called. Implement logic to un-highlight any sacrifice candidates.");
        // Similar approach: loop over board, if a card was highlighted, revert it
    }

    /// <summary>
    /// Places the evolved card at a specific location (like the position of a sacrificed card).
    /// </summary>
    public void PlaceEvolutionCard(CardUI evoCard, Vector2 targetPos)
    {
        Debug.Log("[GridManager] PlaceEvolutionCard called for " + evoCard.cardData.cardName + " at " + targetPos);
        // 1) Convert targetPos to a grid cell or find the nearest cell
        // 2) Possibly call PlaceExistingCard(...) with that cell
        // This is up to your design. If you want to exactly replace a single sacrificed card's position,
        // pass the x,y of that card to PlaceExistingCard.
    }
}
