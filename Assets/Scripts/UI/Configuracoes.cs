using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

// Guarda e aplica as configurações do jogo. Carrega sozinho quando o jogo abre,
// então vale em qualquer cena, mesmo sem passar pelo menu.
//
// Volume: crie um Audio Mixer em Assets/Resources/MixerPrincipal com os parâmetros
// expostos "VolumeGeral", "VolumeMusica" e "VolumeEfeitos". Sem mixer, só o volume
// geral funciona (via AudioListener).
public static class Configuracoes
{
    public static float VolumeGeral, VolumeMusica, VolumeEfeitos;
    public static int Resolucao;          // índice em ResolucoesDisponiveis (-1 = a atual)
    public static bool TelaCheia;
    public static int Qualidade;          // 0 baixa, 1 média, 2 alta
    public static int LimiteFps;          // índice em NomesFps
    public static bool VSync;
    public static bool Daltonico, TremorDeTela, Vibracao;

    public static readonly string[] NomesQualidade = { "Baixa", "Média", "Alta" };
    public static readonly string[] NomesFps = { "60", "120", "144", "Sem limite" };
    static readonly int[] Fps = { 60, 120, 144, -1 };

    // avisa quem depende de uma opção (ex.: símbolos do modo daltônico)
    public static event Action Mudou;

    static Resolution[] resolucoes;
    static AudioMixer mixer;
    static bool mixerProcurado;

    public static Resolution[] ResolucoesDisponiveis
    {
        get
        {
            if (resolucoes == null)
                resolucoes = Screen.resolutions
                    .GroupBy(r => r.width * 100000 + r.height)
                    .Select(g => g.Last())
                    .OrderByDescending(r => r.width).ThenByDescending(r => r.height)
                    .ToArray();
            return resolucoes;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Iniciar()
    {
        Carregar();
        AplicarVideo();
        AplicarAudio();
    }

    public static void Carregar()
    {
        VolumeGeral = PlayerPrefs.GetFloat("cfg_volGeral", 0.8f);
        VolumeMusica = PlayerPrefs.GetFloat("cfg_volMusica", 0.6f);
        VolumeEfeitos = PlayerPrefs.GetFloat("cfg_volEfeitos", 0.75f);
        Resolucao = PlayerPrefs.GetInt("cfg_resolucao", -1);
        TelaCheia = PlayerPrefs.GetInt("cfg_telaCheia", 1) == 1;
        Qualidade = PlayerPrefs.GetInt("cfg_qualidade", 2);
        LimiteFps = PlayerPrefs.GetInt("cfg_fps", 3);
        VSync = PlayerPrefs.GetInt("cfg_vsync", 0) == 1;
        Daltonico = PlayerPrefs.GetInt("cfg_daltonico", 0) == 1;
        TremorDeTela = PlayerPrefs.GetInt("cfg_tremor", 1) == 1;
        Vibracao = PlayerPrefs.GetInt("cfg_vibracao", 1) == 1;
    }

    public static void Salvar()
    {
        PlayerPrefs.SetFloat("cfg_volGeral", VolumeGeral);
        PlayerPrefs.SetFloat("cfg_volMusica", VolumeMusica);
        PlayerPrefs.SetFloat("cfg_volEfeitos", VolumeEfeitos);
        PlayerPrefs.SetInt("cfg_resolucao", Resolucao);
        PlayerPrefs.SetInt("cfg_telaCheia", TelaCheia ? 1 : 0);
        PlayerPrefs.SetInt("cfg_qualidade", Qualidade);
        PlayerPrefs.SetInt("cfg_fps", LimiteFps);
        PlayerPrefs.SetInt("cfg_vsync", VSync ? 1 : 0);
        PlayerPrefs.SetInt("cfg_daltonico", Daltonico ? 1 : 0);
        PlayerPrefs.SetInt("cfg_tremor", TremorDeTela ? 1 : 0);
        PlayerPrefs.SetInt("cfg_vibracao", Vibracao ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void RestaurarPadrao()
    {
        foreach (var k in new[] { "cfg_volGeral", "cfg_volMusica", "cfg_volEfeitos", "cfg_resolucao", "cfg_telaCheia",
                                  "cfg_qualidade", "cfg_fps", "cfg_vsync", "cfg_daltonico", "cfg_tremor", "cfg_vibracao" })
            PlayerPrefs.DeleteKey(k);
        Carregar();
        Salvar();
        AplicarVideo();
        AplicarAudio();
        Notificar();
    }

    public static int IndiceResolucao()
    {
        var lista = ResolucoesDisponiveis;
        if (Resolucao >= 0 && Resolucao < lista.Length) return Resolucao;
        for (int i = 0; i < lista.Length; i++)
            if (lista[i].width == Screen.width && lista[i].height == Screen.height) return i;
        return 0;
    }

    public static void AplicarVideo()
    {
        // Baixa/Média/Alta viram o primeiro, o do meio e o último nível de Project Settings > Quality
        int n = QualitySettings.names.Length;
        if (n > 0) QualitySettings.SetQualityLevel(Mathf.RoundToInt((n - 1) * Mathf.Clamp(Qualidade, 0, 2) / 2f), true);

        // o nível de qualidade pode mexer no VSync, então vem depois
        QualitySettings.vSyncCount = VSync ? 1 : 0;
        Application.targetFrameRate = VSync ? -1 : Fps[Mathf.Clamp(LimiteFps, 0, Fps.Length - 1)];

        var modo = TelaCheia ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        var lista = ResolucoesDisponiveis;
        if (lista.Length > 0)
        {
            var r = lista[IndiceResolucao()];
            if (Screen.width != r.width || Screen.height != r.height || Screen.fullScreenMode != modo)
                Screen.SetResolution(r.width, r.height, modo);
        }
        else Screen.fullScreenMode = modo;
    }

    public static void AplicarAudio()
    {
        if (!mixerProcurado)
        {
            mixer = Resources.Load<AudioMixer>("MixerPrincipal");
            mixerProcurado = true;
        }
        if (mixer != null)
        {
            mixer.SetFloat("VolumeGeral", Db(VolumeGeral));
            mixer.SetFloat("VolumeMusica", Db(VolumeMusica));
            mixer.SetFloat("VolumeEfeitos", Db(VolumeEfeitos));
        }
        else AudioListener.volume = VolumeGeral;
    }

    static float Db(float v) { return Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f; }

    public static void Notificar()
    {
        if (Mudou != null) Mudou();
    }
}
