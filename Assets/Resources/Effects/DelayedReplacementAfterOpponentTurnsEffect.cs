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

    // Optional toggle to block extra plays after replacing
    public bool blockAdditionalPlays = false;

    private Dictionary<CardUI, int> turnTracker = new();
    private Dictionary<CardUI, Action> activeListeners = new();
    private Dictionary<CardUI, GameObject> badgeInstances = new();

    public override void ApplyEffect(CardUI sourceCard)
    {
        if (sourceCard == null) return;

        if (!turnTracker.ContainsKey(sourceCard))
            turnTracker[sourceCard] = 0;

        GameObject badgePrefab = Resources.Load<GameObject>("Prefabs/TurnCountdownBadge");
        if (badgePrefab != null)
        {
            GameObject badge = GameObject.Instantiate(badgePrefab, sourceCard.transform);
            badge.transform.localPosition = new Vector3(0, -40, 0);
            badgeInstances[sourceCard] = badge;
            UpdateBadgeText(sourceCard, turnsToWait);

            badge.transform.localScale = Vector3.zero;
            badge.transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
            badge.transform.DOLocalMoveY(-35, 1f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo);
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

            if (badgeInstances.TryGetValue(sourceCard, out GameObject badge))
                badge.transform.DOPunchScale(Vector3.one * 0.1f, 0.25f, 6, 0.5f);

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
        string oldCardName = sourceCard.cardData.cardName;
        Debug.Log($"[DelayedReplaceEffect] ExecuteReplacement called for '{oldCardName}'");

        CardSO replacementCardSO = DeckManager.instance.FindCardByName(replacementCardName);
        if (replacementCardSO == null)
        {
            Debug.LogError("[DelayedReplaceEffect] ❌ Replacement card not found in deck or database.");
            return;
        }

        PlayerManager owner = sourceCard.GetComponent<CardHandler>()?.cardOwner;
        if (owner == null)
        {
            Debug.LogError("[DelayedReplaceEffect] ❌ No valid owner found for source card.");
            return;
        }

        if (!owner.RemoveCardFromDeck(replacementCardSO))
        {
            Debug.LogWarning($"[DelayedReplaceEffect] {replacementCardSO.cardName} was not found in the owner's deck.");
        }
        else
        {
            Debug.Log($"[DelayedReplaceEffect] Removed {replacementCardSO.cardName} from deck.");
        }

        CardSO[,] grid = GridManager.instance.GetGrid();
        GameObject[,] gridObjects = GridManager.instance.GetGridObjects();
        bool requirementMet = false;

        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                if (grid[x, y] != null && grid[x, y].cardName == oldCardName)
                {
                    Debug.Log($"[DelayedReplaceEffect] Found {oldCardName} on grid at ({x},{y})");

                    GridManager.instance.RemoveCard(x, y, false);

                    GameObject newCardObj = InstantiateReplacementCard(replacementCardSO, owner);
                    Transform cellTransform = GameObject.Find($"GridCell_{x}_{y}").transform;
                    GridManager.instance.PlaceReplacementCard(x, y, newCardObj, replacementCardSO, cellTransform);

                    if (replacementCardSO.baseOrEvo == CardSO.BaseOrEvo.Evolution)
                    {
                        GridManager.instance.ShowEvolutionSplash(oldCardName, replacementCardSO.cardName);
                    }

                    if (blockAdditionalPlays)
                    {
                        Debug.Log("[DelayedReplaceEffect] Blocking additional plays after replacement.");
                        TurnManager.instance.BlockAdditionalCardPlays();
                    }

                    requirementMet = true;
                    return;
                }
            }
        }

        if (!requirementMet)
        {
            Debug.LogWarning($"[DelayedReplaceEffect] ❌ Cannot place {replacementCardSO.cardName}: requirement not met (need 1, found 0)");
        }
    }

    public override void RemoveEffect(CardUI sourceCard) => Cleanup(sourceCard);

    private void Cleanup(CardUI sourceCard)
    {
        if (activeListeners.TryGetValue(sourceCard, out var listener))
        {
            TurnManager.instance.OnOpponentTurnEnd -= listener;
            activeListeners.Remove(sourceCard);
        }

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
                label.text = turnsRemaining.ToString();
        }
    }

    private GameObject InstantiateReplacementCard(CardSO replacementCard, PlayerManager owner)
    {
        GameObject cardPrefab = DeckManager.instance.cardPrefab;
        GameObject newCardObj = GameObject.Instantiate(cardPrefab);
        CardHandler handler = newCardObj.GetComponent<CardHandler>();
        if (handler != null)
        {
            handler.SetCard(replacementCard, false, false);
            handler.cardOwner = owner;
        }
        else
        {
            Debug.LogError("[InstantiateReplacementCard] CardHandler not found on the new card object.");
        }
        return newCardObj;
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
