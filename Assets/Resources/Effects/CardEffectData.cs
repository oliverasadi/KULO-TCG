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
    public List<string> requiredCreatureTypes = new List<string>();
    public int maxTargets = 0;
    public string replacementCardName = "";
    public int turnDelay = 0;
    public bool blockAdditionalPlays = false;
    public GameObject promptPrefab;
    public int powerChange = 0;

    // NEW: When true, effects like ConditionalPowerBoost will count the matching cards
    // and multiply powerChange by the number of matches.
    public bool useCountMode = false;

    // For AdjustPowerAdjacent
    public int powerChangeAmount = 0;
    public PowerChangeType powerChangeType;
    public List<AdjacentPosition> targetPositions = new List<AdjacentPosition>();
    public AdjustPowerAdjacentEffect.OwnerToAffect adjacencyOwnerToAffect = AdjustPowerAdjacentEffect.OwnerToAffect.Both;

    // NEW: Define the SearchOwnerOption enum and field here.
    public enum SearchOwnerOption { Mine, AI, Both }
    public SearchOwnerOption searchOwner = SearchOwnerOption.Both;

    // Enums for AdjustPowerAdjacent
    public enum PowerChangeType { Increase, Decrease }
    public enum AdjacentPosition { North, South, East, West, All }
}
