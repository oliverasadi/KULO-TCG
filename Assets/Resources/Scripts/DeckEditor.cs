using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeckEditor : MonoBehaviour
{
    public static DeckEditor instance;

    [Header("UI Elements")]
    public Transform availableCardsPanel; // Panel for all available cards
    public Transform deckCardsPanel; // Panel for selected deck
    public GameObject cardButtonPrefab; // Prefab for card buttons in UI
    public TMP_InputField deckNameInput; // Input field for deck name
    public Button saveDeckButton; // Button to save the deck

    [Header("Card Management")]
    public List<CardSO> allCards = new List<CardSO>(); // List of all available cards
    public List<CardSO> currentDeck = new List<CardSO>(); // Cards in the selected deck

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        LoadAllCards();
        PopulateAvailableCardsUI();
    }

    // ✅ Load all CardSO files from Resources
    public void LoadAllCards()
    {
        allCards.Clear();
        CardSO[] loadedCards = Resources.LoadAll<CardSO>("Cards"); // Make sure "Cards" is the correct folder
        allCards.AddRange(loadedCards);

        if (allCards.Count == 0)
        {
            Debug.LogError("❌ No CardSO assets found in Resources/Cards!");
        }
    }

    // ✅ Populate the UI with all available cards
    private void PopulateAvailableCardsUI()
    {
        foreach (CardSO card in allCards)
        {
            GameObject cardButton = Instantiate(cardButtonPrefab, availableCardsPanel);
            CardUI cardUI = cardButton.GetComponent<CardUI>();
            cardUI.SetCardData(card, this);
        }
    }

    // ✅ Add a card to the deck
    public void AddCardToDeck(CardSO card)
    {
        if (currentDeck.Count >= 40) // Max deck size limit
        {
            Debug.LogWarning("⚠️ Deck is full!");
            return;
        }

        currentDeck.Add(card);
        GameObject cardButton = Instantiate(cardButtonPrefab, deckCardsPanel);
        CardUI cardUI = cardButton.GetComponent<CardUI>();
        cardUI.SetCardData(card, this);
        cardUI.isInDeck = true; // Mark as deck card
    }

    // ✅ Remove a card from the deck
    public void RemoveCardFromDeck(CardSO card, GameObject cardButton)
    {
        currentDeck.Remove(card);
        Destroy(cardButton);
    }

    // ✅ Save the deck
    public void SaveDeck()
    {
        if (string.IsNullOrEmpty(deckNameInput.text))
        {
            Debug.LogWarning("⚠️ Enter a deck name before saving!");
            return;
        }

        DeckData newDeck = new DeckData(deckNameInput.text);
        foreach (CardSO card in currentDeck)
        {
            newDeck.cardNames.Add(card.cardName);
        }

        DeckManager.instance.savedDecks.Add(newDeck);
        DeckManager.instance.SaveDecks();
        Debug.Log($"✅ Deck '{deckNameInput.text}' saved!");
    }
}
