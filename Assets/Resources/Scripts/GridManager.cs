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
                if ((currentPlayer == 1 && occupantHandler.isAI) ||
                    (currentPlayer == 2 && !occupantHandler.isAI))
                {
                    bool allowed = card.power >= grid[x, y].power;
                    Debug.Log($"[GridManager] Replacement at ({x},{y}): occupant power = {grid[x, y].power}, new card power = {card.power}, allowed = {allowed}");
                    return allowed;
                }
            }
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

        grid[x, y] = cardData;
        gridObjects[x, y] = cardObj;

        bool newOwnerIsAI2 = (TurnManager.instance.GetCurrentPlayer() == 2);
        Debug.Log($"[GridManager] Forcing ownership for {cardData.cardName}: isAI = {newOwnerIsAI2}");
        CardHandler handler = cardObj.GetComponent<CardHandler>();
        if (handler != null) handler.isAI = newOwnerIsAI2;

        TurnManager.instance.RegisterCardPlay(cardData);
        if (audioSource != null && placeCardSound != null)
            audioSource.PlayOneShot(placeCardSound);

        Debug.Log($"[GridManager] Placed {cardData.cardName} at ({x},{y}).");

        // --- Floating Text Display ---
        if (FloatingTextManager.instance != null)
        {
            // Instantiate floating text as a child of the card.
            GameObject floatingText = Instantiate(
                FloatingTextManager.instance.floatingTextPrefab,
                cardObj.transform.position,
                Quaternion.identity,
                cardObj.transform);
            floatingText.transform.localPosition = new Vector3(0, 50f, 0); // Adjust offset as needed.
            TextMeshProUGUI tmp = floatingText.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
                tmp.text = "Power: " + cardData.power;
        }

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

            // Destroy any FloatingText children attached to this card.
            FloatingText[] floatingTexts = cardObj.GetComponentsInChildren<FloatingText>(true);
            foreach (FloatingText ft in floatingTexts)
            {
                Destroy(ft.gameObject);
            }

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

                        if (ch != null && !ch.isAI)
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
        // Existing method placeholder...
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

        // --- Floating Text for Evo Card ---
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
                tmp.text = "Power: " + evoCard.cardData.power;
        }

        Debug.Log($"[GridManager] Evolution complete: {evoCard.cardData.cardName} placed at ({x},{y}).");
    }

    public void PlaceEvolutionCard(CardUI evoCard, Vector2 targetPos)
    {
        Debug.Log("[GridManager] PlaceEvolutionCard called for " + evoCard.cardData.cardName + " at " + targetPos);
        // Implementation as needed...
    }
}
