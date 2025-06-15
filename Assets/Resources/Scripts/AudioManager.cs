using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    public AudioClip mainMenuMusic;         // 🎵 Assign in Inspector
    public AudioClip buttonClickSound;      // 🔊 Assign in Inspector
    public AudioClip buttonHoverSound;      // 🔊 Assign in Inspector
    public AudioClip characterSelectMusic;  // 🎵 Assign in Inspector
    public AudioClip xpResultsMusic;  // 🎵 Assign in Inspector

    public AudioSource SFXSource => sfxSource;


    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        SetupAudioSources();

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public static void EnsureExists()
    {
        if (instance == null)
        {
            GameObject prefab = Resources.Load<GameObject>("Prefabs/AudioManager");
            if (prefab != null)
            {
                Instantiate(prefab);
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
        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            sfxSource = gameObject.AddComponent<AudioSource>();

            musicSource.loop = true;
            musicSource.playOnAwake = false;
            musicSource.volume = 0.5f;

            sfxSource.playOnAwake = false;
            sfxSource.volume = 1f;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("[AudioManager] Scene loaded: " + scene.name);

        switch (scene.name)
        {
            case "MainMenu":
                PlayMusic(mainMenuMusic);
                break;

            case "CharacterSelectScene":
                PlayMusic(characterSelectMusic);
                break;

            case "KULO":
                StartCoroutine(FadeOutMusic(0.5f)); // No music in gameplay
                break;

            case "XPResultsScene": // 🎉 NEW
                PlayMusic(xpResultsMusic);         // 🔊 Assign in inspector
                break;

            default:
                StartCoroutine(FadeOutMusic(0.5f));
                break;
        }
    }


    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null)
            return;

        if (musicSource.clip != clip)
        {
            musicSource.clip = clip;
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        if (musicSource != null && musicSource.isPlaying)
        {
            musicSource.Stop();
            musicSource.clip = null;
        }
    }

    /// <summary>
    /// Play an SFX at full volume.
    /// </summary>
    public void PlaySFX(AudioClip clip)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip);
    }

    /// <summary>
    /// Play an SFX at a specific volume scale (0–1).
    /// </summary>
    public void PlaySFX(AudioClip clip, float volumeScale)
    {
        if (clip != null && sfxSource != null)
            sfxSource.PlayOneShot(clip, Mathf.Clamp01(volumeScale));
    }

    /// <summary>
    /// Click sound. Default volume 0.8f.
    /// </summary>
    public void PlayButtonClickSound(float vol = 0.8f)
    {
        PlaySFX(buttonClickSound, vol);
    }

    /// <summary>
    /// Hover-over ping. Default volume 0.5f.
    /// </summary>
    public void PlayButtonHoverSound(float vol = 0.5f)
    {
        PlaySFX(buttonHoverSound, vol);
    }

    public IEnumerator FadeOutMusic(float duration)
    {
        if (musicSource == null || !musicSource.isPlaying)
            yield break;

        float startVolume = musicSource.volume;

        while (musicSource.volume > 0f)
        {
            musicSource.volume -= startVolume * Time.deltaTime / duration;
            yield return null;
        }

        musicSource.Stop();
        musicSource.clip = null;
        musicSource.volume = startVolume;
    }
}
