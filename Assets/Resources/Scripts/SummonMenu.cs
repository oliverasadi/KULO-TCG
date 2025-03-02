using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SummonMenu : MonoBehaviour
{
    public Button summonButton;
    public Button selectSacrificesButton;
    public Button cancelButton;
    public TextMeshProUGUI sacrificeInfoText; // Displays required sacrifices info

    private CardUI cardUI; // The card this menu is for

    // Call this from CardUI's ShowSummonMenu() to initialize the menu.
    public void Initialize(CardUI card)
    {
        cardUI = card;

        // If the card requires sacrifice (e.g., it's an evolution card), show the sacrifice option.
        if (cardUI.cardData.requiresSacrifice)
        {
            selectSacrificesButton.gameObject.SetActive(true);
            summonButton.gameObject.SetActive(false);
            if (sacrificeInfoText != null)
                sacrificeInfoText.text = "This card requires sacrifices. Click 'Select Sacrifices' to proceed.";
        }
        else
        {
            selectSacrificesButton.gameObject.SetActive(false);
            summonButton.gameObject.SetActive(true);
            if (sacrificeInfoText != null)
                sacrificeInfoText.text = "Click 'Summon' to play this card.";
        }

        // Add listeners to the buttons.
        summonButton.onClick.AddListener(OnSummon);
        selectSacrificesButton.onClick.AddListener(OnSelectSacrifices);
        cancelButton.onClick.AddListener(OnCancel);
    }

    // Called when "Summon" is clicked (for cards that don't require sacrifice).
    private void OnSummon()
    {
        Debug.Log("Summon button clicked for " + cardUI.cardData.cardName);
        // TODO: Call your GridManager placement logic.
        Destroy(gameObject); // Close the menu.
    }

    // Called when "Select Sacrifices" is clicked (for evolution/sacrifice cards).
    private void OnSelectSacrifices()
    {
        Debug.Log("Select Sacrifices clicked for " + cardUI.cardData.cardName);
        // TODO: Notify SacrificeManager to start sacrifice selection.
        // SacrificeManager.instance.StartSacrificeSelection(cardUI);
        Destroy(gameObject); // Close the menu.
    }

    private void OnCancel()
    {
        Debug.Log("Summon menu canceled for " + cardUI.cardData.cardName);
        Destroy(gameObject); // Close the menu.
    }

    // New method to be called when the background is clicked.
    public void OnBackgroundClick()
    {
        Debug.Log("Background clicked. Closing Summon Menu.");
        Destroy(gameObject);
    }
}
