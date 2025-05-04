using System;
using System.Collections.Generic;
using UnityEngine;

public class GraveyardSelectionManager : MonoBehaviour
{
    public static GraveyardSelectionManager Instance;

    [Header("Prefab Reference")]
    public GameObject graveyardDisplayPanelPrefab; // ← assign this in Inspector

    private GraveyardDisplayManager displayPanelInstance;

    private Action<List<GameObject>> onSelectionComplete;
    private List<GameObject> currentTargets = new List<GameObject>();
    private List<GameObject> selectedCards = new List<GameObject>();
    private int maxSelections = 1;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    public bool IsSelecting => onSelectionComplete != null;

    public void StartGraveyardSelection(List<GameObject> targets, int max, Action<List<GameObject>> callback)
    {
        Debug.Log("📣 GraveyardSelectionManager: Selection mode is now ACTIVE.");

        if (targets == null || targets.Count == 0)
        {
            Debug.LogWarning("GraveyardSelectionManager: No valid targets provided for selection.");
            return;
        }

        currentTargets = targets;
        selectedCards.Clear();
        maxSelections = Mathf.Max(1, max);
        onSelectionComplete = callback;

        Debug.Log($"🪦 Starting graveyard selection for {maxSelections} card(s).");

        if (displayPanelInstance == null)
        {
            if (graveyardDisplayPanelPrefab == null)
            {
                Debug.LogError("❌ GraveyardDisplayPanel prefab is not assigned!");
                return;
            }

            GameObject panelGO = Instantiate(graveyardDisplayPanelPrefab);
            panelGO.SetActive(true);

            GameObject overlayCanvas = GameObject.Find("OverlayCanvas");
            if (overlayCanvas != null)
                panelGO.transform.SetParent(overlayCanvas.transform, false);

            displayPanelInstance = panelGO.GetComponent<GraveyardDisplayManager>();
            if (displayPanelInstance == null)
            {
                Debug.LogError("❌ Instantiated prefab is missing GraveyardDisplayManager component!");
                return;
            }
        }

        var owner = targets[0].GetComponent<CardHandler>()?.cardOwner;
        if (owner != null)
        {
            displayPanelInstance.ShowGraveyard(owner.zones.GetGraveyardCards());
            displayPanelInstance.HighlightSelectableCards(currentTargets);
        }
        else
        {
            Debug.LogError("❌ Target card has no CardHandler or owner!");
        }
    }

    public void CancelSelection()
    {
        Debug.Log("📴 GraveyardSelectionManager: Selection mode cleared.");

        currentTargets.Clear();
        selectedCards.Clear();
        onSelectionComplete = null;
        maxSelections = 0;

        if (displayPanelInstance != null)
            displayPanelInstance.ClearHighlights();
    }

    public void SelectCard(GameObject card)
    {
        if (!currentTargets.Contains(card)) return;

        Debug.Log($"✅ GraveyardSelectionManager: Card clicked - {card.name}");

        if (maxSelections == 1)
        {
            CompleteSelection(new List<GameObject> { card });
        }
        else
        {
            if (!selectedCards.Contains(card))
            {
                selectedCards.Add(card);
                card.GetComponent<CardUI>().ApplySacrificeHoverEffect();
            }

            if (selectedCards.Count >= maxSelections)
            {
                CompleteSelection(new List<GameObject>(selectedCards));
            }
        }
    }

    private void CompleteSelection(List<GameObject> selected)
    {
        Debug.Log($"🎯 Selection complete. {selected.Count} card(s) selected.");
        onSelectionComplete?.Invoke(selected);
        CancelSelection();

        // ✅ Close the panel visually
        if (displayPanelInstance != null)
        {
            displayPanelInstance.gameObject.SetActive(false);
            Debug.Log("📪 Graveyard panel closed.");
        }
    }
}
