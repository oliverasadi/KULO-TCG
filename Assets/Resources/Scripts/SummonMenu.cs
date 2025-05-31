// (Same using directives)
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;

public class SummonMenu : MonoBehaviour
{
    public static SummonMenu currentMenu;

    public Button summonButton;
    public Button selectSacrificesButton;
    public Button sacrificeButton;
    public Button cancelButton;
    public TextMeshProUGUI sacrificeInfoText;

    public CardUI cardUI;
    [SerializeField] private RectTransform menuPanelRect;

    void Awake()
    {
        if (currentMenu != null && currentMenu != this)
        {
            Destroy(currentMenu.gameObject);
        }
        currentMenu = this;
    }

    void Start()
    {
        if (menuPanelRect == null)
        {
            Debug.LogWarning("SummonMenu: menuPanelRect is not assigned! Assign the Panel RectTransform in the Inspector.");
        }
    }

    void OnDestroy()
    {
        if (currentMenu == this)
            currentMenu = null;

        // Hide the line when the menu is destroyed
        if (PreviewLineController.Instance != null)
            PreviewLineController.Instance.HideLine();
    }

    public void Initialize(CardUI card)
    {
        cardUI = card;

        if (cardUI == null || cardUI.cardData == null)
        {
            Debug.LogError("SummonMenu.Initialize: CardUI or cardData is null.");
            return;
        }

        if (summonButton == null || selectSacrificesButton == null || cancelButton == null ||
            sacrificeButton == null || sacrificeInfoText == null)
        {
            Debug.LogError("SummonMenu.Initialize: One or more UI references are not assigned.");
            return;
        }

        bool isOnField = cardUI.isOnField;

        if (isOnField)
        {
            summonButton.gameObject.SetActive(false);
            selectSacrificesButton.gameObject.SetActive(false);
            if (SacrificeManager.instance != null && SacrificeManager.instance.IsValidSacrifice(cardUI))
            {
                sacrificeButton.gameObject.SetActive(true);
                sacrificeInfoText.text = "This card can be sacrificed. Click 'Sacrifice' to proceed.";
            }
            else
            {
                sacrificeButton.gameObject.SetActive(false);
                sacrificeInfoText.text = "This card is already in play and cannot be used.";
            }
        }
        else
        {
            bool requiresSacrifice = cardUI.cardData.requiresSacrifice;
            if (requiresSacrifice)
            {
                selectSacrificesButton.gameObject.SetActive(true);
                summonButton.gameObject.SetActive(false);
                sacrificeButton.gameObject.SetActive(false);
                sacrificeInfoText.text = string.IsNullOrEmpty(cardUI.cardData.effectDescription)
                    ? "This card requires sacrifices. Click 'Select Sacrifices' to proceed."
                    : cardUI.cardData.effectDescription;
            }
            else
            {
                selectSacrificesButton.gameObject.SetActive(false);
                summonButton.gameObject.SetActive(true);
                sacrificeButton.gameObject.SetActive(false);
                sacrificeInfoText.text = "Click 'Summon' to play this card.";
            }
        }

        summonButton.onClick.AddListener(OnSummon);
        selectSacrificesButton.onClick.AddListener(OnSelectSacrifices);
        cancelButton.onClick.AddListener(OnCancel);
        sacrificeButton.onClick.AddListener(OnSacrifice);
    }

    private void OnSummon()
    {
        if (cardUI == null || cardUI.cardData == null)
        {
            Debug.LogError("OnSummon: CardUI or cardData is null.");
            StartCoroutine(CloseAfterDelay());
            return;
        }

        if (!TurnManager.instance.CanPlayCard(cardUI.cardData))
        {
            Debug.LogWarning($"❌ Cannot summon {cardUI.cardData.cardName}: turn limit reached.");
            StartCoroutine(CloseAfterDelay());
            return;
        }

        Debug.Log($"[SummonMenu] Summon button clicked for {cardUI.cardData.cardName}");

        // ✅ Pass the card into the selection mode setup
        GridManager.instance.EnableCellSelectionMode((x, y) =>
        {
            Transform cellTransform = GameObject.Find($"GridCell_{x}_{y}")?.transform;
            if (cellTransform != null)
            {
                // Show preview line before placing the card
                if (PreviewLineController.Instance != null)
                {
                    RectTransform cardRect = cardUI.GetComponent<RectTransform>();
                    RectTransform cellRect = cellTransform.GetComponent<RectTransform>();
                    PreviewLineController.Instance.ShowLine(cardRect, cellRect);
                }

                bool success = GridManager.instance.PlaceExistingCard(x, y, cardUI.gameObject, cardUI.cardData, cellTransform);
                if (success)
                {
                    TurnManager.instance.RegisterCardPlay(cardUI.cardData);
                }

                // Hide line after placement
                if (PreviewLineController.Instance != null)
                    PreviewLineController.Instance.HideLine();
            }
            else
            {
                Debug.LogWarning($"GridCell_{x}_{y} not found.");
            }
        }, cardUI); // ✅ <-- pass selected card here

        StartCoroutine(CloseAfterDelay());
    }


    private void OnSelectSacrifices()
    {
        if (cardUI == null || cardUI.cardData == null)
        {
            Debug.LogError("OnSelectSacrifices: CardUI or cardData is null.");
            Destroy(gameObject);
            return;
        }

        Debug.Log("Select Sacrifices clicked for " + cardUI.cardData.cardName);
        SacrificeManager.instance.StartSacrificeSelection(cardUI);
        Destroy(gameObject);
    }

    private void OnSacrifice()
    {
        if (cardUI == null || cardUI.cardData == null)
        {
            Debug.LogError("OnSacrifice: CardUI or cardData is null.");
            Destroy(gameObject);
            return;
        }

        Debug.Log("Sacrifice button clicked for " + cardUI.cardData.cardName);
        SacrificeManager.instance.SelectSacrifice(cardUI.gameObject);
        Destroy(gameObject);
    }

    private void OnCancel()
    {
        Debug.Log("Summon menu canceled.");

        if (PreviewLineController.Instance != null)
            PreviewLineController.Instance.HideLine();

        Destroy(gameObject);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            PointerEventData pointerData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, results);

            bool clickedOnPanel = false;

            foreach (var result in results)
            {
                if (menuPanelRect != null && result.gameObject.transform.IsChildOf(menuPanelRect))
                {
                    clickedOnPanel = true;
                    break;
                }
            }

            if (!clickedOnPanel)
            {
                Debug.Log("[SummonMenu] Immediate outside click detected — closing.");

                if (PreviewLineController.Instance != null)
                    PreviewLineController.Instance.HideLine();

                Destroy(gameObject);
            }
        }
    }

    private IEnumerator CloseAfterDelay()
    {
        yield return null;
        if (PreviewLineController.Instance != null)
            PreviewLineController.Instance.HideLine();

        Destroy(gameObject);
    }
}
