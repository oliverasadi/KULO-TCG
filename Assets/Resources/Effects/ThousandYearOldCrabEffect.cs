using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Card Effects/Thousand Year Old Crab Effect")]
public class ThousandYearOldCrabEffect : CardEffect
{
    // The number of cards to discard from your hand.
    public int discardCost = 2;

    public override void ApplyEffect(CardUI sourceCard)
    {
        // First, check if the player has any other cards on the field.
        if (PlayerHasAnyCardsOnField(sourceCard))
        {
            Debug.Log("1000 Year Old Crab cannot be played because you already have other cards on the field.");
            // Cancel the play; you may also trigger UI feedback here.
            return;
        }
        else
        {
            Debug.Log("Field is empty. You must discard " + discardCost + " cards from your hand to play 1000 Year Old Crab.");
            // Begin the discard process using your HandDiscardManager.
            if (HandDiscardManager.Instance != null)
            {
                // Call the version that takes two parameters.
                HandDiscardManager.Instance.BeginDiscardMode(discardCost, sourceCard);
            }
            else
            {
                Debug.LogError("HandDiscardManager instance not found!");
            }
        }
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        // No cleanup is needed when the card leaves the field.
    }

    // Checks whether the player already has any other cards on the field (besides the source card).
    private bool PlayerHasAnyCardsOnField(CardUI sourceCard)
    {
        CardSO[,] grid = GridManager.instance.GetGrid();
        GameObject[,] gridObjs = GridManager.instance.GetGridObjects();

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                if (grid[x, y] != null)
                {
                    GameObject gridObj = gridObjs[x, y];
                    if (gridObj != null)
                    {
                        CardHandler handler = gridObj.GetComponent<CardHandler>();
                        if (handler != null && handler.cardOwner == sourceCard.GetComponent<CardHandler>().cardOwner)
                        {
                            // Exclude the source card itself.
                            if (gridObj != sourceCard.gameObject)
                                return true;
                        }
                    }
                }
            }
        }
        return false;
    }
}
