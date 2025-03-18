using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Conditional Power Boost")]
public class ConditionalPowerBoostEffect : CardEffect
{
    public int boostAmount = 200;
    // Names of the cards required on the field to trigger the boost.
    public string[] requiredCardNames;

    // Tracks whether the boost is currently applied to avoid stacking.
    private bool boostApplied = false;

    // Keep a reference to the source CardUI, so we can update its runtime power
    // and unsubscribe from events when this effect is removed.
    private CardUI sourceCardRef;

    public override void ApplyEffect(CardUI sourceCard)
    {
        // Store a reference to the source card
        sourceCardRef = sourceCard;

        // Perform an initial check to apply the boost if conditions are already met
        bool conditionNow = IsConditionMet();
        if (conditionNow)
        {
            sourceCardRef.currentPower += boostAmount;
            boostApplied = true;
            Debug.Log($"{sourceCardRef.cardData.cardName} effect: Condition initially met => +{boostAmount} power. New power: {sourceCardRef.currentPower}");
        }

        // Subscribe to events that happen when cards are played or moved.
        // Replace these with whatever your project actually uses.
        // For example, if you only have a TurnManager event for new cards:
        TurnManager.instance.OnCardPlayed += OnFieldChanged;

        // If you have an event that fires when a card moves or leaves the field, subscribe similarly:
        // GridManager.instance.OnCardMoved += OnFieldChanged;
        // GridManager.instance.OnCardRemoved += OnFieldChanged;
    }

    // Called whenever the field changes (e.g., a card is played, moved, or removed).
    private void OnFieldChanged(CardSO updatedCard)
    {
        if (sourceCardRef == null) return; // Defensive check

        bool conditionNow = IsConditionMet();
        if (conditionNow && !boostApplied)
        {
            // Condition just became true; apply the boost
            sourceCardRef.currentPower += boostAmount;
            boostApplied = true;
            Debug.Log($"{sourceCardRef.cardData.cardName} effect: Condition now met => +{boostAmount} power. New power: {sourceCardRef.currentPower}");
        }
        else if (!conditionNow && boostApplied)
        {
            // Condition no longer met; remove the boost
            sourceCardRef.currentPower -= boostAmount;
            boostApplied = false;
            Debug.Log($"{sourceCardRef.cardData.cardName} effect: Condition no longer met => -{boostAmount} power. New power: {sourceCardRef.currentPower}");
        }
        // If conditionNow==true && boostApplied==true, or conditionNow==false && !boostApplied, do nothing.
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        // Unsubscribe from any events we used
        TurnManager.instance.OnCardPlayed -= OnFieldChanged;
        // GridManager.instance.OnCardMoved -= OnFieldChanged; // If used
        // GridManager.instance.OnCardRemoved -= OnFieldChanged; // If used

        // If the boost is still applied, remove it
        if (boostApplied)
        {
            sourceCard.currentPower -= boostAmount;
            boostApplied = false;
            Debug.Log($"{sourceCard.cardData.cardName} effect: Removing conditional power boost of {boostAmount}. New power: {sourceCard.currentPower}");
        }
    }

    // Check if at least one of the requiredCardNames is present on the field
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
                        if (field[x, y].cardName == req)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        return false;
    }
}
