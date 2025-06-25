using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SummonChoiceUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject buttonPrefab;            // Should use your existing CardButtonPrefab with CardUI
    public RectTransform buttonContainer;

    private Action<CardSO> onChoiceSelected;

    public void Show(List<CardSO> options, Action<CardSO> callback)
    {
        onChoiceSelected = callback;

        // Clear old children
        foreach (Transform child in buttonContainer)
        {
            Destroy(child.gameObject);
        }

        // Instantiate buttons for each summon option
        foreach (CardSO card in options)
        {
            GameObject buttonObj = Instantiate(buttonPrefab, buttonContainer);

            // Use your CardUI script to populate visuals
            CardUI ui = buttonObj.GetComponent<CardUI>();
            if (ui != null)
            {
                ui.SetCardData(card, setFaceDown: false);
            }
            else
            {
                Debug.LogWarning($"[SummonChoiceUI] Button prefab is missing CardUI component.");
            }

            // Hook up click logic
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => Select(card));
            }
        }

        gameObject.SetActive(true);
    }

    private void Select(CardSO chosen)
    {
        onChoiceSelected?.Invoke(chosen);
    }
}
