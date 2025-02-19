using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CardUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI cardNameText; // Displays the card name
    public Button cardButton; // The UI button for selecting the card

    private CardSO cardData; // Stores the card's ScriptableObject data
    private DeckEditor deckEditor;
    public bool isInDeck = false; // Checks if the card is in the deck

    public void SetCardData(CardSO card, DeckEditor editor)
    {
        if (card == null || editor == null)
        {
            Debug.LogError("❌ Card data or DeckEditor reference is missing!");
            return;
        }

        cardData = card;
        deckEditor = editor;

        if (cardNameText != null)
            cardNameText.text = card.cardName;
        else
            Debug.LogError($"❌ cardNameText is not assigned in {gameObject.name}!");

        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners(); // Clear previous listeners
            cardButton.onClick.AddListener(OnCardClicked);
        }
        else
        {
            Debug.LogError($"❌ Button component missing on {gameObject.name}!");
        }
    }

    private void OnCardClicked()
    {
        if (cardData == null || deckEditor == null)
        {
            Debug.LogError("❌ Missing card data or deckEditor!");
            return;
        }

        if (isInDeck)
        {
            deckEditor.RemoveCardFromDeck(cardData, gameObject);
            isInDeck = false;
        }
        else
        {
            deckEditor.AddCardToDeck(cardData);
            isInDeck = true;
        }
    }
}
