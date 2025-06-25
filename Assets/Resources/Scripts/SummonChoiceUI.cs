using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SummonChoiceUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject buttonPrefab;            // Card prefab with CardUI
    public RectTransform buttonContainer;
    public TextMeshProUGUI effectDescriptionText;
    public RectTransform rootPanel;            // 👈 Assign in inspector (main panel)

    private Action<CardSO> onChoiceSelected;
    private CardSO selectedCard;               // Store selected card

    /// <summary>
    /// Shows the summon UI with card options and effect description.
    /// </summary>
    public void Show(List<CardSO> options, Action<CardSO> callback, string effectDescription = null)
    {
        onChoiceSelected = callback;

        // Set effect description
        if (effectDescriptionText != null && !string.IsNullOrEmpty(effectDescription))
            effectDescriptionText.text = effectDescription;

        // Clear previous buttons
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        // Create one button per summon option
        foreach (CardSO card in options)
        {
            GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer);

            // Populate visuals
            CardUI ui = buttonObj.GetComponent<CardUI>();
            if (ui != null)
                ui.SetCardData(card, setFaceDown: false);
            else
                Debug.LogWarning("[SummonChoiceUI] Button prefab missing CardUI.");

            // Add click logic
            Button btn = buttonObj.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => Select(card));
        }

        gameObject.SetActive(true);

        // Animate panel in from top
        if (rootPanel != null)
        {
            rootPanel.anchoredPosition = new Vector2(0, 800); // offscreen start
            rootPanel.DOAnchorPos(Vector2.zero, 0.5f).SetEase(Ease.OutBack);
        }
    }

    /// <summary>
    /// Slide out panel, then invoke callback with selected card.
    /// </summary>
    public void HideWithSlideOutAndThen(Action onHidden)
    {
        if (rootPanel != null)
        {
            rootPanel.DOAnchorPosY(rootPanel.anchoredPosition.y + 800f, 0.4f)
                     .SetEase(Ease.InBack)
                     .OnComplete(() =>
                     {
                         gameObject.SetActive(false);
                         onHidden?.Invoke(); // now safe to proceed
                         Destroy(gameObject); // optional
                     });
        }
        else
        {
            gameObject.SetActive(false);
            onHidden?.Invoke();
            Destroy(gameObject);
        }
    }

    private void Select(CardSO chosen)
    {
        selectedCard = chosen;

        // Slide out and defer summon until panel is gone
        HideWithSlideOutAndThen(() =>
        {
            onChoiceSelected?.Invoke(selectedCard);
        });
    }
}
