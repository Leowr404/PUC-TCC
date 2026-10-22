using UnityEngine;

public class LaserPointer : BaseTool
{
    [Header("Configuracoes do Laser")]
    public Transform firePoint;             // Ponta da caneta laser
    public LineRenderer lineRenderer;       // Componente LineRenderer
    public float maxDistance = 30f;         // Alcance maximo do raio
    public float rotationSpeed = 60f;       // Velocidade de ajuste do angulo
    public float minAngle = -45f;           // Angulo maximo para baixo
    public float maxAngle = 45f;            // Angulo maximo para cima
    public float laserWidth = 0.05f;        // Largura do laser
    public LayerMask hitLayers = ~0;        // Camadas de colisao do raio

    [Header("Atracao de Inimigos")]
    public float attractionRadius = 4f;     // Raio de atracao AO REDOR DA PONTA do laser
    public LayerMask enemyLayer = ~0;       // Layer onde os inimigos estao registrados

    [Header("Estado Atual")]
    public bool isAiming = false;
    private float currentLaserAngle = 0f;
    private PlayerController3D ownerPlayer;

    void Start()
    {
        if (lineRenderer != null)
        {
            lineRenderer.startWidth = laserWidth;
            lineRenderer.endWidth = laserWidth;
            lineRenderer.enabled = false;
        }
    }

    public bool IsLockingMovement => isAiming;

    public override void OnActionDown(PlayerController3D player)
    {
        ownerPlayer = player;

        if (!isAiming)
        {
            isAiming = true;
            currentLaserAngle = 0f;
            if (lineRenderer != null) lineRenderer.enabled = true;
        }
    }

    void Update()
    {
        if (!isAiming || ownerPlayer == null) return;

        // Cancela o modo de mira se o jogador mover para os lados
        if (Input.GetKeyDown(ownerPlayer.leftKey) || Input.GetKeyDown(ownerPlayer.rightKey) ||
            Input.GetKey(ownerPlayer.leftKey) || Input.GetKey(ownerPlayer.rightKey))
        {
            ExitAim();
            return;
        }

        // Ajusta angulo
        if (Input.GetKey(ownerPlayer.jumpKey))
        {
            currentLaserAngle += rotationSpeed * Time.deltaTime;
        }

        if (Input.GetKey(ownerPlayer.actionKey))
        {
            currentLaserAngle -= rotationSpeed * Time.deltaTime;
        }

        currentLaserAngle = Mathf.Clamp(currentLaserAngle, minAngle, maxAngle);

        DrawLaser();
    }

    public override void OnActionHold(PlayerController3D player) { }
    public override void OnActionUp(PlayerController3D player) { }

    public void ExitAim()
    {
        isAiming = false;
        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    void DrawLaser()
    {
        if (firePoint == null || lineRenderer == null) return;

        Vector3 aimDirection = Quaternion.AngleAxis(-currentLaserAngle, firePoint.right) * firePoint.forward;

        RaycastHit hit;
        Vector3 startPos = firePoint.position;
        Vector3 endPos = startPos + aimDirection * maxDistance;

        if (Physics.Raycast(startPos, aimDirection, out hit, maxDistance, hitLayers))
        {
            endPos = hit.point; // PONTA DO LASER onde houve o impacto

            // --- ATIVAÇÃO DO BOTAO DE LASER ---
            LaserButton laserBtn = hit.collider.GetComponent<LaserButton>();
            if (laserBtn != null)
            {
                laserBtn.ActivateByLaser();
            }
        }

        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);

        // --- DETECÇÃO DE INIMIGOS NA PONTA DO LASER ---
        Collider[] hitEnemies = Physics.OverlapSphere(endPos, attractionRadius, enemyLayer);
        foreach (Collider col in hitEnemies)
        {
            PatrolEnemy enemy = (PatrolEnemy)col.GetComponent(typeof(PatrolEnemy));
            if (enemy != null)
            {
                // Envia a coordenada exata da ponta do laser para o inimigo
                enemy.SetLaserTarget(endPos);
            }
        }
    }

    void OnDisable()
    {
        ExitAim();
    }

    private void OnDrawGizmosSelected()
    {
        if (firePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(firePoint.position, attractionRadius);
        }
    }
}