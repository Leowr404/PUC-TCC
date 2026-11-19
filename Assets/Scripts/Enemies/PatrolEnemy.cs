using UnityEngine;

// Controla a movimentacao em patrulha e o ataque do inimigo
public class PatrolEnemy : MonoBehaviour
{
    [Header("Movimentacao")]
    public float speed = 3f;             // Velocidade de deslocamento do inimigo
    public Transform pointA;            // Ponto limite da patrulha a esquerda
    public Transform pointB;            // Ponto limite da patrulha a direita
    public LayerMask groundLayer;       // Camada do chao para o Raycast da slope

    [Header("Efeitos do Ataque")]
    public float knockbackForce = 8f;   // Intensidade do empurrao no jogador
    public float stunDuration = 1.0f;   // Tempo em segundos que o jogador fica paralisado

    [Header("Derrota pelo Vento")]
    public AudioClip windDeathSound;    // Efeito sonoro ao ser lançado pelo vento

    private Vector3 targetPosition;     // Guarda a posicao atual para onde o inimigo caminha
    private float freezeTimer = 0f;     // Tempo restante que o inimigo fica congelado/parado
    private Rigidbody rb;
    private bool isDefeatedByWind = false;

    // --- ATRACAO PELO LASER ---
    private bool isChasingLaser = false;
    private Vector3 laserPointTarget;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            // Permite que a gravidade e o Rigidbody atuem corretamente na slope
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        // Define o Ponto B como primeiro objetivo e ajusta o olhar do inimigo
        if (pointA != null && pointB != null)
        {
            targetPosition = pointB.position;
            UpdateRotationToTarget(targetPosition);
        }
    }

    void Update()
    {
        if (isDefeatedByWind) return;

        // Se estiver congelado por causa do clipe, decrementa o tempo e nao se move
        if (freezeTimer > 0f)
        {
            freezeTimer -= Time.deltaTime;
            return; // Interrompe o Update aqui para impedir a movimentação
        }

        // --- SE ESTIVER SENDO ATRAIDO PELA PONTA DO LASER ---
        if (isChasingLaser)
        {
            // Calcula a posição no plano horizontal em direção ao laser, ajustando a altura pelo chão
            Vector3 groundLaserTarget = GetPointOnGround(new Vector3(laserPointTarget.x, transform.position.y + 2f, laserPointTarget.z));

            // Caminha em direcao ao laser ajustando-se à slope
            transform.position = Vector3.MoveTowards(transform.position, groundLaserTarget, speed * Time.deltaTime);
            UpdateRotationToTarget(groundLaserTarget);

            // Reseta o estado. Se o laser continuar ativo no proximo frame,
            // SetLaserTarget sera chamada novamente mantendo a perseguição.
            isChasingLaser = false;
            return;
        }

        // --- MOVIMENTACAO NORMAL DE PATRULHA ---
        if (pointA == null || pointB == null) return;

        // Pega a posição alvo de patrulha ajustada à inclinação do terreno (slope)
        Vector3 groundTargetPosition = GetPointOnGround(new Vector3(targetPosition.x, transform.position.y + 2f, targetPosition.z));
        transform.position = Vector3.MoveTowards(transform.position, groundTargetPosition, speed * Time.deltaTime);

        // Verifica se chegou proximo do ponto limite de patrulha
        if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                             new Vector3(targetTargetX(targetPosition), 0, targetPosition.z)) < 0.05f ||
            Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(targetPosition.x, 0, targetPosition.z)) < 0.05f)
        {
            SwitchDirection();
        }
    }

    // Método auxiliar para pegar o X de um Vector3 com segurança
    float targetTargetX(Vector3 pos) => pos.x;

    // Joga um raio de cima para baixo para encontrar a altura exata do chão (slope)
    Vector3 GetPointOnGround(Vector3 target)
    {
        Vector3 rayStart = target + Vector3.up * 3f;
        if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 10f, groundLayer))
        {
            return hit.point; // Retorna o ponto exato colado na slope
        }
        return target; // Se não achar o chão, mantém o alvo original
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

    // Chamado pelo ventilador quando o inimigo é atingido pelo vento
    public void GetBlownAway(Vector3 forceVector)
    {
        if (isDefeatedByWind) return;
        isDefeatedByWind = true;

        // 1. Para toda a IA e lógica de movimento
        enabled = false;

        // 2. Remove todas as colisões (os players podem atravessar)
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            // Destrava rotações para dar o efeito de "Ragdoll" / cambalhota no ar
            rb.constraints = RigidbodyConstraints.None;

            // Aplica a força do vento lançando para fora do mapa
            rb.AddForce(forceVector, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 15f, ForceMode.Impulse);
        }

        // 3. Toca o efeito sonoro
        if (windDeathSound != null)
        {
            AudioSource.PlayClipAtPoint(windDeathSound, transform.position);
        }

        // 4. Desaparece (destrói o objeto) após 3 segundos
        Destroy(gameObject, 3f);
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
        if (isDefeatedByWind) return;

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