using System.Collections.Generic;
using UnityEngine;

public class SacrificeManager : MonoBehaviour
{
    public static SacrificeManager instance;

    private CardUI currentEvolutionCard;
    public CardUI CurrentEvolutionCard => currentEvolutionCard;

    private int requiredSacrifices;
    private List<GameObject> selectedSacrifices = new List<GameObject>();

    public bool isSelectingSacrifices = false;

    public AudioClip sacrificeSelectSound;
    public GameObject sacrificeSelectFloatingTextPrefab;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

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

        // ✅ Fix: sum all sacrifice requirement counts
        requiredSacrifices = 0;
        foreach (var req in evoCard.cardData.sacrificeRequirements)
        {
            requiredSacrifices += req.count;
        }

        selectedSacrifices.Clear();
        isSelectingSacrifices = true;

        if (GridManager.instance != null)
            GridManager.instance.HighlightEligibleSacrifices(currentEvolutionCard);
        else
            Debug.LogError("StartSacrificeSelection: GridManager.instance is null!");

        Debug.Log($"[SacrificeManager] Activated sacrifice mode for {evoCard.cardData.cardName}. Requires {requiredSacrifices} sacrifices.");
    }


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

            var handler = sacrificeCard.GetComponent<CardHandler>();
            if (handler != null)
            {
                var ui = handler.GetComponent<CardUI>();
                if (ui != null)
                {
                    ui.isSacrificeSelected = true;
                    ui.ApplySacrificeHoverEffect();
                }
                handler.ShowSacrificePopup();
            }

            if (sacrificeSelectSound != null)
                AudioSource.PlayClipAtPoint(sacrificeSelectSound, Camera.main.transform.position);

            if (sacrificeSelectFloatingTextPrefab != null)
            {
                var ftObj = Instantiate(sacrificeSelectFloatingTextPrefab, sacrificeCard.transform.position, Quaternion.identity, sacrificeCard.transform);
                var tmp = ftObj.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (tmp != null) tmp.text = "Selected!";
            }
        }

        // ✅ Only after the final required sacrifice is selected, proceed to confirmation
        if (selectedSacrifices.Count >= requiredSacrifices)
        {
            Debug.Log("[SacrificeManager] All sacrifices selected. Preparing to complete sacrifice.");
            Invoke(nameof(CompleteSacrificeSelection), 0.5f); // Optional short delay for animation
        }
    }

    private void ShowSacrificeConfirmation()
    {
        // Deprecated: no longer auto-invoked directly
        Debug.LogWarning("ShowSacrificeConfirmation is no longer used. Logic moved to SelectSacrifice().");
    }


    private void CompleteSacrificeSelection()
    {
        Debug.Log("[SacrificeManager] All required sacrifices selected. Completing sacrifice process...");
        isSelectingSacrifices = false;

        GameObject[,] gridObjects = GridManager.instance.GetGridObjects();
        GameObject firstGridSacrifice = null;
        int targetX = -1, targetY = -1;

        // 🔍 Loop through all selected sacrifices and:
        // (1) remove them
        // (2) store the first one that came from the field
        foreach (GameObject sacrifice in selectedSacrifices)
        {
            bool isFromGrid = false;
            for (int x = 0; x < 3; x++)
            {
                for (int y = 0; y < 3; y++)
                {
                    if (gridObjects[x, y] == sacrifice)
                    {
                        GridManager.instance.RemoveCard(x, y, false);
                        isFromGrid = true;

                        if (firstGridSacrifice == null)
                        {
                            firstGridSacrifice = sacrifice;
                            targetX = x;
                            targetY = y;
                        }

                        break;
                    }
                }
                if (isFromGrid) break;
            }

            if (!isFromGrid)
            {
                CardHandler handler = sacrifice.GetComponent<CardHandler>();
                if (handler != null && handler.cardOwner != null)
                {
                    CardUI ui = handler.GetComponent<CardUI>();
                    if (ui != null) ui.ResetSacrificeHoverEffect();
                    handler.HideSacrificeHighlight();
                    handler.cardOwner.zones.AddCardToGrave(sacrifice);
                    handler.cardOwner.cardHandlers.Remove(handler);
                    Debug.Log($"[SacrificeManager] Removed {handler.cardData.cardName} from hand.");
                }
            }
        }

        selectedSacrifices.Clear();

        if (firstGridSacrifice != null)
        {
            Debug.Log($"[SacrificeManager] Placing evolution card in same cell as grid-based sacrifice ({targetX},{targetY}).");
            GridManager.instance.PerformEvolutionAtCoords(currentEvolutionCard, targetX, targetY);
        }
        else
        {
            Debug.Log("[SacrificeManager] All sacrifices were from hand. Enabling cell selection mode.");
            GridManager.instance.EnableCellSelectionMode(OnCellSelected);
            return;
        }

        int newLines = WinChecker.instance.CheckWinCondition(GridManager.instance.GetGrid());
        if (newLines > 0)
        {
            Debug.Log($"[WinChecker] New winning lines formed: {newLines}");
        }
    }


    private void OnCellSelected(int selectedX, int selectedY)
    {
        GridManager.instance.DisableCellSelectionMode();
        GridManager.instance.ClearSacrificeHighlights();

        Debug.Log($"[SacrificeManager] Player selected cell: ({selectedX}, {selectedY})");
        GridManager.instance.PerformEvolutionAtCoords(currentEvolutionCard, selectedX, selectedY);
    }

    public void CancelSacrificeSelection()
    {
        if (GridManager.instance != null)
            GridManager.instance.ClearSacrificeHighlights();
        selectedSacrifices.Clear();
        currentEvolutionCard = null;
        isSelectingSacrifices = false;
        Debug.Log("[SacrificeManager] Sacrifice selection cancelled.");
    }

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
