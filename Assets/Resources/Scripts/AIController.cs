using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class AIController : MonoBehaviour
{
    public static AIController instance;
    public List<CardSO> aiDeck = new List<CardSO>(); // AI's deck
    public List<CardSO> aiHand = new List<CardSO>(); // AI's hand (5 cards max)
    private bool aiPlayedCreature = false;
    private bool aiPlayedSpell = false;
    private const int HAND_SIZE = 5;

    [Header("AI Hand UI")]
    [SerializeField] private Transform aiHandPanel; // Assign in Inspector
    public GameObject cardUIPrefab; // Assign in Inspector

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
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
        if (aiDeck.Count > 0 && aiHand.Count < HAND_SIZE)
        {
            CardSO drawnCard = aiDeck[0];
            aiDeck.RemoveAt(0);
            aiHand.Add(drawnCard);
            DisplayCardInAIHand(drawnCard);
        }
    }

    private void DisplayCardInAIHand(CardSO card)
    {
        if (aiHandPanel == null || cardUIPrefab == null) return;
        GameObject cardUI = Instantiate(cardUIPrefab, aiHandPanel);
        CardUI cardUIScript = cardUI.GetComponent<CardUI>();
        if (cardUIScript != null)
        {
            cardUIScript.SetCardData(card, null); // No deck editor needed for AI
        }
    }

    public void AITakeTurn()
    {
        StartCoroutine(AIPlay());
    }

    private IEnumerator AIPlay()
    {
        yield return new WaitForSeconds(1f); // Simulate AI thinking time
        CardSO[,] grid = GridManager.instance.GetGrid(); // Get board state

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
            CardSO selectedCard = GetBestCardFromHand();
            if (selectedCard != null && TurnManager.instance.CanPlayCard(selectedCard))
            {
                GridManager.instance.PlaceCard(bestMove.x, bestMove.y, selectedCard);
                TurnManager.instance.RegisterCardPlay(selectedCard);
                aiHand.Remove(selectedCard);
                DrawCard(); // Draw a new card after playing
            }
        }

        yield return new WaitForSeconds(1f);
        TurnManager.instance.EndTurn();
    }

    private Vector2Int FindWinningMove(CardSO[,] grid)
    {
        return new Vector2Int(-1, -1); // Placeholder logic
    }

    private Vector2Int FindBlockingMove(CardSO[,] grid)
    {
        return new Vector2Int(-1, -1); // Placeholder logic
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

    private CardSO GetBestCardFromHand()
    {
        CardSO bestCreature = null;
        CardSO bestSpell = null;
        float highestPower = 0;

        foreach (CardSO card in aiHand)
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

        return null;
    }
}
