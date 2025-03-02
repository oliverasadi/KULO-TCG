using UnityEngine;
using UnityEngine.UI;

public class CardHandler : MonoBehaviour
{
    public Image cardArtImage;
    public CardSO cardData;

    // Indicates if this card belongs to the AI.
    public bool isAI = false;

    // Reference to an Outline component used to highlight this card when it's eligible for sacrifice.
    // Add an Outline component to your card prefab and disable it by default.
    public Outline sacrificeOutline;

    // Sets the card data and updates visuals.
    public void SetCard(CardSO card, bool setFaceDown = false, bool isAICard = false)
    {
        isAI = isAICard;

        if (card == null)
        {
            Debug.LogError("SetCard was given a null card!");
            return;
        }

        cardData = card; // Assign card data

        // Update the card's image.
        if (cardArtImage != null && cardData.cardImage != null)
        {
            cardArtImage.sprite = cardData.cardImage;
        }
        else
        {
            Debug.LogError($"Card Art Missing for {cardData.cardName}");
        }

        // Also update the CardUI component on the same GameObject.
        CardUI cardUI = GetComponent<CardUI>();
        if (cardUI != null)
        {
            cardUI.SetCardData(card, setFaceDown);
        }
        else
        {
            Debug.LogWarning("No CardUI component found on this card prefab.");
        }
    }

    // Returns the current card data.
    public CardSO GetCardData()
    {
        return cardData;
    }

    /// <summary>
    /// Enables the sacrifice outline to visually highlight this card as eligible for sacrifice.
    /// </summary>
    public void ShowSacrificeHighlight()
    {
        if (sacrificeOutline != null)
        {
            sacrificeOutline.enabled = true;
        }
        else
        {
            Debug.LogWarning("SacrificeOutline not assigned on " + gameObject.name);
        }
    }

    /// <summary>
    /// Disables the sacrifice outline, reverting the card's visual state.
    /// </summary>
    public void HideSacrificeHighlight()
    {
        if (sacrificeOutline != null)
        {
            sacrificeOutline.enabled = false;
        }
    }

    /// <summary>
    /// Optionally displays a popup (or logs a message) indicating that this card has been selected as a sacrifice.
    /// </summary>
    public void ShowSacrificePopup()
    {
        Debug.Log("Sacrifice popup: " + cardData.cardName);
        // Expand this method to display an actual UI popup if needed.
    }
}
