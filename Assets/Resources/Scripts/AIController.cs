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
        // If the card is an Evolution card, ensure the AI has the required base on its board.
        if (card.baseOrEvo == CardSO.BaseOrEvo.Evolution)
        {
            // If there are no sacrifice requirements, this EVO card isn't playable.
            if (!card.requiresSacrifice || card.sacrificeRequirements == null || card.sacrificeRequirements.Count == 0)
            {
                Debug.Log($"[AIController] Evolution card {card.cardName} is not playable: no sacrifice requirements set.");
                return false;
            }

            bool hasValidBase = false;
            CardSO[,] grid = GridManager.instance.GetGrid();

            // Loop over every cell on the board.
            for (int i = 0; i < grid.GetLength(0); i++)
            {
                for (int j = 0; j < grid.GetLength(1); j++)
                {
                    if (grid[i, j] == null)
                        continue;

                    // Only consider cells where the card belongs to the AI.
                    string owner = GetOwnerTagFromCell(i, j);
                    if (owner != "AI")
                        continue;

                    // Check each sacrifice requirement.
                    foreach (var req in card.sacrificeRequirements)
                    {
                        bool match = req.matchByCreatureType
                            ? (grid[i, j].creatureType == req.requiredCardName)
                            : (grid[i, j].cardName == req.requiredCardName);
                        if (match)
                        {
                            hasValidBase = true;
                            break;
                        }
                    }
                    if (hasValidBase)
                        break;
                }
                if (hasValidBase)
                    break;
            }

            if (!hasValidBase)
            {
                Debug.Log($"[AIController] Evolution card {card.cardName} is not playable because a valid base is not found on AI's board.");
                return false;
            }
        }

        // If there are no sacrifice requirements at all, it's playable.
        if (!card.requiresSacrifice || card.sacrificeRequirements.Count == 0)
            return true;

        // Check general sacrifice requirements for any card type (creature/spell).
        CardSO[,] fieldGrid = GridManager.instance.GetGrid();
        foreach (var req in card.sacrificeRequirements)
        {
            int foundCount = 0;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (fieldGrid[i, j] != null)
                    {
                        // Again, only count AI-owned cards
                        if (GetOwnerTagFromCell(i, j) != "AI")
                            continue;

                        bool match = req.matchByCreatureType
                            ? (fieldGrid[i, j].creatureType == req.requiredCardName)
                            : (fieldGrid[i, j].cardName == req.requiredCardName);
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

    // Helper method to determine who owns a grid cell's card.
    private string GetOwnerTagFromCell(int x, int y)
    {
        GameObject obj = GridManager.instance.GetGridObjects()[x, y];
        if (obj == null) return "Empty";

        CardHandler ch = obj.GetComponent<CardHandler>();
        if (ch == null) return "Empty";

        return ch.isAI ? "AI" : "Player";
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
