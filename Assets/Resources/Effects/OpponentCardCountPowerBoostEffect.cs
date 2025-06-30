using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Opponent Card Count Power Boost")]
public class OpponentCardCountPowerBoostEffect : CardEffect
{
    public int boostPerCard = 100;
    private Dictionary<CardUI, int> trackedCards = new();

    public override void ApplyEffect(CardUI sourceCard)
    {
        if (trackedCards.ContainsKey(sourceCard)) return;

        int count = CountOpponentCards(sourceCard);
        sourceCard.temporaryBoost += count * boostPerCard;
        sourceCard.UpdatePowerDisplay();
        trackedCards[sourceCard] = count;

        if (trackedCards.Count == 1)
        {
            TurnManager.instance.OnCardPlayed += OnBoardStateChanged;
        }
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        if (!trackedCards.ContainsKey(sourceCard)) return;

        int lastCount = trackedCards[sourceCard];
        sourceCard.temporaryBoost -= lastCount * boostPerCard;
        sourceCard.UpdatePowerDisplay();
        trackedCards.Remove(sourceCard);

        if (trackedCards.Count == 0)
        {
            TurnManager.instance.OnCardPlayed -= OnBoardStateChanged;
        }
    }

    private void OnBoardStateChanged(CardSO _)
    {
        var keys = new List<CardUI>(trackedCards.Keys);
        foreach (var cardUI in keys)
        {
            if (cardUI == null) continue;

            int newCount = CountOpponentCards(cardUI);
            int lastCount = trackedCards[cardUI];

            if (newCount != lastCount)
            {
                int delta = (newCount - lastCount) * boostPerCard;
                cardUI.temporaryBoost += delta;
                cardUI.UpdatePowerDisplay();
                trackedCards[cardUI] = newCount;
            }
        }
    }

    private int CountOpponentCards(CardUI sourceCard)
    {
        CardHandler sourceHandler = sourceCard.GetComponent<CardHandler>();
        if (sourceHandler == null || sourceHandler.cardOwner == null)
        {
            Debug.LogWarning("[MustacheCatHeroEffect] Could not determine owner.");
            return 0;
        }

        int opponentPlayerNum = sourceHandler.cardOwner.playerNumber == 1 ? 2 : 1;
        GameObject[,] gridObjects = GridManager.instance.GetGridObjects();
        int rows = gridObjects.GetLength(0);
        int cols = gridObjects.GetLength(1);
        int count = 0;

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < cols; j++)
            {
                GameObject obj = gridObjects[i, j];
                if (obj == null) continue;

                CardHandler handler = obj.GetComponent<CardHandler>();
                if (handler != null && handler.cardOwner != null && handler.cardOwner.playerNumber == opponentPlayerNum)
                {
                    count++;
                }
            }
        }
        return count;
    }
}
