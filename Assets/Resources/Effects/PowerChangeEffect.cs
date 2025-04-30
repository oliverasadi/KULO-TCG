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
        // (0) Hand-size gate ----------------------------------------------------
        if (requireFewerCardsInHand)
        {
            var localPM = TurnManager.currentPlayerManager;
            var oppPM = GetOpponentPM();
            if (localPM.cardHandlers.Count >= oppPM.cardHandlers.Count)
                return;
        }

        int local = TurnManager.instance.localPlayerNumber;
        var gridObjs = GridManager.instance.GetGridObjects();

        // ── helper that boosts + logs ─────────────────────────────────────────
        void ApplyTo(CardUI ui)
        {
            int before = ui.CalculateEffectivePower();
            ui.temporaryBoost += amount;
            ui.UpdatePowerDisplay();
            int after = ui.CalculateEffectivePower();

            Debug.Log($"[PowerBuff] {(amount >= 0 ? "+" : "")}{amount} → " +
                      $"{ui.cardData.cardName} ({before}→{after})");

            if (!_affected.Contains(ui))
                _affected.Add(ui);
        }

        // (1) NextTwoTurns scheduling -----------------------------------------
        if (mode == Mode.StaticFilter && duration == Duration.NextTwoTurns)
        {
            sourceCard.StartCoroutine(BuffNextTwoTurns(sourceCard));
            return;
        }

        // ─── STATIC FILTER MODE ───────────────────────────────────────────────
        if (mode == Mode.StaticFilter)
        {
            // (1a) OpponentNextTurn scheduling
            if (duration == Duration.OpponentNextTurn)
            {
                sourceCard.StartCoroutine(ApplyOnOpponentThenExpire(sourceCard));
                return;
            }

            // (1b) gather ALL matching cards (field + hand)
            var allTargets = new List<CardUI>();

            // on-field
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

            // hand
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

            // (1c) apply to each distinct target
            foreach (var ui in allTargets.Distinct())
                ApplyTo(ui);

            // (1d) schedule expiry
            if (duration == Duration.ThisTurn) sourceCard.StartCoroutine(ExpireAfterTurn(sourceCard));
            else if (duration == Duration.BothTurns) sourceCard.StartCoroutine(ExpireAfterBothTurns(sourceCard));
        }
        // ──────────────────────────────────────────────────────────────────────
        // ─── INTERACTIVE DROP TARGET MODE ─────────────────────────────────────
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
            if (candidates.Count == 0) return;

            var boostEffect = ScriptableObject.CreateInstance<MultipleTargetPowerBoostEffect>();
            boostEffect.powerIncrease = amount;
            boostEffect.targetCards = new List<CardUI>();
            TargetSelectionManager.Instance.StartTargetSelection(boostEffect);
            return;
        }
        // ─── RELATIVE N/S MODE ────────────────────────────────────────────────
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
            if (valid.Count == 0) return;
            foreach (var ui in valid)
                ui.GetComponent<CardHandler>()?.ShowSacrificeHighlight();

            var boostEffect = ScriptableObject.CreateInstance<MultipleTargetPowerBoostEffect>();
            boostEffect.powerIncrease = amount;
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
