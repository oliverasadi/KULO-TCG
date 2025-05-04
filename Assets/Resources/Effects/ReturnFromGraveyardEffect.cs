using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "Card Effects/Return From Graveyard")]
public class ReturnFromGraveyardEffect : CardEffect
{
    public enum FilterMode { Type, Category }

    [Header("Graveyard Search Settings")]
    public FilterMode filterMode = FilterMode.Type;

    [Tooltip("Enter a creature type (e.g. 'Cat') or a card category (e.g. 'Spell') depending on the selected filter mode.")]
    public string filterValue = "Cat";

    [Tooltip("Prevents the player from playing any cards during their next turn.")]
    public bool blockPlaysNextTurn = false;

    [Range(1, 5)]
    public int cardsToSelect = 1;

    public override void ApplyEffect(CardUI sourceCard)
    {
        Debug.Log($"🧙 {sourceCard.cardData.cardName} triggered ReturnFromGraveyardEffect.");

        var handler = sourceCard.GetComponent<CardHandler>();
        if (handler == null || handler.cardOwner == null) return;

        var zones = handler.cardOwner.zones;
        var graveyard = zones.GetGraveyardCards();
        var playerManager = handler.cardOwner;

        // Filter valid cards
        List<GameObject> valid = graveyard.Where(obj =>
        {
            var data = obj.GetComponent<CardHandler>()?.cardData;
            if (data == null) return false;

            return filterMode switch
            {
                FilterMode.Type => data.creatureType == filterValue,
                FilterMode.Category => data.category.ToString() == filterValue,
                _ => false
            };
        }).ToList();

        if (valid.Count == 0)
        {
            Debug.Log("⚠️ No valid cards in graveyard for this effect.");
            return;
        }

        GraveyardSelectionManager.Instance.StartGraveyardSelection(valid, cardsToSelect, selected =>
        {
            Debug.Log($"📦 [ReturnFromGraveyardEffect] Callback received {selected.Count} card(s).");

            foreach (var realCard in selected)
            {
                var handler = realCard.GetComponent<CardHandler>();
                if (handler == null || handler.cardData == null)
                {
                    Debug.LogWarning("⚠️ Selected card missing CardHandler or CardData.");
                    continue;
                }

                var cardData = handler.cardData;

                // ✅ Remove the selected card from graveyard
                if (graveyard.Contains(realCard))
                {
                    graveyard.Remove(realCard);
                }
                else
                {
                    Debug.LogWarning($"⚠️ Card {cardData.cardName} not found in tracked graveyard list.");
                }

                // ✅ Spawn the card to hand
                playerManager.SpawnCard(cardData);
                Debug.Log($"✅ {cardData.cardName} returned to hand via ReturnFromGraveyardEffect.");
            }

            if (blockPlaysNextTurn)
            {
                playerManager.blockPlaysNextTurn = true;
                Debug.Log("🔒 Player is blocked from playing cards next turn.");
            }
        });

    }

    public override void RemoveEffect(CardUI sourceCard) { }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ReturnFromGraveyardEffect))]
public class ReturnFromGraveyardEffectEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var modeProp = serializedObject.FindProperty("filterMode");
        EditorGUILayout.PropertyField(modeProp);

        var valueProp = serializedObject.FindProperty("filterValue");
        var selectedMode = (ReturnFromGraveyardEffect.FilterMode)modeProp.enumValueIndex;
        var label = selectedMode == ReturnFromGraveyardEffect.FilterMode.Type ? "Creature Type" : "Card Category";
        EditorGUILayout.PropertyField(valueProp, new GUIContent(label));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("cardsToSelect"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("blockPlaysNextTurn"));

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
