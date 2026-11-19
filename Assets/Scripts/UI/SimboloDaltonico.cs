using UnityEngine;

// Coloque nos objetos que dependem de cor (alvos da Caneta Laser, portas coloridas...).
// O "simbolo" é um filho com um ícone, padrão ou letra que identifica a cor;
// ele só aparece quando o Modo daltônico está ligado.
public class SimboloDaltonico : MonoBehaviour
{
    [Tooltip("Objeto com o símbolo/padrão que identifica a cor.")]
    public GameObject simbolo;

    void OnEnable()
    {
        Configuracoes.Mudou += Atualizar;
        Atualizar();
    }

    void OnDisable() { Configuracoes.Mudou -= Atualizar; }

    void Atualizar()
    {
        if (simbolo != null) simbolo.SetActive(Configuracoes.Daltonico);
    }
}
