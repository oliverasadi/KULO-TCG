using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Conditional Search and Add")]
public class ConditionalSearchAndAddEffect : CardEffect
{
    [Tooltip("Exact card names to look for on the field (e.g., 'Fisherman')")]
    public List<string> requiredCardNames = new List<string>();

    [Tooltip("Creature types to look for on the field (optional, e.g., 'Su Beast')")]
    public List<string> requiredCreatureTypes = new List<string>();

    public enum SearchOwnerOption { Mine, AI, Both }

    [Tooltip("Whose field to search for the required cards")]
    public SearchOwnerOption searchOwner = SearchOwnerOption.Mine;

    [Tooltip("Cards to search for in deck and add to hand if condition is met")]
    public List<string> cardsToAddFromDeck = new List<string>();

    public override void ApplyEffect(CardUI sourceCard)
    {
        if (sourceCard == null) return;

        CardHandler sourceHandler = sourceCard.GetComponent<CardHandler>();
        bool sourceIsAI = sourceHandler != null && sourceHandler.isAI;

        bool checkPlayerField = false;
        bool checkAIField = false;
        switch (searchOwner)
        {
            case SearchOwnerOption.Mine:
                checkPlayerField = !sourceIsAI;
                checkAIField = sourceIsAI;
                break;
            case SearchOwnerOption.AI:
                checkPlayerField = sourceIsAI;
                checkAIField = !sourceIsAI;
                break;
            case SearchOwnerOption.Both:
                checkPlayerField = true;
                checkAIField = true;
                break;
        }

        CardSO[,] gridData = GridManager.instance.GetGrid();
        GameObject[,] gridObjects = GridManager.instance.GetGridObjects();

        bool conditionMet = false;

        for (int x = 0; x < gridData.GetLength(0) && !conditionMet; x++)
        {
            for (int y = 0; y < gridData.GetLength(1) && !conditionMet; y++)
            {
                CardSO cardData = gridData[x, y];
                if (cardData == null) continue;

                CardHandler handler = gridObjects[x, y]?.GetComponent<CardHandler>();
                if (handler == null) continue;

                bool isAI = handler.isAI;
                if ((!checkPlayerField && !isAI) || (!checkAIField && isAI)) continue;

                if ((requiredCardNames != null && requiredCardNames.Contains(cardData.cardName)) ||
                    (requiredCreatureTypes != null && requiredCreatureTypes.Contains(cardData.creatureType)))
                {
                    conditionMet = true;
                }
            }
        }

        if (!conditionMet)
        {
            Debug.Log("[WhatACatchEffect] No matching card found on field, search aborted.");
            return;
        }

        // Proceed to search deck
        PlayerManager pm = TurnManager.currentPlayerManager;
        if (pm == null || pm.currentDeck == null)
        {
            Debug.LogWarning("❌ PlayerManager or deck is null, cannot search.");
            return;
        }

        foreach (string cardName in cardsToAddFromDeck)
        {
            CardSO found = pm.currentDeck.Find(c => c.cardName == cardName);
            if (found != null)
            {
                pm.currentDeck.Remove(found);
                pm.SpawnCard(found);
                Debug.Log($"[WhatACatchEffect] ✅ {cardName} found and added to hand.");
            }
            else
            {
                Debug.LogWarning($"[WhatACatchEffect] ⚠️ {cardName} not found in deck.");
            }
        }
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        // No persistent effect to remove.
    }
}
