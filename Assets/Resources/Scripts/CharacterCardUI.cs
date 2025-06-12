// CharacterCardUI.cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CharacterCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image thumbnailImage;
    public TextMeshProUGUI nameText;

    private CharacterSelectManager.CharacterData data;
    private CharacterSelectManager manager;

    private float fadeDuration = 0.2f;
    private float scaleDuration = 0.4f;
    private Material grayscaleMat;

    public void Setup(CharacterSelectManager.CharacterData newData, CharacterSelectManager mgr)
    {
        data = newData;
        manager = mgr;

        thumbnailImage.sprite = data.characterThumbnail;
        nameText.text = data.characterName;

        // prepare grayscale material
        grayscaleMat = Instantiate(thumbnailImage.material);
        thumbnailImage.material = grayscaleMat;
        grayscaleMat.SetFloat("_GrayAmount", 1f);

        transform.localScale = Vector3.one;

        // clicking the card now starts the game
        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => manager.StartGame(data));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // slide old → swap → slide in new
        manager.ReplaceCharacter(data);

        // thumbnail color & pop
        grayscaleMat.DOKill();
        transform.DOKill();
        grayscaleMat.DOFloat(0f, "_GrayAmount", fadeDuration);
        transform.DOScale(1.1f, scaleDuration).SetEase(Ease.OutElastic);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // only gray & shrink thumbnail
        grayscaleMat.DOKill();
        transform.DOKill();
        grayscaleMat.DOFloat(1f, "_GrayAmount", fadeDuration);
        transform.DOScale(1f, 0.25f).SetEase(Ease.InBack);
    }
}
