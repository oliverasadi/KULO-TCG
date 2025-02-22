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
    public bool isHoldingCard;

    [Header("Audio Settings")] // ✅ Added Audio Fields
    public AudioSource audioSource;  // Drag & assign AudioSource in Inspector
    public AudioClip placeCardSound; // ✅ Sound when a card is placed
    public AudioClip removeCardSound; // ✅ Sound when a card is removed

    void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    public bool IsValidDropPosition(Vector2Int dropPosition, out int x, out int y)
    {
        x = dropPosition.x;
        y = dropPosition.y;
        return grid[x, y] == null;
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

        // ✅ Remove existing card if needed
        if (grid[x, y] != null)
        {
            Debug.Log($"💥 Replacing {grid[x, y].cardName} at {x},{y}!");
            RemoveCard(x, y); // ✅ Remove existing card
        }

        // ✅ Place the new card
        Vector3 snapPosition = new Vector3(x + gridOffset.x, y + gridOffset.y, 0);
        GameObject cardObject = Instantiate(cardPrefab, snapPosition, Quaternion.identity);
        cardObject.GetComponent<CardHandler>().SetCard(card);
        grid[x, y] = card; // Store the new card

        PlayCardPlaceSound(); // ✅ Play sound when a card is placed
        GameManager.instance.CheckForWin();
    }

    private void PlayCardPlaceSound()
    {
        if (audioSource != null && placeCardSound != null)
        {
            audioSource.PlayOneShot(placeCardSound); // ✅ Play sound effect
        }
        else
        {
            Debug.LogWarning("⚠️ No AudioSource or Sound Clip assigned in GridManager!");
        }
    }

    public void RemoveCard(int x, int y)
    {
        if (grid[x, y] != null)
        {
            Debug.Log($"🗑️ Removing {grid[x, y].cardName} at {x},{y}.");
            grid[x, y] = null;

            // ✅ Play remove sound
            if (audioSource != null && removeCardSound != null)
            {
                audioSource.PlayOneShot(removeCardSound);
            }
        }
    }

    public void GrabCard()
    {
        isHoldingCard = true;
    }

    public void ReleaseCard()
    {
        isHoldingCard = false;
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
