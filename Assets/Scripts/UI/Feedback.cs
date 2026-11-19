using DG.Tweening;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Tremor de tela e vibração que respeitam as opções do Settings.
// Exemplo no boss: Feedback.TremerCamera(suporteDaCamera); Feedback.Vibrar(0);
public static class Feedback
{
    // Use num objeto pai da câmera (um "suporte"): se a câmera é movida por script
    // todo frame, o tremor aplicado direto nela é sobrescrito.
    // Treme a câmera do jogo (CoopCamera). Use este na gameplay.
    public static void TremerCamera(float forca = 0.25f, float duracao = 0.3f)
    {
        if (!Configuracoes.TremorDeTela || CoopCamera.instancia == null) return;
        CoopCamera.instancia.Shake(forca, duracao);
    }

    public static void TremerCamera(Transform alvo, float forca = 0.25f, float duracao = 0.3f)
    {
        if (!Configuracoes.TremorDeTela || alvo == null) return;
        alvo.DOComplete();
        alvo.DOShakePosition(duracao, forca, 20, 90f, false, true).SetLink(alvo.gameObject);
    }

#if ENABLE_INPUT_SYSTEM
    // indice = posição do controle na lista de controles conectados (0, 1...)
    public static void Vibrar(int indice, float intensidade = 0.6f, float duracao = 0.2f)
    {
        if (indice < 0 || indice >= Gamepad.all.Count) return;
        Vibrar(Gamepad.all[indice], intensidade, duracao);
    }

    // vibra o(s) controle(s) do jogador (pega do PlayerInput dele)
    public static void Vibrar(Component jogador, float intensidade = 0.6f, float duracao = 0.2f)
    {
        if (jogador == null) return;
        var input = jogador.GetComponentInParent<PlayerInput>();
        if (input == null) return;
        foreach (var d in input.devices)
            if (d is Gamepad g) Vibrar(g, intensidade, duracao);
    }

    // vibra todos os controles conectados (ex.: boss derrotado)
    public static void VibrarTodos(float intensidade = 0.6f, float duracao = 0.2f)
    {
        foreach (var g in Gamepad.all) Vibrar(g, intensidade, duracao);
    }

    // use esta versão se você já sabe o controle do jogador (ex.: PlayerInput.devices)
    public static void Vibrar(Gamepad controle, float intensidade = 0.6f, float duracao = 0.2f)
    {
        if (!Configuracoes.Vibracao || controle == null) return;
        controle.SetMotorSpeeds(intensidade * 0.5f, intensidade);
        DOVirtual.DelayedCall(duracao, () => { if (controle != null) controle.SetMotorSpeeds(0f, 0f); }, true);
    }
#else
    public static void Vibrar(int indice, float intensidade = 0.6f, float duracao = 0.2f) { }
    public static void Vibrar(Component jogador, float intensidade = 0.6f, float duracao = 0.2f) { }
    public static void VibrarTodos(float intensidade = 0.6f, float duracao = 0.2f) { }
#endif
}
