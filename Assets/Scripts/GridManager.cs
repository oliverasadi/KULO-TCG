using System;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public static GridManager instance;

    [Header("Grid Settings")]
    public GameObject cardPrefab;
    private CardSO[,] grid = new CardSO[3, 3]; // Stores placed cards
    public Vector2 gridOffset = new Vector2(-1.5f, -1.5f);

    [Header("Highlight System")]
    public GameObject highlightPrefab;
    private GameObject[,] highlightObjects = new GameObject[3, 3];

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        GenerateGridHighlights();
    }

    private void GenerateGridHighlights()
    {
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                Vector3 position = new Vector3(x + gridOffset.x, y + gridOffset.y, 0);
                highlightObjects[x, y] = Instantiate(highlightPrefab, position, Quaternion.identity);
                highlightObjects[x, y].SetActive(false); // ✅ Ensure they are initially hidden
            }
        }
    }

    public bool IsValidDropPosition(Vector3 dropPosition, out int x, out int y)
    {
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(dropPosition);
        worldPos.z = 0;

        x = Mathf.RoundToInt(worldPos.x - gridOffset.x);
        y = Mathf.RoundToInt(worldPos.y - gridOffset.y);

        return x >= 0 && x < 3 && y >= 0 && y < 3 && grid[x, y] == null;
    }

    public bool CanPlaceCard(int x, int y, CardSO card)
    {
        if (grid[x, y] == null) return true; // ✅ Space is empty

        // ✅ Compare power values: Allow replacing weaker cards
        return grid[x, y].power < card.power;
    }

    public void PlaceCard(int x, int y, CardSO card)
    {
        if (!CanPlaceCard(x, y, card))
        {
            Debug.Log($"❌ Cannot place {card.cardName} at {x},{y}. Stronger card exists.");
            return;
        }

        // ✅ Destroy existing card if needed
        if (grid[x, y] != null)
        {
            Debug.Log($"💥 Replacing {grid[x, y].cardName} at {x},{y}!");
        }

        // ✅ Place the new card
        Vector3 snapPosition = new Vector3(x + gridOffset.x, y + gridOffset.y, 0);
        GameObject cardObject = Instantiate(cardPrefab, snapPosition, Quaternion.identity);
        cardObject.GetComponent<CardHandler>().SetCard(card);
        grid[x, y] = card; // Store the new card

        GameManager.instance.CheckForWin();
    }

    public void ShowGridHighlights()
    {
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (grid[x, y] == null && highlightObjects[x, y] != null)
                {
                    highlightObjects[x, y].SetActive(true);
                }
            }
        }
    }

    public void HideGridHighlights()
    {
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (highlightObjects[x, y] != null)
                {
                    highlightObjects[x, y].SetActive(false);
                }
            }
        }
    }

    public void HideGridHighlightAt(int x, int y)
    {
        if (x >= 0 && x < 3 && y >= 0 && y < 3 && highlightObjects[x, y] != null)
        {
            highlightObjects[x, y].SetActive(false);
        }
    }

    public void ResetGrid()
    {
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                grid[x, y] = null;
            }
        }
        Debug.Log("🔄 Grid Reset!");
    }

    public CardSO[,] GetGrid()
    {
        return grid;
    }
}
