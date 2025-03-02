using UnityEngine;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
    public static DeckManager instance;

    [Header("Deck Settings")]
    public int maxDeckSize = 40; // Maximum number of cards per deck
    public List<CardSO> allAvailableCards = new List<CardSO>(); // All available cards (loaded from Resources/Cards)
    public List<DeckData> savedDecks = new List<DeckData>(); // Saved decks (using DeckData)

    [Header("External Decks (ScriptableObjects)")]
    // Deck assets (of type DeckDataSO) located in Resources/Decks
    public List<DeckDataSO> availableDecks = new List<DeckDataSO>();

    [Header("Game Deck & UI")]
    public List<CardSO> currentDeck = new List<CardSO>(); // The deck currently in use for the game
    public GameObject cardPrefab; // Prefab for the card UI representation
    public Transform cardSpawnArea; // The UI area where drawn cards are displayed

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        LoadAllCards();
        LoadAvailableDecks(); // Load external decks from Resources/Decks
        LoadDecks();         // Load saved decks from PlayerPrefs

        // Prefer loading an external deck if available.
        if (availableDecks.Count > 0)
        {
            // For example, load the first external deck:
            LoadDeck(availableDecks[0]);
        }
        else if (savedDecks.Count > 0)
        {
            // Otherwise, load the first saved deck.
            LoadDeck(savedDecks[0].deckName);
        }
        else
        {
            GenerateRandomDeck();
        }
        ShuffleDeck();
        DrawStartingHand();
    }

    /// <summary>
    /// Loads all CardSO assets from the Resources/Cards folder.
    /// </summary>
    private void LoadAllCards()
    {
        allAvailableCards.Clear();
        CardSO[] loadedCards = Resources.LoadAll<CardSO>("Cards");

        if (loadedCards.Length == 0)
        {
            Debug.LogError("❌ No CardSO assets found in Resources/Cards!");
            return;
        }

        allAvailableCards.AddRange(loadedCards);
        Debug.Log($"✅ Loaded {allAvailableCards.Count} cards from Resources/Cards/");
    }

    /// <summary>
    /// Loads all external deck assets from the Resources/Decks folder.
    /// </summary>
    private void LoadAvailableDecks()
    {
        availableDecks.Clear();
        DeckDataSO[] decks = Resources.LoadAll<DeckDataSO>("Decks");
        if (decks.Length == 0)
        {
            Debug.LogWarning("⚠️ No external deck assets found in Resources/Decks!");
        }
        else
        {
            availableDecks.AddRange(decks);
            Debug.Log($"✅ Loaded {availableDecks.Count} external decks from Resources/Decks/");
        }
    }

    /// <summary>
    /// Generates a random deck by selecting cards randomly from allAvailableCards.
    /// </summary>
    public void GenerateRandomDeck()
    {
        currentDeck.Clear();
        List<CardSO> tempPool = new List<CardSO>(allAvailableCards);

        for (int i = 0; i < maxDeckSize; i++)
        {
            if (tempPool.Count == 0)
                break;
            int randomIndex = Random.Range(0, tempPool.Count);
            currentDeck.Add(tempPool[randomIndex]);
            tempPool.RemoveAt(randomIndex);
        }
        Debug.Log("✅ Random deck generated!");
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

            if (DeckZone.instance != null)
                DeckZone.instance.UpdateDeckCount(currentDeck.Count);
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
            handler.SetCard(cardData, false, false);
        }
        else
        {
            Debug.LogError("❌ CardHandler component is missing on CardPrefab!");
        }
    }

    /// <summary>
    /// Saves the savedDecks list to PlayerPrefs using JSON.
    /// </summary>
    public void SaveDecks()
    {
        PlayerPrefs.SetString("SavedDecks", JsonUtility.ToJson(new DeckSaveWrapper(savedDecks)));
        PlayerPrefs.Save();
        Debug.Log("✅ Decks saved!");
    }

    /// <summary>
    /// Loads saved decks from PlayerPrefs.
    /// </summary>
    public void LoadDecks()
    {
        if (PlayerPrefs.HasKey("SavedDecks"))
        {
            string json = PlayerPrefs.GetString("SavedDecks");
            savedDecks = JsonUtility.FromJson<DeckSaveWrapper>(json).decks;
            Debug.Log($"✅ Loaded {savedDecks.Count} saved decks.");
        }
        else
        {
            savedDecks = new List<DeckData>();
            Debug.LogWarning("⚠️ No saved decks found. Using default decks.");
        }
    }

    /// <summary>
    /// Loads a specific deck by name from the saved decks.
    /// </summary>
    public void LoadDeck(string deckName)
    {
        DeckData selectedDeck = savedDecks.Find(deck => deck.deckName == deckName);
        if (selectedDeck == null)
        {
            Debug.LogError($"❌ Deck '{deckName}' not found!");
            return;
        }

        currentDeck.Clear();
        foreach (string cardName in selectedDeck.cardNames)
        {
            CardSO foundCard = allAvailableCards.Find(card => card.cardName == cardName);
            if (foundCard != null)
            {
                currentDeck.Add(foundCard);
            }
        }
        Debug.Log($"✅ Loaded deck '{deckName}' with {currentDeck.Count} cards.");
    }

    /// <summary>
    /// Loads a deck from a DeckDataSO asset.
    /// </summary>
    public void LoadDeck(DeckDataSO deckData)
    {
        currentDeck.Clear();
        foreach (CardEntry entry in deckData.cardEntries)
        {
            for (int i = 0; i < entry.quantity; i++)
            {
                if (entry.card != null)
                    currentDeck.Add(entry.card);
            }
        }
        Debug.Log($"Loaded deck '{deckData.deckName}' with {currentDeck.Count} cards.");
    }

    /// <summary>
    /// Creates a new deck with the specified cards.
    /// </summary>
    public void CreateDeck(string deckName, List<CardSO> cards)
    {
        if (cards.Count > maxDeckSize)
        {
            Debug.LogWarning($"⚠️ Cannot create '{deckName}'. Too many cards ({cards.Count}/{maxDeckSize})!");
            return;
        }

        DeckData newDeck = new DeckData(deckName);
        foreach (CardSO card in cards)
        {
            newDeck.cardNames.Add(card.cardName);
        }

        savedDecks.Add(newDeck);
        SaveDecks();
        Debug.Log($"✅ New deck '{deckName}' created!");
    }
}

/// <summary>
/// Wrapper class for saving multiple decks via JSON.
/// </summary>
[System.Serializable]
public class DeckSaveWrapper
{
    public List<DeckData> decks;
    public DeckSaveWrapper(List<DeckData> decks)
    {
        this.decks = decks;
    }
}

/// <summary>
/// Data structure for a deck.
/// </summary>
[System.Serializable]
public class DeckData
{
    public string deckName;
    public List<string> cardNames = new List<string>();

    public DeckData(string name)
    {
        deckName = name;
    }
}
