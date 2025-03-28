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
    // Inline effect data container; use this to set effect parameters directly on the card.
    public List<CardEffectData> inlineEffects;

    [Header("Card Effects (Asset)")]
    // Asset-based effects allow you to re-use common effects across cards.
    public List<CardEffect> effects;

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
    // The required card identifier. If matchByCreatureType is true,
    // this is compared against the card's creatureType.
    public string requiredCardName;
    public bool matchByCreatureType;
    public int count;
    public bool allowFromField = true;
    public bool allowFromHand = false;
}

