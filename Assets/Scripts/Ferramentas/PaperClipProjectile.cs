using UnityEngine;

// Controla o projetil de clipe de papel disparado
public class PaperClipProjectile : MonoBehaviour
{
    [Header("Configuracoes do Projetil")]
    public float speed = 20f;             // Velocidade do disparo
    public float lifeTime = 3f;           // Tempo de vida antes de ser destruido
    public float freezeDuration = 3f;     // Tempo em segundos que o inimigo ficara congelado

    private Rigidbody rb;

    void Start()
    {
        // Busca o Rigidbody usando a sintaxe typeof
        rb = (Rigidbody)GetComponent(typeof(Rigidbody));

        // Aplica velocidade para frente no momento da criacao
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
        }

        // Destroi o clipe automaticamente apos alguns segundos
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Ignora o jogador que disparou
        if (!other.CompareTag("Player"))
        {
            // Busca o PatrolEnemy usando a sintaxe typeof
            PatrolEnemy enemy = (PatrolEnemy)other.GetComponent(typeof(PatrolEnemy));
            if (enemy != null)
            {
                enemy.Freeze(freezeDuration);
            }

            Destroy(gameObject);
        }
    }
}