using System.Collections.Generic;
using UnityEngine;

public class SacrificeManager : MonoBehaviour
{
    public static SacrificeManager instance;

    // The evolution card (CardUI) that requires sacrifices.
    private CardUI currentEvolutionCard;
    // Expose the evolving card via a public property.
    public CardUI CurrentEvolutionCard
    {
        get { return currentEvolutionCard; }
    }

    // Number of sacrifices required.
    private int requiredSacrifices;
    // List of selected sacrifice cards.
    private List<GameObject> selectedSacrifices = new List<GameObject>();

    // Flag indicating sacrifice selection mode is active.
    public bool isSelectingSacrifices = false;

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

        // Activate sacrifice selection mode.
        isSelectingSacrifices = true;

        if (GridManager.instance != null)
            GridManager.instance.HighlightEligibleSacrifices(currentEvolutionCard);
        else
            Debug.LogError("StartSacrificeSelection: GridManager.instance is null!");

        Debug.Log($"[SacrificeManager] Activated sacrifice mode for {evoCard.cardData.cardName}. Requires {requiredSacrifices} sacrifices.");
    }

    // Called when a valid sacrifice card is clicked.
    public AudioClip sacrificeSelectSound; // Assign via Inspector.
    public GameObject sacrificeSelectFloatingTextPrefab; // A prefab for floating text, assign via Inspector.

    public void SelectSacrifice(GameObject sacrificeCard)
    {
        if (sacrificeCard == null)
        {
            Debug.LogError("SelectSacrifice: sacrificeCard is null!");
            return;
        }

        Debug.Log("SelectSacrifice called for: " + sacrificeCard.name); // Debug log to check if method is triggered

        if (!selectedSacrifices.Contains(sacrificeCard))
        {
            selectedSacrifices.Add(sacrificeCard);
            Debug.Log("[SacrificeManager] Selected sacrifice: " + sacrificeCard.name);
            CardHandler handler = sacrificeCard.GetComponent<CardHandler>();
            if (handler != null)
            {
                // Mark this card as selected so it can show hover effects.
                CardUI ui = handler.GetComponent<CardUI>();
                if (ui != null)
                {
                    ui.isSacrificeSelected = true;
                    ui.ApplySacrificeHoverEffect();
                }
                handler.ShowSacrificePopup();
            }

            // Play a selection sound at the card's position.
            if (sacrificeSelectSound != null)
            {
                Debug.Log("Playing selection sound at: " + Camera.main.transform.position); // Debug log to confirm
                AudioSource.PlayClipAtPoint(sacrificeSelectSound, Camera.main.transform.position);
            }


            // Instantiate floating text as a child of the sacrifice card.
            if (sacrificeSelectFloatingTextPrefab != null)
            {
                GameObject ftObj = Instantiate(sacrificeSelectFloatingTextPrefab, sacrificeCard.transform.position, Quaternion.identity, sacrificeCard.transform);
                TMPro.TextMeshProUGUI tmp = ftObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmp != null)
                    tmp.text = "Selected!";
            }
        }

        // Check if we've reached the required number of sacrifices.
        if (selectedSacrifices.Count >= requiredSacrifices)
        {
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
        // 1. Find the grid coordinates of the FIRST sacrifice card (if any)
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
                    break;  // Break the inner loop
                }
            }
            if (targetX != -1)
                break;  // Break the outer loop if coordinates are found
        }

        Debug.Log($"[SacrificeManager] Coordinates of first sacrifice: ({targetX}, {targetY})");

        // 2. Remove each selected sacrifice, handling both field and hand cases.
        foreach (GameObject sacrifice in selectedSacrifices)
        {
            bool isOnField = false;
            GameObject[,] gridArray = GridManager.instance.GetGridObjects();
            for (int gx = 0; gx < 3; gx++)
            {
                for (int gy = 0; gy < 3; gy++)
                {
                    if (gridArray[gx, gy] == sacrifice)
                    {
                        GridManager.instance.RemoveCard(gx, gy, false);
                        isOnField = true;
                        break;
                    }
                }
                if (isOnField)
                    break;
            }

            if (!isOnField)
            {
                // The card is not on the grid, so it must be in the player's hand.
                CardHandler handler = sacrifice.GetComponent<CardHandler>();
                if (handler != null && handler.cardOwner != null)
                {
                    // Reset the sacrifice hover effect and remove any selection visuals.
                    CardUI ui = handler.GetComponent<CardUI>();
                    if (ui != null)
                        ui.ResetSacrificeHoverEffect();

                    handler.HideSacrificeHighlight();
                    handler.cardOwner.zones.AddCardToGrave(sacrifice);
                    handler.cardOwner.cardHandlers.Remove(handler);
                    Debug.Log($"[SacrificeManager] Removed {handler.cardData.cardName} from the hand.");
                }
            }
        }

        // 3. If we haven't found a valid grid cell from a sacrificed card, let the player choose one.
        if (targetX == -1 || targetY == -1)
        {
            Debug.Log("[SacrificeManager] No sacrifice on the grid was found. Enabling cell selection mode for evolution placement.");
            GridManager.instance.EnableCellSelectionMode(OnCellSelected);
            return; // Wait for player input.
        }

        // 4. Otherwise, place the evolution card at the freed cell.
        GridManager.instance.PerformEvolutionAtCoords(currentEvolutionCard, targetX, targetY);

        // 5. Check for a win condition after placing the evolved card
        // This checks if there's any new winning line after placing the card
        int newLines = WinChecker.instance.CheckWinCondition(GridManager.instance.GetGrid());
        if (newLines > 0)
        {
            Debug.Log($"[WinChecker] New winning lines formed: {newLines}");
            // Trigger win handling logic here
        }

        // Deactivate sacrifice selection mode.
        isSelectingSacrifices = false;
    }


    // Callback invoked when the player selects an empty grid cell.
    private void OnCellSelected(int selectedX, int selectedY)
    {
        GridManager.instance.DisableCellSelectionMode();
        GridManager.instance.ClearSacrificeHighlights();

        Debug.Log($"[SacrificeManager] Player selected cell: ({selectedX}, {selectedY})");

        GridManager.instance.PerformEvolutionAtCoords(currentEvolutionCard, selectedX, selectedY);

        isSelectingSacrifices = false;
    }

    // Optionally, a method to cancel sacrifice selection.
    public void CancelSacrificeSelection()
    {
        if (GridManager.instance != null)
            GridManager.instance.ClearSacrificeHighlights();
        selectedSacrifices.Clear();
        currentEvolutionCard = null;
        isSelectingSacrifices = false;
        Debug.Log("[SacrificeManager] Sacrifice selection cancelled.");
    }

    // New method to check if a card is a valid sacrifice for the current evolution.
    public bool IsValidSacrifice(CardUI card)
    {
        if (currentEvolutionCard == null || card == null || card.cardData == null)
            return false;

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
