using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(menuName = "Card Effects/Adjust Power Adjacent")]
public class AdjustPowerAdjacentEffect : CardEffect
{
    public enum PowerChangeType { Increase, Decrease }
    public PowerChangeType powerChangeType = PowerChangeType.Increase;

    public enum OwnerToAffect { Self, Opponent, Both }
    public OwnerToAffect ownerToAffect = OwnerToAffect.Both;

    public int powerChangeAmount = 100;

    public enum AdjacentPosition { North, South, East, West, All }
    public List<AdjacentPosition> targetPositions = new List<AdjacentPosition> { AdjacentPosition.All };

    // Keep a list of all occupant CardUI's we've modified, so we can revert them in RemoveEffect
    private List<CardUI> affectedCards = new List<CardUI>();

    public override void ApplyEffect(CardUI sourceCard)
    {
        // 1) Clear from any previous usage
        affectedCards.Clear();

        // 2) Get position of the source card on the grid
        Vector2Int position = GetCardPosition(sourceCard);
        if (position.x < 0 || position.y < 0)
        {
            Debug.LogError($"[{nameof(AdjustPowerAdjacentEffect)}] Source card not found in grid!");
            return;
        }
        Debug.Log($"Card positioned at: {position.x}, {position.y}");

        // 3) Determine all adjacent positions we should affect
        List<Vector2Int> positionsToAdjust = new List<Vector2Int>();

        foreach (AdjacentPosition adjPos in targetPositions)
        {
            switch (adjPos)
            {
                case AdjacentPosition.North:
                    if (position.y < 2) positionsToAdjust.Add(new Vector2Int(position.x, position.y + 1));
                    break;
                case AdjacentPosition.South:
                    if (position.y > 0) positionsToAdjust.Add(new Vector2Int(position.x, position.y - 1));
                    break;
                case AdjacentPosition.East:
                    if (position.x < 2) positionsToAdjust.Add(new Vector2Int(position.x + 1, position.y));
                    break;
                case AdjacentPosition.West:
                    if (position.x > 0) positionsToAdjust.Add(new Vector2Int(position.x - 1, position.y));
                    break;

                case AdjacentPosition.All:
                    // Orthogonals
                    if (position.y < 2) positionsToAdjust.Add(new Vector2Int(position.x, position.y + 1)); // North
                    if (position.y > 0) positionsToAdjust.Add(new Vector2Int(position.x, position.y - 1)); // South
                    if (position.x < 2) positionsToAdjust.Add(new Vector2Int(position.x + 1, position.y)); // East
                    if (position.x > 0) positionsToAdjust.Add(new Vector2Int(position.x - 1, position.y)); // West

                    // Diagonals (NE, NW, SE, SW)
                    // Note: This snippet assumes a 3x3 grid with valid coords 0..2. If your grid is bigger,
                    // use e.g. (position.x < gridWidth - 1) rather than (position.x < 2).

                    // NE
                    if (position.x < 2 && position.y < 2)
                        positionsToAdjust.Add(new Vector2Int(position.x + 1, position.y + 1));
                    // NW
                    if (position.x > 0 && position.y < 2)
                        positionsToAdjust.Add(new Vector2Int(position.x - 1, position.y + 1));
                    // SE
                    if (position.x < 2 && position.y > 0)
                        positionsToAdjust.Add(new Vector2Int(position.x + 1, position.y - 1));
                    // SW
                    if (position.x > 0 && position.y > 0)
                        positionsToAdjust.Add(new Vector2Int(position.x - 1, position.y - 1));
                    break;
            }
        }

        LogAdjacentPositions(positionsToAdjust);

        // 4) For each position, if occupant matches owner filtering, apply the buff/debuff
        foreach (var pos in positionsToAdjust)
        {
            CardUI occupantUI = GetCardUIAtPosition(pos);
            if (occupantUI != null)
            {
                // Check if occupant is ours, opponent's, or either
                if (!ShouldAffect(occupantUI, sourceCard))
                    continue;

                // Apply a "temporary" buff or debuff. We'll revert it in RemoveEffect
                int delta = (powerChangeType == PowerChangeType.Increase) ? powerChangeAmount : -powerChangeAmount;

                occupantUI.temporaryBoost += delta;
                occupantUI.UpdatePower(occupantUI.currentPower + delta);

                affectedCards.Add(occupantUI);
                Debug.Log($"[{nameof(AdjustPowerAdjacentEffect)}] {sourceCard.cardData.cardName} " +
                          $"changed power of {occupantUI.cardData.cardName} by {delta} at {pos}.");
            }
        }
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        // Revert any occupant changes from this effect
        foreach (CardUI occupantUI in affectedCards)
        {
            if (occupantUI == null) continue;

            int delta = (powerChangeType == PowerChangeType.Increase) ? powerChangeAmount : -powerChangeAmount;
            occupantUI.temporaryBoost -= delta;
            occupantUI.UpdatePower(occupantUI.currentPower - delta);

            Debug.Log($"[{nameof(AdjustPowerAdjacentEffect)}] Reverted power change on {occupantUI.cardData.cardName}.");
        }
        affectedCards.Clear();
    }

    // -----------------------------------------------------------
    // Helper for ownership: only affect occupant if correct side.
    // -----------------------------------------------------------
    private bool ShouldAffect(CardUI occupantUI, CardUI sourceCardUI)
    {
        CardHandler occupantHandler = occupantUI.GetComponent<CardHandler>();
        CardHandler sourceHandler = sourceCardUI.GetComponent<CardHandler>();
        if (occupantHandler == null || sourceHandler == null)
            return true; // If we can't tell, default to true or skip

        switch (ownerToAffect)
        {
            case OwnerToAffect.Self:
                // occupant must share isAI with source
                return occupantHandler.isAI == sourceHandler.isAI;

            case OwnerToAffect.Opponent:
                // occupant must be on the opposite side from source
                return occupantHandler.isAI != sourceHandler.isAI;

            case OwnerToAffect.Both:
            default:
                return true;
        }
    }

    // -----------------------------------------
    // Helper: find occupant card
    // -----------------------------------------
    private CardUI GetCardUIAtPosition(Vector2Int pos)
    {
        CardSO[,] grid = GridManager.instance.GetGrid();
        GameObject[,] objs = GridManager.instance.GetGridObjects();

        if (pos.x < 0 || pos.x >= grid.GetLength(0) ||
            pos.y < 0 || pos.y >= grid.GetLength(1))
        {
            Debug.LogWarning($"Position {pos} out of bounds.");
            return null;
        }

        if (grid[pos.x, pos.y] == null) // no occupant
            return null;

        return objs[pos.x, pos.y].GetComponent<CardUI>();
    }

    // -----------------------------------------
    // Helper: find the source card's position
    // -----------------------------------------
    private Vector2Int GetCardPosition(CardUI card)
    {
        CardSO[,] grid = GridManager.instance.GetGrid();
        GameObject[,] objs = GridManager.instance.GetGridObjects();

        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                if (objs[x, y] == card.gameObject)
                    return new Vector2Int(x, y);
            }
        }
        Debug.LogError($"Card {card.cardData.cardName} not found in grid!");
        return new Vector2Int(-1, -1);
    }

    // -----------------------------------------
    // Debug logging
    // -----------------------------------------
    private void LogAdjacentPositions(List<Vector2Int> positions)
    {
        var joined = string.Join(", ", positions.Select(p => $"({p.x},{p.y})"));
        Debug.Log($"[{nameof(AdjustPowerAdjacentEffect)}] Adjacent positions to adjust: {joined}");
    }

    private void LogCardDetails(CardUI card)
    {
        Debug.Log($"Card Details: " +
                  $"Name: {card.cardData.cardName}, " +
                  $"Current Power: {card.currentPower}, " +
                  $"Power Change Type: {powerChangeType}, " +
                  $"Power Change Amount: {powerChangeAmount}, " +
                  $"OwnerToAffect: {ownerToAffect}");
    }
}
