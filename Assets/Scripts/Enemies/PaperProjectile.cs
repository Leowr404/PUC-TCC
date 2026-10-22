using UnityEngine;

public class PaperProjectile : MonoBehaviour
{
    [Header("Configuracoes de Impacto")]
    public float knockbackForce = 6f;   // Forca do empurrao no jogador
    public float stunDuration = 0.8f;   // Tempo que o jogador fica paralisado
    public float lifeTime = 5f;          // Tempo de vida maximo do projetil no ar

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        // Destroi o papel automaticamente apos X segundos se nao acertar nada
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Se acertou o próprio boss ou outro papel, ignora
        if (other.CompareTag("Boss") || other.GetComponent<PaperProjectile>() != null) return;

        // Tenta pegar o controller do jogador
        PlayerController3D player = other.GetComponent<PlayerController3D>();

        if (player != null)
        {
            // 1. CALCULA DIREÇÃO DO KNOCKBACK (com leve impulso para cima)
            Vector3 hitDirection = (player.transform.position - transform.position).normalized;
            hitDirection.y = 0.4f;

            // 2. APLICA O DANO / KNOCKBACK
            player.TakeDamage(hitDirection * knockbackForce, stunDuration);

            // 3. FAZ O JOGADOR LARGAR A FERRAMENTA
            // Chamada adaptável: tenta chamar o método de soltar ferramenta no jogador
            player.DropCurrentTool(); // Substitua pelo nome exato do método de soltar item se for diferente em seu projeto

            // Destrói o papel após impactar o jogador
            Destroy(gameObject);
        }
        else if (!other.isTrigger)
        {
            // Se acertou parede, chão ou obstáculo rígido, destrói o papel
            Destroy(gameObject);
        }
    }
}