using System.Collections;
using UnityEngine;

public class PrinterBoss : MonoBehaviour
{
    public enum BossState { Patrol, Shooting, Reloading }

    [Header("Estado Atual")]
    public BossState currentState = BossState.Patrol;

    [Header("Movimentacao (Patrulha)")]
    public Transform leftPoint;           // Ponto limite esquerdo
    public Transform rightPoint;          // Ponto limite direito
    public float moveSpeed = 3f;          // Velocidade de movimento
    private bool movingRight = true;

    [Header("Visao e Deteccao")]
    public Transform sightOrigin;         // Ponto de onde sai a visao (ex: olhos/sensor)
    public float sightDistance = 10f;     // Distancia da visao para frente
    public LayerMask playerLayer;         // Layer dos Jogadores
    public LayerMask obstacleLayer;       // Layer de paredes/obstaculos para bloquear a visao

    [Header("Ataque de Papel")]
    public GameObject paperProjectilePrefab; // Prefab da folha de papel disparada
    public Transform firePoint;              // Local de onde sai o papel (bandeja de saida)
    public int papersPerBurst = 5;           // Quantidade de papeis por rajada (X)
    public float fireRate = 0.2f;            // Intervalo entre cada papel disparado
    public float projectileSpeed = 12f;      // Velocidade do papel

    [Header("Recarga")]
    public float reloadTime = 3f;            // Tempo parado recarregando em segundos (X)

    private Rigidbody rb;
    private Transform targetPlayer;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        switch (currentState)
        {
            case BossState.Patrol:
                PatrolMovement();
                CheckForPlayer();
                break;

            case BossState.Shooting:
                // Fica parado enquanto atira
                break;

            case BossState.Reloading:
                // Fica parado enquanto recarrega
                break;
        }
    }

    // 1. MOVIMENTO DE PATRULHA
    void PatrolMovement()
    {
        if (leftPoint == null || rightPoint == null) return;

        Transform targetPoint = movingRight ? rightPoint : leftPoint;
        Vector3 direction = (targetPoint.position - transform.position).normalized;
        direction.y = 0; // Mantem no plano horizontal

        transform.position = Vector3.MoveTowards(transform.position, targetPoint.position, moveSpeed * Time.deltaTime);

        // Inverte a direcao ao chegar nos limites
        if (Vector3.Distance(transform.position, targetPoint.position) < 0.2f)
        {
            movingRight = !movingRight;
            FlipFacingDirection();
        }
    }

    void FlipFacingDirection()
    {
        // Gira o boss para olhar para o lado que esta andando
        Vector3 newScale = transform.localScale;
        newScale.z = movingRight ? Mathf.Abs(newScale.z) : -Mathf.Abs(newScale.z);
        transform.localScale = newScale;
    }

    // 2. DETECÇÃO DO JOGADOR
    void CheckForPlayer()
    {
        Vector3 origin = sightOrigin != null ? sightOrigin.position : transform.position;
        Vector3 forwardDir = movingRight ? transform.forward : -transform.forward;

        // Lanca um Raycast/SphereCast na direcao da visao
        if (Physics.Raycast(origin, forwardDir, out RaycastHit hit, sightDistance, playerLayer | obstacleLayer))
        {
            // Se o raio acertou um jogador antes de bater em uma parede
            if (((1 << hit.collider.gameObject.layer) & playerLayer) != 0)
            {
                targetPlayer = hit.transform;
                StartCoroutine(ShootBurstRoutine());
            }
        }
    }

    // 3. ROTINA DE ATAQUE E RECARGA
    IEnumerator ShootBurstRoutine()
    {
        currentState = BossState.Shooting;

        // Dispara X papeis com intervalo entre eles
        for (int i = 0; i < papersPerBurst; i++)
        {
            ShootPaper();
            yield return new WaitForSeconds(fireRate);
        }

        // Entra no estado de Recarga
        currentState = BossState.Reloading;
        yield return new WaitForSeconds(reloadTime);

        // Volta a patrulhar
        currentState = BossState.Patrol;
    }

    void ShootPaper()
    {
        if (paperProjectilePrefab == null || firePoint == null) return;

        // Cria o projétil de papel
        GameObject paper = Instantiate(paperProjectilePrefab, firePoint.position, firePoint.rotation);
        Rigidbody paperRb = paper.GetComponent<Rigidbody>();

        if (paperRb != null)
        {
            Vector3 shootDirection = movingRight ? firePoint.forward : -firePoint.forward;
            paperRb.linearVelocity = shootDirection * projectileSpeed;
        }
    }

    // Desenha o raio de visao no Editor para facilitar a configuracao
    private void OnDrawGizmosSelected()
    {
        Vector3 origin = sightOrigin != null ? sightOrigin.position : transform.position;
        Vector3 forwardDir = movingRight ? transform.forward : -transform.forward;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin, forwardDir * sightDistance);
    }
}