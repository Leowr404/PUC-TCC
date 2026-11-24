using UnityEngine;

public class PaperClipProjectile : MonoBehaviour
{
    [Header("Configuracoes do Projetil")]
    public float speed = 20f;
    public float lifeTimeInAir = 3f;
    public float platformDuration = 5f;
    public float freezeDuration = 3f;

    [Header("Configuracoes de Plataforma")]
    public string wallTag = "Wall";
    public LayerMask wallLayer;

    private Rigidbody rb;
    private Collider clipCollider;
    private Transform selfTransform;
    private bool isAttachedToWall = false;
    private bool isDestroying = false;

    private static readonly PaperClipProjectile[] activeClips = new PaperClipProjectile[4];
    private static readonly RaycastHit[] sphereCastBuffer = new RaycastHit[1];

    void Awake()
    {
        selfTransform = transform;
        rb = GetComponent<Rigidbody>();
        clipCollider = GetComponent<Collider>();
    }

    void Start()
    {
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.linearVelocity = selfTransform.forward * speed;
        }

        RegisterClip(this);

        if (lifeTimeInAir > 0f)
        {
            Invoke(nameof(DestroySelf), lifeTimeInAir);
        }
    }

    void FixedUpdate()
    {
        if (isAttachedToWall || isDestroying) return;

        float stepDistance = speed * Time.fixedDeltaTime;
        float radius = clipCollider != null ? clipCollider.bounds.extents.x : 0.2f;
        Vector3 pos = selfTransform.position;
        Vector3 forward = selfTransform.forward;

        int hitCount = Physics.SphereCastNonAlloc(
            pos,
            radius,
            forward,
            sphereCastBuffer,
            stepDistance + 0.3f,
            ~0,
            QueryTriggerInteraction.Collide
        );

        if (hitCount > 0)
        {
            RaycastHit hit = sphereCastBuffer[0];
            Collider hitCol = hit.collider;
            if (hitCol == null) return;

            if (hitCol.TryGetComponent(out PrinterBoss printer))
            {
                printer.OnHitByPaperClip(null);
                DestroySelf();
                return;
            }

            if (hitCol.TryGetComponent(out ColorPuzzleTrigger puzzleTrigger))
            {
                puzzleTrigger.ActivateByLaser();
                DestroySelf();
                return;
            }

            if (IsWallCollider(hitCol))
            {
                selfTransform.position = hit.point;
                AttachToWall();
                return;
            }

            if (hitCol.TryGetComponent(out PatrolEnemy enemy))
            {
                enemy.Freeze(freezeDuration);
                DestroySelf();
            }
        }
    }

    private bool IsWallCollider(Collider col)
    {
        if (col == null) return false;
        if (!string.IsNullOrEmpty(wallTag) && col.CompareTag(wallTag)) return true;
        return (wallLayer.value & (1 << col.gameObject.layer)) != 0;
    }

    private static void RegisterClip(PaperClipProjectile newClip)
    {
        if (newClip == null) return;

        PaperClipProjectile oldest = activeClips[0];
        if (oldest != null && oldest != newClip)
        {
            oldest.DestroySelf();
        }

        for (int i = 0; i < activeClips.Length - 1; i++)
        {
            activeClips[i] = activeClips[i + 1];
        }

        activeClips[activeClips.Length - 1] = newClip;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isAttachedToWall || isDestroying || other == null || other.CompareTag("Player")) return;

        if (other.TryGetComponent(out PrinterBoss printer))
        {
            printer.OnHitByPaperClip(null);
            DestroySelf();
            return;
        }

        if (other.TryGetComponent(out ColorPuzzleTrigger puzzleTrigger))
        {
            puzzleTrigger.ActivateByLaser();
            DestroySelf();
            return;
        }

        if (other.TryGetComponent(out PatrolEnemy enemy))
        {
            enemy.Freeze(freezeDuration);
            DestroySelf();
            return;
        }

        if (IsWallCollider(other))
        {
            AttachToWall();
            return;
        }

        if (!other.CompareTag("Ground"))
        {
            DestroySelf();
        }
    }

    void AttachToWall()
    {
        if (isAttachedToWall || isDestroying) return;

        isAttachedToWall = true;
        CancelInvoke(nameof(DestroySelf));

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (clipCollider != null)
        {
            clipCollider.isTrigger = false;
        }

        if (platformDuration > 0f)
        {
            Invoke(nameof(DestroySelf), platformDuration);
        }
    }

    public void DestroySelf()
    {
        if (isDestroying) return;
        isDestroying = true;

        CancelInvoke(nameof(DestroySelf));
        RemoveFromRegister(this);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        CancelInvoke(nameof(DestroySelf));
        RemoveFromRegister(this);
    }

    private static void RemoveFromRegister(PaperClipProjectile clip)
    {
        for (int i = 0; i < activeClips.Length; i++)
        {
            if (activeClips[i] == clip)
            {
                activeClips[i] = null;
            }
        }
    }
}