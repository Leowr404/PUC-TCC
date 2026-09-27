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
    private float freezeTimer = 0f;     // Tempo restante que o inimigo fica congelado/parado

    // --- ATRACAO PELO LASER ---
    private bool isChasingLaser = false;
    private Vector3 laserPointTarget;

    void Start()
    {
        // Define o Ponto B como primeiro objetivo e ajusta o olhar do inimigo
        if (pointA != null && pointB != null)
        {
            targetPosition = pointB.position;
            UpdateRotationToTarget(targetPosition);
        }
    }

    void Update()
    {
        // Se estiver congelado por causa do clipe, decrementa o tempo e nao se move
        if (freezeTimer > 0f)
        {
            freezeTimer -= Time.deltaTime;
            return; // Interrompe o Update aqui para impedir a movimentação
        }

        // --- SE ESTIVER SENDO ATRAIDO PELA PONTA DO LASER ---
        if (isChasingLaser)
        {
            // Camina em direcao a PONTA do laser
            transform.position = Vector3.MoveTowards(transform.position, laserPointTarget, speed * Time.deltaTime);
            UpdateRotationToTarget(laserPointTarget);

            // Reseta o estado. Se o laser continuar ativo e proximo no proximo frame,
            // a funcao SetLaserTarget sera chamada novamente e mantera a perseguicao.
            isChasingLaser = false;
            return;
        }

        // --- MOVIMENTACAO NORMAL DE PATRULHA ---
        if (pointA == null || pointB == null) return;

        // Move o inimigo continuamente em direcao ao ponto de destino atual
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        // Verifica se chegou proximo do ponto limite de patrulha
        if (Vector3.Distance(transform.position, targetPosition) < 0.05f)
        {
            SwitchDirection();
        }
    }

    // Chamado pelo LaserPointer quando a ponta do laser esta no alcance deste inimigo
    public void SetLaserTarget(Vector3 point)
    {
        laserPointTarget = point;
        isChasingLaser = true;
    }

    // Método chamado pelo clipe de papel para paralisar o inimigo
    public void Freeze(float duration)
    {
        freezeTimer = duration;
    }

    // Inverte o ponto de destino atual do inimigo
    void SwitchDirection()
    {
        if (pointA == null || pointB == null) return;

        // Se o destino atual e o Ponto B, muda para o Ponto A, e vice-versa
        targetPosition = (targetPosition == pointA.position) ? pointB.position : pointA.position;

        // Atualiza a rotacao para olhar para a nova direcao
        UpdateRotationToTarget(targetPosition);
    }

    // Calcula a direcao e vira o modelo do inimigo para a direcao informada
    void UpdateRotationToTarget(Vector3 destination)
    {
        float directionX = destination.x - transform.position.x;

        if (directionX > 0.05f)
        {
            transform.rotation = Quaternion.Euler(0, 90f, 0);  // Olhando para a direita
        }
        else if (directionX < -0.05f)
        {
            transform.rotation = Quaternion.Euler(0, -90f, 0); // Olhando para a esquerda
        }
    }

    // Detecta colisao com o jogador ou com objetos do cenario (caixas, paredes, etc)
    private void OnCollisionEnter(Collision collision)
    {
        PlayerController3D player = (PlayerController3D)collision.gameObject.GetComponent(typeof(PlayerController3D));
        if (player != null)
        {
            Vector3 hitDirection = (player.transform.position - transform.position).normalized;
            hitDirection.y = 0.5f;

            player.TakeDamage(hitDirection * knockbackForce, stunDuration);
            SwitchDirection();
            return;
        }

        if (!collision.gameObject.CompareTag("Ground"))
        {
            SwitchDirection();
        }
    }
}