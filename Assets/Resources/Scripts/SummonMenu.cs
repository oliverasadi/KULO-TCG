using UnityEngine;
using UnityEngine.UI;
using TMPro; // If using TextMeshProUGUI

public class SummonMenu : MonoBehaviour
{
    public static SummonMenu currentMenu; // Tracks the currently open menu

    public Button summonButton;
    public Button selectSacrificesButton;
    public Button sacrificeButton; // New Sacrifice Button
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

        if (cardUI == null || cardUI.cardData == null)
        {
            Debug.LogError("SummonMenu.Initialize: CardUI or cardData is null.");
            return;
        }

        // Ensure all UI references are assigned.
        if (summonButton == null)
            Debug.LogError("SummonMenu.Initialize: summonButton is not assigned.");
        if (selectSacrificesButton == null)
            Debug.LogError("SummonMenu.Initialize: selectSacrificesButton is not assigned.");
        if (cancelButton == null)
            Debug.LogError("SummonMenu.Initialize: cancelButton is not assigned.");
        if (sacrificeButton == null)
            Debug.LogError("SummonMenu.Initialize: sacrificeButton is not assigned.");
        if (sacrificeInfoText == null)
            Debug.LogError("SummonMenu.Initialize: sacrificeInfoText is not assigned.");

        // Check if the card is already on the field.
        // (Assuming cardUI.isOnField is set to true when the card is played.)
        bool isOnField = cardUI.isOnField;

        if (isOnField)
        {
            // If the card is already on the field, it cannot be summoned again.
            // Show the sacrifice option if this card qualifies as a valid sacrifice.
            summonButton.gameObject.SetActive(false);
            selectSacrificesButton.gameObject.SetActive(false);
            if (SacrificeManager.instance != null && SacrificeManager.instance.IsValidSacrifice(cardUI))
            {
                sacrificeButton.gameObject.SetActive(true);
                if (sacrificeInfoText != null)
                    sacrificeInfoText.text = "This card can be sacrificed. Click 'Sacrifice' to proceed.";
            }
            else
            {
                sacrificeButton.gameObject.SetActive(false);
                if (sacrificeInfoText != null)
                    sacrificeInfoText.text = "This card is already in play and cannot be used.";
            }
        }
        else
        {
            // Card is not on the field; show normal options.
            bool requiresSacrifice = cardUI.cardData.requiresSacrifice;
            if (requiresSacrifice)
            {
                selectSacrificesButton.gameObject.SetActive(true);
                summonButton.gameObject.SetActive(false);
                sacrificeButton.gameObject.SetActive(false);
                if (sacrificeInfoText != null)
                {
                    // Use the card's effect description if available.
                    if (!string.IsNullOrEmpty(cardUI.cardData.effectDescription))
                        sacrificeInfoText.text = cardUI.cardData.effectDescription;
                    else
                        sacrificeInfoText.text = "This card requires sacrifices. Click 'Select Sacrifices' to proceed.";
                }
            }
            else
            {
                // Normal summonable card.
                selectSacrificesButton.gameObject.SetActive(false);
                summonButton.gameObject.SetActive(true);
                sacrificeButton.gameObject.SetActive(false);
                if (sacrificeInfoText != null)
                    sacrificeInfoText.text = "Click 'Summon' to play this card.";
            }
        }

        // Add listeners to the buttons.
        summonButton.onClick.AddListener(OnSummon);
        selectSacrificesButton.onClick.AddListener(OnSelectSacrifices);
        cancelButton.onClick.AddListener(OnCancel);
        sacrificeButton.onClick.AddListener(OnSacrifice);
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

    // Called when "Sacrifice" is clicked (for valid sacrifice cards).
    private void OnSacrifice()
    {
        if (cardUI == null || cardUI.cardData == null)
        {
            Debug.LogError("OnSacrifice: CardUI or cardData is null.");
            Destroy(gameObject);
            return;
        }
        Debug.Log("Sacrifice button clicked for " + cardUI.cardData.cardName);
        SacrificeManager.instance.SelectSacrifice(cardUI.gameObject);
        Destroy(gameObject); // Close the menu.
    }

    // Called when "Cancel" is clicked.
    private void OnCancel()
    {
        if (cardUI != null && cardUI.cardData != null)
            Debug.Log("Summon menu canceled for " + cardUI.cardData.cardName);
        else
            Debug.Log("Summon menu canceled.");
        Destroy(gameObject); // Close the menu.
    }
}
