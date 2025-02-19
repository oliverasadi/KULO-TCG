using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DeckSelection : MonoBehaviour
{
public static DeckSelection instance;
[Header("Deck Selection UI")]
public Transform cardSelectionPanel; // Panel that shows available cards
public Transform selectedDeckPanel; // Panel that shows selected cards
public GameObject cardButtonPrefab; // Prefab for selectable card buttons
public Button startGameButton; // Button to confirm the deck and start the game

[Header("Deck Data")]
public List<CardSO> availableCards = new List<CardSO>(); // Pool of all available cards
public List<CardSO> selectedDeck = new List<CardSO>(); // Player’s chosen deck
public int maxDeckSize = 40; // Enforce 40-card deck rule

private void Awake()
{
if (instance == null) instance = this;
else Destroy(gameObject);
}

void Start()
{
PopulateAvailableCards();
startGameButton.onClick.AddListener(StartGame);
}

void PopulateAvailableCards()
{
foreach (CardSO card in availableCards)
{
GameObject cardButton = Instantiate(cardButtonPrefab, cardSelectionPanel);
cardButton.GetComponentInChildren<Text>().text = card.cardName;
cardButton.GetComponent<Button>().onClick.AddListener(() => AddToDeck(card));
}
}

void AddToDeck(CardSO card)
{
if (selectedDeck.Count < maxDeckSize)
{
selectedDeck.Add(card);

GameObject cardEntry = Instantiate(cardButtonPrefab, selectedDeckPanel);
cardEntry.GetComponentInChildren<Text>().text = card.cardName;
cardEntry.GetComponent<Button>().onClick.AddListener(() => RemoveFromDeck(card, cardEntry));
}
}

void RemoveFromDeck(CardSO card, GameObject cardEntry)
{
selectedDeck.Remove(card);
Destroy(cardEntry);
}

void StartGame()
{
if (selectedDeck.Count == maxDeckSize)
{
DeckManager.instance.deck = new List<CardSO>(selectedDeck);
DeckManager.instance.ShuffleDeck();
DeckManager.instance.DrawStartingHand();
gameObject.SetActive(false); // Hide deck selection screen
}
else
{
Debug.Log("Deck must have exactly " + maxDeckSize + " cards!");
}
}
}