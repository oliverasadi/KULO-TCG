using UnityEngine;
using TMPro;
using UnityEngine.UI; // ✅ this is the fix!

public class CutInOverlayUI : MonoBehaviour
{
    public TextMeshProUGUI topText;
    public TextMeshProUGUI bottomText;
    public Image artImage;

    public Sprite redSealSprite, catTrifectaSprite, whiteTigerSprite;

    public void Setup(string cardName)
    {
        switch (cardName)
        {
            case "Ultimate Red Seal":
                topText.text = "ULTIMATE RED SEAL";
                bottomText.text = "HAS ARRIVED";
                artImage.sprite = redSealSprite;
                break;

            case "Cat TriFecta":
                topText.text = "CAT TRIFECTA";
                bottomText.text = "UNLEASHED!";
                artImage.sprite = catTrifectaSprite;
                break;

            case "Legendary White Tiger of the Pagoda":
                topText.text = "LEGENDARY WHITE TIGER";
                bottomText.text = "AWAKENS!";
                artImage.sprite = whiteTigerSprite;
                break;
        }
    }
}
