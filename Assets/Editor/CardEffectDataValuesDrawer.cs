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

        // Prepare available names from DeckManager or fallback from Resources.
        string[] availableNames = GetAvailableCardNames();

        // Draw replacementCardName as a popup if the effect type is ReplaceAfterOpponentTurn.
        SerializedProperty replacementNameProp = property.FindPropertyRelative("replacementCardName");
        CardEffectData.EffectType effectType = (CardEffectData.EffectType)effectTypeProp.enumValueIndex;
        if (effectType == CardEffectData.EffectType.ReplaceAfterOpponentTurn)
        {
            int currentIndex = System.Array.IndexOf(availableNames, replacementNameProp.stringValue);
            if (currentIndex < 0) currentIndex = 0;
            int newIndex = EditorGUI.Popup(new Rect(position.x, yOffset, position.width, lineHeight), "Replacement Card", currentIndex, availableNames);
            replacementNameProp.stringValue = availableNames[newIndex];
            yOffset += lineHeight + spacing;
        }
        else
        {
            // Draw a simple text field for other effect types.
            EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight), replacementNameProp);
            yOffset += lineHeight + spacing;
        }

        // Draw the remaining fields.
        SerializedProperty cardsToDrawProp = property.FindPropertyRelative("cardsToDraw");
        EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight), cardsToDrawProp);
        yOffset += lineHeight + spacing;

        SerializedProperty powerChangeProp = property.FindPropertyRelative("powerChange");
        EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight), powerChangeProp);
        yOffset += lineHeight + spacing;

        SerializedProperty replacementDelayProp = property.FindPropertyRelative("replacementDelay");
        EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight), replacementDelayProp);
        yOffset += lineHeight + spacing;

        SerializedProperty blockAdditionalPlaysProp = property.FindPropertyRelative("blockAdditionalPlays");
        EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, lineHeight), blockAdditionalPlaysProp);
        yOffset += lineHeight + spacing;

        SerializedProperty requiredCreatureNamesProp = property.FindPropertyRelative("requiredCreatureNames");
        float reqHeight = EditorGUI.GetPropertyHeight(requiredCreatureNamesProp);
        EditorGUI.PropertyField(new Rect(position.x, yOffset, position.width, reqHeight), requiredCreatureNamesProp, true);
        yOffset += reqHeight + spacing;

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
        height += lineHeight + spacing; // effectType
        height += lineHeight + spacing; // replacementCardName
        height += lineHeight + spacing; // cardsToDraw
        height += lineHeight + spacing; // powerChange
        height += lineHeight + spacing; // replacementDelay
        height += lineHeight + spacing; // blockAdditionalPlays
        height += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("requiredCreatureNames")) + spacing;
        height += lineHeight + spacing; // maxTargets
        return height;
    }

    // Helper method to get available card names.
    private string[] GetAvailableCardNames()
    {
        if (DeckManager.instance != null)
        {
            return DeckManager.instance.GetAllCardNames();
        }
        else
        {
            // Fallback: load all CardSO assets from Resources/Cards directly.
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
