using UnityEngine;
using DG.Tweening;
using System.Collections;

public class MainMenuUI : MonoBehaviour
{
    [Header("UI Panels")]
    public RectTransform profilePanel;
    public CanvasGroup profileGroup;

    private Vector2 offScreenPos;
    private float slideDuration = 0.5f;

    void Start()
    {
        StartCoroutine(InitializePanel());
    }

    IEnumerator InitializePanel()
    {
        yield return new WaitForEndOfFrame(); // Wait for layout to initialize

        float panelHeight = profilePanel.rect.height;
        Debug.Log($"[Start] Panel initialized. Height = {panelHeight}");

        offScreenPos = new Vector2(0, -panelHeight);
        profilePanel.anchoredPosition = offScreenPos;
        profileGroup.alpha = 0;
        profilePanel.gameObject.SetActive(false);
    }

    public void ShowProfilePanel()
    {
        float panelHeight = profilePanel.rect.height;
        offScreenPos = new Vector2(0, -panelHeight);

        profilePanel.gameObject.SetActive(true);
        profilePanel.anchoredPosition = offScreenPos;
        profileGroup.alpha = 0;

        Debug.Log("[ShowProfilePanel] Sliding up...");
        profilePanel.DOAnchorPosY(0, slideDuration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() => Debug.Log("[ShowProfilePanel] Slide in complete"));

        profileGroup.DOFade(1f, slideDuration);

        var ui = profilePanel.GetComponent<ProfilePanelUI>();
        if (ui != null)
            ui.RefreshUI();
    }

    public void HideProfilePanel()
    {
        float panelHeight = profilePanel.rect.height;
        Vector2 hidePos = new Vector2(0, -panelHeight * 1.25f); // or even *2f

        Debug.Log($"[HideProfilePanel] Hiding with panel height = {panelHeight}, hidePos = {hidePos}");

        profilePanel.DOAnchorPos(hidePos, slideDuration)
            .SetEase(Ease.InCubic)
            .OnStart(() => Debug.Log("[HideProfilePanel] Slide down started"))
            .OnUpdate(() => Debug.Log($"[HideProfilePanel] Y = {profilePanel.anchoredPosition.y}"))
            .OnComplete(() =>
            {
                Debug.Log("[HideProfilePanel] Slide complete, hiding panel.");
                profilePanel.gameObject.SetActive(false);
            });

        profileGroup.DOFade(0f, slideDuration)
            .OnStart(() => Debug.Log("[HideProfilePanel] Fade started"));
    }
}
