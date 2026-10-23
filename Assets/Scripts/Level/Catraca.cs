using UnityEngine;

public class Catraca : MonoBehaviour
{
    [Header("Configuração da Ferramenta Proibida")]
    [Tooltip("Nome do prefab da ferramenta que bloqueia a passagem (ex: LaserPointer)")]
    public string ferramentaProibidaNome = "LaserPointer";

    [Header("Referência")]
    [Tooltip("O Collider sólido da catraca que vai barrar o player")]
    public Collider colliderSolidoCatraca;

    private void OnTriggerStay(Collider other)
    {
        // Verifica se quem entrou na área é um Player
        PlayerController3D player = other.GetComponent<PlayerController3D>();
        Collider playerCollider = other.GetComponent<Collider>();

        if (player != null && playerCollider != null && colliderSolidoCatraca != null)
        {
            bool temFerramentaProibida = false;

            // Verifica se o player está equipando alguma ferramenta
            GameObject ferramentaAtual = player.GetCurrentToolPrefab();
            if (ferramentaAtual != null)
            {
                // Compara o nome do prefab (remove o "(Clone)" caso exista)
                string nomeLimpo = ferramentaAtual.name.Replace("(Clone)", "").Trim();
                if (nomeLimpo.Equals(ferramentaProibidaNome, System.StringComparison.OrdinalIgnoreCase))
                {
                    temFerramentaProibida = true;
                }
            }

            // Se tiver a ferramenta, a catraca tem física (ignorar colisão = false)
            // Se NÃO tiver, o player atravessa (ignorar colisão = true)
            Physics.IgnoreCollision(colliderSolidoCatraca, playerCollider, !temFerramentaProibida);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Quando o player sai da área da catraca, restauramos a colisão padrão por segurança
        PlayerController3D player = other.GetComponent<PlayerController3D>();
        Collider playerCollider = other.GetComponent<Collider>();

        if (player != null && playerCollider != null && colliderSolidoCatraca != null)
        {
            Physics.IgnoreCollision(colliderSolidoCatraca, playerCollider, false);
        }
    }
}