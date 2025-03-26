using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CardSO))]
public class CardSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw common fields for all cards.
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cardName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cardNumber"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("category"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cardImage"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("effectDescription"));

        // Draw asset-based effects.
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Card Effects (Assets)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("effects"), true);

        // Inline Effects
        SerializedProperty inlineEffectsProp = serializedObject.FindProperty("inlineEffects");
        if (inlineEffectsProp != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Inline Card Effects", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            for (int i = 0; i < inlineEffectsProp.arraySize; i++)
            {
                SerializedProperty effectElement = inlineEffectsProp.GetArrayElementAtIndex(i);
                if (effectElement != null)
                {
                    EditorGUILayout.BeginVertical("box");

                    // 1) Always show effect type
                    EditorGUILayout.PropertyField(effectElement.FindPropertyRelative("effectType"));

                    // 2) Check which effect type
                    CardEffectData.EffectType effectType =
                        (CardEffectData.EffectType)effectElement.FindPropertyRelative("effectType").enumValueIndex;

                    // 3) If AdjustPowerAdjacent, show its unique fields
                    if (effectType == CardEffectData.EffectType.AdjustPowerAdjacent)
                    {
                        // powerChangeAmount
                        EditorGUILayout.PropertyField(effectElement.FindPropertyRelative("powerChangeAmount"));
                        // powerChangeType (Increase/Decrease)
                        EditorGUILayout.PropertyField(effectElement.FindPropertyRelative("powerChangeType"));
                        // adjacencyOwnerToAffect (Self, Opponent, or Both)
                        EditorGUILayout.PropertyField(effectElement.FindPropertyRelative("adjacencyOwnerToAffect"));
                        // targetPositions
                        EditorGUILayout.PropertyField(effectElement.FindPropertyRelative("targetPositions"), true);
                    }

                    // 4) Draw common fields for all effects
                    EditorGUILayout.PropertyField(effectElement.FindPropertyRelative("cardsToDraw"));
                    EditorGUILayout.PropertyField(effectElement.FindPropertyRelative("requiredCreatureNames"), true);
                    EditorGUILayout.PropertyField(effectElement.FindPropertyRelative("maxTargets"));
                    EditorGUILayout.PropertyField(effectElement.FindPropertyRelative("replacementCardName"));
                    EditorGUILayout.PropertyField(effectElement.FindPropertyRelative("turnDelay"));
                    EditorGUILayout.PropertyField(effectElement.FindPropertyRelative("blockAdditionalPlays"));
                    EditorGUILayout.PropertyField(effectElement.FindPropertyRelative("promptPrefab"));
                    EditorGUILayout.PropertyField(effectElement.FindPropertyRelative("powerChange"));

                    EditorGUILayout.EndVertical();
                }
            }

            if (GUILayout.Button("Add Inline Effect"))
            {
                inlineEffectsProp.arraySize++;
            }

            EditorGUI.indentLevel--;
        }
        else
        {
            EditorGUILayout.HelpBox(
                "No Inline Effects field found. Ensure your CardSO script has a public List<CardEffectData> inlineEffects field.",
                MessageType.Info);
        }

        // Draw creature-specific fields if card is a Creature.
        CardSO cardSO = (CardSO)target;
        if (cardSO.category == CardSO.CardCategory.Creature)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("power"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("creatureType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseOrEvo"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("extraDetails"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Evolution / Sacrifice Requirements", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("requiresSacrifice"));
            if (serializedObject.FindProperty("requiresSacrifice").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sacrificeRequirements"), true);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
