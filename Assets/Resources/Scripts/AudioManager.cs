using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    public AudioClip mainMenuMusic;
    public AudioClip buttonClickSound;
    public AudioClip buttonHoverSound;
    public AudioClip characterSelectMusic;
    public AudioClip xpResultsMusic;

    public AudioSource SFXSource => sfxSource;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // 🔑 Make sure we are a ROOT object (no parent) before DDOL:
        if (transform.parent != null)
            transform.SetParent(null, false);

        DontDestroyOnLoad(gameObject);

        SetupAudioSources();

        SceneManager.sceneLoaded -= OnSceneLoaded; // idempotent
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        if (instance == this) instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public static void EnsureExists()
    {
        if (instance == null)
        {
            var prefab = Resources.Load<GameObject>("Prefabs/AudioManager");
            if (prefab != null)
            {
                // Instantiate at root so DDOL is valid even if prefab is nested in a scene
                var go = Instantiate(prefab);
                go.transform.SetParent(null, false);
                Debug.Log("[AudioManager] Instantiated from Resources.");
            }
            else
            {
                Debug.LogError("AudioManager prefab not found in Resources/Prefabs!");
            }
        }
    }

    private void SetupAudioSources()
    {
        if (musicSource == null) musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null) sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = 0.5f;

        sfxSource.playOnAwake = false;
        sfxSource.volume = 1f;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("[AudioManager] Scene loaded: " + scene.name);

        switch (scene.name)
        {
            case "MainMenu": PlayMusic(mainMenuMusic); break;
            case "CharacterSelectScene": PlayMusic(characterSelectMusic); break;
            case "KULO": StartCoroutine(FadeOutMusic(0.5f)); break;
            case "XPResultsScene": PlayMusic(xpResultsMusic); break;
            default: StartCoroutine(FadeOutMusic(0.5f)); break;
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (!clip || !musicSource) return;
        if (musicSource.clip == clip) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    public void StopMusic()
    {
        if (musicSource && musicSource.isPlaying)
        {
            musicSource.Stop();
            musicSource.clip = null;
        }
    }

    public void PlaySFX(AudioClip clip) { if (clip && sfxSource) sfxSource.PlayOneShot(clip); }
    public void PlaySFX(AudioClip clip, float volScale) { if (clip && sfxSource) sfxSource.PlayOneShot(clip, Mathf.Clamp01(volScale)); }
    public void PlayButtonClickSound(float v = 0.8f) => PlaySFX(buttonClickSound, v);
    public void PlayButtonHoverSound(float v = 0.5f) => PlaySFX(buttonHoverSound, v);

    public IEnumerator FadeOutMusic(float duration)
    {
        if (!musicSource || !musicSource.isPlaying) yield break;

        float startVol = musicSource.volume;
        while (musicSource.volume > 0f)
        {
            musicSource.volume -= startVol * Time.deltaTime / duration;
            yield return null;
        }
        musicSource.Stop();
        musicSource.clip = null;
        musicSource.volume = startVol;
    }
}
