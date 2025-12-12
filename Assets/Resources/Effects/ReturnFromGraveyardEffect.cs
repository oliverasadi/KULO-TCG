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
        if (handler == null || handler.cardOwner == null) return;

        // --- Helper to safely get CardSO from GameObject ---
        CardSO GetCardData(GameObject obj)
        {
            if (obj == null) return null;

            if (obj.TryGetComponent<CardHandler>(out var ch) && ch.cardData != null)
                return ch.cardData;

            if (obj.TryGetComponent<CardUI>(out var cu) && cu.cardData != null)
                return cu.cardData;

            return null;
        }

        bool IsValidTarget(GameObject obj)
        {
            var data = GetCardData(obj);
            if (data == null) return false;

            if (filterMode == FilterMode.Type)
                return data.category == CardSO.CardCategory.Creature && data.creatureType == filterValue;

            if (filterMode == FilterMode.Category &&
                Enum.TryParse<CardSO.CardCategory>(filterValue, true, out var parsedCategory))
                return data.category == parsedCategory;

            return false;
        }

        // If the selection UI returns a "clone" object, resolve it back to a real graveyard object.
        GameObject ResolveToRealGraveObject(GameObject selectedObj, List<GameObject> remainingRealCards)
        {
            if (selectedObj == null || remainingRealCards == null) return null;

            // If it IS one of the real objects, perfect.
            if (remainingRealCards.Contains(selectedObj))
                return selectedObj;

            // Otherwise, try to match by CardSO reference (and make sure we don't reuse the same real card twice).
            var selectedData = GetCardData(selectedObj);
            if (selectedData == null) return null;

            return remainingRealCards.FirstOrDefault(go => GetCardData(go) == selectedData);
        }

        // -------------------- AI ------------------------
        if (handler.isAI)
        {
            var graveyard = handler.cardOwner.zones.GetGraveyardCards();
            if (graveyard == null) return;

            var valid = graveyard.Where(IsValidTarget).ToList();

            // ✅ Hard gate: only resolve/apply if we can actually return the required amount.
            if (valid.Count < cardsToSelect)
            {
                Debug.Log($"[ReturnFromGraveyardEffect] AI skipped: needs {cardsToSelect} valid target(s), found {valid.Count}.");
                return;
            }

            var chosen = valid.Take(cardsToSelect).ToList();
            foreach (var realCard in chosen)
            {
                var data = GetCardData(realCard);
                if (data == null) continue;

                // Add to hand
                handler.cardOwner.SpawnCard(data);

                // Remove from graveyard + destroy the old grave object so we don’t duplicate
                graveyard.Remove(realCard);
                if (realCard != null)
                    GameObject.Destroy(realCard);
            }

            if (blockPlaysNextTurn)
                handler.cardOwner.blockPlaysNextTurn = true;

            return;
        }

        // -------------------- Player ------------------------
        var zones = handler.cardOwner.zones;
        var graveList = zones.GetGraveyardCards();
        if (graveList == null) return;

        List<GameObject> validList = graveList.Where(IsValidTarget).ToList();

        Debug.Log($"[GraveyardFilter] FilterMode={filterMode}, FilterValue={filterValue}, ValidCards={validList.Count}");

        if (validList.Count == 0) return;

        GraveyardSelectionManager.Instance.StartGraveyardSelection(
            validList,
            cardsToSelect,
            selected =>
            {
                if (selected == null) return;

                // Keep a "remaining" list so if you pick 2 copies of the same card,
                // we remove 2 *different* real grave objects.
                var remainingReal = new List<GameObject>(graveList);

                int returnedCount = 0;

                foreach (var picked in selected)
                {
                    var real = ResolveToRealGraveObject(picked, remainingReal);
                    if (real == null) continue;

                    var data = GetCardData(real);
                    if (data == null) continue;

                    remainingReal.Remove(real);

                    // Remove from grave list + destroy the grave object so it’s truly moved
                    if (graveList.Contains(real))
                        graveList.Remove(real);

                    if (real != null)
                        GameObject.Destroy(real);

                    handler.cardOwner.SpawnCard(data);
                    returnedCount++;
                }

                // If you want the penalty ALWAYS when the spell is played, remove the "returnedCount > 0" check.
                if (blockPlaysNextTurn && returnedCount > 0)
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
