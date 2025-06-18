using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MemoriesPanelManager : MonoBehaviour
{
    public GameObject panelRoot; // Assign the MemoriesPanel root
    public Button artTabButton, loreTabButton, cutsceneTabButton;
    public GameObject artContent, loreContent, cutsceneContent;

    private enum Tab { Art, Lore, Cutscenes }
    private Tab currentTab;

    void Start()
    {
        artTabButton.onClick.AddListener(() => SwitchTab(Tab.Art));
        loreTabButton.onClick.AddListener(() => SwitchTab(Tab.Lore));
        cutsceneTabButton.onClick.AddListener(() => SwitchTab(Tab.Cutscenes));
    }

    public void OpenPanel()
    {
        panelRoot.SetActive(true);
        SwitchTab(Tab.Art);
    }

    public void ClosePanel()
    {
        panelRoot.SetActive(false);
    }

    private void SwitchTab(Tab tab)
    {
        currentTab = tab;

        artContent.SetActive(tab == Tab.Art);
        loreContent.SetActive(tab == Tab.Lore);
        cutsceneContent.SetActive(tab == Tab.Cutscenes);

        // Optional: highlight current tab visually here
    }
}
