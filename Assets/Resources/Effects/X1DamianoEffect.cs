using UnityEngine;

[CreateAssetMenu(menuName = "Card Effects/X1 Damiano Effect")]
public class X1DamianoEffect : CardEffect
{
    /// <summary>
    /// When applied, this effect immediately sends the card to the graveyard.
    /// This action is not meant to trigger a win condition.
    /// </summary>
    /// <param name="sourceCard">The CardUI that is the source of this effect.</param>
    public override void ApplyEffect(CardUI sourceCard)
    {
        Debug.Log("X1 Damiano Effect: Self-destruct initiated. Sending card to graveyard without triggering win condition.");

        // Use a special method in GridManager that removes the card without checking for win conditions.
        // Ensure you have added a method like RemoveCardWithoutWinCheck in your GridManager.
        if (GridManager.instance != null)
        {
            GridManager.instance.RemoveCardWithoutWinCheck(sourceCard.gameObject);
        }
        else
        {
            Debug.LogError("GridManager instance not found!");
        }
    }

    /// <summary>
    /// No cleanup is necessary for this one-time effect.
    /// </summary>
    /// <param name="sourceCard">The CardUI that is the source of this effect.</param>
    public override void RemoveEffect(CardUI sourceCard)
    {
        // Nothing to remove since the card self-destructs immediately.
    }
}
