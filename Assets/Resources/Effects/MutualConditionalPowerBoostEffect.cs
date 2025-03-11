using UnityEngine;

[CreateAssetMenu(fileName = "MutualConditionalPowerBoostEffect", menuName = "Card Effects/Mutual Conditional Power Boost")]
public class MutualConditionalPowerBoostEffect : CardEffect
{
    // The amount of power to boost (adjustable in the inspector)
    public int powerIncrease = 500;
    // The name of the card that must be present on the field to trigger the boost.
    public string requiredCardName; // e.g., "The Cat with the Mole"

    public override void ApplyEffect(CardUI sourceCard)
    {
        bool conditionMet = false;
        CardSO[,] field = GridManager.instance.GetGrid();
        CardUI requiredCardUI = null;
        CardHandler sourceHandler = sourceCard.GetComponent<CardHandler>();

        Debug.Log($"[MutualEffect] Source card: {sourceCard.cardData.cardName} (Owner: {sourceHandler.cardOwner})");

        // Iterate over the grid to find a friendly card with the required name.
        for (int x = 0; x < field.GetLength(0); x++)
        {
            for (int y = 0; y < field.GetLength(1); y++)
            {
                if (field[x, y] != null)
                {
                    Debug.Log($"[MutualEffect] Checking cell [{x},{y}]: card {field[x, y].cardName}");
                    if (field[x, y].cardName == requiredCardName)
                    {
                        Debug.Log($"[MutualEffect] Found card with required name \"{requiredCardName}\" at [{x},{y}]");
                        CardHandler candidateHandler = GridManager.instance.GetGridObjects()[x, y].GetComponent<CardHandler>();
                        if (candidateHandler != null)
                        {
                            Debug.Log($"[MutualEffect] Candidate's owner: {candidateHandler.cardOwner} vs Source owner: {sourceHandler.cardOwner}");
                            if (sourceHandler != null && candidateHandler.cardOwner == sourceHandler.cardOwner)
                            {
                                Debug.Log($"[MutualEffect] Friendly candidate found at [{x},{y}].");
                                conditionMet = true;
                                requiredCardUI = GridManager.instance.GetGridObjects()[x, y].GetComponent<CardUI>();
                                break;
                            }
                            else
                            {
                                Debug.Log($"[MutualEffect] Candidate at [{x},{y}] is not friendly.");
                            }
                        }
                        else
                        {
                            Debug.LogWarning($"[MutualEffect] Candidate at [{x},{y}] does not have a CardHandler.");
                        }
                    }
                }
                else
                {
                    Debug.Log($"[MutualEffect] Cell [{x},{y}] is empty.");
                }
            }
            if (conditionMet)
                break;
        }

        if (conditionMet)
        {
            Debug.Log($"{sourceCard.cardData.cardName} effect: Condition met (friendly required card \"{requiredCardName}\" is on the field). Boosting both cards by {powerIncrease}.");
            // Boost the source card.
            sourceCard.cardData.power += powerIncrease;
            // Also boost the required friendly card if its CardUI is found.
            if (requiredCardUI != null)
            {
                requiredCardUI.cardData.power += powerIncrease;
                Debug.Log($"{requiredCardUI.cardData.cardName} new power: {requiredCardUI.cardData.power}");
            }
            else
            {
                Debug.LogWarning($"Could not find CardUI for friendly required card \"{requiredCardName}\".");
            }
        }
        else
        {
            Debug.Log($"{sourceCard.cardData.cardName} effect: Condition not met; no boost applied.");
        }
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        Debug.Log($"{sourceCard.cardData.cardName} effect: Removing mutual power boost of {powerIncrease}.");
        sourceCard.cardData.power -= powerIncrease;

        // Also attempt to remove the boost from the friendly required card if it's present.
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
