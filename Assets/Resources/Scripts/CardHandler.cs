using UnityEngine;
using UnityEngine.UI;

public class CardHandler : MonoBehaviour
{
    public Image cardArtImage;
    public CardSO cardData;

    public void SetCard(CardSO card)
    {
        if (card == null)
        {
            Debug.LogError("SetCard was given a null card!");
            return;
        }

        cardData = card; // Assigns card data
    
        // ✅ Set Card Art from Scriptable Object
        if (/*cardArtImage != null &&*/ cardData.cardImage != null)
        {
            cardArtImage.sprite = cardData.cardImage;
        }
        else
        {
            Debug.LogError($"Card Art Missing for {cardData.cardName}");
        }
    }

    public CardSO GetCardData()
    {
        return cardData;
    }
}
