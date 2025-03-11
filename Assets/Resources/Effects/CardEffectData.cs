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
        ReplaceAfterOpponentTurn
            

    }

    public EffectType effectType = EffectType.None;
    public int cardsToDraw = 1;
    public List<string> requiredCreatureNames = new List<string>();
    public int maxTargets = 0;
    public string replacementCardName = "";
    public int replacementDelay = 0;
    public bool blockAdditionalPlays = false;

    // Added field from CardSO.cs:
    public List<CardSO> requiredCardAssets = new List<CardSO>();
    public int powerChange = 0;
}
