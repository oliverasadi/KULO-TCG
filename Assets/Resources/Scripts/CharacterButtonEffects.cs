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

    private Outline _outline;
    private AudioSource _audioSrc;

    void Awake()
    {
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
        _outline.enabled = true;
    }

    // Hover end
    public void OnPointerExit(PointerEventData eventData)
    {
        _outline.enabled = false;
    }

    // Click
    public void OnPointerClick(PointerEventData eventData)
    {
        if (clickSound != null)
            _audioSrc.PlayOneShot(clickSound);
    }
}
