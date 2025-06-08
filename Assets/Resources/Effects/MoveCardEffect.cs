using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Move Card")]
public class MoveCardEffect : CardEffect
{
    public enum FilterMode { SpecificName, NameContains, Type, YoursOnly, OpponentOnly, All }

    [Header("General Filters")]
    public FilterMode filterMode = FilterMode.All;
    public string filterValue;

    [Header("Offsets Mode (Static Moves)")]
    public List<Vector2Int> offsets = new List<Vector2Int>();
    public bool onlyIfFree = true;

    [Header("Interactive Any-Destination Mode (Jump the Fence)")]
    public bool interactiveAnyDestination = false;
    public string allowedName1;
    public string allowedName2;

    [Header("Interactive Relative-to-Opponent Mode (The Jump!)")]
    public bool interactiveRelativeToOpponent = false;
    public bool mustBeYours = true;

    private readonly Color highlightColor = new Color(1f, 1f, 0f, .45f);   // yellow

    private static void ClearHighlights(IEnumerable<Vector2Int> cells)
    {
        foreach (var c in cells)
            GameObject.Find($"GridCell_{c.x}_{c.y}")
                      ?.GetComponent<GridCellHighlighter>()
                      ?.ResetHighlight();
    }

    private static void ResetCellHighlight(int x, int y)
    {
        GameObject.Find($"GridCell_{x}_{y}")
                  ?.GetComponent<GridCellHighlighter>()
                  ?.ResetHighlight();
    }

    public override void ApplyEffect(CardUI sourceCard)
    {
        var p = sourceCard.transform.parent.name.Split('_');
        if (p.Length < 3) return;
        int fx = int.Parse(p[1]), fy = int.Parse(p[2]);

        var grid = GridManager.instance.GetGrid();
        var objs = GridManager.instance.GetGridObjects();
        var go = objs[fx, fy];
        var ui = go?.GetComponent<CardUI>();
        var handle = go?.GetComponent<CardHandler>();
        if (ui == null || handle == null) return;

        int local = TurnManager.instance.localPlayerNumber;

        if (interactiveAnyDestination)
        {
            if (ui.cardData.cardName != allowedName1 && ui.cardData.cardName != allowedName2)
                return;

            handle.ShowSacrificeHighlight();

            var empties = new List<Vector2Int>();
            for (int x = 0; x < 3; ++x)
                for (int y = 0; y < 3; ++y)
                    if (grid[x, y] == null)
                    {
                        empties.Add(new Vector2Int(x, y));
                        GameObject.Find($"GridCell_{x}_{y}")
                                   ?.GetComponent<GridCellHighlighter>()
                                   ?.SetPersistentHighlight(highlightColor);
                    }
            if (empties.Count == 0) return;

            GridManager.instance.EnableCellSelectionMode((sx, sy) =>
            {
                var dest = new Vector2Int(sx, sy);
                if (!empties.Contains(dest)) return;

                ResetCellHighlight(fx, fy); // remove old highlight
                GridManager.instance.MoveCardOnBoard(fx, fy, sx, sy, go);
                ClearHighlights(empties);   // remove all yellow highlights
            });
            return;
        }

        if (interactiveRelativeToOpponent)
        {
            if (mustBeYours && handle.cardOwner.playerNumber != local)
                return;

            var valids = new List<Vector2Int>();

            for (int x = 0; x < 3; ++x)
                for (int y = 0; y < 3; ++y)
                {
                    var opp = objs[x, y]?.GetComponent<CardHandler>();
                    if (opp == null || opp.cardOwner.playerNumber == local) continue;

                    if (y + 1 < 3 && grid[x, y + 1] == null) valids.Add(new Vector2Int(x, y + 1));
                    if (y - 1 >= 0 && grid[x, y - 1] == null) valids.Add(new Vector2Int(x, y - 1));
                }
            if (valids.Count == 0) return;

            foreach (var d in valids)
                GameObject.Find($"GridCell_{d.x}_{d.y}")
                           ?.GetComponent<GridCellHighlighter>()
                           ?.SetPersistentHighlight(highlightColor);

            GridManager.instance.EnableCellSelectionMode((sx, sy) =>
            {
                var chosen = new Vector2Int(sx, sy);
                if (!valids.Contains(chosen)) return;

                ResetCellHighlight(fx, fy);
                GridManager.instance.MoveCardOnBoard(fx, fy, sx, sy, go);
                ClearHighlights(valids);
            });
            return;
        }

        bool ownerOK = filterMode switch
        {
            FilterMode.YoursOnly => handle.cardOwner.playerNumber == local,
            FilterMode.OpponentOnly => handle.cardOwner.playerNumber != local,
            _ => true
        };
        if (!ownerOK) return;
        if (filterMode == FilterMode.SpecificName && ui.cardData.cardName != filterValue) return;
        if (filterMode == FilterMode.NameContains && !ui.cardData.cardName.Contains(filterValue)) return;
        if (filterMode == FilterMode.Type && ui.cardData.creatureType != filterValue) return;

        foreach (var off in offsets)
        {
            int tx = fx + off.x, ty = fy + off.y;
            if (tx < 0 || tx >= 3 || ty < 0 || ty >= 3) continue;
            if (onlyIfFree && grid[tx, ty] != null) continue;

            ResetCellHighlight(fx, fy);
            GridManager.instance.MoveCardOnBoard(fx, fy, tx, ty, go);
            return;
        }
    }

    public override void RemoveEffect(CardUI sourceCard) { }
}
