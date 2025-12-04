using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using DG.Tweening;

public class CharacterCardUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    public Image thumbnailImage;
    public TextMeshProUGUI nameText;

    [Header("Visual Settings")]
    [Range(0.5f, 2f)] public float highlightScale = 1.1f;

    [Header("Selection FX")]
    public Image selectionFlash;           // optional overlay image
    public float flashAlpha = 0.7f;
    public float flashDuration = 0.2f;

    public float punchStrength = 0.08f;    // how strong the punch scale is
    public float punchDuration = 0.2f;
    public int punchVibrato = 8;
    public float punchElasticity = 0.9f;

    private CharacterSelectManager.CharacterData data;
    private CharacterSelectManager manager;

    private float fadeDuration = 0.2f;
    private float scaleDuration = 0.4f;
    private Material grayscaleMat;

    // selection state
    private bool isSelected = false;
    private Vector3 baseScale;

    // global “currently selected” card
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

        // clicking the card starts the game
        var btn = GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => manager.StartGame(data));

        // make sure flash is invisible at start
        if (selectionFlash != null)
        {
            Color c = selectionFlash.color;
            c.a = 0f;
            selectionFlash.color = c;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // update big art
        manager.ReplaceCharacter(data);

        if (isSelected) return;
        ApplyHighlightedVisuals();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (isSelected) return;
        ApplyNormalVisuals();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // deselect previous card
        if (currentSelected != null && currentSelected != this)
            currentSelected.Deselect();

        // select this one
        currentSelected = this;
        isSelected = true;
        ApplyHighlightedVisuals();
        PlaySelectFX();
    }

    private void ApplyHighlightedVisuals()
    {
        if (grayscaleMat == null) return;

        grayscaleMat.DOKill();
        transform.DOKill();

        grayscaleMat.DOFloat(0f, "_GrayAmount", fadeDuration);  // colourful
        transform.DOScale(baseScale * highlightScale, scaleDuration)
                 .SetEase(Ease.OutElastic);                     // enlarged
    }

    private void ApplyNormalVisuals()
    {
        if (grayscaleMat == null) return;

        grayscaleMat.DOKill();
        transform.DOKill();

        grayscaleMat.DOFloat(1f, "_GrayAmount", fadeDuration);  // grey
        transform.DOScale(baseScale, 0.25f).SetEase(Ease.InBack);

        // hide flash when not selected
        if (selectionFlash != null)
        {
            selectionFlash.DOKill();
            Color c = selectionFlash.color;
            c.a = 0f;
            selectionFlash.color = c;
        }
    }

    private void PlaySelectFX()
    {
        // tiny scale punch
        transform.DOKill();
        transform.localScale = baseScale * highlightScale;
        transform.DOPunchScale(Vector3.one * punchStrength, punchDuration, punchVibrato, punchElasticity);

        // white flash overlay
        if (selectionFlash != null)
        {
            selectionFlash.DOKill();

            Color c = selectionFlash.color;
            c.a = 0f;
            selectionFlash.color = c;

            selectionFlash.DOFade(flashAlpha, 0f);                      // jump to bright
            selectionFlash.DOFade(0f, flashDuration).SetEase(Ease.OutQuad); // fade out
        }
    }

    public void Deselect()
    {
        isSelected = false;
        ApplyNormalVisuals();
    }
}
