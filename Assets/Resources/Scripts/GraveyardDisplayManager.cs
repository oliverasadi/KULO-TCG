using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

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
        // Ensure closeButton always hides the panel and resets state
        closeButton.onClick.RemoveAllListeners();
        closeButton.onClick.AddListener(() => {
            gameObject.SetActive(false);
        });
    }

    private void OnEnable()
    {
        // Re-enable interaction and ensure close button is visible
        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(true);
            closeButton.interactable = true;
        }
    }

    public void ShowGraveyard(List<GameObject> graveyardCards)
    {
        // Clear previous content
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        // Instantiate card clones
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
                ui.isCloneInGraveyardPanel = true;
            }

            // Add hover to show card info
            EventTrigger trigger = cardInstance.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            entry.callback.AddListener((eventData) => ShowCardInfo(handler.cardData));
            trigger.triggers.Add(entry);
        }

        // Make sure the panel is visible and interactive
        gameObject.SetActive(true);
        // Reset close button in case it was hidden
        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(true);
            closeButton.interactable = true;
        }
    }

    public void ShowCardInfo(CardSO card)
    {
        if (card == null) return;

        if (cardArtImage != null)
            cardArtImage.sprite = card.cardImage;

        if (nameText != null)
            nameText.text = card.cardName;

        if (typeText != null)
            typeText.text = $"Type: {card.category}";

        if (powerText != null)
            powerText.text = $"Power: {card.power}";

        if (effectText != null)
            effectText.text = card.effectDescription;
    }

    /// <summary>
    /// Highlights cards that can be clicked for effects like Spirit of Mimi.
    /// </summary>
    public void HighlightSelectableCards(List<GameObject> validCards)
    {
        foreach (Transform child in contentParent)
        {
            var ui = child.GetComponent<CardUI>();
            if (ui == null) continue;

            var realCard = validCards.FirstOrDefault(g =>
            {
                var data = g.GetComponent<CardHandler>()?.cardData;
                return data != null && data.cardName == ui.cardData.cardName;
            });

            if (realCard != null)
            {
                var selector = child.gameObject.AddComponent<SelectableGraveCard>();
                selector.Setup(realCard);
            }
        }
    }

    /// <summary>
    /// Clears all selection handlers from cards.
    /// </summary>
    public void ClearHighlights()
    {
        foreach (Transform child in contentParent)
        {
            var select = child.GetComponent<SelectableGraveCard>();
            if (select != null)
                Destroy(select);
        }
    }
}
