using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Card Effects/Restrict High Power Placement")]
public class RestrictHighPowerPlacementEffect : CardEffect
{
    [Tooltip("Cards with power above this threshold cannot be placed in the chosen cell during opponent's next turn.")]
    public int powerThreshold = 1400;

    // Static because only one Magnificent Garden effect is expected at a time.
    private static bool restrictionActive = false;
    private static int restrictedX, restrictedY;

    public override void ApplyEffect(CardUI sourceCard)
    {
        Debug.Log("[MagnificentGarden] Select a cell to restrict.");
        GridManager.instance.EnableCellSelectionMode((x, y) => OnCellSelected(x, y));
    }

    private void OnCellSelected(int x, int y)
    {
        Debug.Log($"[MagnificentGarden] Target cell selected at ({x},{y}).");

        // Highlight the chosen cell in red to show it's locked
        GameObject cellObj = GameObject.Find($"GridCell_{x}_{y}");
        if (cellObj)
        {
            var highlighter = cellObj.GetComponent<GridCellHighlighter>();
            highlighter?.SetPersistentHighlight(new Color(1f, 0f, 0f, 0.3f));
        }

        // Start the restriction timer
        GridManager.instance.StartCoroutine(RestrictionCoroutine(x, y));
    }

    private IEnumerator RestrictionCoroutine(int x, int y)
    {
        int localPlayer = TurnManager.instance.localPlayerNumber;

        // Wait for opponent's turn to start
        yield return new WaitUntil(() => TurnManager.instance.GetCurrentPlayer() != localPlayer);

        restrictionActive = true;
        restrictedX = x;
        restrictedY = y;
        Debug.Log($"[MagnificentGarden] Restriction ACTIVE on cell ({x},{y}).");

        // Wait for opponent's turn to end
        yield return new WaitUntil(() => TurnManager.instance.GetCurrentPlayer() == localPlayer);

        restrictionActive = false;
        Debug.Log($"[MagnificentGarden] Restriction EXPIRED on cell ({x},{y}).");

        // Remove highlight after restriction ends
        GameObject cellObj = GameObject.Find($"GridCell_{x}_{y}");
        if (cellObj)
        {
            var highlighter = cellObj.GetComponent<GridCellHighlighter>();
            highlighter?.ResetHighlight();
        }
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        restrictionActive = false;
    }

    public static bool IsRestrictedCell(int x, int y, CardSO cardData)
    {
        return restrictionActive &&
               x == restrictedX && y == restrictedY &&
               cardData.category == CardSO.CardCategory.Creature &&
               cardData.power > 1400; // Use threshold
    }
}
