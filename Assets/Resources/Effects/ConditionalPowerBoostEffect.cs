using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Conditional Power Boost")]
public class ConditionalPowerBoostEffect : CardEffect
{
    public int boostAmount = 200;
    // Names of the cards required on the field to trigger the boost.
    public string[] requiredCardNames;

    // When true, the effect will count matching cards and add boostAmount per match.
    // (This value can be set via asset-based effects, but for inline effects you can copy it from CardEffectData.useCountMode.)
    public bool useCountMode = false;

    // For non-count mode, we track whether the boost is applied.
    // For count mode, we track the total boost applied.
    private bool boostApplied = false;
    private int currentBoost = 0;

    // Reference to the source CardUI so we can update its runtime power.
    private CardUI sourceCardRef;

    public override void ApplyEffect(CardUI sourceCard)
    {
        sourceCardRef = sourceCard;

        int matchCount = 0;
        if (useCountMode)
            matchCount = CountMatches();
        else
            matchCount = IsConditionMet() ? 1 : 0;

        if (matchCount > 0)
        {
            currentBoost = boostAmount * matchCount;
            sourceCardRef.currentPower += currentBoost;
            boostApplied = true;
            Debug.Log($"{sourceCardRef.cardData.cardName} effect: Condition met for {matchCount} match(es) => +{currentBoost} power. New power: {sourceCardRef.currentPower}");
        }

        // Subscribe to field updates.
        TurnManager.instance.OnCardPlayed += OnFieldChanged;
    }

    private void OnFieldChanged(CardSO updatedCard)
    {
        if (sourceCardRef == null)
            return;

        int newMatchCount = useCountMode ? CountMatches() : (IsConditionMet() ? 1 : 0);
        int newBoost = boostAmount * newMatchCount;

        if (newBoost != currentBoost)
        {
            sourceCardRef.currentPower -= currentBoost;
            sourceCardRef.currentPower += newBoost;
            Debug.Log($"{sourceCardRef.cardData.cardName} effect: Updated boost from {currentBoost} to {newBoost} (based on {newMatchCount} match(es)). New power: {sourceCardRef.currentPower}");
            currentBoost = newBoost;
            boostApplied = (newMatchCount > 0);
        }
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        TurnManager.instance.OnCardPlayed -= OnFieldChanged;
        if (boostApplied)
        {
            sourceCard.currentPower -= currentBoost;
            Debug.Log($"{sourceCard.cardData.cardName} effect: Removing boost of {currentBoost}. New power: {sourceCard.currentPower}");
            currentBoost = 0;
            boostApplied = false;
        }
    }

    // Returns true if at least one matching card is on the field.
    // If no required names are specified, any card with category Creature will count.
    private bool IsConditionMet()
    {
        CardSO[,] field = GridManager.instance.GetGrid();
        for (int x = 0; x < field.GetLength(0); x++)
        {
            for (int y = 0; y < field.GetLength(1); y++)
            {
                if (field[x, y] != null)
                {
                    if (requiredCardNames == null || requiredCardNames.Length == 0)
                    {
                        if (field[x, y].category == CardSO.CardCategory.Creature)
                            return true;
                    }
                    else
                    {
                        foreach (string req in requiredCardNames)
                        {
                            if (!string.IsNullOrEmpty(req) && field[x, y].cardName == req)
                                return true;
                        }
                    }
                }
            }
        }
        return false;
    }

    // Counts the number of matching cards on the field.
    // If requiredCardNames is empty, counts every creature (excluding the source card).
    private int CountMatches()
    {
        int count = 0;
        CardSO[,] field = GridManager.instance.GetGrid();
        GameObject[,] gridObjs = GridManager.instance.GetGridObjects();

        for (int x = 0; x < field.GetLength(0); x++)
        {
            for (int y = 0; y < field.GetLength(1); y++)
            {
                if (field[x, y] != null)
                {
                    if (requiredCardNames == null || requiredCardNames.Length == 0)
                    {
                        if (field[x, y].category == CardSO.CardCategory.Creature)
                        {
                            if (gridObjs[x, y] == sourceCardRef.gameObject)
                                continue;
                            count++;
                        }
                    }
                    else
                    {
                        foreach (string req in requiredCardNames)
                        {
                            if (!string.IsNullOrEmpty(req) && field[x, y].cardName == req)
                            {
                                if (gridObjs[x, y] == sourceCardRef.gameObject)
                                    continue;
                                count++;
                                break;
                            }
                        }
                    }
                }
            }
        }
        return count;
    }
}
