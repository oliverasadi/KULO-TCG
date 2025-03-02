using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDeck", menuName = "Deck/Deck Data")]
public class DeckDataSO : ScriptableObject
{
    public string deckName;
    public List<CardEntry> cardEntries;  // List of cards with quantities
}

[System.Serializable]
public class CardEntry
{
    public CardSO card;   // Reference to the card asset.
    public int quantity;  // How many copies of this card.
}
