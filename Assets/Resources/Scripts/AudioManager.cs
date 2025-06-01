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
    public AudioClip characterSelectMusic;  // 🎵 Assign in Inspector

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

        SceneManager.sceneLoaded -= OnSceneLoaded; // Just in case
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
                StartCoroutine(FadeOutMusic(0.5f)); // 👈 Smooth fade-out
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

    public void PlaySFX(AudioClip clip)
    {
        if (clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void PlayButtonClickSound()
    {
        PlaySFX(buttonClickSound);
    }

    // ✅ Smoothly fade out music
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
