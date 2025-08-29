using UnityEngine;
using UnityEngine.Events;
using System.Collections;

[CreateAssetMenu(menuName = "Card Effects/Replace After Opponent Turn")]
public class ReplaceAfterOpponentTurnEffect : CardEffect
{
    [Tooltip("The name of the card to replace the source card with.")]
    public string replacementCardName;

    [Tooltip("If true, no further cards may be played for the next turn after the replacement.")]
    public bool blockAdditionalPlays = true;

    [Tooltip("The UI prompt prefab to ask the player if they wish to use the effect.")]
    public GameObject promptPanelPrefab;

    public override void ApplyEffect(CardUI sourceCard)
    {
        Debug.Log($"[ReplaceAfterOpponentTurnEffect] ApplyEffect called for {sourceCard.cardData.cardName}");

        // Wait until it's the owner's turn again (i.e. after opponent's turn ends)
        sourceCard.StartCoroutine(TriggerAfterOpponentTurn(sourceCard));
    }

    private IEnumerator TriggerAfterOpponentTurn(CardUI sourceCard)
    {
        if (sourceCard == null || !sourceCard.isOnField)
            yield break;

        var handler = sourceCard.GetComponent<CardHandler>();
        if (handler == null || handler.cardOwner == null)
            yield break;

        int ownerPlayer = handler.cardOwner.playerNumber;

        // 🔁 Wait until the turn loops back to this card's owner
        yield return new WaitUntil(() => TurnManager.instance.GetCurrentPlayer() == ownerPlayer);

        // ⏱ Optional pacing delay
        yield return new WaitForSeconds(0.8f);

        // Show prompt only for local player
        if (ownerPlayer != TurnManager.instance.localPlayerNumber)
        {
            Debug.Log("[ReplaceAfterOpponentTurnEffect] Skipping prompt – not local player.");
            yield break;
        }

        if (sourceCard == null || !sourceCard.isOnField)
        {
            Debug.Log("[ReplaceAfterOpponentTurnEffect] Card removed before prompt.");
            yield break;
        }

        ShowPrompt(sourceCard);
    }

    private void ShowPrompt(CardUI sourceCard)
    {
        GameObject promptInstance = Instantiate(promptPanelPrefab);
        ReplaceEffectPrompt prompt = promptInstance.GetComponent<ReplaceEffectPrompt>();

        if (prompt != null)
        {
            prompt.OnResponse.AddListener((bool accepted) =>
            {
                if (accepted)
                    ExecuteReplacement(sourceCard);
                Destroy(promptInstance);
            });
        }
        else
        {
            Debug.LogError("⚠️ ReplaceAfterOpponentTurnEffect: Missing ReplaceEffectPrompt component.");
        }
    }

    private void ExecuteReplacement(CardUI sourceCard)
    {
        Debug.Log($"[ReplaceAfterOpponentTurnEffect] Executing replacement for '{sourceCard.cardData.cardName}'");

        string oldCardName = sourceCard.cardData.cardName;
        CardSO replacementCard = DeckManager.instance.FindCardByName(replacementCardName);
        if (replacementCard == null)
        {
            Debug.LogError("Replacement card not found!");
            return;
        }

        Vector2Int gridPos = ParseGridPosition(sourceCard.transform.parent.name);
        GridManager.instance.RemoveCard(gridPos.x, gridPos.y, false);

        GameObject newCardObj = InstantiateReplacementCard(replacementCard);
        Transform cellTransform = GameObject.Find($"GridCell_{gridPos.x}_{gridPos.y}").transform;
        GridManager.instance.PlaceExistingCard(gridPos.x, gridPos.y, newCardObj, replacementCard, cellTransform);

        if (replacementCard.baseOrEvo == CardSO.BaseOrEvo.Evolution)
        {
            GridManager.instance.ShowEvolutionSplash(oldCardName, replacementCard.cardName);
        }

        if (blockAdditionalPlays)
        {
            TurnManager.instance.BlockPlaysNextTurn();
        }
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        // No need to unsubscribe anything – coroutine handles its own lifetime.
    }

    private Vector2Int ParseGridPosition(string cellName)
    {
        string[] parts = cellName.Split('_');
        if (parts.Length >= 3 &&
            int.TryParse(parts[1], out int x) &&
            int.TryParse(parts[2], out int y))
        {
            return new Vector2Int(x, y);
        }
        return new Vector2Int(-1, -1);
    }

    private GameObject InstantiateReplacementCard(CardSO replacementCard)
    {
        GameObject cardPrefab = DeckManager.instance.cardPrefab;
        GameObject newCardObj = Instantiate(cardPrefab);
        CardHandler handler = newCardObj.GetComponent<CardHandler>();
        if (handler != null)
        {
            handler.SetCard(replacementCard, false, false);
        }
        return newCardObj;
    }
}
