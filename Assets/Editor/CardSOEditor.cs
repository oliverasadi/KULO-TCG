using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CardSO))]
public class CardSOEditor : Editor
{
    // The array of all known creature types for the dropdown (used elsewhere)
    private static readonly string[] ALL_CREATURE_TYPES =
    {
        "Cat",
        "Wax",
        "Fisherman",
        "Koi",
        "Su Beast"
    };

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
                        EditorGUILayout.PropertyField(effectElement.FindPropertyRelative("powerChangeAmount"));
                        EditorGUILayout.PropertyField(effectElement.FindPropertyRelative("powerChangeType"));
                        EditorGUILayout.PropertyField(effectElement.FindPropertyRelative("adjacencyOwnerToAffect"));
                        EditorGUILayout.PropertyField(effectElement.FindPropertyRelative("targetPositions"), true);
                    }

                    // 4) Draw common fields for all effects
                    EditorGUILayout.PropertyField(effectElement.FindPropertyRelative("cardsToDraw"));
                    EditorGUILayout.PropertyField(effectElement.FindPropertyRelative("requiredCreatureNames"), true);

                    // Draw requiredCreatureTypes using a custom dropdown approach:
                    SerializedProperty typesArrayProp = effectElement.FindPropertyRelative("requiredCreatureTypes");
                    EditorGUILayout.LabelField("Required Creature Types");
                    EditorGUI.indentLevel++;
                    for (int t = 0; t < typesArrayProp.arraySize; t++)
                    {
                        SerializedProperty typeElement = typesArrayProp.GetArrayElementAtIndex(t);
                        int oldIndex = System.Array.IndexOf(ALL_CREATURE_TYPES, typeElement.stringValue);
                        if (oldIndex < 0) oldIndex = 0;

                        EditorGUILayout.BeginHorizontal();
                        int newIndex = EditorGUILayout.Popup($"Type {t}", oldIndex, ALL_CREATURE_TYPES);
                        typeElement.stringValue = ALL_CREATURE_TYPES[newIndex];
                        if (GUILayout.Button("X", GUILayout.Width(20)))
                        {
                            typesArrayProp.DeleteArrayElementAtIndex(t);
                            break;
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    if (GUILayout.Button("+ Add Creature Type"))
                    {
                        typesArrayProp.arraySize++;
                    }
                    EditorGUI.indentLevel--;

                    // NEW: For synergy effects, draw the Search Owner option.
                    if (effectType == CardEffectData.EffectType.ConditionalPowerBoost ||
                        effectType == CardEffectData.EffectType.MutualConditionalPowerBoostEffect)
                    {
                        EditorGUILayout.PropertyField(effectElement.FindPropertyRelative("searchOwner"), new GUIContent("Search Owner"));
                    }

                    // The rest of your fields.
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

            // Replace the standard creatureType field with a dropdown.
            SerializedProperty creatureTypeProp = serializedObject.FindProperty("creatureType");
            string oldVal = creatureTypeProp.stringValue;
            int oldIndex = System.Array.IndexOf(ALL_CREATURE_TYPES, oldVal);
            if (oldIndex < 0) oldIndex = 0;
            int newIndex = EditorGUILayout.Popup("Creature Type", oldIndex, ALL_CREATURE_TYPES);
            creatureTypeProp.stringValue = ALL_CREATURE_TYPES[newIndex];

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
