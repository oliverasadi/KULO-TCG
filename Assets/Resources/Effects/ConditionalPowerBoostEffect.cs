using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Conditional Power Boost")]
public class ConditionalPowerBoostEffect : CardEffect
{
    public int boostAmount = 200;
    // Names of the cards required on the field to trigger the boost.
    public string[] requiredCardNames;

    public override void ApplyEffect(CardUI sourceCard)
    {
        bool conditionMet = false;
        CardSO[,] field = GridManager.instance.GetGrid();
        Debug.Log($"{sourceCard.cardData.cardName} effect: Starting ConditionalPowerBoost search. Required names: {string.Join(", ", requiredCardNames)}");

        // Log the entire grid state for debugging.
        for (int x = 0; x < field.GetLength(0); x++)
        {
            string rowInfo = $"Row {x}: ";
            for (int y = 0; y < field.GetLength(1); y++)
            {
                if (field[x, y] != null)
                    rowInfo += field[x, y].cardName + " | ";
                else
                    rowInfo += "Empty | ";
            }
            Debug.Log(rowInfo);
        }

        // Check each cell for a matching card.
        for (int x = 0; x < field.GetLength(0); x++)
        {
            for (int y = 0; y < field.GetLength(1); y++)
            {
                if (field[x, y] != null)
                {
                    Debug.Log($"Checking cell ({x},{y}) with card: {field[x, y].cardName}");
                    foreach (string req in requiredCardNames)
                    {
                        if (field[x, y].cardName == req)
                        {
                            Debug.Log($"Match found in cell ({x},{y}): {field[x, y].cardName} equals required name: {req}");
                            conditionMet = true;
                            break;
                        }
                    }
                }
                else
                {
                    Debug.Log($"Cell ({x},{y}) is empty.");
                }
                if (conditionMet)
                {
                    Debug.Log($"Condition met at cell ({x},{y}). Stopping search.");
                    break;
                }
            }
            if (conditionMet)
            {
                break;
            }
        }
        if (conditionMet)
        {
            Debug.Log($"{sourceCard.cardData.cardName} effect: Condition met, boosting power by {boostAmount}.");
            sourceCard.cardData.power += boostAmount;
            Debug.Log($"{sourceCard.cardData.cardName} new power: {sourceCard.cardData.power}");
        }
        else
        {
            Debug.Log($"{sourceCard.cardData.cardName} effect: Condition not met; no boost applied.");
        }
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        Debug.Log($"{sourceCard.cardData.cardName} effect: Removing conditional power boost of {boostAmount}.");
        sourceCard.cardData.power -= boostAmount;
        Debug.Log($"{sourceCard.cardData.cardName} reverted power: {sourceCard.cardData.power}");
    }
}
