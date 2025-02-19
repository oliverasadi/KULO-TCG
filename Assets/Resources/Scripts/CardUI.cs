using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    public TextMeshProUGUI cardNameText; // Displays the card name
    public Button cardButton; // The UI button for selecting the card

    private CardSO cardData; // Stores the card's ScriptableObject data
    private DeckEditor deckEditor;
    public bool isInDeck = false; // Checks if the card is in the deck

    public void SetCardData(CardSO card, DeckEditor editor)
    {
        cardData = card;
        deckEditor = editor;
        cardNameText.text = card.cardName;
        cardButton.onClick.AddListener(OnCardClicked);
    }

    private void OnCardClicked()
    {
        if (isInDeck)
        {
            deckEditor.RemoveCardFromDeck(cardData, gameObject);
        }
        else
        {
            deckEditor.AddCardToDeck(cardData);
        }
    }
}
