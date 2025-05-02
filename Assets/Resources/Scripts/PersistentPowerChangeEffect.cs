using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Persistent Power Change")]
public class PersistentPowerChangeEffect : CardEffect
{
    [Tooltip("Include cards in hand as well as on field")]
    public bool includeHand = true;

    public enum TargetOwner { Yours, Opponent }
    [Tooltip("Which player's cards to affect")]
    public TargetOwner targetOwner = TargetOwner.Yours;

    public enum FilterMode { All, Type, NameContains }
    [Tooltip("Which cards to match")]
    public FilterMode filterMode = FilterMode.All;
    [Tooltip("Substring or type to match")]
    public string filterValue = "";

    [Tooltip("Amount to buff (+) or debuff (–)")]
    public int amount = 200;

    // Keep track so we can revert when the source is destroyed
    private readonly List<CardUI> _affected = new List<CardUI>();

    public override void ApplyEffect(CardUI sourceCard)
    {
        int local = TurnManager.instance.localPlayerNumber;
        var allTargets = new List<CardUI>();

        // (1) on-field
        foreach (var ui in Object.FindObjectsOfType<CardUI>())
        {
            if (!ui.isOnField) continue;
            var h = ui.GetComponent<CardHandler>();
            if (h == null) continue;
            bool isOwner = (targetOwner == TargetOwner.Yours)
                ? h.cardOwner.playerNumber == local
                : h.cardOwner.playerNumber != local;
            if (!isOwner) continue;
            if (filterMode == FilterMode.Type && ui.cardData.creatureType != filterValue) continue;
            if (filterMode == FilterMode.NameContains && !ui.cardData.cardName.Contains(filterValue)) continue;
            allTargets.Add(ui);
        }

        // (2) in-hand
        if (includeHand)
        {
            var pm = TurnManager.currentPlayerManager;
            foreach (var handler in pm.cardHandlers)
            {
                var ui = handler.GetComponent<CardUI>();
                if (ui == null || ui.isOnField) continue;
                bool isOwner = (targetOwner == TargetOwner.Yours)
                    ? handler.cardOwner.playerNumber == local
                    : handler.cardOwner.playerNumber != local;
                if (!isOwner) continue;
                if (filterMode == FilterMode.Type && ui.cardData.creatureType != filterValue) continue;
                if (filterMode == FilterMode.NameContains && !ui.cardData.cardName.Contains(filterValue)) continue;
                allTargets.Add(ui);
            }
        }

        // (3) apply
        foreach (var ui in allTargets.Distinct())
        {
            ui.temporaryBoost += amount;
            ui.UpdatePowerDisplay();
            _affected.Add(ui);
            Debug.Log($"[PersistentBuff] +{amount} to {ui.cardData.cardName}");
        }
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        // called automatically when Heart Mask Boy is removed from the grid
        foreach (var ui in _affected)
        {
            if (ui == null) continue;
            ui.temporaryBoost -= amount;
            ui.UpdatePowerDisplay();
            Debug.Log($"[PersistentBuff-End] –{amount} from {ui.cardData.cardName}");
        }
        _affected.Clear();
    }
}
