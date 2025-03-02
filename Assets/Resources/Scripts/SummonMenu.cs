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
        // If a menu is already open and it's not this one, close it.
        if (currentMenu != null && currentMenu != this)
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

        if (cardUI == null)
        {
            Debug.LogError("SummonMenu.Initialize: CardUI reference is null.");
            return;
        }

        if (cardUI.cardData != null && cardUI.cardData.requiresSacrifice)
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
        if (cardUI == null || cardUI.cardData == null)
        {
            Debug.LogError("OnSummon: CardUI or cardData is null.");
            Destroy(gameObject);
            return;
        }
        Debug.Log("Summon button clicked for " + cardUI.cardData.cardName);
        // TODO: Call your placement logic in GridManager here.
        Destroy(gameObject); // Close the menu.
    }

    // Called when "Select Sacrifices" is clicked (for evolution/sacrifice cards).
    private void OnSelectSacrifices()
    {
        if (cardUI == null || cardUI.cardData == null)
        {
            Debug.LogError("OnSelectSacrifices: CardUI or cardData is null.");
            Destroy(gameObject);
            return;
        }
        Debug.Log("Select Sacrifices clicked for " + cardUI.cardData.cardName);
        SacrificeManager.instance.StartSacrificeSelection(cardUI);
        Destroy(gameObject); // Close the menu.
    }

    private void OnCancel()
    {
        if (cardUI != null && cardUI.cardData != null)
            Debug.Log("Summon menu canceled for " + cardUI.cardData.cardName);
        else
            Debug.Log("Summon menu canceled.");
        Destroy(gameObject); // Close the menu.
    }
}
