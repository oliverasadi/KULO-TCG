#if UNITY_EDITOR
using UnityEditor;  // Ensure this is only included in the Editor
#endif

using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class CSVCardImporter : MonoBehaviour
{
    public TextAsset csvFile; // Drag & drop the CSV file in the Inspector

    private Dictionary<string, CardSO.CardCategory> categoryMap = new Dictionary<string, CardSO.CardCategory>
    {
        { "Creature", CardSO.CardCategory.Creature },
        { "Spell", CardSO.CardCategory.Spell },
    };

    [ContextMenu("Generate Cards")]
    public void GenerateCards()
    {
        if (csvFile == null)
        {
            Debug.LogError("❌ CSV file not assigned!");
            return;
        }

        string[] lines = csvFile.text.Split('\n'); // Read CSV line by line
        int totalCards = 0; // Track successful imports

        for (int i = 1; i < lines.Length; i++) // Skip header row
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue; // Skip empty lines

            string[] fields = lines[i].Split(',', System.StringSplitOptions.None);
            if (fields.Length < 9)
            {
                Debug.LogWarning($"⚠️ Skipping malformed row {i + 1}: {lines[i]}");
                continue; // Skip if not enough fields
            }

            string cardName = fields[1].Trim(); // Card Name
            string categoryText = fields[4].Trim(); // Card Category

            Debug.Log($"📄 Processing Card: {cardName} | Category: {categoryText}");

#if UNITY_EDITOR
            string path = $"Assets/Resources/Cards/{cardName}.asset";

            if (AssetDatabase.LoadAssetAtPath<CardSO>(path) != null)
            {
                Debug.LogWarning($"⚠️ Card '{cardName}' already exists. Replacing...");
                AssetDatabase.DeleteAsset(path);
            }
#endif

            // Create new CardSO instance
            CardSO newCard = ScriptableObject.CreateInstance<CardSO>();
            newCard.cardName = cardName;
            newCard.category = categoryMap.ContainsKey(categoryText) ? categoryMap[categoryText] : CardSO.CardCategory.Creature;

            // ✅ Power Assignment (Fix Parsing Issue)
            if (newCard.category == CardSO.CardCategory.Creature)
            {
                string powerValueRaw = fields[7].Trim();
                if (int.TryParse(powerValueRaw, out int powerValue))
                {
                    newCard.power = powerValue;
                }
                else
                {
                    Debug.LogWarning($"⚠️ Skipping card '{cardName}': Invalid power value '{powerValueRaw}' at line {i + 1}");
                    continue;
                }
            }
            else
            {
                newCard.power = 0; // Default for non-creature cards
            }

            newCard.effectDescription = fields[8].Trim();
            newCard.cardImage = LoadCardSprite(cardName);

            if (newCard.cardImage == null)
            {
                Debug.LogError($"❌ No sprite found for '{cardName}'. Expected at 'Resources/CardArt/{cardName.Replace(" ", "_")}.png'");
            }

#if UNITY_EDITOR
            // Save the new asset in Editor Mode
            AssetDatabase.CreateAsset(newCard, path);
#endif

            totalCards++;
        }

#if UNITY_EDITOR
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
#endif

        Debug.Log($"✅ Successfully imported {totalCards} cards!");
    }

    private Sprite LoadCardSprite(string cardName)
    {
        string resourcePath = $"CardArt/{cardName.Replace(" ", "_").Trim()}";
        Sprite sprite = Resources.Load<Sprite>(resourcePath);

        if (sprite == null)
        {
            Debug.LogError($"❌ Missing sprite for '{cardName}'. Expected at 'Resources/CardArt/{cardName.Replace(" ", "_")}.png'");
            return Resources.Load<Sprite>("CardArt/DefaultCardImage"); // Fallback
        }
        return sprite;
    }
}
