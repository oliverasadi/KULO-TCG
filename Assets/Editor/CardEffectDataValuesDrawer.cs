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

        // Draw the Effect Type field.
        SerializedProperty effectTypeProp = property.FindPropertyRelative("effectType");
        EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight), effectTypeProp);
        yOffset += lineHeight + spacing;

        // Get available card names for replacement if needed
        string[] availableNames = GetAvailableCardNames();

        // Handle different effect types
        CardEffectData.EffectType effectType = (CardEffectData.EffectType)effectTypeProp.enumValueIndex;
        if (effectType == CardEffectData.EffectType.ReplaceAfterOpponentTurn)
        {
            // Replacement effect type UI
            SerializedProperty replacementNameProp = property.FindPropertyRelative("replacementCardName");
            int currentIndex = System.Array.IndexOf(availableNames, replacementNameProp.stringValue);
            if (currentIndex < 0) currentIndex = 0;
            int newIndex = EditorGUI.Popup(new Rect(position.x, yOffset, position.width, lineHeight), "Replacement Card", currentIndex, availableNames);
            replacementNameProp.stringValue = availableNames[newIndex];
            yOffset += lineHeight + spacing;
        }
        else
        {
            // Default behavior for all other effect types
            SerializedProperty replacementNameProp = property.FindPropertyRelative("replacementCardName");
            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight), replacementNameProp);
            yOffset += lineHeight + spacing;
        }

        // Show common fields
        EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight), property.FindPropertyRelative("cardsToDraw"));
        yOffset += lineHeight + spacing;

        EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight), property.FindPropertyRelative("powerChange"));
        yOffset += lineHeight + spacing;

        // Handle the new AdjustPowerAdjacent effect
        if (effectType == CardEffectData.EffectType.AdjustPowerAdjacent)
        {
            // Show the specific fields for AdjustPowerAdjacentEffect
            SerializedProperty powerChangeAmountProp = property.FindPropertyRelative("powerChangeAmount");
            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight), powerChangeAmountProp);
            yOffset += lineHeight + spacing;

            SerializedProperty powerChangeTypeProp = property.FindPropertyRelative("powerChangeType");
            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight), powerChangeTypeProp);
            yOffset += lineHeight + spacing;

            SerializedProperty targetPositionsProp = property.FindPropertyRelative("targetPositions");
            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight), targetPositionsProp, true);
            yOffset += lineHeight + spacing;
        }

        // Show additional fields
        EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight), property.FindPropertyRelative("replacementDelay"));
        yOffset += lineHeight + spacing;

        EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight), property.FindPropertyRelative("blockAdditionalPlays"));
        yOffset += lineHeight + spacing;

        SerializedProperty requiredCreatureNamesProp = property.FindPropertyRelative("requiredCreatureNames");
        float reqHeight = EditorGUI.GetPropertyHeight(requiredCreatureNamesProp);
        EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, reqHeight), requiredCreatureNamesProp, true);
        yOffset += reqHeight + spacing;

        EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight), property.FindPropertyRelative("maxTargets"));
        yOffset += lineHeight + spacing;

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float height = 0f;
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        height += lineHeight + spacing; // effectType
        height += lineHeight + spacing; // replacementCardName
        height += lineHeight + spacing; // cardsToDraw
        height += lineHeight + spacing; // powerChange
        height += lineHeight + spacing; // replacementDelay
        height += lineHeight + spacing; // blockAdditionalPlays
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("requiredCreatureNames")) + spacing;
        height += lineHeight + spacing; // maxTargets

        // Add height for AdjustPowerAdjacentEffect fields if selected
        CardEffectData.EffectType effectType = (CardEffectData.EffectType)property.FindPropertyRelative("effectType").enumValueIndex;
        if (effectType == CardEffectData.EffectType.AdjustPowerAdjacent)
        {
            height += (lineHeight * 3) + (spacing * 2); // for powerChangeAmount, powerChangeType, and targetPositions
        }

        return height;
    }

    // Helper method to get available card names
    private string[] GetAvailableCardNames()
    {
        if (DeckManager.instance != null)
        {
            return DeckManager.instance.GetAllCardNames();
        }
        else
        {
            // Fallback: load all CardSO assets from Resources/Cards directly
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
