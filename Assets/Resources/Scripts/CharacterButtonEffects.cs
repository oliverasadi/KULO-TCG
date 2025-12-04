using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class CharacterButtonEffects : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler
{
    [Header("Glow Settings")]
    public Color glowColor = Color.yellow;
    public float glowDistance = 5f;      // how “thick” the glow is

    [Header("Click Sound")]
    public AudioClip clickSound;

    [Header("Behaviour")]
    public bool freezeOnClick = true;    // ✅ if true, button “freezes” after click

    private Outline _outline;
    private AudioSource _audioSrc;
    private Button _button;

    // Internal state
    private bool _isFrozen = false;

    void Awake()
    {
        _button = GetComponent<Button>();

        // 1) Set up the Outline component
        _outline = GetComponent<Outline>();
        if (_outline == null)
            _outline = gameObject.AddComponent<Outline>();

        _outline.effectColor = glowColor;
        _outline.effectDistance = new Vector2(glowDistance, glowDistance);
        _outline.enabled = false;

        // 2) Set up an AudioSource
        _audioSrc = GetComponent<AudioSource>();
        if (_audioSrc == null)
            _audioSrc = gameObject.AddComponent<AudioSource>();
        _audioSrc.playOnAwake = false;
    }

    // Hover start
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_isFrozen) return;         // ✅ don’t change if frozen
        _outline.enabled = true;
    }

    // Hover end
    public void OnPointerExit(PointerEventData eventData)
    {
        if (_isFrozen) return;         // ✅ keep glow if frozen
        _outline.enabled = false;
    }

    // Click
    public void OnPointerClick(PointerEventData eventData)
    {
        if (clickSound != null)
            _audioSrc.PlayOneShot(clickSound);

        if (freezeOnClick && !_isFrozen)
        {
            _isFrozen = true;

            // Keep the glow on
            _outline.enabled = true;

            // Stop further clicks if you want
            if (_button != null)
                _button.interactable = false;
        }
    }

    // Optional: if you later want to change selection from code
    public void Unfreeze()
    {
        _isFrozen = false;
        _outline.enabled = false;
        if (_button != null)
            _button.interactable = true;
    }
}
