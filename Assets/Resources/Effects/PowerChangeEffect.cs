using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Power Change")]
public class PowerChangeEffect : CardEffect
{
    [Tooltip("Only apply if you have fewer cards in hand than opponent (OGN-31)")]
    public bool requireFewerCardsInHand = false;

    [Tooltip("Include cards in hand as well as on field (for buffs like Perfected Soil)")]
    public bool includeHand = false;

    [Tooltip("Positive to buff, negative to debuff")]
    public int amount = 0;

    [Tooltip("Maximum number of cards to target (0 = no limit)")]
    public int maxTargets = 0; // 0 means no limit

    public enum TargetOwner { Yours, Opponent }
    [Tooltip("Which player's cards to affect")]
    public TargetOwner targetOwner = TargetOwner.Yours;

    public enum Mode
    {
        StaticFilter,           // Apply to all matching cards immediately
        InteractiveDropTarget,  // Highlight & click one card to target
        RelativeNS              // Drop on your card, then pick N/S opponent card
    }
    [Tooltip("How this effect chooses its targets")]
    public Mode mode = Mode.StaticFilter;

    public enum FilterMode { All, Type, NameContains }
    [Header("Filter (Static & InteractiveDropTarget)")]
    [Tooltip("Which cards match for static or drop-target modes")]
    public FilterMode filterMode = FilterMode.All;
    [Tooltip("Type name or substring to match when filtering")]
    public string filterValue = "";

    public enum Duration { ThisTurn, OpponentNextTurn, BothTurns, NextTwoTurns }
    [Header("Duration")]
    [Tooltip("How long the buff/debuff lasts")]
    public Duration duration = Duration.ThisTurn;

    private readonly List<CardUI> _affected = new List<CardUI>();

    // ──────────────────────────────────────────────────────────────────────────

    private PlayerManager GetOpponentPM()
    {
        var localPM = TurnManager.currentPlayerManager;
        return (localPM == TurnManager.instance.playerManager1)
            ? TurnManager.instance.playerManager2
            : TurnManager.instance.playerManager1;
    }

    public override void ApplyEffect(CardUI sourceCard)
    {
        if (requireFewerCardsInHand)
        {
            var localPM = TurnManager.currentPlayerManager;
            var oppPM = GetOpponentPM();

            int localHandCount = localPM.cardHandlers.Count(h =>
                h != null &&
                h.cardData != null &&
                h.GetComponent<CardUI>() is CardUI ui &&
                !ui.isOnField &&
                !ui.isInGraveyard);

            int oppHandCount = oppPM.cardHandlers.Count(h =>
                h != null &&
                h.cardData != null &&
                h.GetComponent<CardUI>() is CardUI ui &&
                !ui.isOnField &&
                !ui.isInGraveyard);

            Debug.Log($"[PowerChangeEffect] Cards in hand — You: {localHandCount}, Opponent: {oppHandCount}");

            if (localHandCount >= oppHandCount)
            {
                Debug.Log("[PowerChangeEffect] Condition not met: You don't have fewer cards in hand → skipping effect.");
                return;
            }
        }


        int local = TurnManager.instance.localPlayerNumber;
        var gridObjs = GridManager.instance.GetGridObjects();

        void ApplyTo(CardUI ui)
        {
            int before = ui.CalculateEffectivePower();
            ui.temporaryBoost += amount;
            ui.UpdatePowerDisplay();
            int after = ui.CalculateEffectivePower();

            Debug.Log($"[PowerChangeEffect] Applied {(amount >= 0 ? "+" : "")}{amount} to {ui.cardData.cardName} ({before} → {after})");

            if (!_affected.Contains(ui))
                _affected.Add(ui);
        }

        if (mode == Mode.StaticFilter && duration == Duration.NextTwoTurns)
        {
            sourceCard.StartCoroutine(BuffNextTwoTurns(sourceCard));
            return;
        }

        if (mode == Mode.StaticFilter)
        {
            if (duration == Duration.OpponentNextTurn)
            {
                sourceCard.StartCoroutine(ApplyOnOpponentThenExpire(sourceCard));
                return;
            }

            var allTargets = new List<CardUI>();

            foreach (var ui in Object.FindObjectsOfType<CardUI>())
            {
                if (!ui.isOnField) continue;
                var h = ui.GetComponent<CardHandler>(); if (h == null) continue;

                bool isOwner = (targetOwner == TargetOwner.Yours)
                    ? h.cardOwner.playerNumber == local
                    : h.cardOwner.playerNumber != local;
                if (!isOwner) continue;

                if (filterMode == FilterMode.Type && ui.cardData.creatureType != filterValue) continue;
                if (filterMode == FilterMode.NameContains && !ui.cardData.cardName.Contains(filterValue)) continue;

                allTargets.Add(ui);
            }

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

            var finalTargets = allTargets.Distinct().Take(maxTargets > 0 ? maxTargets : int.MaxValue).ToList();
            Debug.Log($"[PowerChangeEffect] Found {finalTargets.Count} valid targets.");

            foreach (var ui in finalTargets)
                ApplyTo(ui);

            if (duration == Duration.ThisTurn)
                sourceCard.StartCoroutine(ExpireAfterTurn(sourceCard));
            else if (duration == Duration.BothTurns)
                sourceCard.StartCoroutine(ExpireAfterBothTurns(sourceCard));
        }

        else if (mode == Mode.InteractiveDropTarget)
        {
            var candidates = new List<CardUI>();
            foreach (var ui in Object.FindObjectsOfType<CardUI>())
            {
                if (!ui.isOnField) continue;
                var h = ui.GetComponent<CardHandler>(); if (h == null) continue;

                bool isOwner = (targetOwner == TargetOwner.Yours)
                    ? h.cardOwner.playerNumber == local
                    : h.cardOwner.playerNumber != local;
                if (!isOwner) continue;

                if (filterMode == FilterMode.Type && ui.cardData.creatureType != filterValue) continue;
                if (filterMode == FilterMode.NameContains && !ui.cardData.cardName.Contains(filterValue)) continue;

                h.ShowSacrificeHighlight();
                candidates.Add(ui);
            }

            if (candidates.Count == 0)
            {
                Debug.Log("[PowerChangeEffect] No valid candidates for interactive selection.");
                return;
            }

            Debug.Log($"[PowerChangeEffect] Starting target selection for {candidates.Count} valid candidate(s).");

            var boostEffect = ScriptableObject.CreateInstance<MultipleTargetPowerBoostEffect>();
            boostEffect.powerIncrease = amount;
            boostEffect.maxTargets = this.maxTargets;
            boostEffect.targetCards = new List<CardUI>(); // will be populated by selection
            TargetSelectionManager.Instance.StartTargetSelection(boostEffect);
        }

        else if (mode == Mode.RelativeNS)
        {
            var parts = sourceCard.transform.parent.name.Split('_');
            int fx = int.Parse(parts[1]), fy = int.Parse(parts[2]);

            var valid = new List<CardUI>();
            for (int dy = -1; dy <= 1; dy += 2)
            {
                int ty = fy + dy;
                if (ty < 0 || ty >= 3) continue;
                var occ = gridObjs[fx, ty]; if (occ == null) continue;
                var ui = occ.GetComponent<CardUI>();
                var h = occ.GetComponent<CardHandler>();
                if (ui != null && h != null && h.cardOwner.playerNumber != local)
                    valid.Add(ui);
            }

            if (valid.Count == 0)
            {
                Debug.Log("[PowerChangeEffect] No valid RelativeNS targets found.");
                return;
            }

            foreach (var ui in valid)
                ui.GetComponent<CardHandler>()?.ShowSacrificeHighlight();

            var boostEffect = ScriptableObject.CreateInstance<MultipleTargetPowerBoostEffect>();
            boostEffect.powerIncrease = amount;
            boostEffect.maxTargets = this.maxTargets;
            boostEffect.targetCards = new List<CardUI>();
            TargetSelectionManager.Instance.StartTargetSelection(boostEffect);
        }
    }




    // ────────────────────────────────── expiry helpers ───────────────────────

    private IEnumerator ExpireAfterTurn(CardUI sourceCard)
    {
        int local = TurnManager.instance.localPlayerNumber;
        yield return new WaitUntil(() => TurnManager.instance.GetCurrentPlayer() != local);
        Restore();
    }

    private IEnumerator ExpireAfterBothTurns(CardUI sourceCard)
    {
        int local = TurnManager.instance.localPlayerNumber;
        yield return new WaitUntil(() => TurnManager.instance.GetCurrentPlayer() != local);
        yield return new WaitUntil(() => TurnManager.instance.GetCurrentPlayer() == local);
        Restore();
    }

    private IEnumerator ApplyOnOpponentThenExpire(CardUI sourceCard)
    {
        int local = TurnManager.instance.localPlayerNumber;

        // wait for opponent's turn
        yield return new WaitUntil(() => TurnManager.instance.GetCurrentPlayer() != local);

        // apply buff during opponent turn-start
        foreach (var ui in Object.FindObjectsOfType<CardUI>())
        {
            if (!ui.isOnField) continue;
            var h = ui.GetComponent<CardHandler>(); if (h == null) continue;

            bool isOwner = (targetOwner == TargetOwner.Yours)
                ? h.cardOwner.playerNumber == local
                : h.cardOwner.playerNumber != local;
            if (!isOwner) continue;

            if (filterMode == FilterMode.Type && ui.cardData.creatureType != filterValue) continue;
            if (filterMode == FilterMode.NameContains && !ui.cardData.cardName.Contains(filterValue)) continue;

            int before = ui.CalculateEffectivePower();
            ui.temporaryBoost += amount;
            ui.UpdatePowerDisplay();
            int after = ui.CalculateEffectivePower();

            Debug.Log($"[PowerBuff] {(amount >= 0 ? "+" : "")}{amount} → " +
                      $"{ui.cardData.cardName} ({before}→{after})");

            if (!_affected.Contains(ui))
                _affected.Add(ui);
        }

        // wait until control returns to local player, then remove it
        yield return new WaitUntil(() => TurnManager.instance.GetCurrentPlayer() == local);
        Restore();
    }

    private IEnumerator BuffNextTwoTurns(CardUI sourceCard)
    {
        int local = TurnManager.instance.localPlayerNumber;

        for (int i = 0; i < 2; i++)
        {
            // wait until local player's turn starts
            yield return new WaitUntil(() => TurnManager.instance.GetCurrentPlayer() == local);

            foreach (var ui in Object.FindObjectsOfType<CardUI>())
            {
                if (!includeHand && !ui.isOnField) continue;
                var h = ui.GetComponent<CardHandler>(); if (h == null) continue;

                bool isOwner = (targetOwner == TargetOwner.Yours)
                    ? h.cardOwner.playerNumber == local
                    : h.cardOwner.playerNumber != local;
                if (!isOwner) continue;

                if (filterMode == FilterMode.Type && ui.cardData.creatureType != filterValue) continue;
                if (filterMode == FilterMode.NameContains && !ui.cardData.cardName.Contains(filterValue)) continue;

                int before = ui.CalculateEffectivePower();
                ui.temporaryBoost += amount;
                ui.UpdatePowerDisplay();
                int after = ui.CalculateEffectivePower();

                Debug.Log($"[PowerBuff] {(amount >= 0 ? "+" : "")}{amount} → " +
                          $"{ui.cardData.cardName} ({before}→{after})");

                if (!_affected.Contains(ui))
                    _affected.Add(ui);
            }

            // wait for opponent's turn, then remove the boosts
            yield return new WaitUntil(() => TurnManager.instance.GetCurrentPlayer() != local);
            Restore();
        }
    }

    // ────────────────────────────────── un-buff helpers ──────────────────────

    private void Restore()
    {
        foreach (var ui in _affected)
        {
            if (ui == null) continue;

            ui.temporaryBoost -= amount;
            ui.UpdatePowerDisplay();

            Debug.Log($"[PowerBuff-End] {(amount >= 0 ? "-" : "+")}{Mathf.Abs(amount)} from " +
                      $"{ui.cardData.cardName} (now {ui.CalculateEffectivePower()})");
        }
        _affected.Clear();
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        foreach (var ui in _affected)
        {
            if (ui == null) continue;

            ui.temporaryBoost -= amount;
            ui.UpdatePowerDisplay();

            Debug.Log($"[PowerBuff-End] {(amount >= 0 ? "-" : "+")}{Mathf.Abs(amount)} from " +
                      $"{ui.cardData.cardName} (now {ui.CalculateEffectivePower()})");
        }
        _affected.Clear();
    }
}
