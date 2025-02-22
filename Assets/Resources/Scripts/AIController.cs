using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AIController : MonoBehaviour
{
    public static AIController instance;
    public List<CardSO> aiDeck = new List<CardSO>(); // AI deck
    private bool aiPlayedCreature = false;
    private bool aiPlayedSpell = false;

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (aiDeck.Count == 0) // ✅ AI deck is empty, generate a random deck
        {
            GenerateRandomAIDeck();
        }
    }

    public void AITakeTurn()
    {
        StartCoroutine(AIPlay());
    }

    private IEnumerator AIPlay()
    {
        yield return new WaitForSeconds(1f); // Simulate thinking time

        CardSO[,] grid = GridManager.instance.GetGrid(); // Get current board state

        // Reset AI move tracking
        aiPlayedCreature = false;
        aiPlayedSpell = false;

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
            // AI picks a card to play based on turn rules
            CardSO selectedCard = GetBestAvailableCard();
            if (selectedCard != null && TurnManager.instance.CanPlayCard(selectedCard))
            {
                GridManager.instance.PlaceCard(bestMove.x, bestMove.y, selectedCard);
                TurnManager.instance.RegisterCardPlay(selectedCard);
            }
        }

        yield return new WaitForSeconds(1f); // Simulate AI processing

        // ✅ AI ends turn after making a move
        TurnManager.instance.EndTurn();
    }

    private void GenerateRandomAIDeck()
    {
        Debug.Log("⚠️ AI deck is empty! Generating a random deck...");
        aiDeck.Clear();
        CardSO[] allCards = Resources.LoadAll<CardSO>("Cards"); // ✅ Load all cards from the game

        if (allCards.Length == 0)
        {
            Debug.LogError("❌ No available cards found in Resources/Cards!");
            return;
        }

        // ✅ Randomly pick 40 cards
        while (aiDeck.Count < 40)
        {
            CardSO randomCard = allCards[Random.Range(0, allCards.Length)];
            aiDeck.Add(randomCard);
        }

        Debug.Log($"✅ AI deck generated with {aiDeck.Count} cards.");
    }

    private Vector2Int FindWinningMove(CardSO[,] grid)
    {
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (grid[x, y] == null) // If the space is empty
                {
                    grid[x, y] = GetBestAvailableCard();
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

    private CardSO GetBestAvailableCard()
    {
        CardSO bestCreature = null;
        CardSO bestSpell = null;
        float highestPower = 0;

        foreach (CardSO card in aiDeck)
        {
            if (card.category == CardSO.CardCategory.Creature && !aiPlayedCreature && card.power > highestPower)
            {
                bestCreature = card;
                highestPower = card.power;
            }
            else if (card.category == CardSO.CardCategory.Spell && !aiPlayedSpell)
            {
                bestSpell = card;
            }
        }

        if (bestCreature != null)
        {
            aiPlayedCreature = true;
            return bestCreature;
        }
        if (bestSpell != null)
        {
            aiPlayedSpell = true;
            return bestSpell;
        }

        return null; // AI has no valid move left
    }

    private CardSO GetOpponentBestMove()
    {
        return new CardSO { power = 2000 }; // Simulate strong opponent move
    }
}
