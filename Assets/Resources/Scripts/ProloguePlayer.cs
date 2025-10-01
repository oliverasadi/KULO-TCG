using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using UnityEngine.Events;

public class ProloguePlayer : MonoBehaviour
{
    [Header("UI")]
    public Image backgroundImage;        // stills (kept enabled to avoid flicker)
    public RawImage backgroundVideo;     // videos (can be null if images-only)
    public TextMeshProUGUI subtitleText;
    public Image continueIcon;
    public CanvasGroup canvasGroup;      // master fade

    [Header("Cinematic (optional)")]
    public RectTransform letterboxTop;   // black bar (top). Optional.
    public RectTransform letterboxBottom;// black bar (bottom). Optional.
    public float letterboxTargetHeight = 120f; // px at 1080p
    public float letterboxAnimTime = 0.25f;

    [Header("Slide Transitions")]
    public bool useSlideDip = false;        // ❗ default off to avoid black flashes
    public float slideCutDip = 0.15f;       // black dip length between slides (if enabled)
    public float slideWhooshDelay = 0.02f;  // when to play the whoosh relative to dip
    public float kenBurnsScale = 1.06f;     // 1.00 -> 1.06 over time (stills only)
    public float kenBurnsDuration = 6f;     // long, non-blocking

    [Header("Crossfade (optional)")]
    public Image crossfadeImage;       // overlay image above backgroundImage
    public CanvasGroup crossfadeGroup; // CanvasGroup on crossfadeImage
    public float imageCrossfadeTime = 0.18f;

    [Header("Video (optional)")]
    public VideoPlayer videoPlayer;      // optional
    public RenderTexture videoTexture;   // optional

    [Header("Audio")]
    public AudioSource voiceSource;
    public AudioSource bedSource;
    public AudioSource sfxSource;        // optional: for whoosh/pop SFX
    public AudioClip slideWhooshSfx;     // optional
    public AudioClip linePopSfx;         // optional
    [Range(0f, 1f)] public float bedVolume = 0.7f;
    [Range(0f, 1f)] public float bedDuckVolume = 0.35f;
    public float bedDuckSpeed = 12f;     // how fast we lerp to duck/unduck

    [Header("Input")]
    public KeyCode advanceKey = KeyCode.Space; // click or Space -> next line immediately
    public KeyCode skipKey = KeyCode.Escape;   // cancels current wait/voice
    public KeyCode skipAllKey = KeyCode.Escape;// press/hold to skip whole sequence
    public bool clickToAdvance = true;

    [Header("Line Fade")]
    public float lineFadeInDuration = 0.4f;    // fade-in time for each sentence
    public float minLineDuration = 2.4f;       // total time a line should live (fade+hold) if no click

    [Header("Events")]
    public UnityEvent OnFinished;              // invoked when sequence ends

    private bool _wantsAdvance;

    public void Play(PrologueSequence seq) => StartCoroutine(PlayRoutine(seq));

    private IEnumerator PlayRoutine(PrologueSequence seq)
    {
        // Initial vis
        if (continueIcon) continueIcon.gameObject.SetActive(false);
        if (backgroundImage)
        {
            backgroundImage.gameObject.SetActive(true);  // keep enabled to avoid 1-frame black
            backgroundImage.color = Color.white;
        }
        if (backgroundVideo)
        {
            backgroundVideo.gameObject.SetActive(false);
            backgroundVideo.color = Color.white;
        }
        if (crossfadeImage) crossfadeImage.gameObject.SetActive(false);

        // Start with canvas invisible, bars hidden
        if (canvasGroup) canvasGroup.alpha = 0f;
        if (letterboxTop) letterboxTop.sizeDelta = new Vector2(letterboxTop.sizeDelta.x, 0f);
        if (letterboxBottom) letterboxBottom.sizeDelta = new Vector2(letterboxBottom.sizeDelta.x, 0f);

        // Fade in & bring bars in
        yield return StartCoroutine(Fade(0f, 1f, 0.25f));
        yield return StartCoroutine(ToggleLetterbox(true));

        // ambience
        if (bedSource && seq.defaultVoiceBed)
        {
            bedSource.clip = seq.defaultVoiceBed;
            bedSource.loop = true;
            bedSource.volume = bedVolume;
            bedSource.Play();
        }

        for (int si = 0; si < seq.slides.Count; si++)
        {
            var slide = seq.slides[si];

            // Optional punchy dip (disabled by default)
            if (useSlideDip && si > 0)
                yield return StartCoroutine(DipBlack(slideCutDip, slideWhooshDelay));

            // ----- Visual (image OR video) -----
            bool hasVideo = (slide.video != null && videoPlayer && backgroundVideo);

            if (hasVideo)
            {
                if (backgroundVideo && !backgroundVideo.gameObject.activeSelf)
                    backgroundVideo.gameObject.SetActive(true);
                if (videoTexture != null)
                {
                    videoPlayer.targetTexture = videoTexture;
                    backgroundVideo.texture = videoTexture;
                }

                // hide still to ensure no flicker above video
                if (backgroundImage) backgroundImage.color = Color.clear;

                videoPlayer.clip = slide.video;
                videoPlayer.isLooping = slide.loopVideo;
                backgroundVideo.color = Color.white;
                videoPlayer.Play();
            }
            else if (slide.image != null && backgroundImage != null)
            {
                // stop video if we were playing one
                if (backgroundVideo && backgroundVideo.gameObject.activeSelf)
                {
                    if (videoPlayer) videoPlayer.Stop();
                    backgroundVideo.gameObject.SetActive(false);
                }

                // Crossfade to avoid any flash
                yield return StartCoroutine(CrossfadeToSprite(slide.image));

                // Ken Burns (non-blocking)
                StartCoroutine(KenBurns(backgroundImage.rectTransform, kenBurnsScale, kenBurnsDuration));
            }

            // ----- Lines (sentence-by-sentence fade, click -> next) -----
            foreach (var line in slide.lines)
            {
                if (continueIcon) continueIcon.gameObject.SetActive(false);
                _wantsAdvance = false;

                if (subtitleText)
                    yield return StartCoroutine(ShowLineFade(line.text));
                else
                    yield return null;

                if (_wantsAdvance) continue; // user clicked during fade/hold -> next line

                float postHold = (line.postHold >= 0f) ? line.postHold : seq.defaultPostLineHold;

                // voice playback (click -> next line)
                if (line.voice && voiceSource)
                {
                    // Duck ambience while voice plays
                    StartCoroutine(DuckBed(true));

                    voiceSource.Stop();
                    voiceSource.clip = line.voice;
                    voiceSource.Play();
                    yield return StartCoroutine(WaitForVoiceOrAdvance(voiceSource));

                    // Unduck ambience
                    StartCoroutine(DuckBed(false));

                    if (_wantsAdvance) continue;
                }
                else
                {
                    // enforce minimum line life: fade time already elapsed, wait remaining unless click
                    float remaining = Mathf.Max(0f, minLineDuration - lineFadeInDuration);
                    float tHold = 0f;
                    while (tHold < Mathf.Max(remaining, postHold))
                    {
                        if (AdvancePressed()) { _wantsAdvance = true; break; }
                        if (Input.GetKeyDown(skipKey)) break;
                        tHold += Time.deltaTime;
                        yield return null;
                    }
                    if (_wantsAdvance) continue;
                }

                // allow global skip-out at any time
                if (Input.GetKey(skipAllKey))
                    break;
            }

            // end-of-slide pause
            if (slide.waitForClickAtEnd)
            {
                if (continueIcon) continueIcon.gameObject.SetActive(true);
                yield return StartCoroutine(WaitForAdvance());
                if (continueIcon) continueIcon.gameObject.SetActive(false);
            }

            // stop video when leaving slide
            if (hasVideo && videoPlayer) videoPlayer.Stop();

            // global skip-all?
            if (Input.GetKey(skipAllKey))
                break;
        }

        // Fade out, bars out, finish
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

    // Crossfade helper (safe if not wired)
    private IEnumerator CrossfadeToSprite(Sprite s)
    {
        if (!backgroundImage) yield break;

        // If no overlay set up, just swap sprite
        if (!crossfadeImage || !crossfadeGroup)
        {
            backgroundImage.color = Color.white;
            backgroundImage.sprite = s;
            yield break;
        }

        // Put new sprite on overlay and fade it in
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

        // Commit sprite to the real background and hide overlay
        backgroundImage.sprite = s;
        backgroundImage.color = Color.white;
        crossfadeImage.gameObject.SetActive(false);
    }

    // Fade in a whole sentence; any click/Space -> NEXT line immediately
    private IEnumerator ShowLineFade(string text)
    {
        if (!subtitleText) yield break;

        if (sfxSource && linePopSfx) sfxSource.PlayOneShot(linePopSfx);

        subtitleText.text = text;

        // start fully transparent
        Color c = subtitleText.color;
        c.a = 0f;
        subtitleText.color = c;

        float t = 0f;

        // fade in
        while (t < lineFadeInDuration)
        {
            if (AdvancePressed())
            {
                _wantsAdvance = true; // request NEXT line
                yield break;
            }

            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / lineFadeInDuration);
            subtitleText.color = c;
            yield return null;
        }

        // ensure fully visible and guard tiny time
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

    private IEnumerator WaitForVoiceOrAdvance(AudioSource src)
    {
        while (src && src.isPlaying && !_wantsAdvance)
        {
            if (AdvancePressed()) { _wantsAdvance = true; break; }
            if (Input.GetKeyDown(skipKey)) { src.Stop(); break; }
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

        // smooth volume approach
        while (!Mathf.Approximately(bedSource.volume, target))
        {
            bedSource.volume = Mathf.MoveTowards(
                bedSource.volume, target,
                Time.deltaTime * (1f / Mathf.Max(0.0001f, 1f / bedDuckSpeed))
            );
            yield return null;
        }
        bedSource.volume = target;
    }
}
