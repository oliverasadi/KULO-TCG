using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Card Effects/Replace After Opponent Turn")]
public class ReplaceAfterOpponentTurnEffect : CardEffect
{
    [Tooltip("The name of the card to replace the source card with.")]
    public string replacementCardName;

    [Tooltip("If true, no further cards may be played this turn after the replacement.")]
    public bool blockAdditionalPlays = true;

    // This method is called when the effect should start (e.g., when the card is summoned).
    public override void ApplyEffect(CardUI sourceCard)
    {
        // Subscribe to an opponent turn end event.
        // (You must implement OnOpponentTurnEnd in your TurnManager.)
        TurnManager.instance.OnOpponentTurnEnd += () => OnOpponentTurnEnd(sourceCard);
    }

    // Called when the opponent's turn ends.
    private void OnOpponentTurnEnd(CardUI sourceCard)
    {
        // Check that the card is still on the field.
        if (sourceCard != null && sourceCard.isOnField)
        {
            // For now, simulate a UI prompt by auto-accepting.
            // Replace this with actual UI logic as needed.
            bool playerChoosesReplacement = true; // (Auto-accept for now)

            if (playerChoosesReplacement)
            {
                // Get the replacement card from deck or hand.
                CardSO replacementCard = DeckManager.instance.FindCardByName(replacementCardName);
                if (replacementCard != null)
                {
                    // Determine the grid position of the source card.
                    Vector2Int gridPos = ParseGridPosition(sourceCard.transform.parent.name);
                    if (gridPos.x == -1)
                    {
                        Debug.LogError("ReplaceAfterOpponentTurnEffect: Unable to determine grid position.");
                        return;
                    }

                    // Remove the source card from the grid.
                    GridManager.instance.RemoveCard(gridPos.x, gridPos.y, false);

                    // Instantiate the replacement card.
                    GameObject newCardObj = InstantiateReplacementCard(replacementCard);

                    // Place the new card into the same grid cell.
                    Transform cellTransform = GameObject.Find($"GridCell_{gridPos.x}_{gridPos.y}").transform;
                    GridManager.instance.PlaceExistingCard(gridPos.x, gridPos.y, newCardObj, replacementCard, cellTransform);

                    // Optionally block further plays this turn.
                    if (blockAdditionalPlays)
                    {
                        TurnManager.instance.BlockAdditionalCardPlays();
                    }
                }
                else
                {
                    Debug.LogError("ReplaceAfterOpponentTurnEffect: Replacement card not found.");
                }
            }
        }
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        // Unsubscribe from the opponent-turn-end event.
        TurnManager.instance.OnOpponentTurnEnd -= () => OnOpponentTurnEnd(sourceCard);
    }

    // Helper method to parse grid coordinates from a cell's name (e.g., "GridCell_1_2").
    private Vector2Int ParseGridPosition(string cellName)
    {
        // Assumes the name format is "GridCell_x_y".
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
        // Assumes you have a card prefab available via DeckManager.
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
