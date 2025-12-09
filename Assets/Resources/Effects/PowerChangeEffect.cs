using System.Collections;
using System.Collections.Generic;
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
    [Tooltip("Which player's cards to affect (relative to the card's owner)")]
    public TargetOwner targetOwner = TargetOwner.Yours;

    public enum Mode
    {
        StaticFilter,           // Apply to all matching cards immediately
        InteractiveDropTarget,  // Highlight & click card(s) to target
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
    [Tooltip("How long the buff/debuff lasts (relative to the card's owner)")]
    public Duration duration = Duration.ThisTurn;

    private readonly List<CardUI> _affected = new List<CardUI>();

    // ──────────────────────────────────────────────────────────────────────────
    // Helpers to resolve who owns this effect
    // ──────────────────────────────────────────────────────────────────────────

    private bool TryGetOwners(CardUI sourceCard, out PlayerManager ownerPM, out PlayerManager oppPM)
    {
        ownerPM = null;
        oppPM = null;

        if (sourceCard == null)
        {
            Debug.LogError("[PowerChangeEffect] sourceCard is null.");
            return false;
        }

        var handler = sourceCard.GetComponent<CardHandler>();
        if (handler == null || handler.cardOwner == null)
        {
            Debug.LogError("[PowerChangeEffect] Could not resolve card owner from sourceCard.");
            return false;
        }

        ownerPM = handler.cardOwner;
        oppPM = (ownerPM == TurnManager.instance.playerManager1)
            ? TurnManager.instance.playerManager2
            : TurnManager.instance.playerManager1;

        return true;
    }

    // ──────────────────────────────────────────────────────────────────────────

    public override void ApplyEffect(CardUI sourceCard)
    {
        if (!TryGetOwners(sourceCard, out var ownerPM, out var oppPM))
            return;

        int ownerNumber = ownerPM.playerNumber;

        // Condition: owner must have fewer cards in hand than opponent
        if (requireFewerCardsInHand)
        {
            int ownerHand = CountRealHandCards(ownerPM);
            int oppHand = CountRealHandCards(oppPM);

            Debug.Log($"[PowerChangeEffect] Cards in hand — owner({ownerNumber})={ownerHand}, opp={oppHand}");

            if (ownerHand >= oppHand)
            {
                Debug.Log("[PowerChangeEffect] Condition not met (owner does not have fewer cards in hand) → skipping effect.");
                return;
            }
        }

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

        // Special case: NextTwoTurns – handled by its own coroutine
        if (mode == Mode.StaticFilter && duration == Duration.NextTwoTurns)
        {
            sourceCard.StartCoroutine(BuffNextTwoTurns(sourceCard, ownerNumber, ownerPM, oppPM));
            return;
        }

        // ── Static Filter (one-shot application) ──────────────────────────────
        if (mode == Mode.StaticFilter)
        {
            // OpponentNextTurn = deferred apply
            if (duration == Duration.OpponentNextTurn)
            {
                sourceCard.StartCoroutine(ApplyOnOpponentThenExpire(sourceCard, ownerNumber, ownerPM, oppPM));
                return;
            }

            var allTargets = new List<CardUI>();

            // Field targets
            foreach (var ui in Object.FindObjectsOfType<CardUI>())
            {
                if (!ui.isOnField) continue;
                var h = ui.GetComponent<CardHandler>(); if (h == null) continue;

                bool isOwnerSide = (targetOwner == TargetOwner.Yours)
                    ? (h.cardOwner == ownerPM)
                    : (h.cardOwner != ownerPM);
                if (!isOwnerSide) continue;

                if (filterMode == FilterMode.Type && ui.cardData.creatureType != filterValue) continue;
                if (filterMode == FilterMode.NameContains && !ui.cardData.cardName.Contains(filterValue)) continue;

                allTargets.Add(ui);
            }

            // Optional: include hand cards for whichever side is the target
            if (includeHand)
            {
                PlayerManager handTargetPM = (targetOwner == TargetOwner.Yours) ? ownerPM : oppPM;

                foreach (var handler in handTargetPM.cardHandlers)
                {
                    var ui = handler.GetComponent<CardUI>();
                    if (ui == null || ui.isOnField || ui.isInGraveyard) continue;

                    if (filterMode == FilterMode.Type && ui.cardData.creatureType != filterValue) continue;
                    if (filterMode == FilterMode.NameContains && !ui.cardData.cardName.Contains(filterValue)) continue;

                    allTargets.Add(ui);
                }
            }

            // Clamp by maxTargets (if set)
            int max = maxTargets > 0 ? maxTargets : int.MaxValue;
            int appliedCount = 0;

            Debug.Log($"[PowerChangeEffect] Found {allTargets.Count} raw targets for owner {ownerNumber} (max {maxTargets}).");

            foreach (var ui in allTargets)
            {
                if (appliedCount >= max) break;
                ApplyTo(ui);
                appliedCount++;
            }

            // Setup duration expiry
            if (duration == Duration.ThisTurn)
                sourceCard.StartCoroutine(ExpireAfterTurn(sourceCard, ownerNumber));
            else if (duration == Duration.BothTurns)
                sourceCard.StartCoroutine(ExpireAfterBothTurns(sourceCard, ownerNumber));
        }

        // ── Interactive target selection from board ───────────────────────────
        else if (mode == Mode.InteractiveDropTarget)
        {
            var candidates = new List<CardUI>();

            foreach (var ui in Object.FindObjectsOfType<CardUI>())
            {
                if (!ui.isOnField) continue;
                var h = ui.GetComponent<CardHandler>(); if (h == null) continue;

                bool isOwnerSide = (targetOwner == TargetOwner.Yours)
                    ? (h.cardOwner == ownerPM)
                    : (h.cardOwner != ownerPM);
                if (!isOwnerSide) continue;

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

            Debug.Log($"[PowerChangeEffect] Starting target selection for {candidates.Count} candidate(s).");

            var boostEffect = ScriptableObject.CreateInstance<MultipleTargetPowerBoostEffect>();
            boostEffect.powerIncrease = amount;
            boostEffect.maxTargets = this.maxTargets;
            boostEffect.targetCards = new List<CardUI>(); // filled by TargetSelectionManager
            TargetSelectionManager.Instance.StartTargetSelection(boostEffect);
        }

        // ── Relative N/S selection (used for certain debuffs/bursts) ─────────
        else if (mode == Mode.RelativeNS)
        {
            var parts = sourceCard.transform.parent.name.Split('_');
            if (parts.Length < 3) return;

            int fx = int.Parse(parts[1]);
            int fy = int.Parse(parts[2]);

            var gridObjs = GridManager.instance.GetGridObjects();
            var valid = new List<CardUI>();

            // Look north/south for opponent cards
            for (int dy = -1; dy <= 1; dy += 2)
            {
                int ty = fy + dy;
                if (ty < 0 || ty >= 3) continue;

                var occ = gridObjs[fx, ty];
                if (occ == null) continue;

                var ui = occ.GetComponent<CardUI>();
                var h = occ.GetComponent<CardHandler>();
                if (ui == null || h == null) continue;

                bool isOpponentCard = (h.cardOwner != ownerPM);
                if (isOpponentCard)
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

    private IEnumerator ExpireAfterTurn(CardUI sourceCard, int ownerNumber)
    {
        // Wait until it is no longer the owner's turn
        yield return new WaitUntil(() => TurnManager.instance.GetCurrentPlayer() != ownerNumber);
        Restore();
    }

    private IEnumerator ExpireAfterBothTurns(CardUI sourceCard, int ownerNumber)
    {
        // Wait for opponent's turn
        yield return new WaitUntil(() => TurnManager.instance.GetCurrentPlayer() != ownerNumber);
        // Then for owner's turn again
        yield return new WaitUntil(() => TurnManager.instance.GetCurrentPlayer() == ownerNumber);
        Restore();
    }

    private IEnumerator ApplyOnOpponentThenExpire(CardUI sourceCard, int ownerNumber, PlayerManager ownerPM, PlayerManager oppPM)
    {
        // Wait for opponent's turn to start
        yield return new WaitUntil(() => TurnManager.instance.GetCurrentPlayer() != ownerNumber);

        // Apply the buff/debuff at opponent turn start
        foreach (var ui in Object.FindObjectsOfType<CardUI>())
        {
            if (!ui.isOnField) continue;
            var h = ui.GetComponent<CardHandler>(); if (h == null) continue;

            bool isOwnerSide = (targetOwner == TargetOwner.Yours)
                ? (h.cardOwner == ownerPM)
                : (h.cardOwner != ownerPM);
            if (!isOwnerSide) continue;

            if (filterMode == FilterMode.Type && ui.cardData.creatureType != filterValue) continue;
            if (filterMode == FilterMode.NameContains && !ui.cardData.cardName.Contains(filterValue)) continue;

            int before = ui.CalculateEffectivePower();
            ui.temporaryBoost += amount;
            ui.UpdatePowerDisplay();
            int after = ui.CalculateEffectivePower();

            Debug.Log($"[PowerChangeEffect] (OppNextTurn) {(amount >= 0 ? "+" : "")}{amount} → {ui.cardData.cardName} ({before}→{after})");

            if (!_affected.Contains(ui))
                _affected.Add(ui);
        }

        // Wait until control returns to the owner, then remove it
        yield return new WaitUntil(() => TurnManager.instance.GetCurrentPlayer() == ownerNumber);
        Restore();
    }

    private IEnumerator BuffNextTwoTurns(CardUI sourceCard, int ownerNumber, PlayerManager ownerPM, PlayerManager oppPM)
    {
        // Apply on the next 2 of the owner's turns
        for (int i = 0; i < 2; i++)
        {
            // Wait until owner's turn starts
            yield return new WaitUntil(() => TurnManager.instance.GetCurrentPlayer() == ownerNumber);

            foreach (var ui in Object.FindObjectsOfType<CardUI>())
            {
                if (!includeHand && !ui.isOnField) continue;
                var h = ui.GetComponent<CardHandler>(); if (h == null) continue;

                bool isOwnerSide = (targetOwner == TargetOwner.Yours)
                    ? (h.cardOwner == ownerPM)
                    : (h.cardOwner != ownerPM);
                if (!isOwnerSide) continue;

                if (filterMode == FilterMode.Type && ui.cardData.creatureType != filterValue) continue;
                if (filterMode == FilterMode.NameContains && !ui.cardData.cardName.Contains(filterValue)) continue;

                int before = ui.CalculateEffectivePower();
                ui.temporaryBoost += amount;
                ui.UpdatePowerDisplay();
                int after = ui.CalculateEffectivePower();

                Debug.Log($"[PowerChangeEffect] (NextTwoTurns) {(amount >= 0 ? "+" : "")}{amount} → {ui.cardData.cardName} ({before}→{after})");

                if (!_affected.Contains(ui))
                    _affected.Add(ui);
            }

            // Wait for opponent's turn, then remove these boosts for this cycle
            yield return new WaitUntil(() => TurnManager.instance.GetCurrentPlayer() != ownerNumber);
            Restore();
        }
    }

    // ────────────────────────────────── utils ────────────────────────────────

    private int CountRealHandCards(PlayerManager player)
    {
        if (player == null || player.cardHandlers == null) return 0;

        int count = 0;
        foreach (var h in player.cardHandlers)
        {
            if (h == null || h.cardData == null) continue;
            var ui = h.GetComponent<CardUI>();
            if (ui == null) continue;
            if (!ui.isOnField && !ui.isInGraveyard)
                count++;
        }
        return count;
    }

    private void Restore()
    {
        foreach (var ui in _affected)
        {
            if (ui == null) continue;

            ui.temporaryBoost -= amount;
            ui.UpdatePowerDisplay();

            Debug.Log($"[PowerChangeEffect-End] {(amount >= 0 ? "-" : "+")}{Mathf.Abs(amount)} from {ui.cardData.cardName} (now {ui.CalculateEffectivePower()})");
        }
        _affected.Clear();
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        Restore();
    }
}
