using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class AIController : MonoBehaviour
{
    public static AIController instance;
    public List<CardSO> aiDeck = new List<CardSO>(); // AI's deck
    public List<CardHandler> aiHandCardHandlers = new List<CardHandler>(); // AI hand's Card Handlers (5 cards max)
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
        if (aiDeck.Count > 0 && aiHandCardHandlers.Count < HAND_SIZE)
        {
            CardSO drawnCard = aiDeck[0];
            aiDeck.RemoveAt(0);
            DisplayCardInAIHand(drawnCard, true); // Ensure AI's hand starts face-down
        }
    }

    private void DisplayCardInAIHand(CardSO card, bool isFaceDown)
    {
        if (aiHandPanel == null || cardUIPrefab == null) return;
        GameObject cardUI = Instantiate(cardUIPrefab, aiHandPanel);
        CardUI cardUIScript = cardUI.GetComponent<CardUI>();
        if (cardUIScript != null)
        {
            cardUIScript.SetCardData(card, isFaceDown);
        }
        aiHandCardHandlers.Add(cardUI.GetComponent<CardHandler>());
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
            CardHandler selectedCardHandler = GetBestCardFromHand();
            CardSO selectedCard = selectedCardHandler.cardData;
            
            if (selectedCard != null && TurnManager.instance.CanPlayCard(selectedCard))
            {
                Debug.Log($"AI plays {selectedCard.cardName} at {bestMove.x}, {bestMove.y}");
                GridManager.instance.PlaceCard(bestMove.x, bestMove.y, selectedCard);
                
                selectedCardHandler.transform.SetParent(GameObject.Find(("GridCell_"+bestMove.x.ToString()+"_"+bestMove.y.ToString())).transform);
                selectedCardHandler.transform.localPosition = Vector3.zero;
                selectedCardHandler.GetComponentInParent<CardUI>().RevealCard();
                
                TurnManager.instance.RegisterCardPlay(selectedCard);
                
                aiHandCardHandlers.Remove(selectedCardHandler);
                
                DrawCard(); // Draw a new card after playing
            }
        }

        yield return new WaitForSeconds(1f);
        TurnManager.instance.EndTurn(); // AI ends its turn, player can press "End Turn" button again
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

    private CardHandler GetBestCardFromHand()
    {
        CardHandler bestCreature = null;
        CardHandler bestSpell = null;
        float highestPower = 0;

        foreach (CardHandler cardHandler in aiHandCardHandlers)
        {
            CardSO card = cardHandler.cardData;
            if (card.category == CardSO.CardCategory.Creature && !aiPlayedCreature && card.power > highestPower)
            {
                bestCreature = cardHandler;
                highestPower = card.power;
            }
            else if (card.category == CardSO.CardCategory.Spell && !aiPlayedSpell)
            {
                bestSpell = cardHandler;
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
