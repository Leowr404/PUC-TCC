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
    private Transform fireTransform;

    private KeyCode keyLeft, keyRight, keyUp, keyDown;

    private static readonly RaycastHit[] raycastHitBuffer = new RaycastHit[1];
    private static readonly Collider[] enemyOverlapBuffer = new Collider[12];

    void Awake()
    {
        fireTransform = firePoint != null ? firePoint : transform;
    }

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

    private void CachePlayerKeys(PlayerController3D player)
    {
        if (player != null && player.playerIndex == PlayerController3D.PlayerIndex.Player2)
        {
            keyLeft = KeyCode.LeftArrow;
            keyRight = KeyCode.RightArrow;
            keyUp = KeyCode.UpArrow;
            keyDown = KeyCode.DownArrow;
        }
        else
        {
            keyLeft = KeyCode.A;
            keyRight = KeyCode.D;
            keyUp = KeyCode.W;
            keyDown = KeyCode.S;
        }
    }

    public override void OnActionDown(PlayerController3D player)
    {
        ownerPlayer = player;
        CachePlayerKeys(player);

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

        // 1. Cancela o modo de mira se o jogador pressionar para andar para os lados
        if (Input.GetKey(keyLeft) || Input.GetKey(keyRight))
        {
            ExitAim();
            return;
        }

        // 2. Ajusta angulo do laser usando as teclas de cima/baixo do respectivo jogador
        float deltaAngle = rotationSpeed * Time.deltaTime;
        if (Input.GetKey(keyUp))
        {
            currentLaserAngle += deltaAngle;
        }
        if (Input.GetKey(keyDown))
        {
            currentLaserAngle -= deltaAngle;
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
        Transform origin = firePoint != null ? firePoint : fireTransform;
        if (origin == null || lineRenderer == null) return;

        Vector3 aimDirection = Quaternion.AngleAxis(-currentLaserAngle, origin.right) * origin.forward;

        Vector3 startPos = origin.position;
        Vector3 endPos = startPos + aimDirection * maxDistance;

        int hitCount = Physics.RaycastNonAlloc(startPos, aimDirection, raycastHitBuffer, maxDistance, hitLayers, QueryTriggerInteraction.Collide);
        if (hitCount > 0)
        {
            RaycastHit hit = raycastHitBuffer[0];
            endPos = hit.point;

            Collider hitCol = hit.collider;
            if (hitCol != null)
            {
                if (hitCol.TryGetComponent(out LaserButton laserBtn))
                {
                    laserBtn.ActivateByLaser();
                }

                if (hitCol.TryGetComponent(out ColorButtonController colorBtn))
                {
                    colorBtn.ActivateByLaser();
                }

                if (hitCol.TryGetComponent(out PrinterWeakPoint weakPoint))
                {
                    weakPoint.HitByLaser();
                }
            }
        }

        lineRenderer.SetPosition(0, startPos);
        lineRenderer.SetPosition(1, endPos);

        // --- DETECÇÃO DE INIMIGOS NA PONTA DO LASER ---
        int enemyCount = Physics.OverlapSphereNonAlloc(endPos, attractionRadius, enemyOverlapBuffer, enemyLayer, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < enemyCount; i++)
        {
            Collider col = enemyOverlapBuffer[i];
            if (col != null && col.TryGetComponent(out PatrolEnemy enemy))
            {
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
        Transform origin = firePoint != null ? firePoint : transform;
        if (origin != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(origin.position, attractionRadius);
        }
    }
}