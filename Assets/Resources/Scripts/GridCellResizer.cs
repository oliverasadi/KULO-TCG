using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class GridCellResizer : MonoBehaviour
{
    public GridLayoutGroup grid;
    public int columns = 5;
    public float spacing = 20f;
    public float padding = 40f;

    void Update()
    {
        if (grid == null || grid.transform.parent == null) return;

        float parentWidth = ((RectTransform)grid.transform.parent).rect.width;

        // Total spacing between columns
        float totalSpacing = spacing * (columns - 1);

        // Total horizontal padding from the GridLayoutGroup
        float totalPadding = grid.padding.left + grid.padding.right;

        float availableWidth = parentWidth - totalSpacing - totalPadding;

        float cellWidth = availableWidth / columns;
        float cellHeight = cellWidth * 0.6f; // or whatever ratio fits your layout

        grid.cellSize = new Vector2(cellWidth, cellHeight);
    }

}
