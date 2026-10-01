using UnityEngine;

public class PlayerController3D : MonoBehaviour
{
    [Header("Configuracoes de Movimento")]
    public float moveSpeed = 8f;
    public float crouchSpeed = 4f;        // Velocidade reduzida ao agachar
    public float jumpForce = 10f;

    [Header("Configuracoes de Agachar")]
    [Range(0.2f, 0.9f)]
    public float crouchHeightRatio = 0.5f; // Proporcao da altura do Collider ao agachar (50%)
    public bool isCrouching = false;

    [Header("Ajuste de Gravidade")]
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;

    [Header("Mapeamento de Teclas")]
    public KeyCode leftKey;
    public KeyCode rightKey;
    public KeyCode jumpKey;
    public KeyCode actionKey;
    public KeyCode crouchKey;             // Tecla para agachar (Ctrl / 0)

    [Header("Detectador de Chao")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;

    [Header("Ponto de Encaixe da Ferramenta")]
    public Transform toolHoldPoint; // Objeto filho (mao do jogador)

    private BaseTool currentTool; // Ferramenta equipada no momento
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private bool isGrounded;
    private bool isAttachedToWall;
    private float horizontalInput;
    private Toolbox nearbyToolbox;
    private bool isStunned = false;

    // Variaveis para restaurar as dimensoes originais do Collider
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;

    void Start()
    {
        rb = (Rigidbody)GetComponent(typeof(Rigidbody));
        capsuleCollider = (CapsuleCollider)GetComponent(typeof(CapsuleCollider));

        if (capsuleCollider != null)
        {
            originalColliderHeight = capsuleCollider.height;
            originalColliderCenter = capsuleCollider.center;
        }
    }

    void Update()
    {
        if (isStunned) return;

        // 1. Prioridade para interagir/trocar ferramenta na Caixa de Ferramentas
        if (Input.GetKeyDown(actionKey) && nearbyToolbox != null)
        {
            nearbyToolbox.Interact(this);
            return;
        }

        // 2. Comunicacao com a ferramenta equipada
        if (currentTool != null)
        {
            if (Input.GetKeyDown(actionKey)) currentTool.OnActionDown(this);
            if (Input.GetKey(actionKey)) currentTool.OnActionHold(this);
            if (Input.GetKeyUp(actionKey)) currentTool.OnActionUp(this);
        }

        // Verifica se a ferramenta atual e o LaserPointer e se esta mirando
        bool isAimingWithLaser = false;
        LaserPointer laserTool = currentTool as LaserPointer;
        if (laserTool != null && laserTool.IsLockingMovement)
        {
            isAimingWithLaser = true;
        }

        // Se estiver mirando com o laser, bloqueia movimentacao, agachamento e pulo
        if (isAimingWithLaser)
        {
            horizontalInput = 0f;
            return;
        }

        // 3. TOGGLE DE AGACHAR
        if (Input.GetKeyDown(crouchKey) && !isAttachedToWall)
        {
            ToggleCrouch();
        }

        // Processa entrada de movimento normal
        horizontalInput = 0f;
        if (Input.GetKey(leftKey)) horizontalInput -= 1f;
        if (Input.GetKey(rightKey)) horizontalInput += 1f;

        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        }

        // Pulo (se pular agachado, levanta automaticamente)
        if (Input.GetKeyDown(jumpKey) && (isGrounded || isAttachedToWall))
        {
            if (isCrouching) ToggleCrouch();

            SetAttachedToWall(false);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        if (horizontalInput != 0 && !isAttachedToWall)
        {
            transform.rotation = Quaternion.Euler(0, horizontalInput > 0 ? 90f : -90f, 0);
        }
    }

    void FixedUpdate()
    {
        if (isAttachedToWall) return;

        // Define a velocidade atual com base no estado de agachamento
        float currentSpeed = isCrouching ? crouchSpeed : moveSpeed;
        rb.linearVelocity = new Vector3(horizontalInput * currentSpeed, rb.linearVelocity.y, rb.linearVelocity.z);

        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !Input.GetKey(jumpKey))
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    public void ToggleCrouch()
    {
        isCrouching = !isCrouching;

        if (capsuleCollider == null) return;

        if (isCrouching)
        {
            // Reduz a altura do Collider e reajusta o centro para nao afundar no chao
            capsuleCollider.height = originalColliderHeight * crouchHeightRatio;
            capsuleCollider.center = new Vector3(
                originalColliderCenter.x,
                originalColliderCenter.y * crouchHeightRatio,
                originalColliderCenter.z
            );
        }
        else
        {
            // Restaura o tamanho e centro originais
            capsuleCollider.height = originalColliderHeight;
            capsuleCollider.center = originalColliderCenter;
        }
    }

    public void SetAttachedToWall(bool attach)
    {
        isAttachedToWall = attach;
        rb.isKinematic = attach;

        if (attach && isCrouching)
        {
            ToggleCrouch(); // Se grudar na parede, força levantar
        }
    }

    public void EquipToolPrefab(GameObject newToolPrefab)
    {
        if (currentTool != null)
        {
            Destroy(currentTool.gameObject);
        }

        GameObject toolObj = Instantiate(newToolPrefab, toolHoldPoint.position, toolHoldPoint.rotation, toolHoldPoint);
        currentTool = (BaseTool)toolObj.GetComponent(typeof(BaseTool));
    }

    private void OnTriggerEnter(Collider other)
    {
        Toolbox box = (Toolbox)other.GetComponent(typeof(Toolbox));
        if (box != null) nearbyToolbox = box;
    }

    private void OnTriggerExit(Collider other)
    {
        Toolbox box = (Toolbox)other.GetComponent(typeof(Toolbox));
        if (box != null && nearbyToolbox == box) nearbyToolbox = null;
    }

    public void TakeDamage(Vector3 knockback, float duration)
    {
        if (isStunned) return;

        if (isCrouching) ToggleCrouch();

        DropCurrentTool();

        SetAttachedToWall(false);
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(knockback, ForceMode.Impulse);

        StartCoroutine(StunRoutine(duration));
    }

    public void DropCurrentTool()
    {
        if (currentTool != null)
        {
            Destroy(currentTool.gameObject);
            currentTool = null;
        }
    }

    private System.Collections.IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }
}