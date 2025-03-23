using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

public class HandDiscardManager : MonoBehaviour
{
    public static HandDiscardManager Instance;

    // Flag to indicate if discard mode is active.
    public bool isDiscarding = false;

    // The number of cards the player must select.
    private int requiredDiscardCount;
    // List to store the selected cards.
    private List<CardUI> selectedCards = new List<CardUI>();
    // Callback to call when the discard is complete.
    private Action onDiscardComplete;

    // Reference to your discard panel prefab (set this in the Inspector).
    public GameObject handDiscardPanelPrefab;
    // Reference to the DiscardCardButton prefab (set this in the Inspector).
    public GameObject discardCardButtonPrefab;

    // Reference to the instantiated discard panel.
    private GameObject discardPanelInstance;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Begins the discard mode. This instantiates the discard panel UI and populates it with thumbnail buttons
    /// for each card in the player's hand.
    /// </summary>
    /// <param name="discardCount">Number of cards to discard.</param>
    /// <param name="effectCard">The card triggering the discard cost (optional).</param>
    /// <param name="onComplete">Optional callback invoked when discard is complete.</param>
    public void BeginDiscardMode(int discardCount, CardUI effectCard, Action onComplete = null)
    {
        isDiscarding = true;  // Set flag when discard mode begins
        requiredDiscardCount = discardCount;
        selectedCards.Clear();
        onDiscardComplete = onComplete;

        // Find the Canvas (ensure your Canvas is tagged "OverlayCanvas" accordingly).
        GameObject canvas = GameObject.FindWithTag("OverlayCanvas");
        if (canvas == null)
        {
            Debug.LogError("Canvas with tag 'OverlayCanvas' not found!");
            return;
        }

        // Instantiate the discard panel as a child of the Canvas.
        discardPanelInstance = Instantiate(handDiscardPanelPrefab, canvas.transform);

        // Update the instruction text on the panel.
        TextMeshProUGUI instructionText = discardPanelInstance.transform.Find("InstructionText").GetComponent<TextMeshProUGUI>();
        if (instructionText != null)
        {
            instructionText.text = $"Select {requiredDiscardCount} card(s) to discard.";
        }
        else
        {
            Debug.LogWarning("InstructionText not found in HandDiscardPanel.");
        }

        // Find the container where card buttons should be added.
        Transform cardContainer = discardPanelInstance.transform.Find("CardContainer");
        if (cardContainer == null)
        {
            Debug.LogError("CardContainer not found in HandDiscardPanel!");
            return;
        }

        // Retrieve the local player's hand from TurnManager.
        PlayerManager localPlayer = (TurnManager.instance.localPlayerNumber == 1)
            ? TurnManager.instance.playerManager1
            : TurnManager.instance.playerManager2;
        if (localPlayer == null)
        {
            Debug.LogError("Local PlayerManager not found!");
            return;
        }

        // Loop through each card in the player's hand.
        foreach (CardHandler ch in localPlayer.cardHandlers)
        {
            CardUI cardUI = ch.GetComponent<CardUI>();
            if (cardUI == null) continue;
            // Only add cards that are still in hand (not on the board).
            if (cardUI.isOnField)
                continue;

            // Instantiate the DiscardCardButton prefab as a child of the CardContainer.
            GameObject buttonObj = Instantiate(discardCardButtonPrefab, cardContainer);
            // Set the button image to the card's art.
            Image buttonImage = buttonObj.GetComponent<Image>();
            if (buttonImage != null && cardUI.cardData != null)
            {
                buttonImage.sprite = cardUI.cardData.cardImage;
            }
            // Add a listener so that clicking the button calls AddCardToDiscard.
            Button btn = buttonObj.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() =>
                {
                    AddCardToDiscard(cardUI);
                    // Change the visual state to indicate selection (e.g., dim the image).
                    if (buttonImage != null)
                    {
                        Color newColor = buttonImage.color;
                        newColor.a = 0.5f; // Dim the image by reducing alpha.
                        buttonImage.color = newColor;
                    }
                });
            }
        }

        // Find and assign the Confirm button in the panel.
        Button confirmButton = discardPanelInstance.transform.Find("ConfirmButton").GetComponent<Button>();
        if (confirmButton != null)
        {
            confirmButton.onClick.AddListener(() =>
            {
                ProcessDiscard();
                Destroy(discardPanelInstance);
            });
        }
        else
        {
            Debug.LogWarning("ConfirmButton not found in HandDiscardPanel.");
        }
    }


    /// <summary>
    /// Call this method when a card in hand is selected for discard.
    /// </summary>
    /// <param name="card">The CardUI of the selected card.</param>
    public void AddCardToDiscard(CardUI card)
    {
        if (!selectedCards.Contains(card))
        {
            selectedCards.Add(card);
            Debug.Log("Selected card for discard: " + card.cardData.cardName);
            // Optionally, change the card thumbnail visual (e.g., dim it or show a checkmark).
        }

        if (selectedCards.Count >= requiredDiscardCount)
        {
            Debug.Log("Required number of cards selected for discard.");
            // Optionally, you can enable the Confirm button here if it was initially disabled.
        }
    }

    /// <summary>
    /// Processes the discard selection, removes the selected cards from the player's hand, and adds them to the grave.
    /// </summary>
    private void ProcessDiscard()
    {
        // Get the local player's manager from TurnManager.
        PlayerManager localPlayer = (TurnManager.instance.localPlayerNumber == 1)
            ? TurnManager.instance.playerManager1
            : TurnManager.instance.playerManager2;
        if (localPlayer == null)
        {
            Debug.LogError("Local PlayerManager not found during discard processing!");
            return;
        }

        // Remove each selected card from the player's hand.
        foreach (CardUI card in selectedCards)
        {
            localPlayer.DiscardCard(card);
        }

        // Optionally notify that discard is complete.
        onDiscardComplete?.Invoke();

        // Reset discard mode.
        requiredDiscardCount = 0;
        selectedCards.Clear();
        isDiscarding = false;  // Clear flag when discard processing is complete.
        Debug.Log("Discard mode complete.");
    }
}
