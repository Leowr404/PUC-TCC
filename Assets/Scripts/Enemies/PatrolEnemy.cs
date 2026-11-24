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
    public AudioClip windDeathSound;                // Efeito sonoro ao ser lançado pelo vento
    [Range(0f, 1f)]
    public float windDeathSoundChance = 0.25f;    // Chance de tocar o som (0.25 = 25%)

    private Vector3 targetPosition;
    private float freezeTimer;
    private Rigidbody rb;
    private Collider enemyCollider;
    private Transform selfTransform;
    private bool isDefeatedByWind;

    // --- ATRACAO PELO LASER ---
    private bool isChasingLaser;
    private Vector3 laserPointTarget;

    // Buffer reutilizável para evitar alocação em Raycast
    private static readonly RaycastHit[] groundHits = new RaycastHit[1];
    private static readonly Quaternion rightRotation = Quaternion.Euler(0f, 90f, 0f);
    private static readonly Quaternion leftRotation = Quaternion.Euler(0f, -90f, 0f);

    void Start()
    {
        selfTransform = transform;
        rb = GetComponent<Rigidbody>();
        enemyCollider = GetComponent<Collider>();

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        if (pointA != null && pointB != null)
        {
            targetPosition = pointB.position;
            UpdateRotationToTarget(targetPosition);
        }
    }

    void Update()
    {
        if (isDefeatedByWind) return;

        if (freezeTimer > 0f)
        {
            freezeTimer -= Time.deltaTime;
            return;
        }

        Vector3 pos = selfTransform.position;
        float step = speed * Time.deltaTime;

        // --- SE ESTIVER SENDO ATRAIDO PELA PONTA DO LASER ---
        if (isChasingLaser)
        {
            Vector3 groundLaserTarget = GetPointOnGround(new Vector3(laserPointTarget.x, pos.y + 2f, laserPointTarget.z));
            selfTransform.position = Vector3.MoveTowards(pos, groundLaserTarget, step);
            UpdateRotationToTarget(groundLaserTarget);

            isChasingLaser = false;
            return;
        }

        // --- MOVIMENTACAO NORMAL DE PATRULHA ---
        if (pointA == null || pointB == null) return;

        Vector3 groundTargetPosition = GetPointOnGround(new Vector3(targetPosition.x, pos.y + 2f, targetPosition.z));
        selfTransform.position = Vector3.MoveTowards(pos, groundTargetPosition, step);

        float dx = Mathf.Abs(selfTransform.position.x - targetPosition.x);
        float dz = Mathf.Abs(selfTransform.position.z - targetPosition.z);
        if (dx < 0.05f && dz < 0.05f)
        {
            SwitchDirection();
        }
    }

    // Joga um raio de cima para baixo para encontrar a altura exata do chão (slope) sem alocar lixo
    Vector3 GetPointOnGround(Vector3 target)
    {
        Vector3 rayStart = target + Vector3.up * 3f;
        int hitCount = Physics.RaycastNonAlloc(rayStart, Vector3.down, groundHits, 10f, groundLayer, QueryTriggerInteraction.Ignore);
        return hitCount > 0 ? groundHits[0].point : target;
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

        enabled = false;

        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.constraints = RigidbodyConstraints.None;
            rb.AddForce(forceVector, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * 15f, ForceMode.Impulse);
        }

        if (windDeathSound != null && Random.value < windDeathSoundChance)
        {
            AudioSource.PlayClipAtPoint(windDeathSound, selfTransform.position);
        }

        Destroy(gameObject, 3f);
    }

    // Inverte o ponto de destino atual do inimigo
    void SwitchDirection()
    {
        if (pointA == null || pointB == null) return;

        targetPosition = (targetPosition == pointA.position) ? pointB.position : pointA.position;
        UpdateRotationToTarget(targetPosition);
    }

    // Calcula a direcao e vira o modelo do inimigo para a direcao informada
    void UpdateRotationToTarget(Vector3 destination)
    {
        float directionX = destination.x - selfTransform.position.x;

        if (directionX > 0.05f)
        {
            selfTransform.rotation = rightRotation;
        }
        else if (directionX < -0.05f)
        {
            selfTransform.rotation = leftRotation;
        }
    }

    // Detecta colisao com o jogador ou com objetos do cenario (caixas, paredes, etc)
    private void OnCollisionEnter(Collision collision)
    {
        if (isDefeatedByWind) return;

        if (collision.gameObject.TryGetComponent(out PlayerController3D player))
        {
            Vector3 hitDirection = (player.transform.position - selfTransform.position).normalized;
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