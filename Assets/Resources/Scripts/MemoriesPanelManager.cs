using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Collections;

public class MemoriesPanelManager : MonoBehaviour
{
    public GameObject panelRoot; // Assign the MemoriesPanel root
    public Button artTabButton, loreTabButton, cutsceneTabButton;
    public RectTransform tabBar;       // The tab button group
    public RectTransform contentRoot;  // The card/grid container
    private Vector2 tabBarStartPos;
    private Vector2 contentStartPos;

    public float tabSlideX = -1000f;
    public float contentSlideY = -500f;
    public float animDuration = 0.4f;

    private enum Tab { Art, Lore, Cutscenes }
    private Tab currentTab;

    private MemoryTabManager tabManager;

    void Start()
    {
        tabBarStartPos = tabBar.anchoredPosition;
        contentStartPos = contentRoot.anchoredPosition;

        artTabButton.onClick.AddListener(() => SwitchTab(Tab.Art));
        loreTabButton.onClick.AddListener(() => SwitchTab(Tab.Lore));
        cutsceneTabButton.onClick.AddListener(() => SwitchTab(Tab.Cutscenes));

        tabManager = FindObjectOfType<MemoryTabManager>();
    }

    public void OpenPanel()
    {
        panelRoot.SetActive(true);
        Canvas.ForceUpdateCanvases(); // Ensure layout is ready

        // Animate from offscreen
        tabBar.anchoredPosition = tabBarStartPos + new Vector2(-800f, 0f);
        tabBar.DOAnchorPos(tabBarStartPos, animDuration).SetEase(Ease.OutBack);

        contentRoot.anchoredPosition = contentStartPos + new Vector2(0f, -500f);
        contentRoot.DOAnchorPos(contentStartPos, animDuration)
            .SetEase(Ease.OutCubic)
            .SetDelay(0.1f);

        // 🔁 This simulates a click on the Art tab, including any attached listeners
        artTabButton.onClick.Invoke();
    }




    public void ClosePanel()
    {
        panelRoot.SetActive(false);
    }

    private void SwitchTab(Tab tab)
    {
        currentTab = tab;

        if (tabManager == null) return;

        switch (tab)
        {
            case Tab.Art:
                tabManager.LoadTab(tabManager.artCards);
                break;
            case Tab.Lore:
                tabManager.LoadTab(tabManager.loreCards);
                break;
            case Tab.Cutscenes:
                tabManager.LoadTab(tabManager.cutsceneCards);
                break;
        }
    }
}
