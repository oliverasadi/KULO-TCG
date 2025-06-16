// CharacterSelectManager.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening; // ✅ Required for DOTween

public class CharacterSelectManager : MonoBehaviour
{
    [System.Serializable]
    public class CharacterData
    {
        public string characterName;
        public Sprite characterThumbnail;
        public Sprite fullArtImage;
        public string description;
        public AudioClip voiceLine;
        public DeckDataSO deck;
    }

    [Header("Character List")]
    public List<CharacterData> characters = new List<CharacterData>();

    [Header("Card Display Prefab")]
    public GameObject characterCardPrefab;

    [Header("Card Row Parent")]
    public Transform cardRowParent;

    [Header("Full Art Display")]
    public Image fullArtImage;
    public RectTransform fullArtImageRectTransform;

    [Header("UI Text & Button")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Button playButton;    // optional now

    [Header("Tween Offsets")]
    public float artOffscreenOffsetX = 500f;
    public float uiOffscreenOffsetX = 300f;

    // runtime-captured positions
    private Vector2 artOnScreen, artOffScreen;
    private RectTransform nameRT, descRT, btnRT;
    private Vector2 nameOn, nameOff;
    private Vector2 descOn, descOff;
    private Vector2 btnOn, btnOff;

    private AudioSource _audio;
    private CharacterData selectedCharacter;

    void Awake()
    {
        AudioManager.EnsureExists();

        // audio setup
        _audio = GetComponent<AudioSource>();
        if (_audio == null) _audio = gameObject.AddComponent<AudioSource>();
        _audio.playOnAwake = false;

        // capture on/off positions
        if (fullArtImageRectTransform != null)
        {
            artOnScreen = fullArtImageRectTransform.anchoredPosition;
            artOffScreen = artOnScreen + Vector2.left * artOffscreenOffsetX;
            fullArtImageRectTransform.anchoredPosition = artOffScreen;
        }

        if (nameText != null) nameRT = nameText.GetComponent<RectTransform>();
        if (descriptionText != null) descRT = descriptionText.GetComponent<RectTransform>();
        if (playButton != null) btnRT = playButton.GetComponent<RectTransform>();

        if (nameRT != null)
        {
            nameOn = nameRT.anchoredPosition;
            nameOff = nameOn + Vector2.right * uiOffscreenOffsetX;
            nameRT.anchoredPosition = nameOff;
        }
        if (descRT != null)
        {
            descOn = descRT.anchoredPosition;
            descOff = descOn + Vector2.right * uiOffscreenOffsetX;
            descRT.anchoredPosition = descOff;
        }
        if (btnRT != null)
        {
            btnOn = btnRT.anchoredPosition;
            btnOff = btnOn + Vector2.right * uiOffscreenOffsetX;
            btnRT.anchoredPosition = btnOff;
        }
    }

    void Start()
    {
        // spawn all the character cards
        foreach (var data in characters)
        {
            var card = Instantiate(characterCardPrefab, cardRowParent);
            var cardUI = card.GetComponent<CharacterCardUI>();
            cardUI.Setup(data, this);
        }

        // animate the very first character straight in
        if (characters.Count > 0)
            ReplaceCharacter(characters[0]);
    }


    public void SelectCharacter(CharacterData data)
    {
        selectedCharacter = data;
        if (fullArtImage != null) fullArtImage.sprite = data.fullArtImage;
        if (nameText != null) nameText.text = data.characterName;
        if (descriptionText != null) descriptionText.text = data.description;

        // keep the Play Button wired in case you still use it
        playButton.onClick.RemoveAllListeners();
        playButton.onClick.AddListener(() =>
            StartCoroutine(PlayThenLoad(data.voiceLine, data.deck))
        );
    }

    /// <summary>
    /// Called by the card click. Starts voice + scene load.
    /// </summary>
    public void StartGame(CharacterData data)
    {
        StartCoroutine(PlayThenLoad(data.voiceLine, data.deck));
    }

    private IEnumerator PlayThenLoad(AudioClip clip, DeckDataSO deck)
    {
        PlayerManager.selectedCharacterDeck = deck;

        // ✅ Set selected character name for XPResultsScene
        if (selectedCharacter != null)
            PlayerProfile.selectedCharacterName = selectedCharacter.characterName;

        if (clip != null) _audio.PlayOneShot(clip);
        yield return new WaitForSeconds(clip != null ? clip.length : 0f);
        AutoFade.LoadScene("KULO", 0.3f, 0.3f, Color.black);
    }


    public void ReplaceCharacter(CharacterData data)
    {
        // kill any running tweens
        fullArtImageRectTransform?.DOKill();
        nameRT?.DOKill();
        descRT?.DOKill();
        btnRT?.DOKill();

        // sequence: slide out → swap → slide in
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

            // re-wire Play button (optional)
            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(() =>
                StartCoroutine(PlayThenLoad(data.voiceLine, data.deck))
            );
        });

        seq.Append(fullArtImageRectTransform
                    .DOAnchorPos(artOnScreen, 0.4f).SetEase(Ease.OutCubic));
        seq.Join(nameRT.DOAnchorPos(nameOn, 0.4f));
        seq.Join(descRT.DOAnchorPos(descOn, 0.5f));
        seq.Join(btnRT.DOAnchorPos(btnOn, 0.6f));
    }
}
