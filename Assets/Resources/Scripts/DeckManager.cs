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

    [Header("Prefabs")]
    public GameObject cardPrefab; // Reference to a card prefab for instantiation in effects

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);

        // Load Cards, External Decks, and Saved Decks.
        LoadAllCards();
        LoadAvailableDecks(); // Load external decks from Resources/Decks
        LoadDecks();          // Load saved decks from PlayerPrefs
    }

    private void Start()
    {
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
    public List<CardSO> GenerateRandomDeck()
    {
        List<CardSO> tempPool = new List<CardSO>(allAvailableCards);
        List<CardSO> returnDeck = new List<CardSO>();
        for (int i = 0; i < maxDeckSize; i++)
        {
            if (tempPool.Count == 0)
                break;
            int randomIndex = Random.Range(0, tempPool.Count);
            returnDeck.Add(tempPool[randomIndex]);
            tempPool.RemoveAt(randomIndex);
        }
        Debug.Log("✅ Random deck generated!");
        return returnDeck;
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
    public List<CardSO> LoadDeck(string deckName)
    {
        List<CardSO> returnDeck = new List<CardSO>();
        DeckData selectedDeck = savedDecks.Find(deck => deck.deckName == deckName);
        if (selectedDeck == null)
        {
            Debug.LogError($"❌ Deck '{deckName}' not found!");
            return returnDeck;
        }

        foreach (string cardName in selectedDeck.cardNames)
        {
            CardSO foundCard = allAvailableCards.Find(card => card.cardName == cardName);
            if (foundCard != null)
            {
                returnDeck.Add(foundCard);
            }
        }
        Debug.Log($"✅ Loaded deck '{deckName}' with {returnDeck.Count} cards.");
        return returnDeck;
    }

    /// <summary>
    /// Loads a deck from a DeckDataSO asset.
    /// </summary>
    public List<CardSO> LoadDeck(DeckDataSO deckData)
    {
        List<CardSO> returnDeck = new List<CardSO>();
        foreach (CardEntry entry in deckData.cardEntries)
        {
            for (int i = 0; i < entry.quantity; i++)
            {
                if (entry.card != null)
                    returnDeck.Add(entry.card);
            }
        }
        Debug.Log($"Loaded deck '{deckData.deckName}' with {returnDeck.Count} cards.");
        return returnDeck;
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

    /// <summary>
    /// Finds a card by name in the list of all available cards.
    /// </summary>
    public CardSO FindCardByName(string cardName)
    {
        return allAvailableCards.Find(card => card.cardName == cardName);
    }

    /// <summary>
    /// Returns an array of all available card names.
    /// </summary>
    public string[] GetAllCardNames()
    {
        List<string> names = new List<string>();
        foreach (CardSO card in allAvailableCards)
        {
            names.Add(card.cardName);
        }
        return names.ToArray();
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
