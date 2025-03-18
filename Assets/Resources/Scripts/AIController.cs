using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class AIController : PlayerController
{
    TurnManager tm;
    public PlayerManager pm;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        tm = TurnManager.instance;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Helper method: Checks if the card's sacrifice requirements are met on the grid.
private bool IsCardPlayable(CardSO card)
    {
        // If this is an evolution card but has no valid base, it’s not playable.
        if (card.baseOrEvo == CardSO.BaseOrEvo.Evolution)
        {
            if (!card.requiresSacrifice || card.sacrificeRequirements == null || card.sacrificeRequirements.Count == 0)
                return false;

            bool hasValidBase = false;
            CardSO[,] gameGrid = GridManager.instance.GetGrid(); // Renamed to gameGrid to avoid conflict

            // Loop through the grid to check for a valid base card
            foreach (var req in card.sacrificeRequirements)
            {
                int foundCount = 0;
                for (int i = 0; i < 3; i++)
                {
                    for (int j = 0; j < 3; j++)
                    {
                        if (gameGrid[i, j] != null)
                        {
                            bool match = req.matchByCreatureType ?
                                (gameGrid[i, j].creatureType == req.requiredCardName) :
                                (gameGrid[i, j].cardName == req.requiredCardName);
                            if (match)
                                foundCount++;
                        }
                    }
                }
                if (foundCount >= req.count)
                {
                    hasValidBase = true;
                    break;
                }
            }

            if (!hasValidBase)
            {
                Debug.Log($"[AIController] {card.cardName} cannot be played; evolution requirements not met.");
                return false;
            }
        }

        // If no sacrifice requirements, it’s playable
        if (!card.requiresSacrifice || card.sacrificeRequirements.Count == 0)
            return true;

        CardSO[,] fieldGrid = GridManager.instance.GetGrid(); // Renamed to fieldGrid to avoid conflict
        foreach (var req in card.sacrificeRequirements)
        {
            int foundCount = 0;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (fieldGrid[i, j] != null)
                    {
                        bool match = req.matchByCreatureType ?
                            (fieldGrid[i, j].creatureType == req.requiredCardName) :
                            (fieldGrid[i, j].cardName == req.requiredCardName);
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


    public override void StartTurn()
    {
        StartCoroutine(AIPlay());
    }

    private IEnumerator AIPlay()
    {
        yield return new WaitForSeconds(1f); // Simulate AI thinking time
        CardSO[,] grid = GridManager.instance.GetGrid();

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
                    pm.cardHandlers.Remove(selectedCardHandler);
                }
            }
            else
            {
                Debug.Log("AIController: No playable card found in AI hand!");
            }
        }

        yield return new WaitForSeconds(1f);
        EndTurn(); // End AI's turn
    }

    // Helper method: Finds the best playable card in the AI hand (that satisfies sacrifice requirements).
    private CardHandler GetBestPlayableCardFromHand()
    {
        CardHandler bestCandidate = null;
        // Try creatures first, then spells.
        float highestPower = 0;
        foreach (CardHandler ch in pm.cardHandlers)
        {
            CardSO card = ch.cardData;
            if (card.category == CardSO.CardCategory.Creature && !tm.creaturePlayed  && card.power > highestPower && IsCardPlayable(card))
            {
                bestCandidate = ch;
                highestPower = card.power;
            }
        }
        if (bestCandidate != null)
        {
            Debug.Log($"AI SELECTED CREATURE: {bestCandidate.cardData.cardName}");
            return bestCandidate;
        }

        // If no creature is found, try spells.
        foreach (CardHandler ch in pm.cardHandlers)
        {
            CardSO card = ch.cardData;
            if (card.category == CardSO.CardCategory.Spell && !tm.spellPlayed && IsCardPlayable(card))
            {
                bestCandidate = ch;
                break;
            }
        }
        if (bestCandidate != null)
        {
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
        
        if (cardHandler.cardData.category == CardSO.CardCategory.Creature)
            tm.creaturePlayed = true;
        else if (cardHandler.cardData.category == CardSO.CardCategory.Spell)
            tm.spellPlayed = true;
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
