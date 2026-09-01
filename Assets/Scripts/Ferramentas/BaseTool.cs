using UnityEngine;

// Classe base para todas as ferramentas do jogo
public abstract class BaseTool : MonoBehaviour
{
    // Chamado quando o jogador pressiona o botao de acao
    public virtual void OnActionDown(PlayerController3D player) { }

    // Chamado enquanto o jogador MANTEM o botao de acao pressionado (ex: Desentupidor)
    public virtual void OnActionHold(PlayerController3D player) { }

    // Chamado quando o jogador SOLTA o botao de acao
    public virtual void OnActionUp(PlayerController3D player) { }
}