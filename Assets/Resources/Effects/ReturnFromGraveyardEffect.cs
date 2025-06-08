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
        var handler = sourceCard.GetComponent<CardHandler>();
        if (handler == null || handler.cardOwner == null)
            return;

        // --- Helper to safely get CardSO from GameObject ---
        CardSO GetCardData(GameObject obj)
        {
            if (obj.TryGetComponent<CardHandler>(out var ch) && ch.cardData != null)
                return ch.cardData;

            if (obj.TryGetComponent<CardUI>(out var cu) && cu.cardData != null)
                return cu.cardData;

            return null;
        }

        // -------------------- AI ------------------------
        if (handler.isAI)
        {
            var graveyard = handler.cardOwner.zones.GetGraveyardCards();
            var valid = graveyard.Where(obj =>
            {
                var data = GetCardData(obj);
                if (data == null) return false;

                if (filterMode == FilterMode.Type)
                    return data.category == CardSO.CardCategory.Creature && data.creatureType == filterValue;

                if (filterMode == FilterMode.Category &&
                    Enum.TryParse<CardSO.CardCategory>(filterValue, true, out var parsedCategory))
                    return data.category == parsedCategory;

                return false;
            })
            .Take(cardsToSelect)
            .ToList();

            foreach (var realCard in valid)
            {
                var ch = realCard.GetComponent<CardHandler>();
                if (ch != null && ch.cardData != null)
                    handler.cardOwner.SpawnCard(ch.cardData);
            }

            if (blockPlaysNextTurn)
                handler.cardOwner.blockPlaysNextTurn = true;

            return;
        }

        // -------------------- Player ------------------------
        var zones = handler.cardOwner.zones;
        var graveList = zones.GetGraveyardCards();

        List<GameObject> validList = graveList.Where(obj =>
        {
            var data = GetCardData(obj);
            if (data == null) return false;

            if (filterMode == FilterMode.Type)
                return data.category == CardSO.CardCategory.Creature && data.creatureType == filterValue;

            if (filterMode == FilterMode.Category &&
                Enum.TryParse<CardSO.CardCategory>(filterValue, true, out var parsedCategory))
                return data.category == parsedCategory;

            return false;
        }).ToList();

        // Optional Debug
        Debug.Log($"[GraveyardFilter] FilterMode={filterMode}, FilterValue={filterValue}, ValidCards={validList.Count}");

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
