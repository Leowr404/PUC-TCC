using UnityEngine;

// Controla a caixa alternando entre peso elevado e estado carregado
public class MovableBox : MonoBehaviour
{
    [Header("Ajuste de Distancia e Massa")]
    public float holdOffset = 1.2f;        // Distancia para manter a caixa a frente do jogador
    public float defaultMass = 100f;       // Massa elevada quando esta solta no chao

    private Rigidbody rb;
    private Collider boxCollider;
    private Transform selfTransform;
    private Transform attachedTransform;
    private bool isAttached = false;
    private float fixedZPosition;

    private const RigidbodyConstraints FrozenConstraints =
        RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;

    void Start()
    {
        selfTransform = transform;
        rb = GetComponent<Rigidbody>();
        boxCollider = GetComponent<Collider>();

        fixedZPosition = selfTransform.position.z;

        if (rb != null)
        {
            rb.mass = defaultMass;
            rb.isKinematic = false;
            rb.constraints = FrozenConstraints;
        }
    }

    void LateUpdate()
    {
        if (isAttached && attachedTransform != null)
        {
            Vector3 targetPos = attachedTransform.position + (attachedTransform.forward * holdOffset);
            targetPos.z = fixedZPosition;
            selfTransform.position = targetPos;
        }
    }

    // Chamado pelo desentupidor para agarrar a caixa
    public void AttachToTool(Transform holdPoint)
    {
        bool wasAttached = isAttached;

        isAttached = true;
        attachedTransform = holdPoint;

        if (!wasAttached)
        {
            AudioManager audio = AudioManager.instancia;
            if (audio != null && audio.empurrar != null)
            {
                audio.PlaySFX(audio.empurrar);
            }
        }

        if (rb != null)
        {
            rb.isKinematic = true;
        }

        if (boxCollider != null)
        {
            boxCollider.isTrigger = true;
        }
    }

    // Chamado pelo desentupidor para soltar a caixa
    public void DetachFromTool()
    {
        isAttached = false;
        attachedTransform = null;

        if (boxCollider != null)
        {
            boxCollider.isTrigger = false;
        }

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.mass = defaultMass;
            rb.constraints = FrozenConstraints;
        }
    }
}