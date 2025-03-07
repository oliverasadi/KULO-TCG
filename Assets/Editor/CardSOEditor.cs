using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CardSO))]
public class CardSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Update the serialized object.
        serializedObject.Update();

        // Draw common fields for all cards.
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cardName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cardNumber"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("category"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("cardImage"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("effectDescription"));

        // Draw the asset-based Effects list.
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Card Effects (Assets)", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("effects"), true);

        // Draw the inline effects list (if present)
        SerializedProperty inlineEffectsProp = serializedObject.FindProperty("inlineEffects");
        if (inlineEffectsProp != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Inline Card Effects", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(inlineEffectsProp, true);
        }
        else
        {
            EditorGUILayout.HelpBox("No Inline Effects field found. Make sure your CardSO script has a 'public List<CardEffectData> inlineEffects;' field.", MessageType.Info);
        }

        // Get a reference to the target CardSO instance.
        CardSO cardSO = (CardSO)target;

        // If the card is a Creature, show creature-specific fields.
        if (cardSO.category == CardSO.CardCategory.Creature)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("power"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("creatureType"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("baseOrEvo"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("extraDetails"));

            // Evolution / Sacrifice Requirements Section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Evolution / Sacrifice Requirements", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("requiresSacrifice"));
            if (serializedObject.FindProperty("requiresSacrifice").boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("sacrificeRequirements"), true);
            }
        }
        // For Spell cards, add spell-specific fields if needed.

        // Apply changes to the serialized object.
        serializedObject.ApplyModifiedProperties();
    }
}
