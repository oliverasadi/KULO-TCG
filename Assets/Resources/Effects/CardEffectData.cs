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
        ReplaceAfterOpponentTurn
    }

    public EffectType effectType = EffectType.None;
    public int cardsToDraw = 1;
    public List<string> requiredCreatureNames = new List<string>();
    public int maxTargets = 0;
    public string replacementCardName = "";
    public int turnDelay = 0;
    public bool blockAdditionalPlays = false;

    // Add a prompt prefab field
    public GameObject promptPrefab;  // <-- This allows assigning UI prompts in the Inspector

    public int powerChange = 0;
}
