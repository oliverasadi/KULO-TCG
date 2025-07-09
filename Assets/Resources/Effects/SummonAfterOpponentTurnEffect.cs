using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/Summon After Opponent Turn")]
public class SummonAfterOpponentTurnEffect : CardEffect
{
    private readonly Dictionary<CardUI, Action> _registeredHandlers = new Dictionary<CardUI, Action>();

    public override void ApplyEffect(CardUI sourceCard)
    {
        Debug.Log($"[SummonAfterOpponentTurnEffect] Applied to {sourceCard.cardData.cardName}. Waiting for opponent's turn to end.");

        Action onOpponentTurnEnd = null;
        onOpponentTurnEnd = () =>
        {
            if (sourceCard == null || !sourceCard.isActiveAndEnabled)
            {
                Debug.Log($"[SummonAfterOpponentTurnEffect] Source card was removed before trigger, no summon will occur.");
                TurnManager.instance.OnOpponentTurnEnd -= onOpponentTurnEnd;
                _registeredHandlers.Remove(sourceCard);
                return;
            }

            TurnManager.instance.OnOpponentTurnEnd -= onOpponentTurnEnd;
            _registeredHandlers.Remove(sourceCard);

            Debug.Log($"[SummonAfterOpponentTurnEffect] Opponent turn ended. Queuing summon panel after draw...");

            // Wait for draw + splash before triggering panel
            sourceCard.StartCoroutine(TriggerAfterDrawDelay(sourceCard));
        };

        TurnManager.instance.OnOpponentTurnEnd += onOpponentTurnEnd;
        _registeredHandlers[sourceCard] = onOpponentTurnEnd;
    }

    private IEnumerator TriggerAfterDrawDelay(CardUI sourceCard)
    {
        int ownerPlayer = sourceCard.GetComponent<CardHandler>().cardOwner.playerNumber;
        int localPlayer = TurnManager.instance.localPlayerNumber;

        // Wait until it's the card owner's turn again
        yield return new WaitUntil(() => TurnManager.instance.GetCurrentPlayer() == ownerPlayer);

        // Wait a short moment after draw/splash
        yield return new WaitForSeconds(1.0f);

        if (ownerPlayer == localPlayer)
        {
            Debug.Log($"[SummonAfterOpponentTurnEffect] Prompting player to choose a card to summon via UI.");
            TriggerSummonChoice(sourceCard);
        }
        else
        {
            Debug.Log($"[SummonAfterOpponentTurnEffect] Skipping summon panel for AI.");
            // Optional: auto-resolve for AI
        }
    }


    public override void RemoveEffect(CardUI sourceCard)
    {
        if (_registeredHandlers.TryGetValue(sourceCard, out var handler))
        {
            TurnManager.instance.OnOpponentTurnEnd -= handler;
            _registeredHandlers.Remove(sourceCard);
            Debug.Log($"[SummonAfterOpponentTurnEffect] Removed from {sourceCard.cardData.cardName} (card left the field).");
        }
    }

    private void TriggerSummonChoice(CardUI sourceCard)
    {
        var summonOptions = sourceCard.cardData.summonOptionsAfterOpponentTurn;

        if (summonOptions == null || summonOptions.Count == 0)
        {
            Debug.LogWarning("[SummonAfterOpponentTurnEffect] No summon options configured in CardSO.");
            return;
        }

        Debug.Log("[SummonAfterOpponentTurnEffect] Prompting player to choose a card to summon via UI.");

        GameObject uiPrefab = Resources.Load<GameObject>("Prefabs/SummonChoicePanel");
        if (uiPrefab == null)
        {
            Debug.LogError("[SummonAfterOpponentTurnEffect] Could not find SummonChoicePanel prefab in Resources/Prefabs/");
            return;
        }

        GameObject canvas = GameObject.Find("OverlayCanvas");
        if (canvas == null)
        {
            Debug.LogError("[SummonAfterOpponentTurnEffect] Could not find OverlayCanvas in scene.");
            return;
        }

        GameObject uiInstance = GameObject.Instantiate(uiPrefab, canvas.transform);
        SummonChoiceUI choiceUI = uiInstance.GetComponent<SummonChoiceUI>();
        if (choiceUI == null)
        {
            Debug.LogError("[SummonAfterOpponentTurnEffect] SummonChoiceUI component missing on prefab.");
            return;
        }

        string effectMessage = $"{sourceCard.cardData.cardName} effect activated! Choose a card to summon.";

        choiceUI.Show(summonOptions, (CardSO chosenCard) =>
        {
            if (chosenCard == null)
            {
                Debug.Log("[SummonAfterOpponentTurnEffect] Player cancelled the summon.");
                GameObject.Destroy(uiInstance);
                return;
            }

            Debug.Log($"[SummonAfterOpponentTurnEffect] Player selected {chosenCard.cardName}.");
            ChooseSummonLocation(chosenCard, sourceCard, uiInstance);
        }, effectMessage);
    }



    private void ChooseSummonLocation(CardSO cardToSummon, CardUI sourceCard, GameObject uiInstance)
    {
        Debug.Log("[SummonAfterOpponentTurnEffect] ChooseSummonLocation CALLED");

        GridManager.instance.EnableCellSelectionMode(sourceCard, (x, y) =>
        {
            PerformSummon(cardToSummon, sourceCard, x, y);

            if (uiInstance != null)
            {
                Debug.Log("[SummonAfterOpponentTurnEffect] Destroying summon UI panel.");
                GameObject.Destroy(uiInstance);
            }
        });

        Debug.Log("[SummonAfterOpponentTurnEffect] Highlighting empty board cells for player to choose summon location.");
    }

    private void PerformSummon(CardSO cardToSummon, CardUI sourceCard, int targetX, int targetY)
    {
        PlayerManager owner = sourceCard.GetComponent<CardHandler>().cardOwner;
        if (owner == null)
        {
            Debug.LogError("[SummonAfterOpponentTurnEffect] Source card has no owner; cannot summon new card.");
            return;
        }

        bool removedFromDeck = owner.RemoveCardFromDeck(cardToSummon);
        Debug.Log(removedFromDeck
            ? $"[SummonAfterOpponentTurnEffect] {cardToSummon.cardName} was in deck and is now removed for summoning."
            : $"[SummonAfterOpponentTurnEffect] {cardToSummon.cardName} was not in deck (summoning as a special card).");

        GameObject cardPrefab = DeckManager.instance.cardPrefab;
        GameObject newCardObj = GameObject.Instantiate(cardPrefab);

        GameObject cellObj = GameObject.Find($"GridCell_{targetX}_{targetY}");
        if (cellObj == null)
        {
            Debug.LogError($"[SummonAfterOpponentTurnEffect] Target cell GridCell_{targetX}_{targetY} not found. Aborting summon.");
            GameObject.Destroy(newCardObj);
            return;
        }

        Transform cellTransform = cellObj.transform;

        // ✅ Parent it to the cell first
        newCardObj.transform.SetParent(cellTransform, worldPositionStays: false);

        // ✅ Reset scale AFTER parenting to avoid inherited canvas scaling
        newCardObj.transform.localScale = Vector3.one;

        CardHandler newCardHandler = newCardObj.GetComponent<CardHandler>();
        if (newCardHandler != null)
        {
            newCardHandler.SetCard(cardToSummon, setFaceDown: false, isAICard: false);
            newCardHandler.cardOwner = owner;
            Debug.Log($"[SummonAfterOpponentTurnEffect] Created new card object for {cardToSummon.cardName} (Owner: Player {owner.playerNumber}).");
        }
        else
        {
            Debug.LogError("[SummonAfterOpponentTurnEffect] CardHandler component missing on card prefab instance.");
            GameObject.Destroy(newCardObj);
            return;
        }

        CardUI cardUI = newCardObj.GetComponent<CardUI>();
        if (cardUI == null)
        {
            Debug.LogError("[SummonAfterOpponentTurnEffect] CardUI component missing on new card object.");
            GameObject.Destroy(newCardObj);
            return;
        }

        // ✅ Now animate to that cell again
        CardSummonAnimator.AnimateCardToCell(cardUI, cellTransform, () =>
        {
            bool success = GridManager.instance.PlaceReplacementCard(
                targetX,
                targetY,
                newCardObj,
                cardToSummon,
                cellTransform
            );

            if (!success)
            {
                Debug.LogWarning($"[SummonAfterOpponentTurnEffect] Placement failed at ({targetX},{targetY}).");
            }
        });

        TurnManager.instance.BlockAdditionalCardPlays();
        Debug.Log("[SummonAfterOpponentTurnEffect] Player cannot play additional cards this turn (additional plays blocked).");
    }


    private void ResetScaleRecursively(Transform obj)
    {
        obj.localScale = Vector3.one;
        foreach (Transform child in obj)
        {
            ResetScaleRecursively(child);
        }
    }
}
