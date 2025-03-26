using UnityEngine;

[CreateAssetMenu(fileName = "MutualConditionalPowerBoostEffect", menuName = "Card Effects/Mutual Conditional Power Boost")]
public class MutualConditionalPowerBoostEffect : CardEffect
{
    public int boostAmount = 500; // Amount to boost power
    public string[] requiredCardNames; // Names of required cards on the field

    private bool boostApplied = false; // Tracks if the boost has been applied
    private CardUI sourceCardRef;

    public override void ApplyEffect(CardUI sourceCard)
    {
        if (boostApplied)
        {
            Debug.Log($"{sourceCard.cardData.cardName} effect: Boost already applied, skipping.");
            return; // If already applied, do not apply again
        }

        sourceCardRef = sourceCard;

        // Check if the condition is met based on the current field state
        bool conditionNow = IsConditionMet();

        // If the condition is met and the boost hasn't been applied, apply the boost
        if (conditionNow && !boostApplied)
        {
            sourceCardRef.currentPower += boostAmount;
            boostApplied = true;
            Debug.Log($"{sourceCardRef.cardData.cardName} effect: Condition met => +{boostAmount} power. New power: {sourceCardRef.currentPower}");
        }

        // Subscribe to the field change event to recheck conditions dynamically only if the effect hasn't been applied yet
        if (!boostApplied)
        {
            TurnManager.instance.OnCardPlayed += OnFieldChanged; // Listen for any card played to trigger condition recheck
        }
    }

    private void OnFieldChanged(CardSO updatedCard)
    {
        if (sourceCardRef == null) return;

        bool conditionNow = IsConditionMet();

        // Apply boost if the condition becomes true
        if (conditionNow && !boostApplied)
        {
            sourceCardRef.currentPower += boostAmount;
            boostApplied = true;
            Debug.Log($"{sourceCardRef.cardData.cardName} effect: Condition now met => +{boostAmount} power. New power: {sourceCardRef.currentPower}");
        }
        // Remove the boost if the condition is no longer met
        else if (!conditionNow && boostApplied)
        {
            sourceCardRef.currentPower -= boostAmount;
            boostApplied = false;
            Debug.Log($"{sourceCardRef.cardData.cardName} effect: Condition no longer met => -{boostAmount} power. New power: {sourceCardRef.currentPower}");
        }

        // After checking the field condition, we should stop listening to the event if the effect was applied successfully.
        if (boostApplied)
        {
            TurnManager.instance.OnCardPlayed -= OnFieldChanged;  // Stop subscribing once effect is applied.
        }
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        TurnManager.instance.OnCardPlayed -= OnFieldChanged;

        if (boostApplied)
        {
            sourceCardRef.currentPower -= boostAmount;
            boostApplied = false;
            Debug.Log($"{sourceCardRef.cardData.cardName} effect: Removing conditional power boost of {boostAmount}. New power: {sourceCardRef.currentPower}");
        }
    }

    private bool IsConditionMet()
    {
        CardSO[,] field = GridManager.instance.GetGrid();
        for (int x = 0; x < field.GetLength(0); x++)
        {
            for (int y = 0; y < field.GetLength(1); y++)
            {
                if (field[x, y] != null)
                {
                    foreach (string req in requiredCardNames)
                    {
                        Debug.Log($"Checking for required card: {req}");

                        if (field[x, y].cardName == req)
                        {
                            Debug.Log($"Found {req} on the field at position ({x},{y})");
                            return true; // Condition met if a required card is found
                        }
                    }
                }
            }
        }
        return false; // Condition not met
    }
}
