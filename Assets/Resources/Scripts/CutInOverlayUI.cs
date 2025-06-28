using UnityEngine;
using TMPro;
using UnityEngine.UI;
using DG.Tweening;
using SpriteShatter;

public class CutInOverlayUI : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform topTextPanel;
    public RectTransform bottomTextPanel;
    public Image artImage;

    [Header("Card Art")]
    public Sprite redSealSprite;
    public Sprite catTrifectaSprite;
    public Sprite whiteTigerSprite;

    [Header("SFX")]
    public AudioClip slamSFX;

    void Awake() { }

    public void Setup(string cardName)
    {
        switch (cardName)
        {
            case "Ultimate Red Seal":
                topTextPanel.GetComponentInChildren<TextMeshProUGUI>().text = "ULTIMATE RED SEAL";
                bottomTextPanel.GetComponentInChildren<TextMeshProUGUI>().text = "HAS ARRIVED";
                artImage.sprite = redSealSprite;
                break;
            case "Cat TriFecta":
                topTextPanel.GetComponentInChildren<TextMeshProUGUI>().text = "CAT TRIFECTA";
                bottomTextPanel.GetComponentInChildren<TextMeshProUGUI>().text = "UNLEASHED!";
                artImage.sprite = catTrifectaSprite;
                break;
            case "Legendary White Tiger of the Pagoda":
                topTextPanel.GetComponentInChildren<TextMeshProUGUI>().text = "LEGENDARY WHITE TIGER";
                bottomTextPanel.GetComponentInChildren<TextMeshProUGUI>().text = "AWAKENS!";
                artImage.sprite = whiteTigerSprite;
                break;
        }

        PlayArtReveal();
        PlayTextAnimation();
    }

    void PlayArtReveal()
    {
        artImage.color = new Color(1, 1, 1, 0);
        artImage.transform.localScale = Vector3.one * 1.4f;

        DOTween.Sequence()
            .Append(artImage.DOFade(1f, 0.3f))
            .Join(artImage.transform.DOScale(1f, 0.5f).SetEase(Ease.OutExpo));
    }

    void PlayTextAnimation()
    {
        Vector2 topTargetPos = new Vector2(-500, 420);
        Vector2 bottomTargetPos = new Vector2(500, -420);

        topTextPanel.anchoredPosition = new Vector2(-2500, topTargetPos.y);
        bottomTextPanel.anchoredPosition = new Vector2(2500, bottomTargetPos.y);

        topTextPanel.localScale = Vector3.one * 0.9f;
        bottomTextPanel.localScale = Vector3.one * 0.9f;

        if (slamSFX != null)
            AudioSource.PlayClipAtPoint(slamSFX, Camera.main.transform.position);

        if (Camera.main != null)
        {
            Camera.main.transform
                .DOShakePosition(0.3f, strength: new Vector3(0.5f, 0.5f, 0), vibrato: 60, randomness: 90f)
                .SetEase(Ease.OutCirc);
        }

        DOTween.Sequence()
            .Append(topTextPanel.DOAnchorPos(topTargetPos, 0.5f).SetEase(Ease.OutExpo))
            .Join(topTextPanel.DOScale(1.15f, 0.3f).SetEase(Ease.OutBack))
            .AppendInterval(3f)
            .Append(topTextPanel.DOAnchorPos(new Vector2(3000, topTargetPos.y), 0.5f).SetEase(Ease.InExpo));

        DOTween.Sequence()
            .Append(bottomTextPanel.DOAnchorPos(bottomTargetPos, 0.5f).SetEase(Ease.OutExpo))
            .Join(bottomTextPanel.DOScale(1.15f, 0.3f).SetEase(Ease.OutBack))
            .AppendInterval(3f)
            .Append(bottomTextPanel.DOAnchorPos(new Vector2(-3000, bottomTargetPos.y), 0.5f).SetEase(Ease.InExpo));

        DOTween.Sequence()
            .AppendInterval(4.5f)
            .AppendCallback(TriggerShatter)
            .AppendInterval(1.5f)
            .AppendCallback(() => Destroy(gameObject));
    }

    void TriggerShatter()
    {
        // Spawn a temporary world-space GameObject with SpriteRenderer
        GameObject shatterObj = new GameObject("ShatterSprite");
        var spriteRenderer = shatterObj.AddComponent<SpriteRenderer>();
        var shatter = shatterObj.AddComponent<Shatter>();

        spriteRenderer.sprite = artImage.sprite;
        spriteRenderer.sortingOrder = 999;

        // Match size and position with UI image
        Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(Camera.main, artImage.rectTransform.position);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 10f));

        shatterObj.transform.position = worldPos;
        shatterObj.transform.localScale = Vector3.one * 5f;
        shatterObj.transform.rotation = artImage.transform.rotation;

        // ✅ Correct call
        shatter.shatter();
    }

}
