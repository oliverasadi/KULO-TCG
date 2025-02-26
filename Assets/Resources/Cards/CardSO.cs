using UnityEngine;

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
}
