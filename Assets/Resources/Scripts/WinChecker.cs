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

    // Returns "AI" or "Player" based on the CardHandler's isAI property.
    // If cardObj is null or its CardHandler is missing, returns "Empty".
    private string GetOwnerTag(GameObject cardObj)
    {
        if (cardObj == null)
            return "Empty";
        CardHandler handler = cardObj.GetComponent<CardHandler>();
        if (handler == null)
            return "Empty";
        return handler.isAI ? "AI" : "Player";
    }

    // Checks for a win condition (three in a row, column, or diagonal) based on ownership.
    // It only considers a line if all three cells are occupied, and then verifies that
    // the line has not been previously used for a win.
    public bool CheckWinCondition(CardSO[,] grid)
    {
        GameObject[,] gridObjs = GridManager.instance.GetGridObjects();
        GameManager gm = GameManager.instance; // We use GameManager's arrays for unique win tracking

        // Check rows.
        for (int i = 0; i < 3; i++)
        {
            if (grid[i, 0] == null || grid[i, 1] == null || grid[i, 2] == null)
            {
                Debug.Log($"Row {i} is not fully occupied. Skipping win check for this row.");
                continue;
            }

            string owner0 = GetOwnerTag(gridObjs[i, 0]);
            string owner1 = GetOwnerTag(gridObjs[i, 1]);
            string owner2 = GetOwnerTag(gridObjs[i, 2]);

            Debug.Log($"Row {i}: {owner0}, {owner1}, {owner2}");

            if (owner0 != "Empty" && owner0 == owner1 && owner1 == owner2)
            {
                if (!gm.rowUsed[i])
                {
                    gm.rowUsed[i] = true;
                    Debug.Log($"Win detected on row {i} for {owner0}");
                    return true;
                }
                else
                {
                    Debug.Log($"Row {i} already used. Skipping.");
                }
            }
        }

        // Check columns.
        for (int j = 0; j < 3; j++)
        {
            if (grid[0, j] == null || grid[1, j] == null || grid[2, j] == null)
            {
                Debug.Log($"Column {j} is not fully occupied. Skipping win check for this column.");
                continue;
            }

            string owner0 = GetOwnerTag(gridObjs[0, j]);
            string owner1 = GetOwnerTag(gridObjs[1, j]);
            string owner2 = GetOwnerTag(gridObjs[2, j]);

            Debug.Log($"Column {j}: {owner0}, {owner1}, {owner2}");

            if (owner0 != "Empty" && owner0 == owner1 && owner1 == owner2)
            {
                if (!gm.colUsed[j])
                {
                    gm.colUsed[j] = true;
                    Debug.Log($"Win detected on column {j} for {owner0}");
                    return true;
                }
                else
                {
                    Debug.Log($"Column {j} already used. Skipping.");
                }
            }
        }

        // Check diagonal (top-left to bottom-right).
        if (grid[0, 0] != null && grid[1, 1] != null && grid[2, 2] != null)
        {
            string owner0 = GetOwnerTag(gridObjs[0, 0]);
            string owner1 = GetOwnerTag(gridObjs[1, 1]);
            string owner2 = GetOwnerTag(gridObjs[2, 2]);

            Debug.Log($"Diagonal TL-BR: {owner0}, {owner1}, {owner2}");

            if (owner0 != "Empty" && owner0 == owner1 && owner1 == owner2)
            {
                if (!gm.diagUsed[0])
                {
                    gm.diagUsed[0] = true;
                    Debug.Log($"Win detected on diagonal (TL-BR) for {owner0}");
                    return true;
                }
                else
                {
                    Debug.Log("Diagonal TL-BR already used. Skipping.");
                }
            }
        }
        else
        {
            Debug.Log("Diagonal TL-BR is not fully occupied.");
        }

        // Check diagonal (top-right to bottom-left).
        if (grid[0, 2] != null && grid[1, 1] != null && grid[2, 0] != null)
        {
            string owner0 = GetOwnerTag(gridObjs[0, 2]);
            string owner1 = GetOwnerTag(gridObjs[1, 1]);
            string owner2 = GetOwnerTag(gridObjs[2, 0]);

            Debug.Log($"Diagonal TR-BL: {owner0}, {owner1}, {owner2}");

            if (owner0 != "Empty" && owner0 == owner1 && owner1 == owner2)
            {
                if (!gm.diagUsed[1])
                {
                    gm.diagUsed[1] = true;
                    Debug.Log($"Win detected on diagonal (TR-BL) for {owner0}");
                    return true;
                }
                else
                {
                    Debug.Log("Diagonal TR-BL already used. Skipping.");
                }
            }
        }
        else
        {
            Debug.Log("Diagonal TR-BL is not fully occupied.");
        }

        return false;
    }
}
