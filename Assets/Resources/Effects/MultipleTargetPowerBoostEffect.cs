using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Multiple Target Power Boost")]
public class MultipleTargetPowerBoostEffect : CardEffect
{
    public int powerIncrease = 100;
    public int maxTargets = 0; // NEW: limit for how many can be selected (0 = no limit)

    public List<CardUI> targetCards = new List<CardUI>();

    public override void ApplyEffect(CardUI sourceCard)
    {
        Debug.Log($"Boosting selected targets by {powerIncrease} power.");
        foreach (CardUI target in targetCards)
        {
            target.temporaryBoost += powerIncrease;
            Debug.Log($"Target {target.cardData.cardName} new power: {target.CalculateEffectivePower()}");
        }
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        Debug.Log($"Removing power boost of {powerIncrease} from targets.");
        foreach (CardUI target in targetCards)
        {
            target.temporaryBoost -= powerIncrease;
            Debug.Log($"Target {target.cardData.cardName} reverted power: {target.CalculateEffectivePower()}");
        }
    }
}
