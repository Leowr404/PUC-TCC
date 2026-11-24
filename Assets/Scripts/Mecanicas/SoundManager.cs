using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

// Gerencia músicas, efeitos sonoros (SFX), looping e integração com Configuracoes e MixerPrincipal
public class AudioManager : MonoBehaviour
{
    public static AudioManager instancia;

    [Tooltip("Opcional: se ficar vazio, o AudioManager cria as AudioSources sozinho.")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource sfxSource;
    [SerializeField] AudioSource loopingSfxSource;

    [Header("Ajuste de Volume dos Efeitos")]
    [Tooltip("Multiplica o volume dos efeitos (ex: 0.25 = bem mais baixo, 0.5 = metade).")]
    [Range(0f, 1f)]
    [SerializeField] float sfxVolumeMultiplier = 0.15f;

    public const string SFXVolumeKey = "SFXVolume";
    public const string MusicVolumeKey = "MusicVolume";

    [SerializeField] string menuSceneName = "Menu";

    [Header("Faixas De Audios")]
    public AudioClip Hit;
    public AudioClip BossDeath;
    public AudioClip elevadorSubindo;

    [Header("Player Sons")]
    public AudioClip andar;
    public AudioClip atirarGrampo;
    public AudioClip empurrar;

    [Header("Boss Sons")]
    public AudioClip bossAndando;
    public AudioClip bossAtirando;
    public AudioClip bossRecarregando;
    public AudioClip bossDanoGrampo;

    [Header("Músicas")]
    public AudioClip backgroundMusicMenu;
    public AudioClip backgroundMusicGameplay;
    public AudioClip musicaElevador;

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
        if (sfxSource != null)
        {
            sfxSource.ignoreListenerPause = true;
        }
        loopingSfxSource = Preparar(loopingSfxSource, "SFX Loop", grupoEfeitos);

        AplicarMultiplicadorSFX();
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    void Start()
    {
        Configuracoes.AplicarAudio();
        AplicarMultiplicadorSFX();
        OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    void OnValidate()
    {
        AplicarMultiplicadorSFX();
    }

    public void AplicarMultiplicadorSFX()
    {
        if (sfxSource != null) sfxSource.volume = sfxVolumeMultiplier;
        if (loopingSfxSource != null) loopingSfxSource.volume = sfxVolumeMultiplier;
    }

    static AudioMixerGroup Grupo(AudioMixer mixer, string nome)
    {
        if (mixer == null) return null;
        var grupos = mixer.FindMatchingGroups(nome);
        if (grupos == null || grupos.Length == 0)
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
            fonte.spatialBlend = 0f;
        }
        if (grupo != null && fonte.outputAudioMixerGroup == null) fonte.outputAudioMixerGroup = grupo;
        return fonte;
    }

    public float GetSavedVolume(string key) =>
        key == MusicVolumeKey ? Configuracoes.VolumeMusica : Configuracoes.VolumeEfeitos * sfxVolumeMultiplier;

    public void SetSFXVolume(float volume)
    {
        Configuracoes.VolumeEfeitos = volume;
        Configuracoes.AplicarAudio();
        AplicarMultiplicadorSFX();
    }

    public void SetMusicVolume(float volume)
    {
        Configuracoes.VolumeMusica = volume;
        Configuracoes.AplicarAudio();
    }

    public void SaveSettings() => Configuracoes.Salvar();

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

    public void PlaySFX(AudioClip clip, bool loop = false)
    {
        if (clip == null) return;

        if (loop)
        {
            if (loopingSfxSource == null) return;
            loopingSfxSource.volume = sfxVolumeMultiplier;
            loopingSfxSource.clip = clip;
            loopingSfxSource.loop = true;
            loopingSfxSource.Play();
        }
        else
        {
            if (sfxSource == null) return;
            sfxSource.volume = sfxVolumeMultiplier;
            sfxSource.PlayOneShot(clip);
        }
    }

    public void TocarClique(AudioClip clip = null) => PlaySFX(clip != null ? clip : ClickButton);
    public void TocarSelecao(AudioClip clip = null) => PlaySFX(clip != null ? clip : MouseEnter);

    public void StopLoopingSFX()
    {
        if (loopingSfxSource == null) return;
        loopingSfxSource.Stop();
        loopingSfxSource.loop = false;
    }

    void OnDestroy()
    {
        if (instancia == this)
        {
            instancia = null;
        }
    }
}