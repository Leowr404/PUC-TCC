using UnityEngine;

// Controla o projetil de papel higienico disparado
public class ToiletPaperProjectile : MonoBehaviour
{
    public float speed = 15f;
    public float lifeTime = 3f;

    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Aplica velocidade para frente no momento da criacao
        if (rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
        }

        // Destroi o papel automaticamente apos alguns segundos para nao acumular na cena
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Se bater em uma parede, obstaculo ou elemento de puzzle
        if (!other.CompareTag("Player"))
        {
            // Adicione logicas futuras aqui (ex: ativar um botao, cobrir um obstaculo)
            Destroy(gameObject);
        }
    }
}