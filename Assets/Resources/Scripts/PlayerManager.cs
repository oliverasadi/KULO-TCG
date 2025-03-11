using UnityEngine;
using UnityEngine.Serialization;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{

    public enum PlayerTypes { Local, AI, Online };
    public PlayerTypes playerType;
    public int playerNumber;

    [Header("Game Deck & UI")]
    public List<CardSO> currentDeck = new List<CardSO>(); // The deck currently in use for the game
    public List<CardHandler> cardHandlers = new List<CardHandler>(); // Cards currently in hand
    public GameObject cardPrefab; // Prefab for the card UI representation
    public Transform cardSpawnArea; // The UI area where drawn cards are displayed

    [FormerlySerializedAs("Zones")]
    [Header("Deck Objects")]
    public PlayerZones zones;

    public PlayerController pc;

    private const int HAND_SIZE = 5;
    private DeckManager dm;

    private void Start()
    {
        dm = DeckManager.instance;
        Debug.Log("PlayerManager initialized for playerType: " + playerType);

        // Load deck based on available sources.
        if (playerType == PlayerTypes.AI)
        {
            currentDeck = dm.GenerateRandomDeck();
        }
        else if (dm.availableDecks != null && dm.availableDecks.Count > 0)
        {
            // For local players, load the first external deck.
            currentDeck = dm.LoadDeck(dm.availableDecks[0]);
        }
        else if (dm.savedDecks != null && dm.savedDecks.Count > 0)
        {
            // Otherwise, load the first saved deck.
            currentDeck = dm.LoadDeck(dm.savedDecks[0].deckName);
        }
        else
        {
            currentDeck = dm.GenerateRandomDeck();
        }

        Debug.Log("Deck loaded. Count before fallback check: " + currentDeck.Count);
        if (currentDeck == null || currentDeck.Count == 0)
        {
            Debug.LogWarning("Loaded deck is empty. Generating a random deck as fallback.");
            currentDeck = dm.GenerateRandomDeck();
        }
        Debug.Log("Final deck count: " + currentDeck.Count);

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
        Debug.Log("✅ Deck shuffled! Deck count: " + currentDeck.Count);
    }

    /// <summary>
    /// Draws the starting hand by drawing HAND_SIZE cards.
    /// </summary>
    public void DrawStartingHand()
    {
        for (int i = 0; i < HAND_SIZE; i++)
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

    // --- New Methods for Handling Hand Discard Cost ---

    /// <summary>
    /// Initiates the hand discard selection mode for paying a cost (e.g., for 1000 Year Old Crab).
    /// </summary>
    /// <param name="effectCard">The CardUI of the card triggering the discard cost.</param>
    /// <param name="discardCount">The number of cards that must be discarded.</param>
    public void StartHandDiscardSelection(CardUI effectCard, int discardCount)
    {
        if (HandDiscardManager.Instance != null)
        {
            HandDiscardManager.Instance.BeginDiscardMode(discardCount, effectCard);
        }
        else
        {
            Debug.LogError("HandDiscardManager instance not found!");
        }
    }

    /// <summary>
    /// Discards a card from the player's hand.
    /// </summary>
    /// <param name="card">The CardUI of the card to be discarded.</param>
    public void DiscardCard(CardUI card)
    {
        CardHandler handler = card.GetComponent<CardHandler>();
        if (handler != null && cardHandlers.Contains(handler))
        {
            cardHandlers.Remove(handler);
            // Send the card to the grave zone using your existing PlayerZones logic.
            zones.AddCardToGrave(card.gameObject);
            Debug.Log("Discarded card: " + card.cardData.cardName);
        }
        else
        {
            Debug.LogWarning("Attempted to discard a card that is not in hand.");
        }
    }
}
