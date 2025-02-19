using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AIController : MonoBehaviour
{
    public static AIController instance;
    public List<CardSO> aiDeck;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public void AITakeTurn()
    {
        StartCoroutine(AIPlay());
    }

    private IEnumerator AIPlay()
    {
        yield return new WaitForSeconds(1f); // Simulate thinking time

        CardSO[,] grid = GridManager.instance.GetGrid(); // Get current board state

        // 1. Try to find a winning move
        Vector2Int bestMove = FindWinningMove(grid);
        if (bestMove.x == -1)
        {
            // 2. Try to block the player from winning
            bestMove = FindBlockingMove(grid);
        }
        if (bestMove.x == -1)
        {
            // 3. Pick a random available space
            bestMove = FindRandomMove(grid);
        }

        if (bestMove.x != -1) // If a valid move is found
        {
            CardSO selectedCard = GetBestAvailableCreature();
            if (selectedCard != null)
            {
                GridManager.instance.PlaceCard(bestMove.x, bestMove.y, selectedCard);
                TurnManager.instance.EndTurn();
            }
        }
    }

    private Vector2Int FindWinningMove(CardSO[,] grid)
    {
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (grid[x, y] == null) // If the space is empty
                {
                    grid[x, y] = GetBestAvailableCreature();
                    if (WinChecker.instance.CheckWinCondition(grid))
                    {
                        grid[x, y] = null; // Undo test move
                        return new Vector2Int(x, y);
                    }
                    grid[x, y] = null;
                }
            }
        }
        return new Vector2Int(-1, -1); // No winning move found
    }

    private Vector2Int FindBlockingMove(CardSO[,] grid)
    {
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (grid[x, y] == null) // If the space is empty
                {
                    // ✅ Simulate placing an opponent's piece instead of AI's own piece
                    grid[x, y] = GetOpponentBestMove();
                    if (WinChecker.instance.CheckWinCondition(grid))
                    {
                        grid[x, y] = null; // Undo test move
                        return new Vector2Int(x, y);
                    }
                    grid[x, y] = null;
                }
            }
        }
        return new Vector2Int(-1, -1); // No blocking move found
    }

    private Vector2Int FindRandomMove(CardSO[,] grid)
    {
        List<Vector2Int> emptySpaces = new List<Vector2Int>();
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (grid[x, y] == null)
                {
                    emptySpaces.Add(new Vector2Int(x, y));
                }
            }
        }
        if (emptySpaces.Count > 0)
        {
            return emptySpaces[Random.Range(0, emptySpaces.Count)];
        }
        return new Vector2Int(-1, -1);
    }

    private CardSO GetBestAvailableCreature()
    {
        CardSO bestCard = null;
        float highestPower = 0;

        foreach (CardSO card in aiDeck)
        {
            if (card.category == CardSO.CardCategory.Creature && card.power > highestPower)
            {
                bestCard = card;
                highestPower = card.power;
            }
        }
        return bestCard;
    }

    private CardSO GetOpponentBestMove()
    {
        // Placeholder for predicting the best opponent move.
        return new CardSO { power = 2000 }; // Simulates an opponent's strong move for blocking strategy
    }
}
