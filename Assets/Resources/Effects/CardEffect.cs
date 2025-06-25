using UnityEngine;

/// <summary>
/// Base class for card effects. 
/// Derived classes (e.g., DrawCardOnSummonEffect, etc.)
/// must implement how the effect is applied and removed.
/// </summary>
public abstract class CardEffect : ScriptableObject
{
    /// <summary>
    /// Called when the effect should be applied (e.g., when the card is summoned).
    /// </summary>
    /// <param name="sourceCard">The CardUI that is the source of this effect.</param>
    public abstract void ApplyEffect(CardUI sourceCard);

    /// <summary>
    /// Called when the effect should be removed (e.g., when the card leaves the field).
    /// </summary>
    /// <param name="sourceCard">The CardUI that is the source of this effect.</param>
    public abstract void RemoveEffect(CardUI sourceCard);

    /// <summary>
    /// Optional override. Return false to block summon based on custom condition.
    /// </summary>
    /// <param name="sourceCard">The CardUI that is the source of this effect.</param>
    public virtual bool CanBeSummoned(CardUI sourceCard)
    {
        return true; // Default allows summon
    }
}