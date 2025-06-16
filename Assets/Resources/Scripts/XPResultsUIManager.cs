using UnityEngine;
using TMPro;
using UnityEngine.UI;
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
}
