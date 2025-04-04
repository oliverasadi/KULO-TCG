using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(CardEffectData))]
public class CardEffectDataValuesDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float yOffset = position.y;
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        // (1) Draw effectType
        SerializedProperty effectTypeProp = property.FindPropertyRelative("effectType");
        EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight), effectTypeProp);
        yOffset += lineHeight + spacing;

        CardEffectData.EffectType effectType =
            (CardEffectData.EffectType)effectTypeProp.enumValueIndex;

        // (2) Replacement Card logic
        string[] availableNames = GetAvailableCardNames();
        if (effectType == CardEffectData.EffectType.ReplaceAfterOpponentTurn)
        {
            SerializedProperty replacementNameProp = property.FindPropertyRelative("replacementCardName");
            int currentIndex = System.Array.IndexOf(availableNames, replacementNameProp.stringValue);
            if (currentIndex < 0) currentIndex = 0;
            int newIndex = EditorGUI.Popup(new Rect(position.x, yOffset, position.width, lineHeight),
                                             "Replacement Card", currentIndex, availableNames);
            replacementNameProp.stringValue = availableNames[newIndex];
            yOffset += lineHeight + spacing;
        }
        else
        {
            SerializedProperty replacementNameProp = property.FindPropertyRelative("replacementCardName");
            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight), replacementNameProp);
            yOffset += lineHeight + spacing;
        }

        // (3) Common fields
        EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight),
                                property.FindPropertyRelative("cardsToDraw"),
                                new GUIContent("Cards to Draw"));
        yOffset += lineHeight + spacing;

        // (4) If AdjustPowerAdjacent, draw its unique fields
        if (effectType == CardEffectData.EffectType.AdjustPowerAdjacent)
        {
            SerializedProperty powerChangeAmountProp = property.FindPropertyRelative("powerChangeAmount");
            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight),
                                    powerChangeAmountProp, new GUIContent("Power Change Amount"));
            yOffset += lineHeight + spacing;

            SerializedProperty powerChangeTypeProp = property.FindPropertyRelative("powerChangeType");
            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight),
                                    powerChangeTypeProp, new GUIContent("Power Change Type"));
            yOffset += lineHeight + spacing;

            SerializedProperty targetPositionsProp = property.FindPropertyRelative("targetPositions");
            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight),
                                    targetPositionsProp, true);
            yOffset += lineHeight + spacing;

            SerializedProperty adjacencyOwnerProp = property.FindPropertyRelative("adjacencyOwnerToAffect");
            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight),
                                    adjacencyOwnerProp, new GUIContent("Owner to Affect"));
            yOffset += lineHeight + spacing;

            // Also draw requiredCreatureTypes for AdjustPowerAdjacent
            SerializedProperty typesProp = property.FindPropertyRelative("requiredCreatureTypes");
            float typesHeight = EditorGUI.GetPropertyHeight(typesProp, true);
            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, typesHeight),
                                    typesProp, new GUIContent("Required Creature Types"), true);
            yOffset += typesHeight + spacing;
        }

        // (5) Additional fields: replacementDelay, blockAdditionalPlays
        EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight),
                                property.FindPropertyRelative("replacementDelay"));
        yOffset += lineHeight + spacing;

        EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight),
                                property.FindPropertyRelative("blockAdditionalPlays"));
        yOffset += lineHeight + spacing;

        // (6) For synergy effects, show powerChange, requiredCreatureNames, requiredCreatureTypes, and Search Owner
        if (effectType == CardEffectData.EffectType.ConditionalPowerBoost
         || effectType == CardEffectData.EffectType.MutualConditionalPowerBoostEffect)
        {
            SerializedProperty powerChangeProp = property.FindPropertyRelative("powerChange");
            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight),
                                    powerChangeProp, new GUIContent("Power Change"));
            yOffset += lineHeight + spacing;

            SerializedProperty namesProp = property.FindPropertyRelative("requiredCreatureNames");
            float namesHeight = EditorGUI.GetPropertyHeight(namesProp, true);
            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, namesHeight),
                                    namesProp, new GUIContent("Required Card Names"), true);
            yOffset += namesHeight + spacing;

            SerializedProperty synergyTypesProp = property.FindPropertyRelative("requiredCreatureTypes");
            float synergyTypesHeight = EditorGUI.GetPropertyHeight(synergyTypesProp, true);
            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, synergyTypesHeight),
                                    synergyTypesProp, new GUIContent("Required Creature Types"), true);
            yOffset += synergyTypesHeight + spacing;

            // NEW: Draw the Search Owner option field
            SerializedProperty searchOwnerProp = property.FindPropertyRelative("searchOwner");
            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight),
                                    searchOwnerProp, new GUIContent("Search Owner"));
            yOffset += lineHeight + spacing;
        }

        // (7) maxTargets always at the bottom
        SerializedProperty maxTargetsProp = property.FindPropertyRelative("maxTargets");
        EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight), maxTargetsProp);
        yOffset += lineHeight + spacing;

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = 0f;
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        // effectType
        height += lineHeight + spacing;
        // replacementCardName
        height += lineHeight + spacing;
        // cardsToDraw
        height += lineHeight + spacing;

        CardEffectData.EffectType effectType =
            (CardEffectData.EffectType)property.FindPropertyRelative("effectType").enumValueIndex;

        if (effectType == CardEffectData.EffectType.AdjustPowerAdjacent)
        {
            // 4 fields for AdjustPowerAdjacent unique properties.
            height += (lineHeight * 4) + (spacing * 3);
            // requiredCreatureTypes array height for AdjustPowerAdjacent.
            SerializedProperty typesProp = property.FindPropertyRelative("requiredCreatureTypes");
            height += EditorGUI.GetPropertyHeight(typesProp, true) + spacing;
        }

        // replacementDelay
        height += lineHeight + spacing;
        // blockAdditionalPlays
        height += lineHeight + spacing;

        if (effectType == CardEffectData.EffectType.ConditionalPowerBoost
         || effectType == CardEffectData.EffectType.MutualConditionalPowerBoostEffect)
        {
            // 1 line for Power Change.
            height += lineHeight + spacing;

            // requiredCreatureNames array
            SerializedProperty namesProp = property.FindPropertyRelative("requiredCreatureNames");
            height += EditorGUI.GetPropertyHeight(namesProp, true) + spacing;

            // requiredCreatureTypes array
            SerializedProperty synergyTypesProp = property.FindPropertyRelative("requiredCreatureTypes");
            height += EditorGUI.GetPropertyHeight(synergyTypesProp, true) + spacing;

            // Search Owner field (1 line)
            height += lineHeight + spacing;
        }

        // maxTargets
        height += lineHeight + spacing;

        return height;
    }

    private string[] GetAvailableCardNames()
    {
        if (DeckManager.instance != null)
        {
            return DeckManager.instance.GetAllCardNames();
        }
        else
        {
            CardSO[] cards = Resources.LoadAll<CardSO>("Cards");
            if (cards.Length > 0)
            {
                string[] names = new string[cards.Length];
                for (int i = 0; i < cards.Length; i++)
                {
                    names[i] = cards[i].cardName;
                }
                return names;
            }
            else
            {
                return new string[] { "None" };
            }
        }
    }
}
