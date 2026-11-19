using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

// Toca música e efeitos sonoros. O VOLUME fica com a classe Configuracoes
// (painel de configurações), que usa o mixer Assets/Resources/MixerPrincipal.
//
// Como usar de qualquer script:
//   AudioManager.instancia.PlaySFX(AudioManager.instancia.Hit);
//   AudioManager.instancia.PlaySFX(clip, true);   // som em loop
//   AudioManager.instancia.StopLoopingSFX();
//
// IMPORTANTE: deixe este componente num GameObject só dele (ex.: "AudioManager").
// Ele passa de uma cena para outra (DontDestroyOnLoad), então a música não corta.
// Pode ter um em cada cena: o primeiro que existir fica, os outros se apagam.
public class AudioManager : MonoBehaviour
{
    public static AudioManager instancia;

    [Tooltip("Opcional: se ficar vazio, o AudioManager cria as AudioSources sozinho.")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioSource loopingSfxSource;

    // mantidos para o VolumeSlider antigo continuar compilando
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
            Destroy(gameObject);
            return;
        }
        instancia = this;

        // Só persiste se estiver num objeto próprio, para não levar outros scripts junto
        if (GetComponents<MonoBehaviour>().Length == 1)
        {
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("AudioManager: coloque este componente num GameObject só dele para a música não cortar entre cenas.", this);
        }

        var mixer = Resources.Load<AudioMixer>("MixerPrincipal");
        var grupoMusica = Grupo(mixer, "Musica");
        var grupoEfeitos = Grupo(mixer, "Efeitos");

        musicSource = Preparar(musicSource, "Musica", grupoMusica);
        sfxSource = Preparar(sfxSource, "SFX", grupoEfeitos);
        // o PauseManager pausa o AudioListener; sem isto os cliques do menu de pause ficariam mudos
        sfxSource.ignoreListenerPause = true;
        loopingSfxSource = Preparar(loopingSfxSource, "SFX Loop", grupoEfeitos);
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void Start()
    {
        Configuracoes.AplicarAudio();
        // toca a música da cena em que o jogo começou
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    static AudioMixerGroup Grupo(AudioMixer mixer, string nome)
    {
        if (mixer == null) return null;
        var grupos = mixer.FindMatchingGroups(nome);
        if (grupos.Length == 0)
        {
            Debug.LogWarning("AudioManager: grupo \"" + nome + "\" não encontrado no MixerPrincipal.");
            return null;
        }
        return grupos[0];
    }

    AudioSource Preparar(AudioSource fonte, string nome, AudioMixerGroup grupo)
    {
        if (fonte == null)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(transform, false);
            fonte = go.AddComponent<AudioSource>();
            fonte.playOnAwake = false;
            fonte.spatialBlend = 0f; // som 2D
        }
        if (grupo != null && fonte.outputAudioMixerGroup == null) fonte.outputAudioMixerGroup = grupo;
        return fonte;
    }

    // ---------- Volume (agora quem manda é o Configuracoes) ----------
    public float GetSavedVolume(string key) =>
        key == MusicVolumeKey ? Configuracoes.VolumeMusica : Configuracoes.VolumeEfeitos;

    public void SetSFXVolume(float volume) { Configuracoes.VolumeEfeitos = volume; Configuracoes.AplicarAudio(); }
    public void SetMusicVolume(float volume) { Configuracoes.VolumeMusica = volume; Configuracoes.AplicarAudio(); }
    public void SaveSettings() => Configuracoes.Salvar();

    // ---------- Música ----------
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusic(scene.name == menuSceneName ? backgroundMusicMenu : backgroundMusicGameplay);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null || musicSource == null || musicSource.clip == clip) return;
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

    // ---------- UI ----------
    // usados pelo FocoVisual; o clipe passado substitui o padrão
    public void TocarClique(AudioClip clip = null) => PlaySFX(clip != null ? clip : ClickButton);
    public void TocarSelecao(AudioClip clip = null) => PlaySFX(clip != null ? clip : MouseEnter);

    public void StopLoopingSFX()
    {
        loopingSfxSource.Stop();
        loopingSfxSource.loop = false;
    }
}
