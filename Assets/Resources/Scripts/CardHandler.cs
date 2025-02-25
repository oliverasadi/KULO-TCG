using UnityEngine;
using UnityEngine.UI;

public class CardHandler : MonoBehaviour
{
    public Image cardArtImage;
    public CardSO cardData;

    // Optionally include a parameter for whether the card should start face-down
    public void SetCard(CardSO card, bool setFaceDown = false)
    {
        if (card == null)
        {
            Debug.LogError("SetCard was given a null card!");
            return;
        }

        cardData = card; // Assign the card data to CardHandler

        // Update this script's own Image
        if (cardArtImage != null && cardData.cardImage != null)
        {
            cardArtImage.sprite = cardData.cardImage;
        }
        else
        {
            Debug.LogError($"Card Art Missing for {cardData.cardName}");
        }

        // Also update the CardUI script on the same GameObject
        CardUI cardUI = GetComponent<CardUI>();
        if (cardUI != null)
        {
            // Pass along the same CardSO, and the face-down setting if desired
            cardUI.SetCardData(card, setFaceDown);
        }
        else
        {
            Debug.LogWarning("No CardUI component found on this card prefab.");
        }
    }

    public CardSO GetCardData()
    {
        return cardData;
    }
}
