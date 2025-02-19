using UnityEngine;

[CreateAssetMenu(fileName = "New Card", menuName = "Card")]
public class CardSO : ScriptableObject
{
    public string cardName;
    public enum CardCategory { Creature, Spell, Evolution }
    public CardCategory category;
    public int power;
    public string effectDescription;
    public Sprite cardImage;
}
