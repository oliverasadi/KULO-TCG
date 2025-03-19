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
    // This prefab should have a TextMeshProUGUI for instructions/target list
    // and a Button for "Complete".
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
            GameObject overlayCanvas = GameObject.Find("OverlayCanvas");
            if (overlayCanvas != null)
            {
                targetSelectionUIInstance = Instantiate(targetSelectionUIPrefab, overlayCanvas.transform);
                targetSelectionText = targetSelectionUIInstance.GetComponentInChildren<TextMeshProUGUI>();
                completeButton = targetSelectionUIInstance.GetComponentInChildren<Button>();
                if (completeButton != null)
                    completeButton.onClick.AddListener(FinalizeTargetSelection);
                UpdateTargetSelectionText();
            }
            else
            {
                Debug.LogWarning("OverlayCanvas not found.");
            }
        }
        else
        {
            Debug.LogWarning("TargetSelectionUIPrefab not assigned in TargetSelectionManager!");
        }
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
            // Calculate the new temporary power.
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
            // Apply the boost effect (this temporarily increases power).
            CurrentBoostEffect.ApplyEffect(null); // You can pass null if the source isn't needed.
        }
        IsSelectingTargets = false;
        if (targetSelectionUIInstance != null)
            Destroy(targetSelectionUIInstance);
    }

    public void CancelTargetSelection()
    {
        IsSelectingTargets = false;
        if (CurrentBoostEffect != null && CurrentBoostEffect.targetCards != null)
            CurrentBoostEffect.targetCards.Clear();
        if (targetSelectionUIInstance != null)
            Destroy(targetSelectionUIInstance);
        Debug.Log("Target selection canceled.");
    }
}
