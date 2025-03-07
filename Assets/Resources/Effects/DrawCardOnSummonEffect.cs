using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Draw Card On Summon")]
public class DrawCardOnSummonEffect : CardEffect
{
    public int cardsToDraw = 1;

    public override void ApplyEffect(CardUI sourceCard)
    {
        Debug.Log($"{sourceCard.cardData.cardName} effect: Drawing {cardsToDraw} card(s) on summon.");

        // This assumes your CardUI or CardHandler has a reference to the player's PlayerManager
        // e.g. sourceCard.cardOwner.DrawCard();
        // If you need to draw multiple, you can loop it or modify your draw method.
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        // For a one-shot effect, nothing to remove.
    }
}
