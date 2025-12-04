using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CharacterCardUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler   // ✅ added
{
    public Image thumbnailImage;
    public TextMeshProUGUI nameText;

    private CharacterSelectManager.CharacterData data;
    private CharacterSelectManager manager;

    private float fadeDuration = 0.2f;
    private float scaleDuration = 0.4f;
    private Material grayscaleMat;

    // ✅ selection state
    private bool isSelected = false;
    private Vector3 baseScale;

    // ✅ global “currently selected” card
    private static CharacterCardUI currentSelected;

    public void Setup(CharacterSelectManager.CharacterData newData, CharacterSelectManager mgr)
    {
        data = newData;
        manager = mgr;

        thumbnailImage.sprite = data.characterThumbnail;
        nameText.text = data.characterName;

        // prepare grayscale material
        grayscaleMat = Instantiate(thumbnailImage.material);
        thumbnailImage.material = grayscaleMat;
        grayscaleMat.SetFloat("_GrayAmount", 1f); // start grey

        baseScale = Vector3.one;
        transform.localScale = baseScale;

        isSelected = false;

        // clicking the card starts the game (keep this)
        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => manager.StartGame(data));
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // Update big art on hover
        manager.ReplaceCharacter(data);

        if (isSelected) return; // already locked in

        ApplyHighlightedVisuals();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // ✅ if selected, do NOT go back to grey/small
        if (isSelected) return;

        ApplyNormalVisuals();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // ✅ deselect previous selected card
        if (currentSelected != null && currentSelected != this)
        {
            currentSelected.Deselect();
        }

        // ✅ select this card
        currentSelected = this;
        isSelected = true;
        ApplyHighlightedVisuals();  // stay colourful + enlarged
    }

    private void ApplyHighlightedVisuals()
    {
        if (grayscaleMat == null) return;

        grayscaleMat.DOKill();
        transform.DOKill();

        grayscaleMat.DOFloat(0f, "_GrayAmount", fadeDuration);          // colourful
        transform.DOScale(baseScale * 1.1f, scaleDuration)
                 .SetEase(Ease.OutElastic);                             // enlarged
    }

    private void ApplyNormalVisuals()
    {
        if (grayscaleMat == null) return;

        grayscaleMat.DOKill();
        transform.DOKill();

        grayscaleMat.DOFloat(1f, "_GrayAmount", fadeDuration);          // grey
        transform.DOScale(baseScale, 0.25f).SetEase(Ease.InBack);       // normal size
    }

    public void Deselect()
    {
        isSelected = false;
        ApplyNormalVisuals();
    }
}
