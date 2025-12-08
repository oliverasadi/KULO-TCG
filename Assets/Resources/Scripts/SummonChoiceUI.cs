using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.EventSystems;

public class SummonChoiceUI : MonoBehaviour,
    IPointerDownHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("UI References")]
    public GameObject buttonPrefab;
    public RectTransform buttonContainer;
    public TextMeshProUGUI effectDescriptionText;
    public RectTransform rootPanel;
    public Button cancelButton;

    private CanvasGroup canvasGroup;
    private Action<CardSO> onChoiceSelected;
    private CardSO selectedCard;

    private Tween hoverTween;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            Debug.LogWarning("SummonChoiceUI: CanvasGroup not found – add one for dimming effect.");
    }

    public void Show(
        List<CardSO> options,
        Action<CardSO> callback,
        string effectDescription = null,
        Vector2? screenOffset = null
    )
    {
        onChoiceSelected = callback;
        selectedCard = null;

        if (effectDescriptionText != null && !string.IsNullOrEmpty(effectDescription))
            effectDescriptionText.text = effectDescription;

        foreach (Transform child in buttonContainer)
            Destroy(child.gameObject);

        foreach (CardSO card in options)
        {
            GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer);

            CardUI ui = buttonObj.GetComponent<CardUI>();
            if (ui != null) ui.SetCardData(card, setFaceDown: false);
            else Debug.LogWarning("[SummonChoiceUI] Button prefab missing CardUI.");

            Button btn = buttonObj.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(() => Select(card));
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
            cancelButton.onClick.AddListener(() =>
            {
                selectedCard = null;
                HideWithSlideOutAndThen(() =>
                {
                    onChoiceSelected?.Invoke(null);
                });
            });
        }

        gameObject.SetActive(true);

        // NOTE: no SetFocused() call here – focus is controlled from GridManager

        if (rootPanel != null)
        {
            Vector2 targetPos = screenOffset ?? Vector2.zero;
            rootPanel.anchoredPosition = targetPos + new Vector2(0, 800);
            rootPanel.DOAnchorPos(targetPos, 0.5f).SetEase(Ease.OutBack);
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
                         Destroy(gameObject);
                     });
        }
        else
        {
            gameObject.SetActive(false);
            onHidden?.Invoke();
            Destroy(gameObject);
        }
    }

    // CLICK → bring to front & focus
    public void OnPointerDown(PointerEventData eventData)
    {
        transform.SetAsLastSibling();
        SetFocused(true);
        UnfocusOtherPanels();
    }

    // HOVER → wiggle if behind another panel
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsTopPanel()) return;

        if (hoverTween != null) hoverTween.Kill();

        if (rootPanel != null)
        {
            hoverTween = rootPanel.DOAnchorPos(
                    rootPanel.anchoredPosition + new Vector2(4f, 0f),
                    0.08f
                )
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.OutQuad);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (hoverTween != null)
            hoverTween.Kill();
    }

    private bool IsTopPanel()
    {
        Transform parent = transform.parent;
        if (parent == null || parent.childCount == 0) return true;
        return parent.GetChild(parent.childCount - 1) == transform;
    }

    // 👇 make this PUBLIC so GridManager can control focus
    public void SetFocused(bool focused)
    {
        float targetAlpha = focused ? 1f : 0.75f;
        float targetScale = focused ? 1f : 0.95f;

        if (canvasGroup != null)
            canvasGroup.DOFade(targetAlpha, 0.2f);

        transform.DOScale(targetScale, 0.2f);
    }

    private void UnfocusOtherPanels()
    {
        Transform parent = transform.parent;
        if (parent == null) return;

        foreach (Transform child in parent)
        {
            if (child == transform) continue;

            SummonChoiceUI ui = child.GetComponent<SummonChoiceUI>();
            if (ui != null)
                ui.SetFocused(false);
        }
    }
}
