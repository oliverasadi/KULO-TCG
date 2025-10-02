using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using UnityEngine.Events;

public class ProloguePlayer : MonoBehaviour
{
    [Header("UI")]
    public Image backgroundImage;
    public RawImage backgroundVideo;
    public TextMeshProUGUI subtitleText;
    public Image continueIcon;
    public CanvasGroup canvasGroup;

    [Header("Cinematic (optional)")]
    public RectTransform letterboxTop;
    public RectTransform letterboxBottom;
    public float letterboxTargetHeight = 120f;
    public float letterboxAnimTime = 0.25f;

    [Header("Slide Transitions")]
    public bool useSlideDip = false;
    public float slideCutDip = 0.15f;
    public float slideWhooshDelay = 0.02f;
    public float kenBurnsScale = 1.06f;
    public float kenBurnsDuration = 6f;

    [Header("Crossfade (optional)")]
    public Image crossfadeImage;
    public CanvasGroup crossfadeGroup;
    public float imageCrossfadeTime = 0.18f;

    [Header("Video (optional)")]
    public VideoPlayer videoPlayer;
    public RenderTexture videoTexture;

    [Header("Audio")]
    public AudioSource voiceSource;
    public AudioSource bedSource;
    public AudioSource sfxSource;
    public AudioClip slideWhooshSfx;
    public AudioClip linePopSfx;
    [Range(0f, 1f)] public float bedVolume = 0.7f;
    [Range(0f, 1f)] public float bedDuckVolume = 0.35f;
    public float bedDuckSpeed = 12f;

    [Header("Input")]
    public KeyCode advanceKey = KeyCode.Space;
    public KeyCode skipKey = KeyCode.Escape;
    public KeyCode skipAllKey = KeyCode.Escape;
    public bool clickToAdvance = true;

    [Header("Line Presentation")]
    public float lineFadeInDuration = 0.4f;

    [Header("Flow")]
    public bool requireClickPerLine = true;

    [Header("Events")]
    public UnityEvent OnFinished;

    private bool _wantsAdvance;

    private void Awake()
    {
        if (voiceSource)
        {
            voiceSource.loop = false;
            voiceSource.playOnAwake = false;
        }
        if (bedSource)
        {
            bedSource.loop = true;
            bedSource.playOnAwake = false;
        }
    }

    public void Play(PrologueSequence seq) => StartCoroutine(PlayRoutine(seq));

    private IEnumerator PlayRoutine(PrologueSequence seq)
    {
        if (continueIcon) continueIcon.gameObject.SetActive(false);
        if (backgroundImage)
        {
            backgroundImage.gameObject.SetActive(true);
            backgroundImage.color = Color.white;
        }
        if (backgroundVideo)
        {
            backgroundVideo.gameObject.SetActive(false);
            backgroundVideo.color = Color.white;
        }
        if (crossfadeImage) crossfadeImage.gameObject.SetActive(false);

        if (canvasGroup) canvasGroup.alpha = 0f;
        if (letterboxTop) letterboxTop.sizeDelta = new Vector2(letterboxTop.sizeDelta.x, 0f);
        if (letterboxBottom) letterboxBottom.sizeDelta = new Vector2(letterboxBottom.sizeDelta.x, 0f);

        yield return StartCoroutine(Fade(0f, 1f, 0.25f));
        yield return StartCoroutine(ToggleLetterbox(true));

        // 🎵 Start default voice bed if provided
        if (bedSource && seq.defaultVoiceBed)
        {
            bedSource.clip = seq.defaultVoiceBed;
            bedSource.loop = true;
            bedSource.volume = bedVolume;

            Debug.Log($"[ProloguePlayer] Starting bed clip: {seq.defaultVoiceBed.name}, " +
                      $"length: {seq.defaultVoiceBed.length}s, " +
                      $"bedSource output: {(bedSource.outputAudioMixerGroup != null ? bedSource.outputAudioMixerGroup.name : "None")}");

            bedSource.Play();

            if (!bedSource.isPlaying)
            {
                Debug.LogWarning("[ProloguePlayer] bedSource.Play() called but AudioSource did not start playing.");
            }
        }
        else
        {
            Debug.LogWarning("[ProloguePlayer] Bed not started — bedSource or defaultVoiceBed missing.");
        }


        for (int si = 0; si < seq.slides.Count; si++)
        {
            var slide = seq.slides[si];

            if (useSlideDip && si > 0)
                yield return StartCoroutine(DipBlack(slideCutDip, slideWhooshDelay));

            bool hasVideo = (slide.video != null && videoPlayer && backgroundVideo);

            if (hasVideo)
            {
                if (!backgroundVideo.gameObject.activeSelf)
                    backgroundVideo.gameObject.SetActive(true);

                if (videoTexture != null)
                {
                    videoPlayer.targetTexture = videoTexture;
                    backgroundVideo.texture = videoTexture;
                }

                if (backgroundImage) backgroundImage.color = Color.clear;

                videoPlayer.clip = slide.video;
                videoPlayer.isLooping = slide.loopVideo;
                backgroundVideo.color = Color.white;
                videoPlayer.Play();
            }
            else if (slide.image != null && backgroundImage != null)
            {
                if (backgroundVideo && backgroundVideo.gameObject.activeSelf)
                {
                    if (videoPlayer) videoPlayer.Stop();
                    backgroundVideo.gameObject.SetActive(false);
                }

                yield return StartCoroutine(CrossfadeToSprite(slide.image));
                StartCoroutine(KenBurns(backgroundImage.rectTransform, kenBurnsScale, kenBurnsDuration));
            }

            foreach (var line in slide.lines)
            {
                if (continueIcon) continueIcon.gameObject.SetActive(false);
                _wantsAdvance = false;

                if (subtitleText)
                    yield return StartCoroutine(ShowLineFade(line.text));
                else
                    yield return null;

                if (_wantsAdvance) continue;

                if (line.voice && voiceSource)
                {
                    StartCoroutine(DuckBed(true));

                    voiceSource.Stop();
                    voiceSource.clip = line.voice;
                    voiceSource.loop = false;
                    voiceSource.playOnAwake = false;
                    voiceSource.time = 0f;
                    voiceSource.Play();

                    while (!AdvancePressed())
                    {
                        if (Input.GetKeyDown(skipKey) && voiceSource.isPlaying)
                            voiceSource.Stop();

                        if (!voiceSource.isPlaying)
                            StartCoroutine(DuckBed(false));

                        if (Input.GetKey(skipAllKey)) break;
                        yield return null;
                    }

                    if (voiceSource.isPlaying) voiceSource.Stop();
                    StartCoroutine(DuckBed(false));

                    if (Input.GetKey(skipAllKey)) break;
                }
                else
                {
                    yield return StartCoroutine(WaitForAdvance());
                }

                if (Input.GetKey(skipAllKey))
                    break;
            }

            if (slide.waitForClickAtEnd)
            {
                if (continueIcon) continueIcon.gameObject.SetActive(true);
                yield return StartCoroutine(WaitForAdvance());
                if (continueIcon) continueIcon.gameObject.SetActive(false);
            }

            if (hasVideo && videoPlayer) videoPlayer.Stop();

            if (Input.GetKey(skipAllKey))
                break;
        }

        yield return StartCoroutine(ToggleLetterbox(false));
        yield return StartCoroutine(Fade(1f, 0f, 0.25f));
        OnFinished?.Invoke();
        Destroy(gameObject);
    }

    // --- Helpers -------------------------------------------------------------

    private bool AdvancePressed()
    {
        return Input.GetKeyDown(advanceKey) || (clickToAdvance && Input.GetMouseButtonDown(0));
    }

    private IEnumerator CrossfadeToSprite(Sprite s)
    {
        if (!backgroundImage) yield break;

        if (!crossfadeImage || !crossfadeGroup)
        {
            backgroundImage.color = Color.white;
            backgroundImage.sprite = s;
            yield break;
        }

        crossfadeImage.sprite = s;
        crossfadeGroup.alpha = 0f;
        crossfadeImage.gameObject.SetActive(true);

        float t = 0f;
        while (t < imageCrossfadeTime)
        {
            t += Time.deltaTime;
            crossfadeGroup.alpha = Mathf.Clamp01(t / imageCrossfadeTime);
            yield return null;
        }

        backgroundImage.sprite = s;
        backgroundImage.color = Color.white;
        crossfadeImage.gameObject.SetActive(false);
    }

    private IEnumerator ShowLineFade(string text)
    {
        if (!subtitleText) yield break;

        if (sfxSource && linePopSfx) sfxSource.PlayOneShot(linePopSfx);

        subtitleText.text = text;
        Color c = subtitleText.color; c.a = 0f; subtitleText.color = c;

        float t = 0f;
        while (t < lineFadeInDuration)
        {
            if (AdvancePressed())
            {
                _wantsAdvance = true;
                yield break;
            }
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / lineFadeInDuration);
            subtitleText.color = c;
            yield return null;
        }

        c.a = 1f;
        subtitleText.color = c;

        float guard = 0.12f;
        float g = 0f;
        while (g < guard)
        {
            if (AdvancePressed()) { _wantsAdvance = true; yield break; }
            g += Time.deltaTime;
            yield return null;
        }
    }

    private IEnumerator WaitForAdvance()
    {
        _wantsAdvance = false;
        while (!_wantsAdvance)
        {
            if (AdvancePressed()) _wantsAdvance = true;
            if (Input.GetKeyDown(skipKey)) break;
            yield return null;
        }
    }

    private IEnumerator Fade(float a, float b, float dur)
    {
        if (!canvasGroup) yield break;

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / dur);
            canvasGroup.alpha = Mathf.Lerp(a, b, k);
            yield return null;
        }
        canvasGroup.alpha = b;
    }

    private IEnumerator DipBlack(float dipTime, float whooshDelay)
    {
        if (!canvasGroup) yield break;

        float half = Mathf.Max(0.01f, dipTime * 0.5f);
        if (sfxSource && slideWhooshSfx) StartCoroutine(DelayedWhoosh(whooshDelay));
        yield return StartCoroutine(Fade(canvasGroup.alpha, 0f, half));
        yield return StartCoroutine(Fade(0f, 1f, half));
    }

    private IEnumerator DelayedWhoosh(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (sfxSource && slideWhooshSfx) sfxSource.PlayOneShot(slideWhooshSfx);
    }

    private IEnumerator ToggleLetterbox(bool show)
    {
        if (!letterboxTop || !letterboxBottom) yield break;

        float t = 0f;
        float dur = letterboxAnimTime;
        float startH = letterboxTop.sizeDelta.y;
        float endH = show ? letterboxTargetHeight : 0f;

        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / dur);
            float h = Mathf.Lerp(startH, endH, k);
            letterboxTop.sizeDelta = new Vector2(letterboxTop.sizeDelta.x, h);
            letterboxBottom.sizeDelta = new Vector2(letterboxBottom.sizeDelta.x, h);
            yield return null;
        }
        letterboxTop.sizeDelta = new Vector2(letterboxTop.sizeDelta.x, endH);
        letterboxBottom.sizeDelta = new Vector2(letterboxBottom.sizeDelta.x, endH);
    }

    private IEnumerator KenBurns(RectTransform rt, float targetScale, float duration)
    {
        if (!rt || targetScale <= 1f) yield break;
        Vector3 start = Vector3.one;
        Vector3 end = new Vector3(targetScale, targetScale, 1f);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, t / duration);
            rt.localScale = Vector3.LerpUnclamped(start, end, k);
            yield return null;
        }
        rt.localScale = end;
    }

    private IEnumerator DuckBed(bool duck)
    {
        if (!bedSource) yield break;
        float target = duck ? bedDuckVolume : bedVolume;
        while (!Mathf.Approximately(bedSource.volume, target))
        {
            bedSource.volume = Mathf.MoveTowards(
                bedSource.volume, target,
                Time.deltaTime * bedDuckSpeed
            );
            yield return null;
        }
        bedSource.volume = target;
    }

    // 🔊 Utility fade
    private IEnumerator FadeAudio(AudioSource src, float to, float dur)
    {
        float from = src.volume; float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            src.volume = Mathf.Lerp(from, to, t / dur);
            yield return null;
        }
        src.volume = to;
    }
}
