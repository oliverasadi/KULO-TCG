using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Card Effects/Lose Power Each Turn")]
public class LosePowerEachTurnEffect : CardEffect
{
    public int amount = 100;

    // Track running coroutines per card to avoid duplicates
    private static Dictionary<CardUI, Coroutine> runningCoroutines = new();

    public override void ApplyEffect(CardUI sourceCard)
    {
        if (sourceCard == null)
        {
            Debug.LogWarning("[LosePowerEachTurnEffect] Source card is null.");
            return;
        }

        Debug.Log($"[LosePowerEachTurnEffect] Applied to {sourceCard.cardData.cardName}");

        // Stop any existing coroutine for this card
        if (runningCoroutines.TryGetValue(sourceCard, out var existing))
        {
            sourceCard.StopCoroutine(existing);
            runningCoroutines.Remove(sourceCard);
        }

        Coroutine routine = sourceCard.StartCoroutine(LosePowerRoutine(sourceCard));
        runningCoroutines[sourceCard] = routine;
    }

    private IEnumerator LosePowerRoutine(CardUI sourceCard)
    {
        Debug.Log($"[LosePowerEachTurnEffect] Starting loop on {sourceCard.cardData.cardName} (ID: {sourceCard.GetInstanceID()})");

        while (sourceCard != null && sourceCard.isOnField)
        {
            int playerNum = sourceCard.GetComponent<CardHandler>()?.cardOwner?.playerNumber ?? -1;
            yield return new WaitUntil(() => TurnManager.instance.GetCurrentPlayer() == playerNum);

            if (!sourceCard.isOnField)
            {
                Debug.Log($"[LosePowerEachTurnEffect] {sourceCard.cardData.cardName} left the field — stopping effect.");
                break;
            }

            int currentPower = sourceCard.CalculateEffectivePower();
            if (currentPower > 0)
            {
                int amountToLose = Mathf.Min(amount, currentPower);
                sourceCard.temporaryBoost -= amountToLose;

                Debug.Log($"[LosePowerEachTurnEffect] {sourceCard.cardData.cardName} loses {amountToLose} → {sourceCard.CalculateEffectivePower()}");

                sourceCard.UpdatePowerDisplay();
                if (sourceCard.cardInfoPanel?.CurrentCardUI == sourceCard)
                    sourceCard.cardInfoPanel.UpdatePowerDisplay();
            }

            yield return new WaitUntil(() => TurnManager.instance.GetCurrentPlayer() != playerNum);
        }

        Debug.Log($"[LosePowerEachTurnEffect] Coroutine ending for {sourceCard?.cardData?.cardName}");
        runningCoroutines.Remove(sourceCard);
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        if (sourceCard != null && runningCoroutines.TryGetValue(sourceCard, out var routine))
        {
            sourceCard.StopCoroutine(routine);
            runningCoroutines.Remove(sourceCard);
            Debug.Log($"[LosePowerEachTurnEffect] Effect removed from {sourceCard.cardData.cardName}");
        }
    }
}
