// In CardEffectData.cs
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
        MutualConditionalPowerBoostEffect,
        ConditionalPowerBoost,
        ReplaceAfterOpponentTurn,
        AdjustPowerAdjacent
    }

    public EffectType effectType = EffectType.None;
    public int cardsToDraw = 1;
    public List<string> requiredCreatureNames = new List<string>();
    public int maxTargets = 0;
    public string replacementCardName = "";
    public int turnDelay = 0;
    public bool blockAdditionalPlays = false;
    public GameObject promptPrefab;
    public int powerChange = 0;

    // Fields for AdjustPowerAdjacentEffect
    public int powerChangeAmount = 0;
    public PowerChangeType powerChangeType;
    public List<AdjacentPosition> targetPositions = new List<AdjacentPosition>();

    // ADD THIS: Which side(s) to affect
    public AdjustPowerAdjacentEffect.OwnerToAffect adjacencyOwnerToAffect
        = AdjustPowerAdjacentEffect.OwnerToAffect.Both;

    // Enums
    public enum PowerChangeType { Increase, Decrease }
    public enum AdjacentPosition { North, South, East, West, All }
}
