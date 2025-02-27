using UnityEngine;
using System.Collections.Generic;

public class DeckManager : MonoBehaviour
{
    public static DeckManager instance;

    [Header("Deck Settings")]
    public int maxDeckSize = 40; // Maximum number of cards per deck
    public List<CardSO> allAvailableCards = new List<CardSO>(); // Stores all cards
    public List<DeckData> savedDecks = new List<DeckData>(); // Stores player decks

    [Header("Game Deck & UI")]
    public List<CardSO> currentDeck = new List<CardSO>(); // Active deck in play
    public GameObject cardPrefab; // Prefab for UI representation
    public Transform cardSpawnArea; // Where the drawn cards appear

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        LoadAllCards();
        LoadDecks();
        if (savedDecks.Count > 0)
        {
            LoadDeck(savedDecks[0].deckName); // Load first saved deck as default
        }
        else
        {
            GenerateRandomDeck();
        }
        ShuffleDeck();
        DrawStartingHand();
    }

    // ✅ Load all cards from Resources/Cards/
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

    // ✅ Generate a random deck if no pre-built deck is selected
    public void GenerateRandomDeck()
    {
        currentDeck.Clear();
        List<CardSO> tempPool = new List<CardSO>(allAvailableCards);

        for (int i = 0; i < maxDeckSize; i++)
        {
            if (tempPool.Count == 0) break;
            int randomIndex = Random.Range(0, tempPool.Count);
            currentDeck.Add(tempPool[randomIndex]);
            tempPool.RemoveAt(randomIndex);
        }

        Debug.Log("✅ Random deck generated!");
    }

    // ✅ Shuffle the deck
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

    // ✅ Draw the starting hand
    public void DrawStartingHand()
    {
        for (int i = 0; i < 5; i++)
        {
            DrawCard();
        }
    }

    // ✅ Draw a card from the deck
    public void DrawCard()
    {
        if (currentDeck.Count > 0)
        {
            CardSO drawnCard = currentDeck[0];
            currentDeck.RemoveAt(0);
            SpawnCard(drawnCard);

            // Update player deck count UI.
            if (DeckZone.instance != null)
                DeckZone.instance.UpdateDeckCount(currentDeck.Count);
        }
        else
        {
            Debug.LogWarning("⚠️ Deck is empty! No more cards to draw.");
        }
    }


    // ✅ Spawn a card in the player's hand and assign card data to both CardHandler and CardUI.
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
            // Option B: Set the card on the CardHandler, which in turn updates CardUI.
            handler.SetCard(cardData, false);
        }
        else
        {
            Debug.LogError("❌ CardHandler component is missing on CardPrefab!");
        }
    }

    // ✅ Save player decks
    public void SaveDecks()
    {
        PlayerPrefs.SetString("SavedDecks", JsonUtility.ToJson(new DeckSaveWrapper(savedDecks)));
        PlayerPrefs.Save();
        Debug.Log("✅ Decks saved!");
    }

    // ✅ Load saved decks
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

    // ✅ Load a specific deck by name
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

    // ✅ Create a new deck
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

// ✅ Helper class for saving multiple decks
[System.Serializable]
public class DeckSaveWrapper
{
    public List<DeckData> decks;
    public DeckSaveWrapper(List<DeckData> decks)
    {
        this.decks = decks;
    }
}

// ✅ Structure to store deck information
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
