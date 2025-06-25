using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class SummonChoiceUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject buttonPrefab;                 // Card prefab with CardUI
    public RectTransform buttonContainer;
    public TextMeshProUGUI effectDescriptionText;
    public RectTransform rootPanel;                 // Main panel (assign in inspector)
    public Button cancelButton;                     // 👈 Assign in Inspector

    private Action<CardSO> onChoiceSelected;
    private CardSO selectedCard;                    // Track selection

    /// <summary>
    /// Show summon UI with options and description.
    /// </summary>
    public void Show(List<CardSO> options, Action<CardSO> callback, string effectDescription = null)
    {
        onChoiceSelected = callback;
        selectedCard = null;

        // Set optional text
        if (effectDescriptionText != null && !string.IsNullOrEmpty(effectDescription))
            effectDescriptionText.text = effectDescription;

        // Clean previous buttons
        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        // Create buttons for each option
        foreach (CardSO card in options)
        {
            GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer);

            CardUI ui = buttonObj.GetComponent<CardUI>();
            if (ui != null) ui.SetCardData(card, setFaceDown: false);
            else Debug.LogWarning("[SummonChoiceUI] Button prefab missing CardUI.");

            Button btn = buttonObj.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => Select(card));
        }

        // Cancel button logic
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(() =>
            {
                selectedCard = null;
                HideWithSlideOutAndThen(() =>
                {
                    onChoiceSelected?.Invoke(null); // null = cancel
                });
            });
        }

        gameObject.SetActive(true);

        // Animate in
        if (rootPanel != null)
        {
            rootPanel.anchoredPosition = new Vector2(0, 800);
            rootPanel.DOAnchorPos(Vector2.zero, 0.5f).SetEase(Ease.OutBack);
        }
    }

    private void Select(CardSO chosen)
    {
        selectedCard = chosen;
        HideWithSlideOutAndThen(() =>
        {
            onChoiceSelected?.Invoke(selectedCard);
        });
    }

    public void HideWithSlideOutAndThen(Action onHidden)
    {
        if (rootPanel != null)
        {
            rootPanel.DOAnchorPosY(rootPanel.anchoredPosition.y + 800f, 0.4f)
                     .SetEase(Ease.InBack)
                     .OnComplete(() =>
                     {
                         gameObject.SetActive(false);
                         onHidden?.Invoke();
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
}
