using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[CreateAssetMenu(menuName = "Card Effects/Adjust Power Adjacent")]
public class AdjustPowerAdjacentEffect : CardEffect
{
    public enum PowerChangeType { Increase, Decrease }
    public PowerChangeType powerChangeType = PowerChangeType.Increase;
    public int powerChangeAmount = 100;

    public enum AdjacentPosition { North, South, East, West, All }
    public List<AdjacentPosition> targetPositions = new List<AdjacentPosition> { AdjacentPosition.All };

    public override void ApplyEffect(CardUI sourceCard)
    {
        // Detailed logging of card and effect details
        LogCardDetails(sourceCard);

        // Get the position of the card on the grid
        Vector2Int position = GetCardPosition(sourceCard);
        Debug.Log($"Card positioned at: {position.x}, {position.y}");

        // List to store adjacent positions to apply the effect
        List<Vector2Int> positionsToAdjust = new List<Vector2Int>();

        foreach (AdjacentPosition adjPos in targetPositions)
        {
            // Depending on the position of the card, adjust the valid target positions
            switch (adjPos)
            {
                case AdjacentPosition.North:
                    if (position.y < 2) positionsToAdjust.Add(new Vector2Int(position.x, position.y + 1)); // Valid if not at the top row
                    break;
                case AdjacentPosition.South:
                    if (position.y > 0) positionsToAdjust.Add(new Vector2Int(position.x, position.y - 1)); // Valid if not at the bottom row
                    break;
                case AdjacentPosition.East:
                    if (position.x < 2) positionsToAdjust.Add(new Vector2Int(position.x + 1, position.y)); // Valid if not at the far right column
                    break;
                case AdjacentPosition.West:
                    if (position.x > 0) positionsToAdjust.Add(new Vector2Int(position.x - 1, position.y)); // Valid if not at the far left column
                    break;
                case AdjacentPosition.All:
                    if (position.y < 2) positionsToAdjust.Add(new Vector2Int(position.x, position.y + 1)); // North
                    if (position.y > 0) positionsToAdjust.Add(new Vector2Int(position.x, position.y - 1)); // South
                    if (position.x < 2) positionsToAdjust.Add(new Vector2Int(position.x + 1, position.y)); // East
                    if (position.x > 0) positionsToAdjust.Add(new Vector2Int(position.x - 1, position.y)); // West
                    break;
            }
        }

        // Log adjacent positions for debugging
        LogAdjacentPositions(positionsToAdjust);

        // Apply the power change to the selected adjacent positions
        foreach (var pos in positionsToAdjust)
        {
            ApplyPowerChange(pos);
        }
    }

    // Modified GetCardPosition to use the grid coordinates directly
    private Vector2Int GetCardPosition(CardUI card)
    {
        CardSO[,] grid = GridManager.instance.GetGrid();
        GameObject[,] gridObjects = GridManager.instance.GetGridObjects();

        // Search through the grid to find the position of the card
        for (int x = 0; x < grid.GetLength(0); x++)
        {
            for (int y = 0; y < grid.GetLength(1); y++)
            {
                if (gridObjects[x, y] == card.gameObject)
                {
                    return new Vector2Int(x, y); // Return grid position of the card
                }
            }
        }

        Debug.LogError($"Card {card.cardData.cardName} not found in grid!");
        return Vector2Int.zero; // Default value in case the card is not found in the grid
    }

    private void ApplyPowerChange(Vector2Int position)
    {
        CardSO[,] grid = GridManager.instance.GetGrid();
        GameObject[,] gridObjects = GridManager.instance.GetGridObjects();

        // Check if the position is within bounds and if a card exists in that position
        if (position.x >= 0 && position.x < grid.GetLength(0) && position.y >= 0 && position.y < grid.GetLength(1))
        {
            if (grid[position.x, position.y] != null)
            {
                CardUI cardUI = gridObjects[position.x, position.y].GetComponent<CardUI>();
                if (cardUI != null)
                {
                    // Log the current power before applying the change
                    Debug.Log($"Current power of card at ({position.x},{position.y}): {cardUI.currentPower}");

                    // Apply the power change based on the type (Increase or Decrease)
                    int newPower;
                    if (powerChangeType == PowerChangeType.Increase)
                    {
                        newPower = cardUI.currentPower + powerChangeAmount;
                    }
                    else
                    {
                        newPower = cardUI.currentPower - powerChangeAmount;
                    }

                    // Update the card's power
                    cardUI.UpdatePower(newPower);

                    // Log the updated power
                    Debug.Log($"Updated power of card at ({position.x},{position.y}): {cardUI.currentPower}");
                }
            }
        }
        else
        {
            Debug.LogWarning($"Position {position} is out of bounds, cannot apply power change.");
        }
    }

    // Debugging helper methods
    private void LogAdjacentPositions(List<Vector2Int> positions)
    {
        Debug.Log($"Adjacent positions to adjust: {string.Join(", ", positions.Select(p => $"({p.x},{p.y})"))}");
    }

    private void LogCardDetails(CardUI card)
    {
        Debug.Log($"Card Details: " +
                  $"Name: {card.cardData.cardName}, " +
                  $"Current Power: {card.currentPower}, " +
                  $"Power Change Type: {powerChangeType}, " +
                  $"Power Change Amount: {powerChangeAmount}");
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        // Optional: Implement if you want to reverse the effect when needed
        // This could subtract the power change instead of adding/subtracting
    }
}