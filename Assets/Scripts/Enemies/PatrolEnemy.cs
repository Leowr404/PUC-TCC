using UnityEngine;

// Controla a movimentacao em patrulha e o ataque do inimigo
public class PatrolEnemy : MonoBehaviour
{
    [Header("Movimentacao")]
    public float speed = 3f;             // Velocidade de deslocamento do inimigo
    public Transform pointA;            // Ponto limite da patrulha a esquerda
    public Transform pointB;            // Ponto limite da patrulha a direita

    [Header("Efeitos do Ataque")]
    public float knockbackForce = 8f;   // Intensidade do empurrao no jogador
    public float stunDuration = 1.0f;   // Tempo em segundos que o jogador fica paralisado

    private Vector3 targetPosition;     // Guarda a posicao atual para onde o inimigo caminha

    void Start()
    {
        // Define o Ponto B como primeiro objetivo e ajusta o olhar do inimigo
        if (pointA != null && pointB != null)
        {
            targetPosition = pointB.position;
            UpdateRotation();
        }
    }

    void Update()
    {
        // Se os pontos nao foram configurados no Inspector, interrompe o script
        if (pointA == null || pointB == null) return;

        // Move o inimigo continuamente em direcao ao ponto de destino atual
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        // Verifica se chegou proximo do ponto limite de patrulha
        if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            SwitchDirection();
        }
    }

    // Inverte o ponto de destino atual do inimigo
    void SwitchDirection()
    {
        if (pointA == null || pointB == null) return;

        // Se o destino atual e o Ponto B, muda para o Ponto A, e vice-versa
        targetPosition = (targetPosition == pointA.position) ? pointB.position : pointA.position;

        // Atualiza a rotacao para olhar para a nova direcao
        UpdateRotation();
    }

    // Calcula a direcao da caminhada e vira o modelo do inimigo para o lado correto
    void UpdateRotation()
    {
        float directionX = targetPosition.x - transform.position.x;

        if (directionX > 0)
        {
            transform.rotation = Quaternion.Euler(0, 90f, 0);  // Olhando para a direita
        }
        else if (directionX < 0)
        {
            transform.rotation = Quaternion.Euler(0, -90f, 0); // Olhando para a esquerda
        }
    }

    // Detecta colisao com o jogador ou com objetos do cenario (caixas, paredes, etc)
    private void OnCollisionEnter(Collision collision)
    {
        // 1. Verifica se colidiu com o Jogador
        PlayerController3D player = collision.gameObject.GetComponent<PlayerController3D>();
        if (player != null)
        {
            // Calcula a direcao do impacto (para tras e levemente para cima)
            Vector3 hitDirection = (player.transform.position - transform.position).normalized;
            hitDirection.y = 0.5f;

            // Aplica o dano, empurrao e atordoamento no jogador
            player.TakeDamage(hitDirection * knockbackForce, stunDuration);

            // Inverte a direcao da patrulha apos acertar o jogador
            SwitchDirection();
            return;
        }

        // 2. Se colidiu com qualquer outro objeto solido (como uma caixa arrastada pelo player)
        // Ignora apenas o chao para nao mudar de direcao enquanto caminha normalmente
        if (!collision.gameObject.CompareTag("Ground"))
        {
            SwitchDirection();
        }
    }
}