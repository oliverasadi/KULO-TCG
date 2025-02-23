using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CardUI : MonoBehaviour
{
    [Header("UI Elements")]
    public Image cardArtImage; // Displays the card image
    public TextMeshProUGUI cardNameText; // Displays the card name

    public Sprite cardBackSprite; // Assign in Prefab Inspector // Holds the back of the card sprite
    private CardSO cardData; // Stores the card's ScriptableObject data
    private bool isFaceDown = false;
    public bool isInDeck = false; // Track if card is in deck

    void Start()
    {
        LoadCardBack();
    }

    private void LoadCardBack()
    {
        if (cardBackSprite == null)
        {
            cardBackSprite = Resources.Load<Sprite>("CardArt/CardBack"); // Supports .jpeg and .jpg
            if (cardBackSprite == null)
            {
                Debug.LogError("❌ Card back image not found! Ensure it's in Resources/CardArt/CardBack.png");
            }
        }
    }

    public void SetCardData(CardSO card, bool setFaceDown = false)
    {
        if (card == null)
        {
            Debug.LogError("❌ Card data is missing!");
            return;
        }

        cardData = card;
        isInDeck = false;

        if (cardData != null)
        {
            Debug.Log($"✅ Card name set: {cardData.cardName}");
        }
        else
        {
            Debug.LogError($"❌ cardNameText is not assigned in {gameObject.name}!");
        }

        if (cardArtImage != null)
        {
            if (setFaceDown)
            {
                SetFaceDown();
            }
            else if (cardData.cardImage != null)
            {
                cardArtImage.sprite = cardData.cardImage;
            }
        }
        else
        {
            Debug.LogError($"Card Art Missing for {cardData.cardName}");
        }
    }

    public void SetFaceDown()
    {
        isFaceDown = true;
        if (cardArtImage != null && cardBackSprite != null)
        {
            cardArtImage.sprite = cardBackSprite;
        }
    }

    public void RevealCard()
    {
        if (isFaceDown)
        {
            isFaceDown = false;
            StartCoroutine(FlipCardAnimationWithRotation());
        }
        else
        {
            if (cardArtImage != null && cardData != null && cardData.cardImage != null)
            {
                cardArtImage.sprite = cardData.cardImage;
            }
        }
    }

    private IEnumerator FlipCardAnimationWithRotation()
    {
        float duration = 0.5f;
        float time = 0;

        while (time < duration / 2)
        {
            float angle = Mathf.Lerp(0, 90, time / (duration / 2));
            transform.rotation = Quaternion.Euler(0, angle, 0);
            time += Time.deltaTime;
            yield return null;
        }

        if (cardArtImage != null && cardData != null && cardData.cardImage != null)
        {
            cardArtImage.sprite = cardData.cardImage;
        }

        yield return null; // Ensures UI updates before continuing

        time = 0;
        while (time < duration / 2)
        {
            float angle = Mathf.Lerp(90, 0, time / (duration / 2));
            transform.rotation = Quaternion.Euler(0, angle, 0);
            time += Time.deltaTime;
            yield return null;
        }

        isFaceDown = false;
    }
}
