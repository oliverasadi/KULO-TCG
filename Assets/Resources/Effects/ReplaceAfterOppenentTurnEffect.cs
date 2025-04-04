using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Card Effects/Replace After Opponent Turn")]
public class ReplaceAfterOpponentTurnEffect : CardEffect
{
    [Tooltip("The name of the card to replace the source card with.")]
    public string replacementCardName;

    [Tooltip("If true, no further cards may be played for the next turn after the replacement.")]
    public bool blockAdditionalPlays = true;

    [Tooltip("The UI prompt prefab to ask the player if they wish to use the effect.")]
    public GameObject promptPanelPrefab;  // Assign this via the Inspector.

    // Store the delegate so we can unsubscribe properly.
    private System.Action onOpponentTurnEndAction;

    public override void ApplyEffect(CardUI sourceCard)
    {
        // Subscribe to the opponent turn end event.
        Debug.Log($"[ReplaceAfterOpponentTurnEffect] ApplyEffect called for {sourceCard.cardData.cardName}");

        onOpponentTurnEndAction = () => OnOpponentTurnEnd(sourceCard);
        TurnManager.instance.OnOpponentTurnEnd += onOpponentTurnEndAction;
    }

    private void OnOpponentTurnEnd(CardUI sourceCard)
    {
        // Only proceed if the source card is still on the field.
        if (sourceCard != null && sourceCard.isOnField)
        {
            // Instantiate the prompt panel.
            GameObject promptInstance = Instantiate(promptPanelPrefab);
            // Assume the prompt panel has a ReplaceEffectPrompt component.
            ReplaceEffectPrompt prompt = promptInstance.GetComponent<ReplaceEffectPrompt>();
            if (prompt != null)
            {
                // Subscribe to the response event.
                prompt.OnResponse.AddListener((bool accepted) =>
                {
                    if (accepted)
                    {
                        ExecuteReplacement(sourceCard);
                    }
                    // Clean up the prompt panel.
                    Destroy(promptInstance);
                });
            }
            else
            {
                Debug.LogError("ReplaceAfterOpponentTurnEffect: The prompt prefab is missing the ReplaceEffectPrompt component.");
            }
        }
    }

    private void ExecuteReplacement(CardUI sourceCard)
    {
        // Log to confirm this method is called
        Debug.Log($"[ReplaceAfterOpponentTurnEffect] ExecuteReplacement called for '{sourceCard.cardData.cardName}'");

        // 1) Grab occupant's name before removing
        string oldCardName = sourceCard.cardData.cardName;
        Debug.Log($"[ReplaceAfterOpponentTurnEffect] oldCardName = '{oldCardName}'");

        // 2) Find and remove occupant
        CardSO replacementCard = DeckManager.instance.FindCardByName(replacementCardName);
        if (replacementCard == null)
        {
            Debug.LogError("Replacement card not found!");
            return;
        }

        Vector2Int gridPos = ParseGridPosition(sourceCard.transform.parent.name);
        GridManager.instance.RemoveCard(gridPos.x, gridPos.y, false);

        // 3) Instantiate & place the new card
        GameObject newCardObj = InstantiateReplacementCard(replacementCard);
        Transform cellTransform = GameObject.Find($"GridCell_{gridPos.x}_{gridPos.y}").transform;
        GridManager.instance.PlaceExistingCard(gridPos.x, gridPos.y, newCardObj, replacementCard, cellTransform);

        // 4) If it's an Evolution, show splash
        if (replacementCard.baseOrEvo == CardSO.BaseOrEvo.Evolution)
        {
            Debug.Log($"[ReplaceAfterOpponentTurnEffect] Attempting ShowEvolutionSplash with oldCardName='{oldCardName}' and new evo='{replacementCard.cardName}'");
            GridManager.instance.ShowEvolutionSplash(oldCardName, replacementCard.cardName);
        }

        // 5) Optionally block the next turn
        if (blockAdditionalPlays)
        {
            Debug.Log("Replacing & blocking next turn!");
            TurnManager.instance.BlockPlaysNextTurn();
        }
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        if (onOpponentTurnEndAction != null)
        {
            TurnManager.instance.OnOpponentTurnEnd -= onOpponentTurnEndAction;
        }
    }

    // Helper method to parse grid coordinates from a cell's name (e.g., "GridCell_1_2").
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

    // Helper method to instantiate the replacement card.
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
