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
    public string creatureType;    // e.g., "Dragon," "Beast," etc.

    public enum BaseOrEvo { Base, Evolution }
    public BaseOrEvo baseOrEvo;    // Indicates whether this card is a base creature or an evolution

    // New extra field for creature cards only.
    [TextArea]
    public string extraDetails;

    // --- Evolution / Sacrifice Requirements ---
    [Header("Evolution / Sacrifice Requirements")]
    public bool requiresSacrifice;  // Indicates if playing this card requires sacrificing other card(s).
    public List<SacrificeRequirement> sacrificeRequirements;  // List of sacrifice requirements for this card.

    // Optional helper method to check if a given base card qualifies for evolving into this card.
    public bool CanEvolveFrom(CardSO baseCard)
    {
        if (!requiresSacrifice || baseCard == null)
            return false;

        // Iterate through each requirement and check for a match.
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

// SacrificeRequirement class can either be in a separate file or included here.
[Serializable]
public class SacrificeRequirement
{
    // Use requiredCardName as the identifier. If matchByCreatureType is true,
    // the requirement will be checked against the card's creatureType instead.
    public string requiredCardName;
    public bool matchByCreatureType;
    public int count;
}
