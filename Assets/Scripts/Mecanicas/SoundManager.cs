using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instancia;

    public AudioMixer mixer;
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioSource loopingSfxSource;

    public const string SFXVolumeKey = "SFXVolume";
    public const string MusicVolumeKey = "MusicVolume";
    [SerializeField] string menuSceneName = "Menu";

    [Header("Faixas De Audios")]
    public AudioClip purchase;
    public AudioClip rewardMoney;
    public AudioClip HeavyHit;
    public AudioClip Scream;
    public AudioClip Coletavel;
    public AudioClip Select;
    public AudioClip Hit;
    public AudioClip Danger;
    public AudioClip Explosion;
    public AudioClip BossDeath;

    [Header("Músicas")]
    public AudioClip backgroundMusicMenu;
    public AudioClip backgroundMusicGameplay;

    [Header("UI Sons")]
    public AudioClip ClickButton;
    public AudioClip MouseEnter;
    public AudioClip MouseClick;

    void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(this); // só o componente; o GameManager cuida do GameObject
            return;
        }
        instancia = this;
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void Start()
    {
        // Aplica o volume salvo mesmo sem slider na cena
        SetSFXVolume(GetSavedVolume(SFXVolumeKey));
        SetMusicVolume(GetSavedVolume(MusicVolumeKey));
    }

    // ---------- Volume ----------
    public float GetSavedVolume(string key) => PlayerPrefs.GetFloat(key, 1f);

    public void SetSFXVolume(float volume) => ApplyVolume("SfxAudio", SFXVolumeKey, volume);
    public void SetMusicVolume(float volume) => ApplyVolume("MusicAudio", MusicVolumeKey, volume);

    void ApplyVolume(string mixerParam, string key, float volume)
    {
        volume = Mathf.Clamp(volume, 0.0001f, 1f);
        if (mixer != null) mixer.SetFloat(mixerParam, Mathf.Log10(volume) * 20f);
        PlayerPrefs.SetFloat(key, volume);
    }

    public void SaveSettings() => PlayerPrefs.Save();
    void OnApplicationQuit() => SaveSettings();

    // ---------- Música ----------
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusic(scene.name == menuSceneName ? backgroundMusicMenu : backgroundMusicGameplay);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource.clip == clip) return;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    // ---------- SFX ----------
    public void PlaySFX(AudioClip clip, bool loop = false)
    {
        if (clip == null) return;

        if (loop)
        {
            loopingSfxSource.clip = clip;
            loopingSfxSource.loop = true;
            loopingSfxSource.Play();
        }
        else
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    public void StopLoopingSFX()
    {
        loopingSfxSource.Stop();
        loopingSfxSource.loop = false;
    }
}