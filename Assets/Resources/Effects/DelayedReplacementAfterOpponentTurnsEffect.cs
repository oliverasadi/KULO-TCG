using System;
using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

[CreateAssetMenu(menuName = "Card Effects/Delayed Replacement After Opponent Turns")]
public class DelayedReplacementAfterOpponentTurnsEffect : CardEffect
{
    [Tooltip("The name of the card to replace this one with.")]
    public string replacementCardName;

    [Tooltip("How many opponent turns to wait before replacing.")]
    public int turnsToWait = 3;

    [Tooltip("If true, also search hand in addition to deck.")]
    public bool includeHand = true;

    private Dictionary<CardUI, int> turnTracker = new();
    private Dictionary<CardUI, Action> activeListeners = new();
    private Dictionary<CardUI, GameObject> badgeInstances = new();

    public override void ApplyEffect(CardUI sourceCard)
    {
        if (sourceCard == null) return;

        if (!turnTracker.ContainsKey(sourceCard))
            turnTracker[sourceCard] = 0;

        // Instantiate badge UI
        GameObject badgePrefab = Resources.Load<GameObject>("Prefabs/TurnCountdownBadge");
        if (badgePrefab != null)
        {
            Debug.Log($"[DelayedReplaceEffect] ✅ Loaded badge prefab for {sourceCard.cardData.cardName}");
            GameObject badge = GameObject.Instantiate(badgePrefab, sourceCard.transform);
            badge.transform.localPosition = new Vector3(0, -40, 0); // Lower and centered
            badgeInstances[sourceCard] = badge;
            UpdateBadgeText(sourceCard, turnsToWait);

            // 🔹 Animate: Pop in
            badge.transform.localScale = Vector3.zero;
            badge.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);

            // 🔹 Animate: Idle bobbing
            badge.transform.DOLocalMoveY(-35, 1f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
        else
        {
            Debug.LogWarning("[DelayedReplaceEffect] ❌ Could not find 'TurnCountdownBadge' prefab in Resources/Prefabs/");
        }

        Action onOpponentTurnEnd = () =>
        {
            if (sourceCard == null || !sourceCard.isOnField)
            {
                Cleanup(sourceCard);
                return;
            }

            turnTracker[sourceCard]++;
            int turnsLeft = turnsToWait - turnTracker[sourceCard];
            UpdateBadgeText(sourceCard, turnsLeft);

            // 🔹 Animate: Turn tick punch
            if (badgeInstances.TryGetValue(sourceCard, out GameObject badge))
            {
                badge.transform.DOPunchScale(Vector3.one * 0.1f, 0.25f, 6, 0.5f);
            }

            Debug.Log($"[DelayedReplaceEffect] {sourceCard.cardData.cardName} has waited {turnTracker[sourceCard]}/{turnsToWait} opponent turns.");

            if (turnTracker[sourceCard] >= turnsToWait)
            {
                ExecuteReplacement(sourceCard);
                Cleanup(sourceCard);
            }
        };

        TurnManager.instance.OnOpponentTurnEnd += onOpponentTurnEnd;
        activeListeners[sourceCard] = onOpponentTurnEnd;
    }

    private void ExecuteReplacement(CardUI sourceCard)
    {
        if (sourceCard == null || sourceCard.cardData == null) return;

        PlayerManager owner = sourceCard.GetComponent<CardHandler>().cardOwner;
        if (owner == null)
        {
            Debug.LogError("[DelayedReplaceEffect] No owner found for the card.");
            return;
        }

        CardSO replacementCard = owner.currentDeck.Find(c => c.cardName == replacementCardName);

        if (replacementCard == null && includeHand)
        {
            foreach (var cardHandler in owner.cardHandlers)
            {
                if (!cardHandler.GetComponent<CardUI>().isOnField &&
                    cardHandler.cardData.cardName == replacementCardName)
                {
                    replacementCard = cardHandler.cardData;
                    break;
                }
            }
        }

        if (replacementCard == null)
        {
            Debug.LogWarning("[DelayedReplaceEffect] Replacement card not found in deck or hand.");
            return;
        }

        Vector2Int pos = ParseGridPosition(sourceCard.transform.parent.name);
        GridManager.instance.RemoveCard(pos.x, pos.y, false);

        GameObject newCardObj = Instantiate(DeckManager.instance.cardPrefab);
        CardHandler handler = newCardObj.GetComponent<CardHandler>();
        handler.SetCard(replacementCard, false, owner.playerType == PlayerManager.PlayerTypes.AI);
        handler.cardOwner = owner;

        Transform cellTransform = GameObject.Find($"GridCell_{pos.x}_{pos.y}").transform;
        newCardObj.transform.SetParent(cellTransform, false);
        newCardObj.transform.localScale = Vector3.one;

        GridManager.instance.PlaceExistingCard(pos.x, pos.y, newCardObj, replacementCard, cellTransform);

        // ✅ Show floating power text manually (guaranteed)
        if (FloatingTextManager.instance != null)
        {
            GameObject floatingText = GameObject.Instantiate(
                FloatingTextManager.instance.floatingTextPrefab,
                newCardObj.transform.position,
                Quaternion.identity,
                newCardObj.transform
            );
            floatingText.transform.localPosition = new Vector3(0, 50f, 0);

            TMPro.TextMeshProUGUI tmp = floatingText.GetComponent<TMPro.TextMeshProUGUI>();
            CardUI ui = newCardObj.GetComponent<CardUI>();
            if (tmp != null && ui != null)
                tmp.text = "Power: " + ui.CalculateEffectivePower();

            FloatingText ft = floatingText.GetComponent<FloatingText>();
            if (ft != null)
                ft.sourceCard = newCardObj;
        }

        Debug.Log($"[DelayedReplaceEffect] {sourceCard.cardData.cardName} was replaced with {replacementCard.cardName}.");
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        Cleanup(sourceCard);
    }

    private void Cleanup(CardUI sourceCard)
    {
        if (activeListeners.ContainsKey(sourceCard))
        {
            TurnManager.instance.OnOpponentTurnEnd -= activeListeners[sourceCard];
            activeListeners.Remove(sourceCard);
        }

        if (turnTracker.ContainsKey(sourceCard))
            turnTracker.Remove(sourceCard);

        if (badgeInstances.TryGetValue(sourceCard, out GameObject badge))
        {
            GameObject.Destroy(badge);
            badgeInstances.Remove(sourceCard);
        }
    }

    private void UpdateBadgeText(CardUI card, int turnsRemaining)
    {
        if (badgeInstances.TryGetValue(card, out GameObject badge))
        {
            TMPro.TextMeshProUGUI label = badge.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (label != null)
            {
                label.text = turnsRemaining.ToString();
            }
        }
    }

    private Vector2Int ParseGridPosition(string cellName)
    {
        string[] parts = cellName.Split('_');
        if (parts.Length >= 3 &&
            int.TryParse(parts[1], out int x) &&
            int.TryParse(parts[2], out int y))
        {
            return new Vector2Int(x, y);
        }
        return new Vector2Int(-1, -1);
    }
}
