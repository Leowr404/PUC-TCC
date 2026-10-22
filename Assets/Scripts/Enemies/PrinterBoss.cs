using System.Collections;
using UnityEngine;

public class PrinterBoss : MonoBehaviour
{
    public enum BossState { Patrol, Shooting, Reloading, Dash }

    [Header("Estado Atual")]
    public BossState currentState = BossState.Patrol;

    [Header("Modelo 3D (Opcional)")]
    public Transform modelMesh;

    [Header("Movimentacao (Patrulha)")]
    public Transform leftPoint;           // Ponto limite esquerdo
    public Transform rightPoint;          // Ponto limite direito
    public float moveSpeed = 3f;          // Velocidade de movimento normal
    public bool startFacingRight = false; // Defina no Inspector para qual lado ele começa olhando
    private bool movingRight;

    [Header("Ataque Especial (Investida / Dash)")]
    public float dashSpeed = 10f;         // Velocidade da investida
    public float dashStunTime = 1.5f;     // Tempo parado após o dash antes de voltar a patrulhar
    public float upwardDashForce = 15f;   // Força com que o jogador é lançado para CIMA no Dash
    private Vector3 dashTargetPosition;   // Ponto fixo onde o jogador estava

    [Header("Dano por Colisao Direta (Toque Normal)")]
    public float contactKnockback = 8f;   // Força do empurrão normal (fora do dash)
    public float contactStunDuration = 1f;// Tempo de stun ao tocar no jogador

    [Header("Visao e Deteccao por Area (Box)")]
    public Transform sightOrigin;         // Ponto de onde sai a visao (Vision)
    public Vector3 sightBoxSize = new Vector3(8f, 3f, 4f); // Largura (distância X), Altura (Y), Profundidade (Z)
    public Vector3 sightOffset = Vector3.zero;             // Ajuste fino da posição da caixa no Inspector
    public LayerMask playerLayer;         // Layer dos Jogadores
    public LayerMask obstacleLayer;       // Layer de paredes/obstaculos

    [Header("Ataque de Papel")]
    public GameObject paperProjectilePrefab;   // Prefab padrao da folha de papel
    public Transform firePoint;                // Local de onde sai o papel
    public int papersPerBurst = 5;             // Quantidade por rajada
    public float fireRate = 0.2f;              // Intervalo entre tiros
    public float projectileSpeed = 12f;        // Velocidade do papel

    [Header("Recarga")]
    public float reloadTime = 3f;              // Tempo recarregando
    private int burstCount = 0;                // Contador de rajadas de tiro efetuadas

    private Collider bossCollider;
    private Rigidbody rb;

    void Start()
    {
        bossCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.isKinematic = true;
        }

        movingRight = startFacingRight;
        ApplyFacingVisual();
    }

    void Update()
    {
        switch (currentState)
        {
            case BossState.Patrol:
                PatrolMovement();
                CheckForPlayerArea();
                break;

            case BossState.Shooting:
                // Fica parado enquanto atira
                break;

            case BossState.Reloading:
                // Fica parado enquanto recarrega
                break;

            case BossState.Dash:
                // O movimento de investida é processado pela Corrotina DoDashAttack()
                break;
        }
    }

    // 1. MOVIMENTO DE PATRULHA
    void PatrolMovement()
    {
        if (leftPoint == null || rightPoint == null) return;

        Transform targetPoint = movingRight ? rightPoint : leftPoint;

        Vector3 targetPosition = new Vector3(targetPoint.position.x, transform.position.y, targetPoint.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                             new Vector3(targetPoint.position.x, 0, targetPoint.position.z)) < 0.2f)
        {
            movingRight = !movingRight;
            ApplyFacingVisual();
        }
    }

    void ApplyFacingVisual()
    {
        Transform targetTransform = modelMesh != null ? modelMesh : transform;
        Vector3 scale = targetTransform.localScale;
        scale.x = Mathf.Abs(scale.x) * (movingRight ? 1f : -1f);
        targetTransform.localScale = scale;
    }

    private Vector3 GetFacingDirection()
    {
        return movingRight ? Vector3.right : Vector3.left;
    }

    private Vector3 GetSightBoxCenter()
    {
        Vector3 baseOrigin = sightOrigin != null ? sightOrigin.position : transform.position;
        Vector3 forwardDir = GetFacingDirection();

        Vector3 center = baseOrigin + forwardDir * (sightBoxSize.x / 2f);
        center += new Vector3(sightOffset.x * (movingRight ? 1f : -1f), sightOffset.y, sightOffset.z);

        return center;
    }

    // 2. DETECÇÃO DO JOGADOR POR ÁREA (OverlapBox)
    void CheckForPlayerArea()
    {
        Vector3 center = GetSightBoxCenter();
        Vector3 halfExtents = sightBoxSize / 2f;

        Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, Quaternion.identity, playerLayer);

        foreach (Collider hit in hitColliders)
        {
            if (bossCollider != null && hit == bossCollider) continue;

            Vector3 origin = sightOrigin != null ? sightOrigin.position : transform.position;
            Vector3 dirToPlayer = (hit.transform.position - origin).normalized;
            float distToPlayer = Vector3.Distance(origin, hit.transform.position);

            if (!Physics.Raycast(origin, dirToPlayer, distToPlayer, obstacleLayer))
            {
                Debug.Log("JOGADOR DETECTADO NA ÁREA DE VISÃO! Iniciando ataque.");

                if (burstCount >= 2)
                {
                    dashTargetPosition = new Vector3(hit.transform.position.x, transform.position.y, hit.transform.position.z);
                    StartCoroutine(DoDashAttack());
                }
                else
                {
                    StartCoroutine(ShootBurstRoutine());
                }
                break;
            }
        }
    }

    // 3. ROTINA DE ATAQUE DE PAPEL
    IEnumerator ShootBurstRoutine()
    {
        currentState = BossState.Shooting;

        for (int i = 0; i < papersPerBurst; i++)
        {
            ShootPaper();
            yield return new WaitForSeconds(fireRate);
        }

        currentState = BossState.Reloading;
        burstCount++;

        yield return new WaitForSeconds(reloadTime);

        currentState = BossState.Patrol;
    }

    void ShootPaper()
    {
        if (paperProjectilePrefab == null) return;

        Transform spawnPoint = firePoint != null ? firePoint : transform;
        Vector3 shootDirection = GetFacingDirection();

        GameObject paper = Instantiate(paperProjectilePrefab, spawnPoint.position, Quaternion.LookRotation(shootDirection));
        Rigidbody paperRb = paper.GetComponent<Rigidbody>();

        if (paperRb != null)
        {
            paperRb.linearVelocity = shootDirection * projectileSpeed;
        }
    }

    // 4. ROTINA DE INVESTIDA (DASH)
    IEnumerator DoDashAttack()
    {
        currentState = BossState.Dash;
        burstCount = 0;

        while (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                                new Vector3(dashTargetPosition.x, 0, dashTargetPosition.z)) > 0.3f)
        {
            transform.position = Vector3.MoveTowards(transform.position, dashTargetPosition, dashSpeed * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(dashStunTime);

        currentState = BossState.Patrol;
    }

    // 5. COLISÃO DIRETA COM O JOGADOR (ARREMESSO PARA CIMA NO DASH)
    private void OnCollisionEnter(Collision collision)
    {
        PlayerController3D player = collision.gameObject.GetComponent<PlayerController3D>();

        if (player != null)
        {
            Vector3 hitDirection = (player.transform.position - transform.position).normalized;

            if (currentState == BossState.Dash)
            {
                // Durante o Dash: Direciona a força fortemente para CIMA (eixo Y)
                hitDirection.x *= 0.3f; // Reduz empurrão horizontal
                hitDirection.z *= 0.3f;
                hitDirection.y = 1.5f;  // Força vertical elevada

                player.TakeDamage(hitDirection.normalized * upwardDashForce, contactStunDuration);
            }
            else
            {
                // Toque normal durante a patrulha
                hitDirection.y = 0.4f;
                player.TakeDamage(hitDirection * contactKnockback, contactStunDuration);
            }

            player.DropCurrentTool(); // Solta a ferramenta ao ser atingido
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = GetSightBoxCenter();
        Gizmos.DrawWireCube(center, sightBoxSize);
    }
}