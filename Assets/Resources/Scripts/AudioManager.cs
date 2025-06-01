using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    private AudioSource musicSource;
    private AudioSource sfxSource;

    public AudioClip mainMenuMusic; // 🎵 Assign this in Inspector
    public AudioClip buttonClickSound; // 🔊 Assign UI click sound

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Keeps AudioManager across scenes
        }
        else
        {
            Destroy(gameObject); // Prevent duplicate AudioManagers
            return;
        }

        // Create & configure AudioSources
        musicSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = 0.5f; // Adjust volume as needed

        sfxSource.playOnAwake = false;
        sfxSource.volume = 1f; // SFX volume

        SceneManager.sceneLoaded += OnSceneLoaded; // Detects scene change
    }

    void Start()
    {
        PlayMusic(mainMenuMusic); // Start Main Menu Music
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "MainMenu") // ✅ Stop music when leaving Main Menu
        {
            StopMusic();
        }
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip != null && musicSource.clip != clip)
        {
            musicSource.clip = clip;
            musicSource.Play();
        }
    }

    public void StopMusic()
    {
        if (musicSource.isPlaying)
        {
            musicSource.Stop();
            musicSource.clip = null; // ✅ Ensure no music restarts
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
}
