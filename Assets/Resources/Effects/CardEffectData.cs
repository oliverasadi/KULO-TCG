using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CardEffectData
{
    public enum EffectType
    {
        None,
        DrawOnSummon,
        MultipleTargetPowerBoost,
        ConditionalPowerBoost,
        // You can add more effect types as needed.
    }

    public EffectType effectType = EffectType.None;

    // For DrawOnSummon:
    public int cardsToDraw = 1;

    // For Power Boost effects:
    public int powerChange = 0; // e.g., +100 or +200

    // For conditional power boost (e.g., "if X or Y is on the field")
    public List<string> requiredCreatureNames = new List<string>();

    // For multiple target boost (e.g., choose up to 3 cards to boost)
    public int maxTargets = 0;
}
