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

        // Check for aggressive moves (replacing player's weaker cards).
        Vector2Int bestMove = FindBestAggressiveMove(grid);
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
            CardHandler selectedCardHandler = GetBestPlayableCardFromHand();
            if (selectedCardHandler != null)
            {
                CardSO selectedCard = selectedCardHandler.cardData;
                if (selectedCard != null && TurnManager.instance.CanPlayCard(selectedCard))
                {
                    Debug.Log($"AI plays {selectedCard.cardName} at {bestMove.x}, {bestMove.y}");
                    PlaceAICardOnGrid(bestMove.x, bestMove.y, selectedCardHandler);
                    pm.cardHandlers.Remove(selectedCardHandler); // Remove the played card from AI's hand.
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
    private Vector2Int FindBestAggressiveMove(CardSO[,] grid)
    {
        // 1. Priority 1: Try to win by completing 3 in a row
        Vector2Int winningMove = FindWinningMove(grid);
        if (winningMove.x != -1)
        {
            return winningMove; // If a winning move is found, prioritize it
        }

        List<Vector2Int> replaceableZones = new List<Vector2Int>();
        List<Vector2Int> blockingZones = new List<Vector2Int>();
        List<Vector2Int> openZones = new List<Vector2Int>();

        // 2. Priority 2: Destroy opponent's cards (especially in the center grid)
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                if (grid[x, y] != null && GetOwnerTagFromCell(x, y) == "Player") // Check if the cell belongs to the player
                {
                    CardSO playerCard = grid[x, y];
                    if (playerCard != null && CheckForReplacement(x, y, playerCard)) // If the AI can replace it
                    {
                        // Prioritize the center grid (1,1)
                        if (x == 1 && y == 1)
                        {
                            return new Vector2Int(x, y); // Immediately return the center grid if available
                        }

                        replaceableZones.Add(new Vector2Int(x, y)); // Add to replaceable zones
                    }
                }
                else if (grid[x, y] == null) // Open zone
                {
                    openZones.Add(new Vector2Int(x, y));
                }
            }
        }

        // 3. Priority 3: Block potential player wins (3 in a row)
        blockingZones = FindBlockingMoves(grid);

        if (blockingZones.Count > 0)
        {
            return blockingZones[0]; // Block the first available move
        }

        // 4. Priority 4: If nothing to block, destroy opponent's card in replaceable zones
        if (replaceableZones.Count > 0)
        {
            return replaceableZones[0]; // Destroy the first replaceable card
        }

        // 5. Priority 5: If nothing to block or replace, play anywhere in open zones
        if (openZones.Count > 0)
        {
            return openZones[0]; // Place in the first available open zone
        }

        return new Vector2Int(-1, -1); // If no valid move is found, return -1
    }



    private bool CheckForReplacement(int x, int y, CardSO playerCard)
    {
        // Check if any AI creature can replace the player's card (i.e., AI card has higher or equal power).
        CardHandler aiCardHandler = GetBestPlayableCardFromHand();
        if (aiCardHandler != null)
        {
            CardSO aiCard = aiCardHandler.cardData;
            if (aiCard != null && aiCard.power >= playerCard.power) // Allow replacing if equal or stronger
            {
                return true; // AI can replace this card because it's stronger or equal in power
            }
        }
        return false;
    }

    private List<Vector2Int> FindBlockingMoves(CardSO[,] grid)
    {
        List<Vector2Int> blockingZones = new List<Vector2Int>();

        // Check rows, columns, and diagonals for potential 3 in a row
        for (int i = 0; i < 3; i++)
        {
            // Check rows
            if (grid[i, 0] != null && grid[i, 1] != null && grid[i, 2] == null &&
                GetOwnerTagFromCell(i, 0) == "Player" && GetOwnerTagFromCell(i, 1) == "Player")
            {
                blockingZones.Add(new Vector2Int(i, 2)); // Block the third cell in the row
            }
            if (grid[i, 0] != null && grid[i, 2] != null && grid[i, 1] == null &&
                GetOwnerTagFromCell(i, 0) == "Player" && GetOwnerTagFromCell(i, 2) == "Player")
            {
                blockingZones.Add(new Vector2Int(i, 1)); // Block the second cell in the row
            }
            if (grid[i, 1] != null && grid[i, 2] != null && grid[i, 0] == null &&
                GetOwnerTagFromCell(i, 1) == "Player" && GetOwnerTagFromCell(i, 2) == "Player")
            {
                blockingZones.Add(new Vector2Int(i, 0)); // Block the first cell in the row
            }

            // Check columns
            if (grid[0, i] != null && grid[1, i] != null && grid[2, i] == null &&
                GetOwnerTagFromCell(0, i) == "Player" && GetOwnerTagFromCell(1, i) == "Player")
            {
                blockingZones.Add(new Vector2Int(2, i)); // Block the third cell in the column
            }
            if (grid[0, i] != null && grid[2, i] != null && grid[1, i] == null &&
                GetOwnerTagFromCell(0, i) == "Player" && GetOwnerTagFromCell(2, i) == "Player")
            {
                blockingZones.Add(new Vector2Int(1, i)); // Block the second cell in the column
            }
            if (grid[1, i] != null && grid[2, i] != null && grid[0, i] == null &&
                GetOwnerTagFromCell(1, i) == "Player" && GetOwnerTagFromCell(2, i) == "Player")
            {
                blockingZones.Add(new Vector2Int(0, i)); // Block the first cell in the column
            }
        }

        // Check diagonals for potential 3 in a row
        if (grid[0, 0] != null && grid[1, 1] != null && grid[2, 2] == null &&
            GetOwnerTagFromCell(0, 0) == "Player" && GetOwnerTagFromCell(1, 1) == "Player")
        {
            blockingZones.Add(new Vector2Int(2, 2)); // Block the third cell in the diagonal
        }
        if (grid[0, 0] != null && grid[2, 2] != null && grid[1, 1] == null &&
            GetOwnerTagFromCell(0, 0) == "Player" && GetOwnerTagFromCell(2, 2) == "Player")
        {
            blockingZones.Add(new Vector2Int(1, 1)); // Block the second cell in the diagonal
        }
        if (grid[1, 1] != null && grid[2, 2] != null && grid[0, 0] == null &&
            GetOwnerTagFromCell(1, 1) == "Player" && GetOwnerTagFromCell(2, 2) == "Player")
        {
            blockingZones.Add(new Vector2Int(0, 0)); // Block the first cell in the diagonal
        }

        if (grid[0, 2] != null && grid[1, 1] != null && grid[2, 0] == null &&
            GetOwnerTagFromCell(0, 2) == "Player" && GetOwnerTagFromCell(1, 1) == "Player")
        {
            blockingZones.Add(new Vector2Int(2, 0)); // Block the third cell in the opposite diagonal
        }
        if (grid[0, 2] != null && grid[2, 0] != null && grid[1, 1] == null &&
            GetOwnerTagFromCell(0, 2) == "Player" && GetOwnerTagFromCell(2, 0) == "Player")
        {
            blockingZones.Add(new Vector2Int(1, 1)); // Block the second cell in the opposite diagonal
        }
        if (grid[1, 1] != null && grid[2, 0] != null && grid[0, 2] == null &&
            GetOwnerTagFromCell(1, 1) == "Player" && GetOwnerTagFromCell(2, 0) == "Player")
        {
            blockingZones.Add(new Vector2Int(0, 2)); // Block the first cell in the opposite diagonal
        }

        return blockingZones; // Return the list of blocking zones
    }


    // Helper method: Finds the best playable card in the AI hand (that satisfies sacrifice requirements).
    private CardHandler GetBestPlayableCardFromHand()
    {
        CardHandler bestCandidate = null;
        float highestPower = 0;

        // Prioritize replacing cards with higher power creatures.
        foreach (CardHandler ch in pm.cardHandlers)
        {
            CardSO card = ch.cardData;

            if (card.category == CardSO.CardCategory.Creature && !tm.creaturePlayed && card.power > highestPower && IsCardPlayable(card))
            {
                bestCandidate = ch;
                highestPower = card.power;
            }
        }

        // If no strong creatures are found, try spells.
        if (bestCandidate == null)
        {
            foreach (CardHandler ch in pm.cardHandlers)
            {
                CardSO card = ch.cardData;
                if (card.category == CardSO.CardCategory.Spell && !tm.spellPlayed && IsCardPlayable(card))
                {
                    bestCandidate = ch;
                    break;
                }
            }
        }

        return bestCandidate;
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
        // Check all rows, columns, and diagonals for potential winning moves

        // Check rows for 2 AI cards and 1 empty space
        for (int i = 0; i < 3; i++)
        {
            if (grid[i, 0] != null && grid[i, 1] != null && grid[i, 2] == null &&
                GetOwnerTagFromCell(i, 0) == "AI" && GetOwnerTagFromCell(i, 1) == "AI")
            {
                return new Vector2Int(i, 2); // Winning move: Complete the row
            }
            if (grid[i, 0] != null && grid[i, 2] != null && grid[i, 1] == null &&
                GetOwnerTagFromCell(i, 0) == "AI" && GetOwnerTagFromCell(i, 2) == "AI")
            {
                return new Vector2Int(i, 1); // Winning move: Complete the row
            }
            if (grid[i, 1] != null && grid[i, 2] != null && grid[i, 0] == null &&
                GetOwnerTagFromCell(i, 1) == "AI" && GetOwnerTagFromCell(i, 2) == "AI")
            {
                return new Vector2Int(i, 0); // Winning move: Complete the row
            }
        }

        // Check columns for 2 AI cards and 1 empty space
        for (int i = 0; i < 3; i++)
        {
            if (grid[0, i] != null && grid[1, i] != null && grid[2, i] == null &&
                GetOwnerTagFromCell(0, i) == "AI" && GetOwnerTagFromCell(1, i) == "AI")
            {
                return new Vector2Int(2, i); // Winning move: Complete the column
            }
            if (grid[0, i] != null && grid[2, i] != null && grid[1, i] == null &&
                GetOwnerTagFromCell(0, i) == "AI" && GetOwnerTagFromCell(2, i) == "AI")
            {
                return new Vector2Int(1, i); // Winning move: Complete the column
            }
            if (grid[1, i] != null && grid[2, i] != null && grid[0, i] == null &&
                GetOwnerTagFromCell(1, i) == "AI" && GetOwnerTagFromCell(2, i) == "AI")
            {
                return new Vector2Int(0, i); // Winning move: Complete the column
            }
        }

        // Check diagonals for 2 AI cards and 1 empty space
        if (grid[0, 0] != null && grid[1, 1] != null && grid[2, 2] == null &&
            GetOwnerTagFromCell(0, 0) == "AI" && GetOwnerTagFromCell(1, 1) == "AI")
        {
            return new Vector2Int(2, 2); // Winning move: Complete the diagonal
        }
        if (grid[0, 0] != null && grid[2, 2] != null && grid[1, 1] == null &&
            GetOwnerTagFromCell(0, 0) == "AI" && GetOwnerTagFromCell(2, 2) == "AI")
        {
            return new Vector2Int(1, 1); // Winning move: Complete the diagonal
        }
        if (grid[1, 1] != null && grid[2, 2] != null && grid[0, 0] == null &&
            GetOwnerTagFromCell(1, 1) == "AI" && GetOwnerTagFromCell(2, 2) == "AI")
        {
            return new Vector2Int(0, 0); // Winning move: Complete the diagonal
        }

        if (grid[0, 2] != null && grid[1, 1] != null && grid[2, 0] == null &&
            GetOwnerTagFromCell(0, 2) == "AI" && GetOwnerTagFromCell(1, 1) == "AI")
        {
            return new Vector2Int(2, 0); // Winning move: Complete the opposite diagonal
        }
        if (grid[0, 2] != null && grid[2, 0] != null && grid[1, 1] == null &&
            GetOwnerTagFromCell(0, 2) == "AI" && GetOwnerTagFromCell(2, 0) == "AI")
        {
            return new Vector2Int(1, 1); // Winning move: Complete the opposite diagonal
        }
        if (grid[1, 1] != null && grid[2, 0] != null && grid[0, 2] == null &&
            GetOwnerTagFromCell(1, 1) == "AI" && GetOwnerTagFromCell(2, 0) == "AI")
        {
            return new Vector2Int(0, 2); // Winning move: Complete the opposite diagonal
        }

        return new Vector2Int(-1, -1); // No winning move found
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
