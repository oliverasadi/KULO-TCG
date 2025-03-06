using System.Collections.Generic;
using UnityEngine;

public class SacrificeManager : MonoBehaviour
{
    public static SacrificeManager instance;

    // The evolution card (CardUI) that requires sacrifices.
    private CardUI currentEvolutionCard;
    // Number of sacrifices required.
    private int requiredSacrifices;
    // List of selected sacrifice cards.
    private List<GameObject> selectedSacrifices = new List<GameObject>();

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    // Called when the player chooses to evolve a card.
    public void StartSacrificeSelection(CardUI evoCard)
    {
        if (evoCard == null || evoCard.cardData == null)
        {
            Debug.LogError("StartSacrificeSelection: evoCard or cardData is null!");
            return;
        }

        if (evoCard.cardData.sacrificeRequirements == null || evoCard.cardData.sacrificeRequirements.Count == 0)
        {
            Debug.LogError($"StartSacrificeSelection: {evoCard.cardData.cardName} has no sacrifice requirements.");
            return;
        }

        currentEvolutionCard = evoCard;
        requiredSacrifices = evoCard.cardData.sacrificeRequirements[0].count;
        selectedSacrifices.Clear();

        if (GridManager.instance != null)
            GridManager.instance.HighlightEligibleSacrifices(currentEvolutionCard);
        else
            Debug.LogError("StartSacrificeSelection: GridManager.instance is null!");

        Debug.Log($"[SacrificeManager] Activated sacrifice mode for {evoCard.cardData.cardName}. Requires {requiredSacrifices} sacrifices.");
    }

    // Called when a valid sacrifice card is clicked.
    public void SelectSacrifice(GameObject sacrificeCard)
    {
        if (sacrificeCard == null)
        {
            Debug.LogError("SelectSacrifice: sacrificeCard is null!");
            return;
        }

        if (!selectedSacrifices.Contains(sacrificeCard))
        {
            selectedSacrifices.Add(sacrificeCard);
            Debug.Log("[SacrificeManager] Selected sacrifice: " + sacrificeCard.name);
            CardHandler handler = sacrificeCard.GetComponent<CardHandler>();
            if (handler != null)
                handler.ShowSacrificePopup();
        }

        // Check if we've reached the required number of sacrifices.
        if (selectedSacrifices.Count >= requiredSacrifices)
        {
            // Instead of immediately completing the sacrifice, show a confirmation prompt.
            ShowSacrificeConfirmation();
        }
    }

    // Displays a confirmation prompt to the player.
    private void ShowSacrificeConfirmation()
    {
        Debug.Log("[SacrificeManager] Please confirm your sacrifice selection for " + currentEvolutionCard.cardData.cardName);
        // TODO: Replace the auto-confirm with your own confirmation UI logic.
        // For now, auto-confirm after 1 second.
        Invoke("CompleteSacrificeSelection", 1f);
    }

    private void CompleteSacrificeSelection()
    {
        // 1. Find the grid coordinates of the FIRST sacrifice card
        int targetX = -1, targetY = -1;
        GameObject firstSacrifice = selectedSacrifices[0];

        GameObject[,] gridObjs = GridManager.instance.GetGridObjects();
        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (gridObjs[x, y] == firstSacrifice)
                {
                    targetX = x;
                    targetY = y;
                    break;
                }
            }
            if (targetX != -1) break;
        }
        Debug.Log($"[SacrificeManager] Coordinates of first sacrifice: ({targetX}, {targetY})");

        // 2. Remove each selected sacrifice from the board
        foreach (GameObject sacrifice in selectedSacrifices)
        {
            GridManager.instance.RemoveSacrificeCard(sacrifice);
        }

        // 3. Place the evolution card at the freed cell
        if (targetX != -1 && targetY != -1)
        {
            // Use a new method in GridManager that places the card at (targetX, targetY).
            GridManager.instance.PerformEvolutionAtCoords(currentEvolutionCard, targetX, targetY);
        }
        else
        {
            Debug.LogError("[SacrificeManager] Could not find valid coordinates for the sacrifice. Evolution canceled.");
        }

        // Clear highlights, log, reset
        GridManager.instance.ClearSacrificeHighlights();
        Debug.Log("[SacrificeManager] Sacrifice selection complete. Evolution card summoned.");
        currentEvolutionCard = null;
        selectedSacrifices.Clear();
    }


    // Optionally, a method to cancel sacrifice selection.
    public void CancelSacrificeSelection()
    {
        if (GridManager.instance != null)
            GridManager.instance.ClearSacrificeHighlights();
        selectedSacrifices.Clear();
        currentEvolutionCard = null;
        Debug.Log("[SacrificeManager] Sacrifice selection cancelled.");
    }

    // New method to check if a card is a valid sacrifice for the current evolution.
    public bool IsValidSacrifice(CardUI card)
    {
        if (currentEvolutionCard == null || card == null || card.cardData == null)
        {
            return false;
        }

        foreach (var req in currentEvolutionCard.cardData.sacrificeRequirements)
        {
            bool match = req.matchByCreatureType
                ? (card.cardData.creatureType == req.requiredCardName)
                : (card.cardData.cardName == req.requiredCardName);

            if (match)
                return true;
        }

        return false;
    }
}
