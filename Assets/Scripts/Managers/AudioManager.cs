using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    private const string MusicVolumeKey = "Settings.MusicVolume";
    private const string SfxVolumeKey = "Settings.SfxVolume";

    public static AudioManager Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip mainMenuMusic;
    [SerializeField] private AudioClip battleMusic;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioClip cardHoverSound;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (musicSource == null)
        {
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
        }

        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }

        SetMusicVolume(PlayerPrefs.GetFloat(MusicVolumeKey, 0.5f));
        SetSFXVolume(PlayerPrefs.GetFloat(SfxVolumeKey, 0.7f));
    }

    private void Start()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
        PlayMusicForCurrentScene();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.activeSceneChanged -= OnSceneChanged;
        }
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        PlayMusicForCurrentScene();
    }

    private void PlayMusicForCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        if (sceneName == "MainMenu" && mainMenuMusic != null)
        {
            PlayMusic(mainMenuMusic);
            return;
        }

        if (sceneName == "CardSystemTest" && battleMusic != null)
        {
            PlayMusic(battleMusic);
            return;
        }

        StopMusic();
    }

    public void PlayMusic(AudioClip music)
    {
        if (musicSource == null || music == null)
        {
            return;
        }

        if (musicSource.clip != music)
        {
            musicSource.clip = music;
        }

        if (!musicSource.isPlaying)
        {
            musicSource.Play();
        }
    }

    public void PlayButtonClick()
    {
        if (buttonClickSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(buttonClickSound);
        }
    }

    public void PlayCardHoverSound()
    {
        if (cardHoverSound != null && sfxSource != null)
        {
            sfxSource.PlayOneShot(cardHoverSound);
        }
    }

    public void StopMusic()
    {
        if (musicSource != null)
        {
            musicSource.Stop();
        }
    }

    public void SetMusicVolume(float volume)
    {
        if (musicSource == null)
        {
            return;
        }

        musicSource.volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(MusicVolumeKey, musicSource.volume);
    }

    public void SetSFXVolume(float volume)
    {
        if (sfxSource == null)
        {
            return;
        }

        sfxSource.volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(SfxVolumeKey, sfxSource.volume);
    }
}
