using System.Collections.Generic;
using UnityEngine;

public class DeckManager : MonoBehaviour
{
    public static DeckManager instance;

    private List<CardSO> availableCards = new List<CardSO>();
    public int deckSize = 40;

    public List<CardSO> deck = new List<CardSO>();
    public GameObject cardPrefab;
    public Transform cardSpawnArea;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        LoadAllCards();
        GenerateRandomDeck();
        ShuffleDeck();
        DrawStartingHand();
    }

    private void LoadAllCards()
    {
        availableCards.Clear();
        CardSO[] loadedCards = Resources.LoadAll<CardSO>("Cards");
        availableCards.AddRange(loadedCards);

        if (availableCards.Count == 0)
        {
            Debug.LogError("No CardSO assets found in Resources/Cards!");
        }
    }

    public void GenerateRandomDeck()
    {
        deck.Clear();
        List<CardSO> tempPool = new List<CardSO>(availableCards);

        for (int i = 0; i < deckSize; i++)
        {
            if (tempPool.Count == 0) break;

            int randomIndex = Random.Range(0, tempPool.Count);
            deck.Add(tempPool[randomIndex]);
            tempPool.RemoveAt(randomIndex);
        }
    }

    public void ShuffleDeck()
    {
        for (int i = 0; i < deck.Count; i++)
        {
            CardSO temp = deck[i];
            int randomIndex = Random.Range(i, deck.Count);
            deck[i] = deck[randomIndex];
            deck[randomIndex] = temp;
        }
    }

    public void DrawStartingHand()
    {
        for (int i = 0; i < 5; i++)
        {
            DrawCard();
        }
    }

    public void DrawCard()
    {
        if (deck.Count > 0)
        {
            CardSO drawnCard = deck[0];
            deck.RemoveAt(0);
            SpawnCard(drawnCard);
        }
    }

    public void SpawnCard(CardSO cardData)
    {
        if (cardSpawnArea == null)
        {
            Debug.LogError("Card Spawn Area is not assigned!");
            return;
        }

        GameObject newCard = Instantiate(cardPrefab, cardSpawnArea);
        CardHandler handler = newCard.GetComponent<CardHandler>();

        if (handler != null)
        {
            handler.SetCard(cardData);
        }
        else
        {
            Debug.LogError("CardHandler component is missing on CardPrefab!");
        }
    }
}
