using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

    // Store the source card's UI for persistent effect evaluation.
    private CardUI sourceCardUI;

    public override void ApplyEffect(CardUI sourceCard)
    {
        // Clear any previous usage data.
        affectedCards.Clear();

        // Get position of the source card on the grid.
        Vector2Int position = GetCardPosition(sourceCard);
        if (position.x < 0 || position.y < 0)
        {
            Debug.LogError($"[{nameof(AdjustPowerAdjacentEffect)}] Source card not found in grid!");
            return;
        }
        Debug.Log($"Card positioned at: {position.x}, {position.y}");

        // Store source reference for later checks.
        sourceCardUI = sourceCard;

        // Determine all adjacent positions we should affect.
        List<Vector2Int> positionsToAdjust = GetAdjacentPositions(position);

        LogAdjacentPositions(positionsToAdjust);

        // For each position, if occupant matches owner filtering, apply the debuff/buff.
        foreach (var pos in positionsToAdjust)
        {
            CardUI occupantUI = GetCardUIAtPosition(pos);
            if (occupantUI != null)
            {
                if (!ShouldAffect(occupantUI, sourceCard))
                    continue;

                int delta = (powerChangeType == PowerChangeType.Increase) ? powerChangeAmount : -powerChangeAmount;

                occupantUI.temporaryBoost += delta;
                occupantUI.UpdatePower(occupantUI.CalculateEffectivePower());

                affectedCards.Add(occupantUI);
                Debug.Log($"[{nameof(AdjustPowerAdjacentEffect)}] {sourceCard.cardData.cardName} changed power of {occupantUI.cardData.cardName} by {delta} at {pos}.");
            }
        }

        // Subscribe to board changes for persistent effect updates.
        TurnManager.instance.OnCardPlayed += OnAnyCardPlayed;
    }

    /// <summary>
    /// Event handler to react to any card being played (or removed) on the board.
    /// </summary>
    /// <param name="playedCardData">The CardSO of the card that was played/removed.</param>
    private void OnAnyCardPlayed(CardSO playedCardData)
    {
        if (sourceCardUI == null)
            return; // Safety check: source no longer exists.

        Vector2Int srcPos = GetCardPosition(sourceCardUI);
        List<Vector2Int> adjPositions = GetAdjacentPositions(srcPos);

        foreach (Vector2Int pos in adjPositions)
        {
            CardUI neighbor = GetCardUIAtPosition(pos);
            // Apply effect only if the neighbor exists, meets filter criteria, and hasn't already been affected.
            if (neighbor != null && ShouldAffect(neighbor, sourceCardUI) && !affectedCards.Contains(neighbor))
            {
                int delta = (powerChangeType == PowerChangeType.Decrease) ? -powerChangeAmount : powerChangeAmount;
                neighbor.temporaryBoost += delta;
                neighbor.UpdatePower(neighbor.CalculateEffectivePower());
                affectedCards.Add(neighbor);
                Debug.Log($"{sourceCardUI.cardData.cardName} debuffed new neighbor {neighbor.cardData.cardName} at {pos}");
            }
        }

        // (Optional) You could also remove or revert the effect on cards that are no longer adjacent.
        // For each card in affectedCards, if its position is no longer in adjPositions, revert the change.
        List<CardUI> toRemove = new List<CardUI>();
        foreach (CardUI affected in affectedCards)
        {
            Vector2Int affectedPos = GetCardPosition(affected);
            if (!adjPositions.Contains(affectedPos))
            {
                int delta = (powerChangeType == PowerChangeType.Decrease) ? -powerChangeAmount : powerChangeAmount;
                affected.temporaryBoost -= delta;
                affected.UpdatePower(affected.CalculateEffectivePower());
                Debug.Log($"{sourceCardUI.cardData.cardName} reverted debuff on {affected.cardData.cardName} as it is no longer adjacent.");
                toRemove.Add(affected);
            }
        }
        foreach (CardUI removeItem in toRemove)
        {
            affectedCards.Remove(removeItem);
        }
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        // Unsubscribe from the event to stop receiving further updates.
        TurnManager.instance.OnCardPlayed -= OnAnyCardPlayed;

        // Revert all applied changes.
        foreach (CardUI occupantUI in affectedCards)
        {
            if (occupantUI == null)
                continue;

            int delta = (powerChangeType == PowerChangeType.Increase) ? powerChangeAmount : -powerChangeAmount;
            occupantUI.temporaryBoost -= delta;
            occupantUI.UpdatePower(occupantUI.CalculateEffectivePower());

            Debug.Log($"[{nameof(AdjustPowerAdjacentEffect)}] Reverted power change on {occupantUI.cardData.cardName}.");
        }
        affectedCards.Clear();
    }

    private bool ShouldAffect(CardUI occupantUI, CardUI sourceCardUI)
    {
        CardHandler occupantHandler = occupantUI.GetComponent<CardHandler>();
        CardHandler sourceHandler = sourceCardUI.GetComponent<CardHandler>();
        if (occupantHandler == null || sourceHandler == null)
            return true;

        switch (ownerToAffect)
        {
            case OwnerToAffect.Self:
                return occupantHandler.isAI == sourceHandler.isAI;
            case OwnerToAffect.Opponent:
                return occupantHandler.isAI != sourceHandler.isAI;
            case OwnerToAffect.Both:
            default:
                return true;
        }
    }

    /// <summary>
    /// Get the CardUI located at a specified grid position.
    /// </summary>
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

        if (grid[pos.x, pos.y] == null)
            return null;

        return objs[pos.x, pos.y].GetComponent<CardUI>();
    }

    /// <summary>
    /// Get the grid position for a given card.
    /// </summary>
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

    /// <summary>
    /// Determine adjacent positions based on the source position and targetPositions settings.
    /// </summary>
    private List<Vector2Int> GetAdjacentPositions(Vector2Int position)
    {
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
                    if (position.y < 2) positionsToAdjust.Add(new Vector2Int(position.x, position.y + 1));
                    if (position.y > 0) positionsToAdjust.Add(new Vector2Int(position.x, position.y - 1));
                    if (position.x < 2) positionsToAdjust.Add(new Vector2Int(position.x + 1, position.y));
                    if (position.x > 0) positionsToAdjust.Add(new Vector2Int(position.x - 1, position.y));
                    // Diagonals
                    if (position.x < 2 && position.y < 2) positionsToAdjust.Add(new Vector2Int(position.x + 1, position.y + 1));
                    if (position.x > 0 && position.y < 2) positionsToAdjust.Add(new Vector2Int(position.x - 1, position.y + 1));
                    if (position.x < 2 && position.y > 0) positionsToAdjust.Add(new Vector2Int(position.x + 1, position.y - 1));
                    if (position.x > 0 && position.y > 0) positionsToAdjust.Add(new Vector2Int(position.x - 1, position.y - 1));
                    break;
            }
        }
        return positionsToAdjust;
    }

    private void LogAdjacentPositions(List<Vector2Int> positions)
    {
        var joined = string.Join(", ", positions.Select(p => $"({p.x},{p.y})"));
        Debug.Log($"[{nameof(AdjustPowerAdjacentEffect)}] Adjacent positions to adjust: {joined}");
    }

    private void LogCardDetails(CardUI card)
    {
        Debug.Log($"Card Details: Name: {card.cardData.cardName}, Current Power: {card.currentPower}, Power Change Type: {powerChangeType}, Power Change Amount: {powerChangeAmount}, OwnerToAffect: {ownerToAffect}");
    }
}
