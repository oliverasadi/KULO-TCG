using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class ReplaceEffectPrompt : MonoBehaviour
{
    [Header("Button References")]
    public Button yesButton;
    public Button noButton;

    [Header("UI Text Reference")]
    public TextMeshProUGUI descriptionText;  // Drag your text field (in the prompt prefab) here

    // This event will be invoked with true for Yes, false for No.
    public UnityEvent<bool> OnResponse;

    void Start()
    {
        // Ensure OnResponse is not null.
        if (OnResponse == null)
            OnResponse = new UnityEvent<bool>();

        if (yesButton != null)
            yesButton.onClick.AddListener(() => OnResponse.Invoke(true));
        else
            Debug.LogError("Yes Button is not assigned on the ReplaceEffectPrompt prefab!");

        if (noButton != null)
            noButton.onClick.AddListener(() => OnResponse.Invoke(false));
        else
            Debug.LogError("No Button is not assigned on the ReplaceEffectPrompt prefab!");
    }

    /// <summary>
    /// Initializes the prompt by setting the description text.
    /// </summary>
    /// <param name="cardName">The name of the card whose effect is being activated.</param>
    /// <param name="effectDescription">The effect description to display.</param>
    public void Initialize(string cardName, string effectDescription)
    {
        if (descriptionText != null)
        {
            descriptionText.text = $"Do you want to activate the effect of '{cardName}' - '{effectDescription}'?";
        }
        else
        {
            Debug.LogWarning("Description text field is not assigned in ReplaceEffectPrompt.");
        }
    }
}
