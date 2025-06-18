using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class IconManager : MonoBehaviour
{
    [Header("Tam Tam UI")]
    public Image tamTamImage;

    [Header("Sprites")]
    public Sprite defaultSprite;
    public Sprite memoriesSprite, wardrobeSprite, soundSprite;

    [Header("Offsets")]
    public Vector2 defaultOffset = Vector2.zero;
    public Vector2 memoriesOffset, wardrobeOffset, soundOffset;

    private Vector2 baseOffset;
    private Tween floatYTween;
    private Tween floatXTween;

    public enum IconType { Memories, Wardrobe, Sound }
    private IconType? currentHoverIcon = null;
    private Coroutine resetCoroutine;

    private bool iconLocked = false; // 🔒 Prevents reset on hover exit

    void Start()
    {
        baseOffset = defaultOffset;
        tamTamImage.sprite = defaultSprite;
        tamTamImage.enabled = true;
        StartFloatLoop();
    }

    public void ShowIcon(IconType type)
    {
        if (iconLocked) return; // 🔐 If locked, don't change icon

        currentHoverIcon = type;

        switch (type)
        {
            case IconType.Memories:
                tamTamImage.sprite = memoriesSprite;
                baseOffset = memoriesOffset;
                break;
            case IconType.Wardrobe:
                tamTamImage.sprite = wardrobeSprite;
                baseOffset = wardrobeOffset;
                break;
            case IconType.Sound:
                tamTamImage.sprite = soundSprite;
                baseOffset = soundOffset;
                break;
        }

        tamTamImage.enabled = true;

        if (resetCoroutine != null)
            StopCoroutine(resetCoroutine);
    }

    public void StartResetDelay()
    {
        if (iconLocked) return; // 🔐 Don’t reset if icon is locked

        if (resetCoroutine != null)
            StopCoroutine(resetCoroutine);

        resetCoroutine = StartCoroutine(ResetToDefaultAfterDelay(0f));
    }

    private IEnumerator ResetToDefaultAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ShowDefault();
    }

    private void ShowDefault()
    {
        if (iconLocked) return;

        currentHoverIcon = null;
        tamTamImage.sprite = defaultSprite;
        baseOffset = defaultOffset;
        tamTamImage.enabled = true;
    }

    private void StartFloatLoop()
    {
        floatYTween?.Kill();
        floatXTween?.Kill();

        float floatAmountY = Random.Range(7f, 11f);
        float durationY = Random.Range(1.1f, 1.6f);

        floatYTween = DOVirtual.Float(-floatAmountY, floatAmountY, durationY, val =>
        {
            Vector2 current = tamTamImage.rectTransform.anchoredPosition;
            tamTamImage.rectTransform.anchoredPosition = new Vector2(current.x, baseOffset.y + val);
        })
        .SetEase(Ease.InOutSine)
        .SetLoops(-1, LoopType.Yoyo);

        float floatAmountX = Random.Range(2f, 5f);
        float durationX = Random.Range(1.4f, 2.0f);

        floatXTween = DOVirtual.Float(-floatAmountX, floatAmountX, durationX, val =>
        {
            Vector2 current = tamTamImage.rectTransform.anchoredPosition;
            tamTamImage.rectTransform.anchoredPosition = new Vector2(baseOffset.x + val, current.y);
        })
        .SetEase(Ease.InOutSine)
        .SetLoops(-1, LoopType.Yoyo);
    }

    // 🔓 External control
    public void LockIcon() => iconLocked = true;
    public void UnlockIcon()
    {
        iconLocked = false;
        ShowDefault(); // Optionally reset if you want to unlock and revert
    }
}