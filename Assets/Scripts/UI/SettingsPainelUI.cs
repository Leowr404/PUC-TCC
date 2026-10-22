using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Liga os controles da tela de Settings às Configuracoes.
// Cada mudança é aplicada e salva na hora.
public class SettingsPainelUI : MonoBehaviour
{
    [Header("Áudio")]
    public Slider volumeGeral;
    public Slider volumeMusica;
    public Slider volumeEfeitos;

    [Header("Vídeo")]
    public TMP_Dropdown resolucao;
    public Toggle telaCheia;
    public TMP_Dropdown qualidade;
    public TMP_Dropdown limiteFps;
    public Toggle vsync;

    [Header("Acessibilidade")]
    public Toggle daltonico;
    public Toggle tremor;
    public Toggle vibracao;

    bool carregando;

    void Awake()
    {
        Opcoes(resolucao, Configuracoes.ResolucoesDisponiveis.Select(r => r.width + " × " + r.height));
        Opcoes(qualidade, Configuracoes.NomesQualidade);
        Opcoes(limiteFps, Configuracoes.NomesFps);

        Ouvir(volumeGeral, v => Configuracoes.VolumeGeral = v, false);
        Ouvir(volumeMusica, v => Configuracoes.VolumeMusica = v, false);
        Ouvir(volumeEfeitos, v => Configuracoes.VolumeEfeitos = v, false);
        Ouvir(resolucao, i => Configuracoes.Resolucao = i);
        Ouvir(qualidade, i => Configuracoes.Qualidade = i);
        Ouvir(limiteFps, i => Configuracoes.LimiteFps = i);
        Ouvir(telaCheia, b => Configuracoes.TelaCheia = b, true);
        Ouvir(vsync, b => { Configuracoes.VSync = b; AtualizarFps(); }, true);
        Ouvir(daltonico, b => Configuracoes.Daltonico = b, false);
        Ouvir(tremor, b => Configuracoes.TremorDeTela = b, false);
        Ouvir(vibracao, b => Configuracoes.Vibracao = b, false);
    }

    void OnEnable() { MostrarValores(); }

    void MostrarValores()
    {
        carregando = true; // não salva enquanto preenche
        if (volumeGeral) volumeGeral.value = Configuracoes.VolumeGeral;
        if (volumeMusica) volumeMusica.value = Configuracoes.VolumeMusica;
        if (volumeEfeitos) volumeEfeitos.value = Configuracoes.VolumeEfeitos;
        Valor(resolucao, Configuracoes.IndiceResolucao());
        Valor(qualidade, Configuracoes.Qualidade);
        Valor(limiteFps, Configuracoes.LimiteFps);
        if (telaCheia) telaCheia.isOn = Configuracoes.TelaCheia;
        if (vsync) vsync.isOn = Configuracoes.VSync;
        if (daltonico) daltonico.isOn = Configuracoes.Daltonico;
        if (tremor) tremor.isOn = Configuracoes.TremorDeTela;
        if (vibracao) vibracao.isOn = Configuracoes.Vibracao;
        carregando = false;
        AtualizarFps();
    }

    // com VSync ligado o limite de FPS não tem efeito
    void AtualizarFps()
    {
        if (limiteFps != null) limiteFps.interactable = !Configuracoes.VSync;
    }

    public void RestaurarPadrao()
    {
        Configuracoes.RestaurarPadrao();
        MostrarValores();
    }

    static void Opcoes(TMP_Dropdown d, IEnumerable<string> itens)
    {
        if (d == null) return;
        d.ClearOptions();
        d.AddOptions(itens.ToList());
    }

    static void Valor(TMP_Dropdown d, int i)
    {
        if (d == null || d.options.Count == 0) return;
        d.value = Mathf.Clamp(i, 0, d.options.Count - 1);
        d.RefreshShownValue();
    }

    void Ouvir(Slider s, Action<float> alterar, bool video)
    {
        if (s) s.onValueChanged.AddListener(v => Mudar(() => alterar(v), video));
    }

    void Ouvir(Toggle t, Action<bool> alterar, bool video)
    {
        if (t) t.onValueChanged.AddListener(v => Mudar(() => alterar(v), video));
    }

    void Ouvir(TMP_Dropdown d, Action<int> alterar)
    {
        if (d) d.onValueChanged.AddListener(v => Mudar(() => alterar(v), true));
    }

    void Mudar(Action alterar, bool video)
    {
        if (carregando) return;
        alterar();
        Configuracoes.Salvar();
        if (video) Configuracoes.AplicarVideo();
        else Configuracoes.AplicarAudio();
        Configuracoes.Notificar();
    }
}
