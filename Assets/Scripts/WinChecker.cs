using UnityEngine;

public class WinChecker : MonoBehaviour
{
public static WinChecker instance;

void Awake()
{
if (instance == null) instance = this;
else Destroy(gameObject);
}

public bool CheckWinCondition(CardSO[,] grid)
{
for (int i = 0; i < 3; i++)
{
if (CheckRow(grid, i) || CheckColumn(grid, i)) return true;
}
return CheckDiagonals(grid);
}

private bool CheckRow(CardSO[,] grid, int row)
{
return grid[row, 0] != null && grid[row, 1] != null && grid[row, 2] != null;
}

private bool CheckColumn(CardSO[,] grid, int col)
{
return grid[0, col] != null && grid[1, col] != null && grid[2, col] != null;
}

private bool CheckDiagonals(CardSO[,] grid)
{
return (grid[0, 0] != null && grid[1, 1] != null && grid[2, 2] != null) ||
(grid[0, 2] != null && grid[1, 1] != null && grid[2, 0] != null);
}
}