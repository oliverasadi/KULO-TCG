using UnityEngine;
using System;
using System.Collections.Generic;

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
        AdjustPowerAdjacent // <-- Add this line for AdjustPowerAdjacentEffect
    }

    public EffectType effectType = EffectType.None;
    public int cardsToDraw = 1;
    public List<string> requiredCreatureNames = new List<string>();
    public int maxTargets = 0;
    public string replacementCardName = "";
    public int turnDelay = 0;
    public bool blockAdditionalPlays = false;

    // Add a prompt prefab field for UI prompt if needed
    public GameObject promptPrefab;  // <-- This allows assigning UI prompts in the Inspector

    public int powerChange = 0;

    // --- New Properties for AdjustPowerAdjacentEffect ---
    public int powerChangeAmount = 0;  // The amount by which power will change
    public PowerChangeType powerChangeType;  // Type of change: Increase or Decrease
    public List<AdjacentPosition> targetPositions = new List<AdjacentPosition>();  // Which adjacent positions to target

    // Enum for the type of power change (Increase or Decrease)
    public enum PowerChangeType
    {
        Increase,
        Decrease
    }

    // Enum to define valid adjacent positions (North, South, East, West, All)
    public enum AdjacentPosition
    {
        North,
        South,
        East,
        West,
        All
    }
}
