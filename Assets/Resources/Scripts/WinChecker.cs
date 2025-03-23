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

    private string GetOwnerTag(GameObject cardObj)
    {
        if (cardObj == null)
            return "Empty";
        CardHandler handler = cardObj.GetComponent<CardHandler>();
        if (handler == null)
            return "Empty";
        return handler.isAI ? "AI" : "Player";
    }

    /// <summary>
    /// Checks for *all* new winning lines on the board. Returns how many new lines were formed.
    /// </summary>
    public int CheckWinCondition(CardSO[,] grid)
    {
        GameObject[,] gridObjs = GridManager.instance.GetGridObjects();
        GameManager gm = GameManager.instance; // We use GameManager's arrays for unique win tracking

        int newlyFormedLines = 0;

        // Check rows.
        for (int i = 0; i < 3; i++)
        {
            if (grid[i, 0] == null || grid[i, 1] == null || grid[i, 2] == null)
            {
                Debug.Log($"[WinChecker] Row {i} is not fully occupied. Skipping.");
                continue;
            }

            string owner0 = GetOwnerTag(gridObjs[i, 0]);
            string owner1 = GetOwnerTag(gridObjs[i, 1]);
            string owner2 = GetOwnerTag(gridObjs[i, 2]);

            Debug.Log($"[WinChecker] Row {i}: {owner0}, {owner1}, {owner2}");

            if (owner0 != "Empty" && owner0 == owner1 && owner1 == owner2)
            {
                // If this row wasn't used before, mark it and increment newlyFormedLines
                if (!gm.rowUsed[i])
                {
                    gm.rowUsed[i] = true;
                    newlyFormedLines++;
                    Debug.Log($"[WinChecker] New line formed on row {i} for {owner0}");
                }
                else
                {
                    Debug.Log($"[WinChecker] Row {i} already used. Skipping.");
                }
            }
        }

        // Check columns.
        for (int j = 0; j < 3; j++)
        {
            if (grid[0, j] == null || grid[1, j] == null || grid[2, j] == null)
            {
                Debug.Log($"[WinChecker] Column {j} is not fully occupied. Skipping.");
                continue;
            }

            string owner0 = GetOwnerTag(gridObjs[0, j]);
            string owner1 = GetOwnerTag(gridObjs[1, j]);
            string owner2 = GetOwnerTag(gridObjs[2, j]);

            Debug.Log($"[WinChecker] Column {j}: {owner0}, {owner1}, {owner2}");

            if (owner0 != "Empty" && owner0 == owner1 && owner1 == owner2)
            {
                // If this column wasn't used before, mark it and increment newlyFormedLines
                if (!gm.colUsed[j])
                {
                    gm.colUsed[j] = true;
                    newlyFormedLines++;
                    Debug.Log($"[WinChecker] New line formed on column {j} for {owner0}");
                }
                else
                {
                    Debug.Log($"[WinChecker] Column {j} already used. Skipping.");
                }
            }
        }

        // Check diagonal (top-left to bottom-right).
        if (grid[0, 0] != null && grid[1, 1] != null && grid[2, 2] != null)
        {
            string owner0 = GetOwnerTag(gridObjs[0, 0]);
            string owner1 = GetOwnerTag(gridObjs[1, 1]);
            string owner2 = GetOwnerTag(gridObjs[2, 2]);

            Debug.Log($"[WinChecker] Diagonal TL-BR: {owner0}, {owner1}, {owner2}");

            if (owner0 != "Empty" && owner0 == owner1 && owner1 == owner2)
            {
                if (!gm.diagUsed[0])
                {
                    gm.diagUsed[0] = true;
                    newlyFormedLines++;
                    Debug.Log($"[WinChecker] New line formed on diagonal (TL-BR) for {owner0}");
                }
                else
                {
                    Debug.Log("[WinChecker] Diagonal TL-BR already used. Skipping.");
                }
            }
        }
        else
        {
            Debug.Log("[WinChecker] Diagonal TL-BR is not fully occupied.");
        }

        // Check diagonal (top-right to bottom-left).
        if (grid[0, 2] != null && grid[1, 1] != null && grid[2, 0] != null)
        {
            string owner0 = GetOwnerTag(gridObjs[0, 2]);
            string owner1 = GetOwnerTag(gridObjs[1, 1]);
            string owner2 = GetOwnerTag(gridObjs[2, 0]);

            Debug.Log($"[WinChecker] Diagonal TR-BL: {owner0}, {owner1}, {owner2}");

            if (owner0 != "Empty" && owner0 == owner1 && owner1 == owner2)
            {
                if (!gm.diagUsed[1])
                {
                    gm.diagUsed[1] = true;
                    newlyFormedLines++;
                    Debug.Log($"[WinChecker] New line formed on diagonal (TR-BL) for {owner0}");
                }
                else
                {
                    Debug.Log("[WinChecker] Diagonal TR-BL already used. Skipping.");
                }
            }
        }
        else
        {
            Debug.Log("[WinChecker] Diagonal TR-BL is not fully occupied.");
        }

        return newlyFormedLines;
    }
}
