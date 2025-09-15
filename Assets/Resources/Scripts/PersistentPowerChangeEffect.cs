using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Persistent Power Change")]
public class PersistentPowerChangeEffect : CardEffect
{
    [Header("Behaviour")]
    public bool Continuous = true;           // aura vs one-off
    public bool includeHand = true;          // include hand targets
    [Range(0.05f, 1.0f)]
    public float recalcInterval = 0.15f;     // ✅ polling fallback, seconds

    public enum TargetOwner { Yours, Opponent }
    public TargetOwner targetOwner = TargetOwner.Yours;

    public enum FilterMode { All, Type, NameContains }
    public FilterMode filterMode = FilterMode.All;
    public string filterValue = "";

    public int amount = 200;

    // runtime
    private readonly List<CardUI> _affected = new List<CardUI>();
    private CardUI _source;
    private int _sourceOwner = -1;

    private bool _subscribed;
    private Action<CardSO> _boardChangedHandler;

    // keep PM refs for clean unsubscribe
    private PlayerManager _pm1, _pm2;

    // polling handle
    private Coroutine _pollRoutine;

    public override void ApplyEffect(CardUI sourceCard)
    {
        _source = sourceCard;

        var srcHandler = sourceCard != null ? sourceCard.GetComponent<CardHandler>() : null;
        _sourceOwner = (srcHandler != null && srcHandler.cardOwner != null) ? srcHandler.cardOwner.playerNumber : -1;

        Debug.Log($"[PersistentPCE] ApplyEffect from '{_source?.cardData.cardName ?? "null"}' | Continuous={Continuous} includeHand={includeHand} owner={_sourceOwner}");

        if (!Continuous)
        {
            ApplyOnce();
            return;
        }

        // First pass
        Recalculate();

        // Subscribe to updates
        if (!_subscribed && TurnManager.instance != null)
        {
            _boardChangedHandler = card =>
            {
                Debug.Log($"[PersistentPCE] OnCardPlayed received: {card?.cardName ?? "null"} → queue Recalculate next frame");
                RecalcNextFrame();
            };

            TurnManager.instance.OnCardPlayed += _boardChangedHandler; // plays, removals, and draw proxy
            Debug.Log("[PersistentPCE] Subscribed to TurnManager.OnCardPlayed");

            // Also listen directly to per-player draw events for bullet-proof timing
            _pm1 = TurnManager.instance.playerManager1;
            _pm2 = TurnManager.instance.playerManager2;

            if (_pm1 != null)
            {
                _pm1.OnCardDrawn += RecalcNextFrame;
                Debug.Log("[PersistentPCE] Subscribed to playerManager1.OnCardDrawn");
            }
            if (_pm2 != null)
            {
                _pm2.OnCardDrawn += RecalcNextFrame;
                Debug.Log("[PersistentPCE] Subscribed to playerManager2.OnCardDrawn");
            }

            _subscribed = true;
        }

        // ✅ Start polling fallback (covers any race conditions)
        StartPolling();
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        // Stop polling
        StopPolling();

        // Unsubscribe
        if (_subscribed && TurnManager.instance != null && _boardChangedHandler != null)
        {
            TurnManager.instance.OnCardPlayed -= _boardChangedHandler;
            Debug.Log("[PersistentPCE] Unsubscribed from TurnManager.OnCardPlayed");
        }

        if (_pm1 != null) _pm1.OnCardDrawn -= RecalcNextFrame;
        if (_pm2 != null) _pm2.OnCardDrawn -= RecalcNextFrame;

        _subscribed = false;
        _boardChangedHandler = null;
        _pm1 = null; _pm2 = null;

        // Remove current aura from all affected
        foreach (var ui in _affected)
        {
            if (ui == null) continue;
            int before = ui.CalculateEffectivePower();
            ui.temporaryBoost -= amount;
            ui.UpdatePowerDisplay();
            int after = ui.CalculateEffectivePower();
            Debug.Log($"[PersistentPCE] ❌ Removed +{amount} from {ui.cardData.cardName}: {before} → {after}");
        }
        _affected.Clear();

        _source = null;
        _sourceOwner = -1;
    }

    // ── helpers ──

    private void StartPolling()
    {
        if (TurnManager.instance == null) return;
        if (_pollRoutine != null) StopPolling();
        _pollRoutine = TurnManager.instance.StartCoroutine(PollRoutine());
        Debug.Log($"[PersistentPCE] Polling started @ {recalcInterval:0.00}s");
    }

    private void StopPolling()
    {
        if (_pollRoutine != null && TurnManager.instance != null)
        {
            TurnManager.instance.StopCoroutine(_pollRoutine);
            Debug.Log("[PersistentPCE] Polling stopped");
        }
        _pollRoutine = null;
    }

    private System.Collections.IEnumerator PollRoutine()
    {
        var wait = new WaitForSeconds(recalcInterval);
        while (_source != null) // while source still exists (on field)
        {
            // Optional: only poll if source still on field (if you have such a flag on CardUI)
            // if (!_source.isOnField) break;

            // small, cheap check each tick
            // Debug.Log("[PersistentPCE] Poll tick → Recalculate()");
            Recalculate();
            yield return wait;
        }
        _pollRoutine = null;
    }

    private void RecalcNextFrame()
    {
        // Defer a frame so newly drawn/instantiated CardUI definitely exists
        if (TurnManager.instance != null)
            TurnManager.instance.StartCoroutine(_RecalcCo());
        else if (_source != null)
            _source.StartCoroutine(_RecalcCo());
    }

    private System.Collections.IEnumerator _RecalcCo()
    {
        yield return null; // wait 1 frame
        Recalculate();
    }

    // ── core logic ──

    private void ApplyOnce()
    {
        Debug.Log("[PersistentPCE] ApplyOnce()");
        foreach (var ui in FindTargets().Distinct())
        {
            if (ui == null) continue;
            if (_source != null && ui.gameObject == _source.gameObject) continue; // exclude self

            int before = ui.CalculateEffectivePower();
            ui.temporaryBoost += amount;
            ui.UpdatePowerDisplay();
            int after = ui.CalculateEffectivePower();

            Debug.Log($"[PersistentPCE] ✅ One-off +{amount} to {ui.cardData.cardName}: {before} → {after}");
        }
    }

    private void Recalculate()
    {
        if (_sourceOwner == -1)
        {
            Debug.LogWarning("[PersistentPCE] Recalculate skipped: _sourceOwner == -1 (source has no owner?)");
            return;
        }
        if (!includeHand)
        {
            Debug.Log("[PersistentPCE] Note: includeHand=false → will not buff cards in hand");
        }

        // Remove previous buffs
        foreach (var ui in _affected)
        {
            if (ui == null) continue;
            int before = ui.CalculateEffectivePower();
            ui.temporaryBoost -= amount;
            ui.UpdatePowerDisplay();
            int after = ui.CalculateEffectivePower();
            // Debug each removal to track churn
            // Debug.Log($"[PersistentPCE]   – Removed from {ui.cardData.cardName}: {before} → {after}");
        }
        _affected.Clear();

        // Apply to current valid targets
        foreach (var ui in FindTargets().Distinct())
        {
            if (ui == null) continue;
            if (_source != null && ui.gameObject == _source.gameObject) continue; // exclude self

            int before = ui.CalculateEffectivePower();
            ui.temporaryBoost += amount;
            ui.UpdatePowerDisplay();
            int after = ui.CalculateEffectivePower();

            Debug.Log($"[PersistentPCE]   + Applied to {ui.cardData.cardName}: {before} → {after}");
            _affected.Add(ui);
        }
    }

    private IEnumerable<CardUI> FindTargets()
    {
        if (_sourceOwner == -1) yield break;

        // Field
        foreach (var ui in GameObject.FindObjectsOfType<CardUI>())
        {
            if (!ui.isOnField) continue;
            var h = ui.GetComponent<CardHandler>();
            if (h == null || h.cardOwner == null) continue;
            if (!OwnerMatches(h.cardOwner.playerNumber)) continue;
            if (!PassesFilter(ui)) continue;

            // Debug.Log($"[PersistentPCE][Targets] FIELD: {ui.cardData.cardName} (owner {h.cardOwner.playerNumber})");
            yield return ui;
        }

        // Hand
        if (!includeHand || TurnManager.instance == null) yield break;

        foreach (var pm in new[] { TurnManager.instance.playerManager1, TurnManager.instance.playerManager2 })
        {
            if (pm == null || pm.cardHandlers == null) continue;
            foreach (var h in pm.cardHandlers)
            {
                if (h == null || h.cardOwner == null) continue;
                var ui = h.GetComponent<CardUI>();
                if (ui == null || ui.isOnField) continue; // only IN-HAND here
                if (!OwnerMatches(h.cardOwner.playerNumber)) continue;
                if (!PassesFilter(ui)) continue;

                // Debug.Log($"[PersistentPCE][Targets] HAND: {ui.cardData.cardName} (owner {h.cardOwner.playerNumber})");
                yield return ui;
            }
        }
    }

    private bool OwnerMatches(int cardOwnerNumber)
    {
        return (targetOwner == TargetOwner.Yours)
            ? (cardOwnerNumber == _sourceOwner)
            : (cardOwnerNumber != _sourceOwner);
    }

    private bool PassesFilter(CardUI ui)
    {
        if (filterMode == FilterMode.All) return true;
        if (filterMode == FilterMode.Type) return ui.cardData.creatureType == filterValue;
        if (filterMode == FilterMode.NameContains) return
            !string.IsNullOrEmpty(ui.cardData.cardName) &&
            ui.cardData.cardName.IndexOf(filterValue, StringComparison.OrdinalIgnoreCase) >= 0;
        return true;
    }
}
