using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;

public class XPResultsUIManager : MonoBehaviour
{
    [Header("XP Reward Display")]
    public GameObject rewardItemPrefab;
    public Transform rewardListContainer;
    public TextMeshProUGUI totalXPText;

    [Header("Splash Backgrounds")]
    public Image backgroundImage;
    public Sprite mrWaxSprite;
    public Sprite mrWaxRedSealSprite;
    public Sprite nekomataSprite;
    public Sprite nekomataCatTriFectaSprite;

    [Header("Stars")]
    public StarRatingDisplay starRatingDisplay;

    [Header("SFX")]
    public AudioClip rewardSound;
    public AudioClip totalXPSound;
    public AudioClip starSound;

    [Header("Results Panel Entrance")]
    public RectTransform resultsPanelRoot;

    [Header("Navigation")]
    public Button homeButton;

    [Header("Museum Unlock UI")]
    public GameObject museumUnlockPrefab;
    public Transform popupContainer;

    private List<string> pendingMemoryIDsToSave = new List<string>();

    void Start()
    {
        string selectedCharacter = PlayerProfile.selectedCharacterName;
        List<string> playerCardsPlayed = XPResultDataHolder.instance.cardsPlayed;

        var data = XPResultDataHolder.instance;
        if (data == null || data.rewards == null || data.rewards.Count == 0)
        {
            Debug.LogError("❌ XPResultDataHolder or rewards list is null or empty.");
            return;
        }

        // 🟢 Dynamic Splash Logic
        XPResultDataHolder.SplashBackgroundType finalBG = XPResultDataHolder.SplashBackgroundType.MrWax;

        if (selectedCharacter == "Mr.Wax")
        {
            finalBG = playerCardsPlayed.Contains("Ultimate Red Seal")
                ? XPResultDataHolder.SplashBackgroundType.MrWaxWithRedSeal
                : XPResultDataHolder.SplashBackgroundType.MrWax;
        }
        else if (selectedCharacter == "Nekomata")
        {
            finalBG = playerCardsPlayed.Contains("Cat TriFecta")
                ? XPResultDataHolder.SplashBackgroundType.NekomataWithCatTriFecta
                : XPResultDataHolder.SplashBackgroundType.Nekomata;
        }

        SetBackground(finalBG);

        // ✅ Memory Unlocks
        bool playedRedSeal = playerCardsPlayed.Contains("Ultimate Red Seal");
        bool playedCatTriFecta = playerCardsPlayed.Contains("Cat TriFecta");

        TryUnlockMemory("MrWax", selectedCharacter == "Mr.Wax");
        TryUnlockMemory("MrWax_RedSeal", selectedCharacter == "Mr.Wax" && playedRedSeal);
        TryUnlockMemory("Nekomata", selectedCharacter == "Nekomata");
        TryUnlockMemory("Nekomata_CatTriFecta", selectedCharacter == "Nekomata" && playedCatTriFecta);

        if (homeButton != null)
            homeButton.onClick.AddListener(GoToMainMenu);

        if (resultsPanelRoot != null)
        {
            Vector2 originalPos = resultsPanelRoot.anchoredPosition;
            resultsPanelRoot.anchoredPosition = new Vector2(-Screen.width, originalPos.y);

            resultsPanelRoot
                .DOAnchorPos(originalPos, 0.7f)
                .SetEase(Ease.OutExpo)
                .SetDelay(0.1f)
                .OnComplete(() => StartCoroutine(RevealRewardsSequentially(data)));
        }
        else
        {
            StartCoroutine(RevealRewardsSequentially(data));
        }
    }

    void TryUnlockMemory(string memoryResourceName, bool condition)
    {
        if (!condition) return;

        var memory = Resources.Load<MemoryCardData>("MuseumAssets/Memories/" + memoryResourceName);
        if (memory == null)
        {
            Debug.LogWarning($"❌ Could not load memory: {memoryResourceName}");
            return;
        }

        string memoryID = memory.cardID;

        if (!XPResultDataHolder.instance.memoryCardsToUnlock.Contains(memory))
        {
            XPResultDataHolder.instance.memoryCardsToUnlock.Add(memory);
            Debug.Log($"🟢 Unlocked memory (session): {memory.memoryTitle}");
        }

        var profile = ProfileManager.instance?.currentProfile;
        if (profile != null && !profile.unlockedMemories.Contains(memoryID))
        {
            pendingMemoryIDsToSave.Add(memoryID);
        }
    }

    void SavePendingMemoryUnlocks()
    {
        var profile = ProfileManager.instance?.currentProfile;
        if (profile == null) return;

        foreach (var memoryID in pendingMemoryIDsToSave)
        {
            if (!profile.unlockedMemories.Contains(memoryID))
            {
                profile.unlockedMemories.Add(memoryID);
                Debug.Log($"💾 Permanently saved unlock: {memoryID}");
            }
        }

        if (pendingMemoryIDsToSave.Count > 0)
            ProfileManager.instance.SaveProfile();

        pendingMemoryIDsToSave.Clear();
    }

    void SetBackground(XPResultDataHolder.SplashBackgroundType bg)
    {
        if (backgroundImage == null) return;

        switch (bg)
        {
            case XPResultDataHolder.SplashBackgroundType.MrWax: backgroundImage.sprite = mrWaxSprite; break;
            case XPResultDataHolder.SplashBackgroundType.MrWaxWithRedSeal: backgroundImage.sprite = mrWaxRedSealSprite; break;
            case XPResultDataHolder.SplashBackgroundType.Nekomata: backgroundImage.sprite = nekomataSprite; break;
            case XPResultDataHolder.SplashBackgroundType.NekomataWithCatTriFecta: backgroundImage.sprite = nekomataCatTriFectaSprite; break;
            default: Debug.LogWarning("⚠️ No matching splash background found."); break;
        }
    }

    IEnumerator RevealRewardsSequentially(XPResultDataHolder data)
    {
        if (totalXPText != null)
            totalXPText.alpha = 0f;

        foreach (var reward in data.rewards)
        {
            GameObject item = Instantiate(rewardItemPrefab, rewardListContainer);
            var text = item.GetComponentInChildren<TextMeshProUGUI>();
            var cg = item.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            if (text != null)
                text.text = $"{reward.Name} +{reward.XP} XP";

            cg.DOFade(1f, 0.4f).SetEase(Ease.InOutCubic);
            item.transform.localScale = Vector3.one * 0.8f;
            item.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack);

            if (rewardSound != null && AudioManager.instance != null)
            {
                AudioManager.instance.SFXSource.pitch = Random.Range(0.95f, 1.05f);
                AudioManager.instance.PlaySFX(rewardSound, 0.6f);
                AudioManager.instance.SFXSource.pitch = 1f;
            }

            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(0.3f);

        if (totalXPText != null)
        {
            totalXPText.text = $"Total XP: {data.totalXP}";
            totalXPText.alpha = 0f;

            float t = 0;
            while (t < 1f)
            {
                t += Time.deltaTime * 4f;
                totalXPText.alpha = Mathf.Clamp01(t);
                yield return null;
            }

            totalXPText.rectTransform.localScale = Vector3.one;
            totalXPText.rectTransform
                .DOScale(1.2f, 0.25f)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    totalXPText.rectTransform.DOScale(1f, 0.2f);
                });

            if (totalXPSound != null && AudioManager.instance != null)
                AudioManager.instance.PlaySFX(totalXPSound, 0.9f);
        }

        yield return new WaitForSeconds(0.3f);

        if (starRatingDisplay != null)
            yield return StartCoroutine(AnimateStarsWithSound(data.totalXP));

        SavePendingMemoryUnlocks();

        // 🏛️ Show unlocked museum memories sequentially (no overlap)
        foreach (var memory in data.memoryCardsToUnlock)
        {
            string title = "Museum Piece Unlocked";
            string description = $"Gallery Item {memory.memoryTitle}";
            Sprite icon = memory.thumbnail;

            yield return StartCoroutine(ShowMuseumUnlockSequence(title, description, icon));
        }
    }

    IEnumerator ShowMuseumUnlockSequence(string title, string description, Sprite icon)
    {
        GameObject popup = Instantiate(museumUnlockPrefab, popupContainer);
        popup.transform.SetAsLastSibling();

        var titleText = popup.transform.Find("TextContainer/TitleText")?.GetComponent<TextMeshProUGUI>();
        var subtitleText = popup.transform.Find("TextContainer/SubtitleText")?.GetComponent<TextMeshProUGUI>();
        var iconImage = popup.transform.Find("Icon")?.GetComponent<Image>();
        var rt = popup.GetComponent<RectTransform>();
        var cg = popup.GetComponent<CanvasGroup>();

        if (titleText != null) titleText.text = title;
        if (subtitleText != null) subtitleText.text = description;
        if (iconImage != null) iconImage.sprite = icon;

        Vector2 targetPos = new Vector2(-276, 98);
        Vector2 startPos = new Vector2(400, 98);
        Vector2 exitPos = new Vector2(600, 98);

        rt.anchoredPosition = startPos;
        if (cg != null) cg.alpha = 0;

        bool done = false;

        Sequence seq = DOTween.Sequence();
        seq.Append(rt.DOAnchorPos(targetPos, 0.5f).SetEase(Ease.OutCubic));
        if (cg != null) seq.Join(cg.DOFade(1, 0.4f));
        seq.AppendInterval(4f);
        seq.Append(rt.DOAnchorPos(exitPos, 0.5f).SetEase(Ease.InCubic));
        if (cg != null) seq.Join(cg.DOFade(0, 0.4f));
        seq.OnComplete(() =>
        {
            Destroy(popup);
            done = true;
        });

        yield return new WaitUntil(() => done);
    }

    IEnumerator AnimateStarsWithSound(int totalXP)
    {
        int filledCount = Mathf.Clamp(totalXP / 50, 0, 5);
        var stars = starRatingDisplay.stars;

        for (int i = 0; i < stars.Count; i++)
        {
            var star = stars[i];
            star.material = starRatingDisplay.grayscaleMat;
            star.transform.localScale = Vector3.one;
        }

        for (int i = 0; i < filledCount; i++)
        {
            var star = starRatingDisplay.stars[i];
            yield return new WaitForSeconds(0.1f);

            star.material = starRatingDisplay.normalMat;
            star.transform
                .DOScale(1.3f, 0.15f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => star.transform.DOScale(1f, 0.15f));

            if (starSound != null && AudioManager.instance != null)
                AudioManager.instance.PlaySFX(starSound, 0.8f);
        }
    }

    private void GoToMainMenu()
    {
        if (XPResultDataHolder.instance != null)
            XPResultDataHolder.instance.Clear();

        SceneManager.LoadScene("MainMenu");
    }
}
