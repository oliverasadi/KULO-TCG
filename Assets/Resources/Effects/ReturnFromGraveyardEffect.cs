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
        // Get the CardHandler; bail if missing or no owner
        var handler = sourceCard.GetComponent<CardHandler>();
        if (handler == null || handler.cardOwner == null)
            return;

        // 1) AI auto-select: no UI, just return the first N valid cards
        if (handler.isAI)
        {
            var graveyard = handler.cardOwner.zones.GetGraveyardCards();
            var valid = graveyard.Where(obj =>
            {
                var data = obj.GetComponent<CardHandler>()?.cardData;
                if (data == null) return false;
                return filterMode == FilterMode.Type
                    ? data.creatureType == filterValue
                    : data.category.ToString() == filterValue;
            })
            .Take(cardsToSelect)
            .ToList();

            foreach (var realCard in valid)
            {
                var ch = realCard.GetComponent<CardHandler>();
                if (ch != null && ch.cardData != null)
                {
                    handler.cardOwner.SpawnCard(ch.cardData);
                }
            }

            if (blockPlaysNextTurn)
                handler.cardOwner.blockPlaysNextTurn = true;

            return;
        }

        // 2) Player: show graveyard selection UI
        var zones = handler.cardOwner.zones;
        var graveList = zones.GetGraveyardCards();
        List<GameObject> validList = graveList.Where(obj =>
        {
            var data = obj.GetComponent<CardHandler>()?.cardData;
            if (data == null) return false;
            return filterMode == FilterMode.Type
                ? data.creatureType == filterValue
                : data.category.ToString() == filterValue;
        }).ToList();

        if (validList.Count == 0)
            return;

        GraveyardSelectionManager.Instance.StartGraveyardSelection(
            validList,
            cardsToSelect,
            selected =>
            {
                foreach (var realCard in selected)
                {
                    var ch = realCard.GetComponent<CardHandler>();
                    if (ch != null && ch.cardData != null)
                    {
                        if (graveList.Contains(realCard))
                            graveList.Remove(realCard);

                        handler.cardOwner.SpawnCard(ch.cardData);
                    }
                }
                if (blockPlaysNextTurn)
                    handler.cardOwner.blockPlaysNextTurn = true;
            }
        );
    }

    public override void RemoveEffect(CardUI sourceCard)
    {
        // No removal logic needed
    }
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
        var label = selectedMode == ReturnFromGraveyardEffect.FilterMode.Type
            ? "Creature Type"
            : "Card Category";
        EditorGUILayout.PropertyField(valueProp, new GUIContent(label));

        EditorGUILayout.PropertyField(serializedObject.FindProperty("cardsToSelect"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("blockPlaysNextTurn"));

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
