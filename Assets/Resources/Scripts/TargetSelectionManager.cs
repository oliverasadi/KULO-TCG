using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TargetSelectionManager : MonoBehaviour
{
    public static TargetSelectionManager Instance;

    // Whether we're currently selecting targets.
    public bool IsSelectingTargets = false;

    // The active boost effect (runtime instance).
    public MultipleTargetPowerBoostEffect CurrentBoostEffect;

    // Maximum number of targets allowed.
    public int MaxTargets = 3;

    // Reference to the UI prefab for target selection.
    public GameObject targetSelectionUIPrefab;

    private GameObject targetSelectionUIInstance;
    private TextMeshProUGUI targetSelectionText;
    private Button completeButton;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // Call this to start target selection mode.
    public void StartTargetSelection(MultipleTargetPowerBoostEffect boostEffect)
    {
        CurrentBoostEffect = boostEffect;
        if (CurrentBoostEffect.targetCards == null)
            CurrentBoostEffect.targetCards = new List<CardUI>();
        else
            CurrentBoostEffect.targetCards.Clear();

        IsSelectingTargets = true;
        Debug.Log("Target selection started for Multiple Target Power Boost effect.");

        // Instantiate the target selection UI on the OverlayCanvas.
        if (targetSelectionUIPrefab != null)
        {
            var overlayCanvas = GameObject.Find("OverlayCanvas");
            if (overlayCanvas != null)
            {
                targetSelectionUIInstance = Instantiate(targetSelectionUIPrefab, overlayCanvas.transform);
                targetSelectionText = targetSelectionUIInstance.GetComponentInChildren<TextMeshProUGUI>();
                completeButton = targetSelectionUIInstance.GetComponentInChildren<Button>();
                if (completeButton != null)
                    completeButton.onClick.AddListener(FinalizeTargetSelection);
                UpdateTargetSelectionText();
            }
            else Debug.LogWarning("OverlayCanvas not found.");
        }
        else Debug.LogWarning("TargetSelectionUIPrefab not assigned in TargetSelectionManager!");
    }

    // Called when a board card is clicked during target selection.
    public void AddTarget(CardUI targetCard)
    {
        if (!IsSelectingTargets || CurrentBoostEffect == null)
            return;

        if (CurrentBoostEffect.targetCards.Contains(targetCard))
        {
            Debug.Log("Target already selected: " + targetCard.cardData.cardName);
            return;
        }

        if (CurrentBoostEffect.targetCards.Count < MaxTargets)
        {
            CurrentBoostEffect.targetCards.Add(targetCard);
            Debug.Log("Added target: " + targetCard.cardData.cardName + ". Total targets: " + CurrentBoostEffect.targetCards.Count);
            UpdateTargetSelectionText();
        }
        else
        {
            Debug.Log("Maximum targets reached.");
        }
    }

    // Update the on-screen text showing selected targets and their new power.
    private void UpdateTargetSelectionText()
    {
        if (targetSelectionText == null || CurrentBoostEffect == null)
            return;

        string text = "Select Targets:\n";
        foreach (var target in CurrentBoostEffect.targetCards)
        {
            int newPower = target.CalculateEffectivePower() + CurrentBoostEffect.powerIncrease;
            text += $"{target.cardData.cardName}: +{CurrentBoostEffect.powerIncrease} => {newPower}\n";
        }
        text += "Click 'Complete' when finished.";
        targetSelectionText.text = text;
    }

    // Called when the Complete button is clicked.
    public void FinalizeTargetSelection()
    {
        if (CurrentBoostEffect != null)
        {
            Debug.Log("Applying boost effect to selected targets.");
            CurrentBoostEffect.ApplyEffect(null);
        }

        // ─── CLEAR ALL CARD HIGHLIGHTS ────────────────────────────────────
        // This makes sure any pulsating "sacrifice" glow on cards is turned off.
        foreach (var handler in Object.FindObjectsOfType<CardHandler>())
        {
            handler.HideSacrificeHighlight();
        }
        // ─────────────────────────────────────────────────────────────────

        // Tear down the selection UI and reset state.
        IsSelectingTargets = false;
        CurrentBoostEffect?.targetCards?.Clear();
        if (targetSelectionUIInstance != null)
            Destroy(targetSelectionUIInstance);
    }

    // Cancel without applying.
    public void CancelTargetSelection()
    {
        Debug.Log("Target selection canceled.");
        IsSelectingTargets = false;
        CurrentBoostEffect?.targetCards?.Clear();

        // Also clear any lingering glows.
        foreach (var handler in Object.FindObjectsOfType<CardHandler>())
        {
            handler.HideSacrificeHighlight();
        }

        if (targetSelectionUIInstance != null)
            Destroy(targetSelectionUIInstance);
    }
}
