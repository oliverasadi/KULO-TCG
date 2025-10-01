using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;

public class CharacterSelectManager : MonoBehaviour
{
    [System.Serializable]
    public class CharacterData
    {
        public string characterName;
        public Sprite characterThumbnail;
        public Sprite fullArtImage;
        public string description;

        // Plays on selection click (we play this BEFORE changing scenes)
        public AudioClip voiceLine;

        public DeckDataSO deck;

        // If assigned → we route to Prologue scene
        public PrologueSequence prologue;
    }

    [Header("Character List")]
    public List<CharacterData> characters = new List<CharacterData>();

    [Header("Card Display Prefab")] public GameObject characterCardPrefab;
    [Header("Card Row Parent")] public Transform cardRowParent;

    [Header("Full Art Display")]
    public Image fullArtImage;
    public RectTransform fullArtImageRectTransform;

    [Header("UI Text & Button")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Button playButton;

    [Header("Tween Offsets")]
    public float artOffscreenOffsetX = 500f;
    public float uiOffscreenOffsetX = 300f;

    // timing for fade into Prologue
    [Header("Transitions")]
    public float fadeOutTime = 0.25f;
    public float fadeInTime = 0.25f;

    private Vector2 artOnScreen, artOffScreen;
    private RectTransform nameRT, descRT, btnRT;
    private Vector2 nameOn, nameOff, descOn, descOff, btnOn, btnOff;

    private AudioSource _audio;
    private CharacterData selectedCharacter;

    void Awake()
    {
        AudioManager.EnsureExists();

        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;

        if (fullArtImageRectTransform != null)
        {
            artOnScreen = fullArtImageRectTransform.anchoredPosition;
            artOffScreen = artOnScreen + Vector2.left * artOffscreenOffsetX;
            fullArtImageRectTransform.anchoredPosition = artOffScreen;
        }

        if (nameText != null) nameRT = nameText.GetComponent<RectTransform>();
        if (descriptionText != null) descRT = descriptionText.GetComponent<RectTransform>();
        if (playButton != null) btnRT = playButton.GetComponent<RectTransform>();

        if (nameRT != null) { nameOn = nameRT.anchoredPosition; nameOff = nameOn + Vector2.right * uiOffscreenOffsetX; nameRT.anchoredPosition = nameOff; }
        if (descRT != null) { descOn = descRT.anchoredPosition; descOff = descOn + Vector2.right * uiOffscreenOffsetX; descRT.anchoredPosition = descOff; }
        if (btnRT != null) { btnOn = btnRT.anchoredPosition; btnOff = btnOn + Vector2.right * uiOffscreenOffsetX; btnRT.anchoredPosition = btnOff; }
    }

    void Start()
    {
        foreach (var data in characters)
        {
            var card = Instantiate(characterCardPrefab, cardRowParent);
            var cardUI = card.GetComponent<CharacterCardUI>();
            cardUI.Setup(data, this);
        }

        if (characters.Count > 0)
            ReplaceCharacter(characters[0]);
    }

    public void SelectCharacter(CharacterData data)
    {
        selectedCharacter = data;
        if (fullArtImage) fullArtImage.sprite = data.fullArtImage;
        if (nameText) nameText.text = data.characterName;
        if (descriptionText) descriptionText.text = data.description;

        if (playButton != null)
        {
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(() => StartGame(data));
        }
    }

    /// Plays the prologue (if any) with a FADE, after playing the selection voice immediately.
    public void StartGame(CharacterData data)
    {
        if (data != null && data.prologue != null)
        {
            StartCoroutine(FadeToPrologue(data));
        }
        else
        {
            // No prologue → old flow
            StartCoroutine(PlayThenLoad(data.voiceLine, data.deck));
        }
    }

    private IEnumerator FadeToPrologue(CharacterData data)
    {
        // Play the selection voice right now
        if (data.voiceLine != null)
        {
            _audio.Stop();
            _audio.PlayOneShot(data.voiceLine);
            yield return new WaitForSeconds(data.voiceLine.length);
        }

        // Pass context to Prologue (no after-voice; we already played it)
        PrologueContext.Set(
            data.prologue,
            data.deck,
            null,                      // voiceLineAfter = null (already played)
            data.characterName
        );

        // Stop menu music (instance-safe)
        StopMenuMusicSafe();

        // Fade to Prologue scene
        AutoFade.LoadScene("Prologue", fadeOutTime, fadeInTime, Color.black);
    }

    private IEnumerator PlayThenLoad(AudioClip clip, DeckDataSO deck)
    {
        PlayerManager.selectedCharacterDeck = deck;

        if (selectedCharacter != null)
            PlayerProfile.selectedCharacterName = selectedCharacter.characterName;

        if (clip != null) { _audio.PlayOneShot(clip); yield return new WaitForSeconds(clip.length); }

        AutoFade.LoadScene("KULO", fadeOutTime, fadeInTime, Color.black);
    }

    private void StopMenuMusicSafe()
    {
        var am = FindObjectOfType<AudioManager>();
        if (am == null) return;

        // Replace with your real API if you have it (e.g., am.StopMusic();)
        try { am.SendMessage("StopMusic", SendMessageOptions.DontRequireReceiver); } catch { }
        try { am.SendMessage("StopAllMusic", SendMessageOptions.DontRequireReceiver); } catch { }
        // Or by name:
        // try { am.SendMessage("Stop", "CharacterSelectTheme", SendMessageOptions.DontRequireReceiver); } catch {}
    }

    public void ReplaceCharacter(CharacterData data)
    {
        fullArtImageRectTransform?.DOKill();
        nameRT?.DOKill();
        descRT?.DOKill();
        btnRT?.DOKill();

        var seq = DOTween.Sequence();

        seq.Append(fullArtImageRectTransform
                    .DOAnchorPos(artOffScreen, 0.2f).SetEase(Ease.InBack));
        seq.Join(nameRT.DOAnchorPos(nameOff, 0.2f));
        seq.Join(descRT.DOAnchorPos(descOff, 0.2f));
        seq.Join(btnRT.DOAnchorPos(btnOff, 0.2f));

        seq.AppendCallback(() =>
        {
            selectedCharacter = data;
            fullArtImage.sprite = data.fullArtImage;
            nameText.text = data.characterName;
            descriptionText.text = data.description;

            if (playButton != null)
            {
                playButton.onClick.RemoveAllListeners();
                playButton.onClick.AddListener(() => StartGame(data));
            }
        });

        seq.Append(fullArtImageRectTransform
                    .DOAnchorPos(artOnScreen, 0.4f).SetEase(Ease.OutCubic));
        seq.Join(nameRT.DOAnchorPos(nameOn, 0.4f));
        seq.Join(descRT.DOAnchorPos(descOn, 0.5f));
        seq.Join(btnRT.DOAnchorPos(btnOn, 0.6f));
    }
}
