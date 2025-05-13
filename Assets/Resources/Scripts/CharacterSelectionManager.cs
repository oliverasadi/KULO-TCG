using UnityEngine;

public class CharacterSelectManager : MonoBehaviour
{
    // Assign these via Inspector to the respective DeckDataSO assets for each character
    public DeckDataSO waxDeck;
    public DeckDataSO nekomataDeck;
    public DeckDataSO xuTaishiDeck;

    // This method is called when the Mr. Wax option is clicked
    public void SelectMrWax()
    {
        // Store the selected deck for the player
        PlayerManager.selectedCharacterDeck = waxDeck;
        // (Optional) Store the character choice if needed for other purposes, e.g.:
        // PlayerManager.selectedCharacterName = "Mr. Wax";

        // Load the main game scene with a fade transition
        AutoFade.LoadScene("KULO", 0.3f, 0.3f, Color.black);
    }

    public void SelectNekomata()
    {
        PlayerManager.selectedCharacterDeck = nekomataDeck;
        // Optional: PlayerManager.selectedCharacterName = "Nekomata";
        AutoFade.LoadScene("KULO", 0.3f, 0.3f, Color.black);
    }

    public void SelectXuTaishi()
    {
        PlayerManager.selectedCharacterDeck = xuTaishiDeck;
        // Optional: PlayerManager.selectedCharacterName = "Xu Taishi";
        AutoFade.LoadScene("KULO", 0.3f, 0.3f, Color.black);
    }
}
