using UnityEngine;

[CreateAssetMenu(fileName = "MutualConditionalPowerBoostEffect", menuName = "Card Effects/Mutual Conditional Power Boost")]
public class MutualConditionalPowerBoostEffect : CardEffect
{
    public int boostAmount = -100;
    public string[] requiredCardNames;
    public string[] requiredCreatureTypes;

    public enum SearchOwnerOption { Mine, AI, Both }
    public SearchOwnerOption searchOwner = SearchOwnerOption.Both;

    private int currentTotalPenalty = 0;
    private CardUI sourceCardRef;

    public override void ApplyEffect(CardUI sourceCard)
    {
        sourceCardRef = sourceCard;
        Debug.Log($"[MutualConditionalPowerBoostEffect] ApplyEffect called on {sourceCard.cardData.cardName}");

        int matchCount = CountMatches();
        currentTotalPenalty = boostAmount * matchCount;
        if (matchCount > 0)
        {
            // Instead of directly modifying currentPower, update the aggregated modifier.
            sourceCardRef.temporaryBoost += currentTotalPenalty;
            sourceCardRef.UpdatePower(sourceCardRef.CalculateEffectivePower());
            Debug.Log($"{sourceCardRef.cardData.cardName} synergy: {currentTotalPenalty} applied for {matchCount} matching card(s). New power: {sourceCardRef.currentPower}");
        }

        TurnManager.instance.OnCardPlayed += OnFieldChanged;
    }

    public void OnFieldChanged(CardSO updatedCard)
    {
        if (sourceCardRef == null)
            return;

        int newMatchCount = CountMatches();
        int newTotalPenalty = boostAmount * newMatchCount;

        if (newTotalPenalty != currentTotalPenalty)
        {
            // Remove the previous penalty and then apply the new one.
            sourceCardRef.temporaryBoost -= currentTotalPenalty;
            sourceCardRef.temporaryBoost += newTotalPenalty;
            sourceCardRef.UpdatePower(sourceCardRef.CalculateEffectivePower());
            Debug.Log($"{sourceCardRef.cardData.cardName} synergy updated: from {currentTotalPenalty} to {newTotalPenalty} for {newMatchCount} matching card(s). New power: {sourceCardRef.currentPower}");
            currentTotalPenalty = newTotalPenalty;
        }
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        TurnManager.instance.OnCardPlayed -= OnFieldChanged;
        if (currentTotalPenalty != 0)
        {
            sourceCardRef.temporaryBoost -= currentTotalPenalty;
            sourceCardRef.UpdatePower(sourceCardRef.CalculateEffectivePower());
            Debug.Log($"{sourceCardRef.cardData.cardName} synergy removed: reversing {currentTotalPenalty}. New power: {sourceCardRef.currentPower}");
            currentTotalPenalty = 0;
        }
    }

    private int CountMatches()
    {
        int count = 0;
        CardSO[,] field = GridManager.instance.GetGrid();
        GameObject[,] gridObjs = GridManager.instance.GetGridObjects();

        CardHandler sourceHandler = sourceCardRef.GetComponent<CardHandler>();
        PlayerManager sourceOwner = (sourceHandler != null) ? sourceHandler.cardOwner : null;

        for (int x = 0; x < field.GetLength(0); x++)
        {
            for (int y = 0; y < field.GetLength(1); y++)
            {
                CardSO occupant = field[x, y];
                if (occupant == null)
                    continue;

                // Skip only the source card instance.
                if (gridObjs[x, y] == sourceCardRef.gameObject)
                    continue;

                if (searchOwner == SearchOwnerOption.Mine)
                {
                    CardHandler occupantHandler = gridObjs[x, y].GetComponent<CardHandler>();
                    if (occupantHandler == null || occupantHandler.cardOwner != sourceOwner)
                        continue;
                }
                else if (searchOwner == SearchOwnerOption.AI)
                {
                    CardHandler occupantHandler = gridObjs[x, y].GetComponent<CardHandler>();
                    if (occupantHandler == null || !occupantHandler.isAI)
                        continue;
                }

                bool nameMatch = false;
                bool typeMatch = false;

                if (requiredCardNames != null)
                {
                    foreach (string reqName in requiredCardNames)
                    {
                        if (!string.IsNullOrEmpty(reqName) && occupant.cardName == reqName)
                        {
                            nameMatch = true;
                            break;
                        }
                    }
                }

                if (requiredCreatureTypes != null)
                {
                    foreach (string reqType in requiredCreatureTypes)
                    {
                        if (!string.IsNullOrEmpty(reqType) && occupant.creatureType == reqType)
                        {
                            typeMatch = true;
                            break;
                        }
                    }
                }

                if (nameMatch || typeMatch)
                    count++;
            }
        }
        return count;
    }
}
