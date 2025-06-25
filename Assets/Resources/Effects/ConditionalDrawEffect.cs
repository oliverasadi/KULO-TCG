using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Conditional Draw")]
public class ConditionalDrawEffect : CardEffect
{
    // Configurable fields for the effect
    [Tooltip("How many cards to draw if the condition is met")]
    public int cardsToDraw = 1;

    [Tooltip("Exact card names to look for on the field")]
    public List<string> requiredCreatureNames = new List<string>();

    [Tooltip("Creature types to look for on the field")]
    public List<string> requiredCreatureTypes = new List<string>();

    public enum SearchOwnerOption { Mine, AI, Both }

    [Tooltip("Whose field to search for the required creatures (Mine = this card's owner, AI = the opponent's field, Both = both fields)")]
    public SearchOwnerOption searchOwner = SearchOwnerOption.Mine;

    /// <summary>
    /// Apply the effect when the card is played. Checks the field for matching creatures and draws cards if the condition is met.
    /// </summary>
    /// <param name="sourceCard">The CardUI of the card that activated this effect.</param>
    public override void ApplyEffect(CardUI sourceCard)
    {
        if (sourceCard == null) return;

        // Determine the sides of the field to search based on card owner and searchOwner setting
        CardHandler sourceHandler = sourceCard.GetComponent<CardHandler>();
        bool sourceIsAI = (sourceHandler != null && sourceHandler.isAI);  // true if the source card belongs to AI:contentReference[oaicite:7]{index=7}

        bool checkPlayerField = false;
        bool checkAIField = false;
        switch (searchOwner)
        {
            case SearchOwnerOption.Mine:
                // Search the field of the card's owner
                if (sourceIsAI)
                {
                    checkAIField = true;
                }
                else
                {
                    checkPlayerField = true;
                }
                break;
            case SearchOwnerOption.AI:
                // Search the opponent's field
                if (sourceIsAI)
                {
                    checkPlayerField = true;  // source is AI, so opponent is the player
                }
                else
                {
                    checkAIField = true;      // source is player, so opponent is AI
                }
                break;
            case SearchOwnerOption.Both:
                // Check both sides of the field
                checkPlayerField = true;
                checkAIField = true;
                break;
        }

        // Get the current grid state from the GridManager
        CardSO[,] gridData = GridManager.instance.GetGrid();
        GameObject[,] gridObjects = GridManager.instance.GetGridObjects();

        bool conditionMet = false;
        int rows = gridData.GetLength(0);
        int cols = gridData.GetLength(1);

        // Iterate over all grid cells to find any matching creature
        for (int x = 0; x < rows && !conditionMet; x++)
        {
            for (int y = 0; y < cols && !conditionMet; y++)
            {
                CardSO cardData = gridData[x, y];
                if (cardData == null) continue;  // empty cell

                // Determine the owner of this card on the grid via its CardHandler
                CardHandler occupantHandler = gridObjects[x, y]?.GetComponent<CardHandler>();
                if (occupantHandler == null) continue;

                bool occupantIsAI = occupantHandler.isAI;
                // Filter out cards that are not on the side(s) we want to check
                if ((!checkPlayerField && !occupantIsAI) || (!checkAIField && occupantIsAI))
                {
                    continue;
                }

                // Check if this card matches any of the required names or types
                string creatureName = cardData.cardName;
                string creatureType = cardData.creatureType;
                if ((requiredCreatureNames != null && requiredCreatureNames.Contains(creatureName)) ||
                    (requiredCreatureTypes != null && requiredCreatureTypes.Contains(creatureType)))
                {
                    conditionMet = true;
                    break;  // exit inner loop early as condition is satisfied
                }
            }
        }

        // If any matching creature was found on the appropriate side(s), draw the specified number of cards
        if (conditionMet && TurnManager.currentPlayerManager != null)
        {
            for (int i = 0; i < cardsToDraw; i++)
            {
                TurnManager.currentPlayerManager.DrawCard();  // draw one card for the current player:contentReference[oaicite:8]{index=8}
            }
        }
    }

    /// <summary>
    /// RemoveEffect is not used for this one-shot effect (no persistent state to clean up).
    /// </summary>
    public override void RemoveEffect(CardUI sourceCard)
    {
        // No continuous effect to remove once the card leaves the field.
    }
}
