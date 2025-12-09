using UnityEngine;
using System.Collections;
using System.Collections.Generic;   // ← add this

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

        var selectableCells = new List<Vector2Int>();

        // Highlight ALL 3×3 cells as selectable for this spell
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                selectableCells.Add(new Vector2Int(x, y));

                GameObject cellObj = GameObject.Find($"GridCell_{x}_{y}");
                if (cellObj == null) continue;

                var highlighter = cellObj.GetComponent<GridCellHighlighter>();
                if (highlighter != null)
                {
                    // Temporarily override any stored highlight
                    highlighter.ClearStoredPersistentHighlight();
                    highlighter.SetPersistentHighlight(new Color(1f, 1f, 0f, 0.5f)); // yellow
                    highlighter.isSacrificeHighlight = true;
                }
            }
        }

        // This method will add click listeners and a Cancel button
        GridManager.instance.EnableClickSelectionForCells(
            selectableCells,
            (x, y) => OnCellSelected(x, y)
        );
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

        // 🔗 Show chain icon on this cell
        GridManager.instance.ShowChainIcon(x, y);

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

        // 🔓 Remove chain icon
        GridManager.instance.HideChainIcon(x, y);
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
               cardData.power >= 1400; // block 1400 and higher
    }
}
