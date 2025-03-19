using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Multiple Target Power Boost")]
public class MultipleTargetPowerBoostEffect : CardEffect
{
    public int powerIncrease = 100;
    // This list can be set at runtime based on player selection.
    public List<CardUI> targetCards = new List<CardUI>();

    public override void ApplyEffect(CardUI sourceCard)
    {
        Debug.Log($"Boosting selected targets by {powerIncrease} power.");
        foreach (CardUI target in targetCards)
        {
            target.temporaryBoost += powerIncrease;
            Debug.Log($"Target {target.cardData.cardName} new power: {target.cardData.power}");
        }
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        Debug.Log($"Removing power boost of {powerIncrease} from targets.");
        foreach (CardUI target in targetCards)
        {
            target.temporaryBoost -= powerIncrease;
            Debug.Log($"Target {target.cardData.cardName} reverted effective power: {target.CalculateEffectivePower()}");
        }
    }
}

