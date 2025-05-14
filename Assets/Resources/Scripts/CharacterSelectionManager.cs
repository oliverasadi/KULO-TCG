using System.Collections;
using UnityEngine;

public class CharacterSelectManager : MonoBehaviour
{
    [Header("Decks (assign in Inspector)")]
    public DeckDataSO waxDeck;
    public DeckDataSO nekomataDeck;
    public DeckDataSO xuTaishiDeck;

    [Header("Voice Lines (assign in Inspector)")]
    public AudioClip waxVoiceLine;
    public AudioClip nekomataVoiceLine;
    public AudioClip xuVoiceLine;

    private AudioSource _audio;

    void Awake()
    {
        // Ensure we have an AudioSource for playing voice lines
        _audio = GetComponent<AudioSource>();
        if (_audio == null)
            _audio = gameObject.AddComponent<AudioSource>();

        _audio.playOnAwake = false;
    }

    public void SelectMrWax()
    {
        // Set deck & play voice, then load scene
        StartCoroutine(PlayThenLoad(waxVoiceLine, waxDeck));
    }

    public void SelectNekomata()
    {
        StartCoroutine(PlayThenLoad(nekomataVoiceLine, nekomataDeck));
    }

    public void SelectXuTaishi()
    {
        StartCoroutine(PlayThenLoad(xuVoiceLine, xuTaishiDeck));
    }

    private IEnumerator PlayThenLoad(AudioClip clip, DeckDataSO deck)
    {
        // 1) Store the deck choice
        PlayerManager.selectedCharacterDeck = deck;

        // 2) Play the clip (if assigned)
        if (clip != null)
            _audio.PlayOneShot(clip);

        // 3) Wait for its length (or skip if no clip)
        yield return new WaitForSeconds(clip != null ? clip.length : 0f);

        // 4) Fade into the KULO scene
        AutoFade.LoadScene("KULO", 0.3f, 0.3f, Color.black);
    }
}
