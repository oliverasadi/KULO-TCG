using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class HelpMenuController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text contentText;
    public AudioSource uiAudioSource;

    [Header("Tab Buttons")]
    public Button howToPlayButton;
    public Button cardTypesButton;
    public Button evolutionButton;
    public Button cardEffectsButton;
    public Button controlsButton;

    [TextArea(3, 10)] public string howToPlayText;
    [TextArea(3, 10)] public string cardTypesText;
    [TextArea(3, 10)] public string evolutionText;
    [TextArea(3, 10)] public string cardEffectsText;
    [TextArea(3, 10)] public string controlsText;

    private Button currentSelected;

    // Custom highlight colors
    private Color normalColor = Color.white;
    private Color selectedColor = new Color(1f, 0.55f, 0f); // Orange

    void Start()
    {
        // Default to first tab on launch
        HighlightTab(howToPlayButton);
        PlayAndSet(howToPlayText);
    }

    public void ShowHowToPlay() => SelectTab(howToPlayButton, howToPlayText);
    public void ShowCardTypes() => SelectTab(cardTypesButton, cardTypesText);
    public void ShowEvolution() => SelectTab(evolutionButton, evolutionText);
    public void ShowCardEffects() => SelectTab(cardEffectsButton, cardEffectsText);
    public void ShowControls() => SelectTab(controlsButton, controlsText);

    private void SelectTab(Button tabButton, string newText)
    {
        HighlightTab(tabButton);
        PlayAndSet(newText);
    }

    private void PlayAndSet(string newText)
    {
        contentText.text = newText;
        if (uiAudioSource != null) uiAudioSource.Play();
    }

    private void HighlightTab(Button newSelected)
    {
        // Reset previous tab
        if (currentSelected != null)
        {
            var resetColors = currentSelected.colors;
            resetColors.normalColor = normalColor;
            currentSelected.colors = resetColors;
        }

        // Set new tab highlight
        currentSelected = newSelected;
        if (currentSelected != null)
        {
            var highlightColors = currentSelected.colors;
            highlightColors.normalColor = selectedColor;
            currentSelected.colors = highlightColors;
        }
    }

    public void GoBackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu"); // Replace with your real scene name
    }
}
