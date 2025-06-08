using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TargetSelectionManager : MonoBehaviour
{
    public static TargetSelectionManager Instance;

    public bool IsSelectingTargets = false;
    public MultipleTargetPowerBoostEffect CurrentBoostEffect;

    public GameObject targetSelectionUIPrefab;

    private GameObject targetSelectionUIInstance;
    private TextMeshProUGUI targetSelectionText;
    private Button completeButton;

    private int maxSelectableTargets = 0;
    private bool autoCompleteOnMax = false;

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void StartTargetSelection(MultipleTargetPowerBoostEffect boostEffect)
    {
        CurrentBoostEffect = boostEffect;

        if (CurrentBoostEffect.targetCards == null)
            CurrentBoostEffect.targetCards = new List<CardUI>();
        else
            CurrentBoostEffect.targetCards.Clear();

        IsSelectingTargets = true;

        maxSelectableTargets = (boostEffect.maxTargets > 0) ? boostEffect.maxTargets : int.MaxValue;
        autoCompleteOnMax = (boostEffect.maxTargets > 0);

        Debug.Log($"[TargetSelectionManager] Target selection started (max: {maxSelectableTargets})");

        // Create UI
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

    public void AddTarget(CardUI targetCard)
    {
        if (!IsSelectingTargets || CurrentBoostEffect == null)
            return;

        if (CurrentBoostEffect.targetCards.Contains(targetCard))
        {
            Debug.Log("Target already selected: " + targetCard.cardData.cardName);
            return;
        }

        if (CurrentBoostEffect.targetCards.Count >= maxSelectableTargets)
        {
            Debug.Log("🚫 Maximum targets reached.");
            return;
        }

        CurrentBoostEffect.targetCards.Add(targetCard);
        Debug.Log("✅ Added target: " + targetCard.cardData.cardName);
        UpdateTargetSelectionText();

        if (autoCompleteOnMax && CurrentBoostEffect.targetCards.Count >= maxSelectableTargets)
        {
            Debug.Log("🎯 Max targets selected — auto-finalizing.");
            FinalizeTargetSelection();
        }
    }

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

        if (!autoCompleteOnMax)
            text += "Click 'Complete' when finished.";

        targetSelectionText.text = text;
    }

    public void FinalizeTargetSelection()
    {
        if (CurrentBoostEffect != null)
        {
            Debug.Log("✅ Applying boost effect to selected targets.");
            CurrentBoostEffect.ApplyEffect(null);
        }

        foreach (var handler in Object.FindObjectsOfType<CardHandler>())
            handler.HideSacrificeHighlight();

        IsSelectingTargets = false;
        CurrentBoostEffect?.targetCards?.Clear();

        if (targetSelectionUIInstance != null)
            Destroy(targetSelectionUIInstance);
    }

    public void CancelTargetSelection()
    {
        Debug.Log("⚠️ Target selection canceled.");
        IsSelectingTargets = false;
        CurrentBoostEffect?.targetCards?.Clear();

        foreach (var handler in Object.FindObjectsOfType<CardHandler>())
            handler.HideSacrificeHighlight();

        if (targetSelectionUIInstance != null)
            Destroy(targetSelectionUIInstance);
    }
}
