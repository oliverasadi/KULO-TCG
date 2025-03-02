using UnityEngine;
using UnityEngine.UI;
using TMPro; // If using TextMeshProUGUI

public class SummonMenu : MonoBehaviour
{
    public static SummonMenu currentMenu; // Tracks the currently open menu

    public Button summonButton;
    public Button selectSacrificesButton;
    public Button cancelButton;
    public TextMeshProUGUI sacrificeInfoText; // Displays required sacrifices info

    public CardUI cardUI; // The card this menu is for

    void Awake()
    {
        // If a menu is already open, destroy it.
        if (currentMenu != null)
        {
            Destroy(currentMenu.gameObject);
        }
        currentMenu = this;
    }

    void OnDestroy()
    {
        if (currentMenu == this)
            currentMenu = null;
    }

    // Call this from CardUI's ShowSummonMenu() to initialize the menu.
    public void Initialize(CardUI card)
    {
        cardUI = card;

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
        // TODO: Call your GridManager placement logic here.
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

    // Optional: You can add a method here to close the menu if a background click is detected.
    public void OnBackgroundClick()
    {
        Debug.Log("Background clicked. Closing Summon Menu.");
        Destroy(gameObject);
    }
}
