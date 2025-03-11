using UnityEngine;
using System.Collections.Generic;
using System;

public class HandDiscardManager : MonoBehaviour
{
    public static HandDiscardManager Instance;

    // The number of cards the player must select.
    private int requiredDiscardCount;
    // List to store the selected cards.
    private List<CardUI> selectedCards = new List<CardUI>();
    // Callback to call when the discard is complete.
    private Action onDiscardComplete;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    /// <summary>
    /// Begins the discard mode for paying a cost (for example, for 1000 Year Old Crab).
    /// </summary>
    /// <param name="discardCount">Number of cards to discard.</param>
    /// <param name="effectCard">The card triggering the discard cost.</param>
    /// <param name="onComplete">Optional callback invoked when discard is complete.</param>
    public void BeginDiscardMode(int discardCount, CardUI effectCard, Action onComplete = null)
    {
        requiredDiscardCount = discardCount;
        selectedCards.Clear();
        onDiscardComplete = onComplete;

        // Optionally, disable other UI interactions in the hand and highlight selectable cards.
        // You might call a method on each CardUI in the local player's hand to enable discard selection mode.
        PlayerManager localPlayer = TurnManager.instance.localPlayerManager;
        if (localPlayer == null)
        {
            Debug.LogError("Local PlayerManager not found!");
            return;
        }

        Debug.Log("Hand discard mode activated for " + effectCard.cardData.cardName + ". Discard " + requiredDiscardCount + " cards.");

        // Here, you can loop through localPlayer.cardHandlers and enable a discard selection visual.
        // For example:
        // foreach (CardHandler ch in localPlayer.cardHandlers)
        // {
        //     ch.EnableDiscardSelection(); // (This is a method you would implement on your CardHandler/CardUI)
        // }
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
            // Optionally update the card's visual (e.g., dim it or add a checkmark).
            Debug.Log("Selected card for discard: " + card.cardData.cardName);
        }

        if (selectedCards.Count >= requiredDiscardCount)
        {
            ProcessDiscard();
        }
    }

    /// <summary>
    /// Processes the discard selection, removes the selected cards from the player's hand, and adds them to the grave.
    /// </summary>
    private void ProcessDiscard()
    {
        // Get the local player's manager from TurnManager.
        PlayerManager localPlayer = TurnManager.instance.localPlayerManager;
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
        Debug.Log("Discard mode complete.");
    }
}
