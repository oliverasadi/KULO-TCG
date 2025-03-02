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
        if (evoCard == null)
        {
            Debug.LogError("StartSacrificeSelection: evoCard is null!");
            return;
        }
        if (evoCard.cardData == null)
        {
            Debug.LogError("StartSacrificeSelection: cardData is null in evoCard!");
            return;
        }
        if (evoCard.cardData.sacrificeRequirements == null || evoCard.cardData.sacrificeRequirements.Count == 0)
        {
            Debug.LogError("StartSacrificeSelection: No sacrifice requirements found in cardData!");
            return;
        }

        currentEvolutionCard = evoCard;
        requiredSacrifices = evoCard.cardData.sacrificeRequirements[0].count;
        selectedSacrifices.Clear();

        // Highlight eligible sacrifice cards on the board.
        if (GridManager.instance != null)
            GridManager.instance.HighlightEligibleSacrifices();
        else
            Debug.LogError("StartSacrificeSelection: GridManager.instance is null!");

        Debug.Log("[SacrificeManager] Sacrifice selection mode activated. Required sacrifices: " + requiredSacrifices);
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
            CompleteSacrificeSelection();
        }
    }

    private void CompleteSacrificeSelection()
    {
        // Remove each selected sacrifice from the board.
        foreach (GameObject sacrifice in selectedSacrifices)
        {
            if (GridManager.instance != null)
                GridManager.instance.RemoveSacrificeCard(sacrifice);
        }

        if (selectedSacrifices.Count > 0)
        {
            // Use the position of the first sacrificed card as the target position.
            Vector2 targetPos = selectedSacrifices[0].transform.position;
            if (GridManager.instance != null)
                GridManager.instance.PlaceEvolutionCard(currentEvolutionCard, targetPos);
            else
                Debug.LogError("CompleteSacrificeSelection: GridManager.instance is null!");
        }
        else
        {
            Debug.LogError("CompleteSacrificeSelection: No sacrifices selected!");
        }

        if (GridManager.instance != null)
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
}
