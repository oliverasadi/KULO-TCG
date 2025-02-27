using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class AIController : MonoBehaviour
{
    public static AIController instance;
    public List<CardSO> aiDeck = new List<CardSO>(); // AI's deck
    public List<CardHandler> aiHandCardHandlers = new List<CardHandler>(); // AI hand's Card Handlers (max 5 cards)
    private bool aiPlayedCreature = false;
    private bool aiPlayedSpell = false;
    private const int HAND_SIZE = 5;

    [Header("AI Hand UI")]
    [SerializeField] private Transform aiHandPanel; // Assign in Inspector
    public GameObject cardUIPrefab; // Assign in Inspector

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        if (aiDeck.Count == 0)
        {
            GenerateRandomAIDeck();
        }
        DrawStartingHand();
    }

    private void GenerateRandomAIDeck()
    {
        aiDeck.Clear();
        CardSO[] allCards = Resources.LoadAll<CardSO>("Cards");
        for (int i = 0; i < 40; i++)
        {
            aiDeck.Add(allCards[Random.Range(0, allCards.Length)]);
        }
    }

    private void DrawStartingHand()
    {
        for (int i = 0; i < HAND_SIZE; i++)
        {
            DrawCard();
        }
    }

    private void DrawCard()
    {
        if (aiDeck.Count > 0 && aiHandCardHandlers.Count < HAND_SIZE)
        {
            CardSO drawnCard = aiDeck[0];
            aiDeck.RemoveAt(0);
            // Cards in the AI hand start face-down.
            DisplayCardInAIHand(drawnCard, true);
        }
    }

    private void DisplayCardInAIHand(CardSO card, bool isFaceDown)
    {
        if (aiHandPanel == null || cardUIPrefab == null)
            return;
        GameObject cardUI = Instantiate(cardUIPrefab, aiHandPanel);
        CardHandler cardHandler = cardUI.GetComponent<CardHandler>();
        if (cardHandler != null)
        {
            cardHandler.SetCard(card, isFaceDown);
        }
        else
        {
            Debug.LogError("AIController: CardHandler component missing on instantiated AI card!");
        }
        aiHandCardHandlers.Add(cardHandler);
    }

    // Helper method: Checks if the card's sacrifice requirements are met on the grid.
    private bool IsCardPlayable(CardSO card)
    {
        // If the card does not require sacrifice, it's playable.
        if (!card.requiresSacrifice || card.sacrificeRequirements == null || card.sacrificeRequirements.Count == 0)
            return true;

        CardSO[,] grid = GridManager.instance.GetGrid();
        // Loop through each sacrifice requirement.
        foreach (var req in card.sacrificeRequirements)
        {
            int foundCount = 0;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (grid[i, j] != null)
                    {
                        bool match = req.matchByCreatureType ?
                            (grid[i, j].creatureType == req.requiredCardName) :
                            (grid[i, j].cardName == req.requiredCardName);
                        if (match)
                            foundCount++;
                    }
                }
            }
            if (foundCount < req.count)
            {
                Debug.Log($"[AIController] {card.cardName} is not playable; sacrifice requirement for {req.requiredCardName} not met (need {req.count}, found {foundCount}).");
                return false;
            }
        }
        return true;
    }

    public void AITakeTurn()
    {
        StartCoroutine(AIPlay());
    }

    private IEnumerator AIPlay()
    {
        yield return new WaitForSeconds(1f); // Simulate AI thinking time
        CardSO[,] grid = GridManager.instance.GetGrid();

        aiPlayedCreature = false;
        aiPlayedSpell = false;

        Vector2Int bestMove = FindWinningMove(grid);
        if (bestMove.x == -1)
        {
            bestMove = FindBlockingMove(grid);
        }
        if (bestMove.x == -1)
        {
            bestMove = FindRandomMove(grid);
        }

        if (bestMove.x != -1)
        {
            // Find the best playable card from hand.
            CardHandler selectedCardHandler = GetBestPlayableCardFromHand();
            if (selectedCardHandler != null)
            {
                CardSO selectedCard = selectedCardHandler.cardData;
                if (selectedCard != null && TurnManager.instance.CanPlayCard(selectedCard))
                {
                    Debug.Log($"AI plays {selectedCard.cardName} at {bestMove.x}, {bestMove.y}");
                    PlaceAICardOnGrid(bestMove.x, bestMove.y, selectedCardHandler);
                    // Remove the played card from AI's hand and draw a new one.
                    aiHandCardHandlers.Remove(selectedCardHandler);
                    DrawCard();
                }
            }
            else
            {
                Debug.Log("AIController: No playable card found in AI hand!");
            }
        }

        yield return new WaitForSeconds(1f);
        TurnManager.instance.EndTurn(); // End AI's turn
    }

    // Helper method: Finds the best playable card in the AI hand (that satisfies sacrifice requirements).
    private CardHandler GetBestPlayableCardFromHand()
    {
        CardHandler bestCandidate = null;
        // Try creatures first, then spells.
        float highestPower = 0;
        foreach (CardHandler ch in aiHandCardHandlers)
        {
            CardSO card = ch.cardData;
            if (card.category == CardSO.CardCategory.Creature && !aiPlayedCreature && card.power > highestPower && IsCardPlayable(card))
            {
                bestCandidate = ch;
                highestPower = card.power;
            }
        }
        if (bestCandidate != null)
        {
            aiPlayedCreature = true;
            Debug.Log($"AI SELECTED CREATURE: {bestCandidate.cardData.cardName}");
            return bestCandidate;
        }

        // If no creature is found, try spells.
        foreach (CardHandler ch in aiHandCardHandlers)
        {
            CardSO card = ch.cardData;
            if (card.category == CardSO.CardCategory.Spell && !aiPlayedSpell && IsCardPlayable(card))
            {
                bestCandidate = ch;
                break;
            }
        }
        if (bestCandidate != null)
        {
            aiPlayedSpell = true;
            Debug.Log($"AI SELECTED SPELL: {bestCandidate.cardData.cardName}");
            return bestCandidate;
        }
        return null;
    }

    // Helper method: Finds the grid cell by name and places the card there.
    private void PlaceAICardOnGrid(int x, int y, CardHandler cardHandler)
    {
        string cellName = $"GridCell_{x}_{y}";
        GameObject cellObj = GameObject.Find(cellName);
        if (cellObj != null)
        {
            Transform cellParent = cellObj.transform;
            GridManager.instance.PlaceExistingCard(x, y, cardHandler.gameObject, cardHandler.cardData, cellParent);
            // After placing, reveal the card.
            CardUI cardUI = cardHandler.GetComponent<CardUI>();
            if (cardUI != null)
            {
                cardUI.RevealCard();
            }
            // Also, show card preview.
            CardPreviewManager.Instance.ShowCardPreview(cardHandler.cardData);
        }
        else
        {
            Debug.LogError($"[AIController] Could not find a cell named '{cellName}' for the AI to place a card!");
        }
    }

    private Vector2Int FindWinningMove(CardSO[,] grid)
    {
        // Placeholder logic for a winning move.
        return new Vector2Int(-1, -1);
    }

    private Vector2Int FindBlockingMove(CardSO[,] grid)
    {
        // Placeholder logic for a blocking move.
        return new Vector2Int(-1, -1);
    }

    private Vector2Int FindRandomMove(CardSO[,] grid)
    {
        List<Vector2Int> availableMoves = new List<Vector2Int>();
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (grid[x, y] == null)
                {
                    availableMoves.Add(new Vector2Int(x, y));
                }
            }
        }
        if (availableMoves.Count > 0)
        {
            return availableMoves[Random.Range(0, availableMoves.Count)];
        }
        return new Vector2Int(-1, -1);
    }
}
