using UnityEngine;

public class WinChecker : MonoBehaviour
{
    public static WinChecker instance;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    // Checks for three in a row (horizontal, vertical, or diagonal) based on card ownership.
    public bool CheckWinCondition(CardSO[,] grid)
    {
        // Get the grid objects from GridManager so we can check ownership.
        GameObject[,] gridObjs = GridManager.instance.GetGridObjects();

        // Check rows
        for (int i = 0; i < 3; i++)
        {
            if (grid[i, 0] != null && grid[i, 1] != null && grid[i, 2] != null)
            {
                bool owner0 = gridObjs[i, 0].GetComponent<CardHandler>().isAI;
                bool owner1 = gridObjs[i, 1].GetComponent<CardHandler>().isAI;
                bool owner2 = gridObjs[i, 2].GetComponent<CardHandler>().isAI;
                if (owner0 == owner1 && owner1 == owner2)
                {
                    Debug.Log($"Win detected on row {i} for {(owner0 ? "AI" : "Player")}");
                    return true;
                }
            }
        }

        // Check columns
        for (int j = 0; j < 3; j++)
        {
            if (grid[0, j] != null && grid[1, j] != null && grid[2, j] != null)
            {
                bool owner0 = gridObjs[0, j].GetComponent<CardHandler>().isAI;
                bool owner1 = gridObjs[1, j].GetComponent<CardHandler>().isAI;
                bool owner2 = gridObjs[2, j].GetComponent<CardHandler>().isAI;
                if (owner0 == owner1 && owner1 == owner2)
                {
                    Debug.Log($"Win detected on column {j} for {(owner0 ? "AI" : "Player")}");
                    return true;
                }
            }
        }

        // Check diagonal (top-left to bottom-right)
        if (grid[0, 0] != null && grid[1, 1] != null && grid[2, 2] != null)
        {
            bool owner0 = gridObjs[0, 0].GetComponent<CardHandler>().isAI;
            bool owner1 = gridObjs[1, 1].GetComponent<CardHandler>().isAI;
            bool owner2 = gridObjs[2, 2].GetComponent<CardHandler>().isAI;
            if (owner0 == owner1 && owner1 == owner2)
            {
                Debug.Log($"Win detected on diagonal (top-left to bottom-right) for {(owner0 ? "AI" : "Player")}");
                return true;
            }
        }

        // Check diagonal (top-right to bottom-left)
        if (grid[0, 2] != null && grid[1, 1] != null && grid[2, 0] != null)
        {
            bool owner0 = gridObjs[0, 2].GetComponent<CardHandler>().isAI;
            bool owner1 = gridObjs[1, 1].GetComponent<CardHandler>().isAI;
            bool owner2 = gridObjs[2, 0].GetComponent<CardHandler>().isAI;
            if (owner0 == owner1 && owner1 == owner2)
            {
                Debug.Log($"Win detected on diagonal (top-right to bottom-left) for {(owner0 ? "AI" : "Player")}");
                return true;
            }
        }

        return false;
    }
}
