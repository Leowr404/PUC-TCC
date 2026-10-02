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
    private bool isAttachedToWall = false;

    private static PaperClipProjectile[] activeClips = new PaperClipProjectile[4];

    void Start()
    {
        rb = (Rigidbody)GetComponent(typeof(Rigidbody));
        clipCollider = (Collider)GetComponent(typeof(Collider));

        RegisterClip(this);

        if (rb != null)
        {
            rb.linearVelocity = transform.forward * speed;
        }

        Invoke(nameof(DestroySelf), lifeTimeInAir);
    }

    void FixedUpdate()
    {
        if (isAttachedToWall) return;

        // Deteção por SphereCast no FixedUpdate (sincronizado com a física da Unity)
        float stepDistance = speed * Time.fixedDeltaTime;
        float radius = clipCollider != null ? clipCollider.bounds.extents.x : 0.2f;

        if (Physics.SphereCast(transform.position, radius, transform.forward, out RaycastHit hit, stepDistance + 0.1f))
        {
            // Verifica se bateu numa parede (por Tag ou por Layer)
            bool isWall = hit.collider.CompareTag(wallTag) || ((1 << hit.collider.gameObject.layer) & wallLayer) != 0;

            if (isWall)
            {
                transform.position = hit.point;
                AttachToWall();
                return;
            }

            // Se for inimigo
            PatrolEnemy enemy = (PatrolEnemy)hit.collider.GetComponent(typeof(PatrolEnemy));
            if (enemy != null)
            {
                enemy.Freeze(freezeDuration);
                DestroySelf();
            }
        }
    }

    private static void RegisterClip(PaperClipProjectile newClip)
    {
        if (activeClips[0] != null)
        {
            activeClips[0].DestroySelf();
        }

        for (int i = 0; i < activeClips.Length - 1; i++)
        {
            activeClips[i] = activeClips[i + 1];
        }

        activeClips[activeClips.Length - 1] = newClip;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isAttachedToWall || other.CompareTag("Player")) return;

        PatrolEnemy enemy = (PatrolEnemy)other.GetComponent(typeof(PatrolEnemy));
        if (enemy != null)
        {
            enemy.Freeze(freezeDuration);
            DestroySelf();
            return;
        }

        bool isWall = other.CompareTag(wallTag) || ((1 << other.gameObject.layer) & wallLayer) != 0;
        if (isWall)
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
        if (isAttachedToWall) return;

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

        Invoke(nameof(DestroySelf), platformDuration);
    }

    public void DestroySelf()
    {
        RemoveFromRegister(this);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
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