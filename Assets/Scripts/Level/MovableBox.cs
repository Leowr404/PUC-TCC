using UnityEngine;

// Controla a caixa alternando entre peso elevado e estado carregado
public class MovableBox : MonoBehaviour
{
    [Header("Ajuste de Distancia e Massa")]
    public float holdOffset = 1.2f;        // Distancia para manter a caixa a frente do jogador
    public float defaultMass = 100f;       // Massa elevada quando esta solta no chao

    private Rigidbody rb;
    private Collider boxCollider;
    private Transform attachedTransform;
    private bool isAttached = false;
    private float fixedZPosition;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        boxCollider = GetComponent<Collider>();

        // Salva a posicao Z inicial para travar a caixa no plano 2.5D
        fixedZPosition = transform.position.z;

        if (rb != null)
        {
            rb.mass = defaultMass;
            rb.isKinematic = false;
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        }
    }

    void LateUpdate()
    {
        // Acompanha a posicao da ferramenta do jogador durante o transporte
        if (isAttached && attachedTransform != null)
        {
            Vector3 targetPos = attachedTransform.position + (attachedTransform.forward * holdOffset);
            targetPos.z = fixedZPosition;
            transform.position = targetPos;
        }
    }

    // Chamado pelo desentupidor para agarrar a caixa
    public void AttachToTool(Transform holdPoint)
    {
        isAttached = true;
        attachedTransform = holdPoint;

        if (rb != null)
        {
            rb.isKinematic = true; // Trava forcas fisicas para o jogador carregar sem peso
        }

        // Virar Trigger impede que a caixa empurre o jogador enquanto e carregada
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

        // Restaura a colidao solida da caixa
        if (boxCollider != null)
        {
            boxCollider.isTrigger = false;
        }

        if (rb != null)
        {
            rb.isKinematic = false; // Ativa a fisica normal
            rb.mass = defaultMass;  // Restaura a massa pesada
            rb.constraints = RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        }
    }
}