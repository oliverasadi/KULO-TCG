using UnityEngine;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Card", menuName = "Card")]
public class CardSO : ScriptableObject
{
    public string cardName;

    public enum CardCategory { Creature, Spell }
    public CardCategory category;

    public int power;
    public string effectDescription;
    public Sprite cardImage;

    // NEW FIELDS
    public string cardNumber;      // e.g., "OGN-01"
    public string creatureType;    // e.g., "Dragon", "Beast", etc.

    public enum BaseOrEvo { Base, Evolution }
    public BaseOrEvo baseOrEvo;    // Indicates whether this card is a base creature or an evolution

    [TextArea]
    public string extraDetails;

    // --- Card Effects ---
    [Header("Card Effects (Inline)")]
    public List<CardEffectDataValues> inlineEffects;  // Inline effect data container

    [Header("Card Effects (Asset)")]
    public List<CardEffect> effects;  // Asset-based effects (derived ScriptableObjects)

    // --- Evolution / Sacrifice Requirements ---
    [Header("Evolution / Sacrifice Requirements")]
    public bool requiresSacrifice;  // Indicates if playing this card requires sacrificing other card(s).
    public List<SacrificeRequirement> sacrificeRequirements;  // List of sacrifice requirements for this card.

    // Optional helper method to check if a given base card qualifies for evolving into this card.
    public bool CanEvolveFrom(CardSO baseCard)
    {
        if (!requiresSacrifice || baseCard == null)
            return false;

        foreach (var req in sacrificeRequirements)
        {
            bool match = req.matchByCreatureType
                ? (baseCard.creatureType == req.requiredCardName)
                : (baseCard.cardName == req.requiredCardName);
            if (match)
                return true;
        }
        return false;
    }
}

[Serializable]
public class SacrificeRequirement
{
    // Use requiredCardName as the identifier. If matchByCreatureType is true,
    // the requirement will be checked against the card's creatureType instead.
    public string requiredCardName;
    public bool matchByCreatureType;
    public int count;
}

/// <summary>
/// Inline effect data container. Configure effect parameters directly on the card.
/// This class is now renamed to CardEffectDataValues to avoid conflicts.
/// </summary>
[Serializable]
public class CardEffectDataValues
{
    public enum EffectType
    {
        None,
        DrawOnSummon,
        MultipleTargetPowerBoost,
        ConditionalPowerBoost
        // Add additional effect types as needed.
    }

    public EffectType effectType = EffectType.None;

    // Parameters for a DrawOnSummon effect.
    public int cardsToDraw = 1;

    // Parameters for power boost effects.
    public int powerChange = 0; // e.g., +100 or +200

    // For conditional effects: list of required creature names (e.g., for hitocon).
    public List<string> requiredCreatureNames = new List<string>();

    // For multiple target boosts: maximum number of targets allowed.
    public int maxTargets = 0;
}
