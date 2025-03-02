using System.Collections.Generic;
using UnityEngine;

public class SacrificeManager : MonoBehaviour
{
    public static SacrificeManager instance;

    // The evolution card (CardUI) that is waiting for sacrifices.
    private CardUI currentEvolutionCard;
    // The number of sacrifices required.
    private int requiredSacrifices;
    // List to hold selected sacrifice cards.
    private List<GameObject> selectedSacrifices = new List<GameObject>();

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    // Called by the SummonMenu when the evolve option is chosen.
    public void StartSacrificeSelection(CardUI evoCard)
    {
        currentEvolutionCard = evoCard;
        // For simplicity, assume only one sacrifice requirement exists.
        requiredSacrifices = evoCard.cardData.sacrificeRequirements[0].count;
        selectedSacrifices.Clear();

        // Highlight eligible sacrifice cards on the board.
        GridManager.instance.HighlightEligibleSacrifices();
        Debug.Log("Sacrifice selection mode activated. Required sacrifices: " + requiredSacrifices);
    }

    // This method should be called when the player clicks on a valid sacrifice card on the board.
    public void SelectSacrifice(GameObject sacrificeCard)
    {
        if (!selectedSacrifices.Contains(sacrificeCard))
        {
            selectedSacrifices.Add(sacrificeCard);
            // Optionally, mark the card visually (e.g., add an outline).
            Debug.Log("Selected sacrifice: " + sacrificeCard.name);
        }

        // Update UI if needed (e.g., "1 of X selected").
        if (selectedSacrifices.Count >= requiredSacrifices)
        {
            CompleteSacrificeSelection();
        }
    }

    private void CompleteSacrificeSelection()
    {
        // Remove the selected sacrifice cards from the board.
        foreach (GameObject sacrifice in selectedSacrifices)
        {
            GridManager.instance.RemoveSacrificeCard(sacrifice);
        }

        // Use the position of the first sacrificed card as the target position for the evolution.
        Vector2 targetPos = selectedSacrifices[0].transform.position;

        // Place the evolved card at the target position.
        GridManager.instance.PlaceEvolutionCard(currentEvolutionCard, targetPos);

        // Clear any sacrifice highlights.
        GridManager.instance.ClearSacrificeHighlights();

        Debug.Log("Sacrifice selection complete. Evolution card summoned.");
        currentEvolutionCard = null;
        selectedSacrifices.Clear();
    }

    // Optionally, provide a method to cancel sacrifice selection.
    public void CancelSacrificeSelection()
    {
        GridManager.instance.ClearSacrificeHighlights();
        selectedSacrifices.Clear();
        currentEvolutionCard = null;
        Debug.Log("Sacrifice selection cancelled.");
    }
}
