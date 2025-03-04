using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    public enum PlayerTypes {Local, AI, Online};
    public PlayerTypes playerType;
    public int playerNumber;
    
    [Header("Game Deck & UI")]
    public List<CardSO> currentDeck = new List<CardSO>(); // The deck currently in use for the game
    public List<CardHandler> cardHandlers = new List<CardHandler>(); // AI hand's Card Handlers (max 5 cards)
    public GameObject cardPrefab; // Prefab for the card UI representation
    public Transform cardSpawnArea; // The UI area where drawn cards are displayed

    [FormerlySerializedAs("Zones")] [Header("Deck Objects")]
    public PlayerZones zones;

    public PlayerController pc;

    private const int HAND_SIZE = 5;
    private DeckManager dm;

    private void Start()
    {
        dm = DeckManager.instance;

        Debug.Log("PlayerManager initialized");
        
        if (playerType == PlayerTypes.AI)
        {
            currentDeck = dm.GenerateRandomDeck();
        }
        // Prefer loading an external deck if available.
        else if (dm.availableDecks.Count > 0)
        {
            // For example, load the first external deck:
            currentDeck = dm.LoadDeck(dm.availableDecks[0]);
        }
        else if (dm.savedDecks.Count > 0)
        {
            // Otherwise, load the first saved deck.
            currentDeck = dm.LoadDeck(dm.savedDecks[0].deckName);
        }
        else
        {
            currentDeck = dm.GenerateRandomDeck();
        }
        
        ShuffleDeck();
        DrawStartingHand();
    }

    /// <summary>
    /// Shuffles the current deck.
    /// </summary>
    public void ShuffleDeck()
    {
        for (int i = 0; i < currentDeck.Count; i++)
        {
            CardSO temp = currentDeck[i];
            int randomIndex = Random.Range(i, currentDeck.Count);
            currentDeck[i] = currentDeck[randomIndex];
            currentDeck[randomIndex] = temp;
        }
        Debug.Log("✅ Deck shuffled!");
    }

    /// <summary>
    /// Draws the starting hand by drawing 5 cards.
    /// </summary>
    public void DrawStartingHand()
    {
        for (int i = 0; i < 5; i++)
        {
            DrawCard();
        }
    }

    /// <summary>
    /// Draws a card from the current deck.
    /// </summary>
    public void DrawCard()
    {
        if (currentDeck.Count > 0)
        {
            CardSO drawnCard = currentDeck[0];
            currentDeck.RemoveAt(0);
            SpawnCard(drawnCard);

            if (zones != null)
                zones.UpdateDeckCount(currentDeck.Count);
        }
        else
        {
            Debug.LogWarning("⚠️ Deck is empty! No more cards to draw.");
        }
    }

    /// <summary>
    /// Spawns a card in the player's hand and assigns its data.
    /// </summary>
    public void SpawnCard(CardSO cardData)
    {
        if (cardSpawnArea == null)
        {
            Debug.LogError("❌ Card Spawn Area is not assigned!");
            return;
        }

        GameObject newCard = Instantiate(cardPrefab, cardSpawnArea);
        CardHandler handler = newCard.GetComponent<CardHandler>();

        if (handler != null)
        {
            handler.SetCard(cardData, playerNumber == 2, playerType == PlayerTypes.AI);
            handler.cardOwner = this;
            cardHandlers.Add(handler);
        }
        else
        {
            Debug.LogError("❌ CardHandler component is missing on CardPrefab!");
        }
    }
}
