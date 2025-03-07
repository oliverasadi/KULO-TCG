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
                            conditionMet = true;
                            break;
                        }
                    }
                }
            }
        }
        if (conditionMet)
        {
            Debug.Log($"{sourceCard.cardData.cardName} effect: Condition met, boosting power by {boostAmount}.");
            sourceCard.cardData.power += boostAmount;
        }
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        Debug.Log($"{sourceCard.cardData.cardName} effect: Removing conditional power boost of {boostAmount}.");
        sourceCard.cardData.power -= boostAmount;
    }
}
