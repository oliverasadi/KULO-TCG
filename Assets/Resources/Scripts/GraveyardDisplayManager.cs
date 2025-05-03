using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class GraveyardDisplayManager : MonoBehaviour
{
    [Header("Grid Content")]
    public Transform contentParent;           // Assign the Content of ScrollView
    public GameObject cardDisplayPrefab;      // Assign a lightweight CardUI prefab
    public Button closeButton;                // Assign in Inspector

    [Header("Info Panel UI")]
    [SerializeField] private Image cardArtImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI powerText;
    [SerializeField] private TextMeshProUGUI effectText;

    private void Awake()
    {
        closeButton.onClick.AddListener(() => gameObject.SetActive(false));
    }

    public void ShowGraveyard(List<GameObject> graveyardCards)
    {
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        foreach (GameObject cardObj in graveyardCards)
        {
            var handler = cardObj.GetComponent<CardHandler>();
            if (handler == null || handler.cardData == null) continue;

            GameObject cardInstance = Instantiate(cardDisplayPrefab, contentParent);
            var ui = cardInstance.GetComponent<CardUI>();
            if (ui != null)
            {
                ui.SetCardData(handler.cardData);
                ui.isInDeck = false;
                ui.isOnField = false;
                ui.isInGraveyard = true;
                ui.isCloneInGraveyardPanel = true; // ← mark as a UI clone, not an actual card in zone
            }

            // Add hover trigger to show info
            EventTrigger trigger = cardInstance.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener((eventData) => ShowCardInfo(handler.cardData));
            trigger.triggers.Add(entry);
        }

        gameObject.SetActive(true);
    }

    public void ShowCardInfo(CardSO card)
    {
        if (card == null) return;

        if (cardArtImage != null)
            cardArtImage.sprite = card.cardImage;

        if (nameText != null)
            nameText.text = card.cardName;

        if (typeText != null)
            typeText.text = $"Type: {card.category}"; // ← using .category, not .cardType

        if (powerText != null)
            powerText.text = $"Power: {card.power}";

        if (effectText != null)
            effectText.text = card.effectDescription;
    }
}
