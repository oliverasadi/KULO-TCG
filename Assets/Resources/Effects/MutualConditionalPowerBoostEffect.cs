using UnityEngine;

[CreateAssetMenu(fileName = "MutualConditionalPowerBoostEffect", menuName = "Card Effects/Mutual Conditional Power Boost")]
public class MutualConditionalPowerBoostEffect : CardEffect
{
    // The amount of power to boost
    public int powerIncrease = 500;
    // The name of the card that must be present on the field to trigger the boost.
    public string requiredCardName; // e.g., "The Cat with the Mole"

    public override void ApplyEffect(CardUI sourceCard)
    {
        // Check if the required card is on the field.
        bool conditionMet = false;
        CardSO[,] field = GridManager.instance.GetGrid();
        CardUI requiredCardUI = null;
        for (int x = 0; x < field.GetLength(0); x++)
        {
            for (int y = 0; y < field.GetLength(1); y++)
            {
                if (field[x, y] != null && field[x, y].cardName == requiredCardName)
                {
                    conditionMet = true;
                    // Try to get the CardUI for the required card from the grid objects.
                    requiredCardUI = GridManager.instance.GetGridObjects()[x, y].GetComponent<CardUI>();
                    break;
                }
            }
            if (conditionMet)
                break;
        }

        if (conditionMet)
        {
            Debug.Log($"{sourceCard.cardData.cardName} effect: Condition met (required card \"{requiredCardName}\" is on the field). Boosting both cards by {powerIncrease}.");
            // Boost the source card (if not already boosted—note: you may want to prevent duplicate boosts)
            sourceCard.cardData.power += powerIncrease;
            // Also boost the required card, if found.
            if (requiredCardUI != null)
            {
                requiredCardUI.cardData.power += powerIncrease;
                Debug.Log($"{requiredCardUI.cardData.cardName} new power: {requiredCardUI.cardData.power}");
            }
            else
            {
                Debug.LogWarning($"Could not find CardUI for required card \"{requiredCardName}\".");
            }
        }
        else
        {
            Debug.Log($"{sourceCard.cardData.cardName} effect: Condition not met; no boost applied.");
        }
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        // When the effect is removed (for instance, if the card leaves the field), remove the boost from the source card.
        Debug.Log($"{sourceCard.cardData.cardName} effect: Removing mutual power boost of {powerIncrease}.");
        sourceCard.cardData.power -= powerIncrease;

        // Also attempt to remove the boost from the required card if it's present.
        CardSO[,] field = GridManager.instance.GetGrid();
        for (int x = 0; x < field.GetLength(0); x++)
        {
            for (int y = 0; y < field.GetLength(1); y++)
            {
                if (field[x, y] != null && field[x, y].cardName == requiredCardName)
                {
                    CardUI reqUI = GridManager.instance.GetGridObjects()[x, y].GetComponent<CardUI>();
                    if (reqUI != null)
                    {
                        reqUI.cardData.power -= powerIncrease;
                        Debug.Log($"{reqUI.cardData.cardName} power reverted to {reqUI.cardData.power}");
                    }
                }
            }
        }
    }
}
