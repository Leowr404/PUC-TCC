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

    [Header("Ponto de Encaixe da Ferramenta")]
    public Transform toolHoldPoint; // Objeto filho (mao do jogador)

    private BaseTool currentTool; // Ferramenta equipada no momento
    private GameObject currentToolPrefab; // Guarda o Prefab da ferramenta atual
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private bool isGrounded;
    private bool isAttachedToWall;
    private float horizontalInput;

    private Toolbox nearbyToolbox;
    private StorageToolbox nearbyStorageBox; // Referencia da caixa de deposito/troca
    private bool isStunned = false;

    // Variaveis para restaurar as dimensoes originais do Collider
    private float originalColliderHeight;
    private Vector3 originalColliderCenter;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        if (capsuleCollider != null)
        {
            originalColliderHeight = capsuleCollider.height;
            originalColliderCenter = capsuleCollider.center;
        }
    }

    void Update()
    {
        if (isStunned) return;

        // 1. Interacao com as Caixas
        if (Input.GetKeyDown(actionKey))
        {
            if (nearbyToolbox != null)
            {
                nearbyToolbox.Interact(this);
                return;
            }
            else if (nearbyStorageBox != null)
            {
                nearbyStorageBox.Interact(this);
                return;
            }
        }

        // 2. Comunicacao com a ferramenta equipada
        if (currentTool != null)
        {
            if (Input.GetKeyDown(actionKey)) currentTool.OnActionDown(this);
            if (Input.GetKey(actionKey)) currentTool.OnActionHold(this);
            if (Input.GetKeyUp(actionKey)) currentTool.OnActionUp(this);
        }

        bool isAimingWithLaser = false;
        LaserPointer laserTool = currentTool as LaserPointer;
        if (laserTool != null && laserTool.IsLockingMovement)
        {
            isAimingWithLaser = true;
        }

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

        // 4. PULO SIMPLIFICADO (Qualquer superfície detectada pelo Rigidbody)
        if (Input.GetKeyDown(jumpKey) && (isGrounded || isAttachedToWall))
        {
            if (isCrouching)
            {
                ToggleCrouch(); // Desativa o agachamento para restaurar o tamanho normal no salto
            }

            SetAttachedToWall(false);
            isGrounded = false; // Consome o pulo imediatamente

            // Reseta velocidade vertical para garantir o impulso limpo
            Vector3 velocity = rb.linearVelocity;
            velocity.y = 0f;
            rb.linearVelocity = velocity;

            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        if (horizontalInput != 0 && !isAttachedToWall)
        {
            transform.rotation = Quaternion.Euler(0, horizontalInput > 0 ? 90f : -90f, 0);
        }
    }

    // DETECÇÃO DE CHÃO POR COLISÃO DIRETA (Qualquer Superfície)
    private void OnCollisionStay(Collision collision)
    {
        // Verifica se pelo menos um dos pontos de contato é uma superfície abaixo do jogador
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.3f) // Confirma que é uma superfície plana/rampa e não uma parede reta
            {
                isGrounded = true;
                return;
            }
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        isGrounded = false;
    }

    void FixedUpdate()
    {
        if (isAttachedToWall) return;

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
            capsuleCollider.height = originalColliderHeight * crouchHeightRatio;
        }
        else
        {
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
            ToggleCrouch();
        }
    }

    public void EquipToolPrefab(GameObject newToolPrefab)
    {
        if (currentTool != null)
        {
            Destroy(currentTool.gameObject);
        }

        currentToolPrefab = newToolPrefab;
        GameObject toolObj = Instantiate(newToolPrefab, toolHoldPoint.position, toolHoldPoint.rotation, toolHoldPoint);
        currentTool = (BaseTool)toolObj.GetComponent(typeof(BaseTool));
    }

    public GameObject GetCurrentToolPrefab()
    {
        return currentToolPrefab;
    }

    private void OnTriggerEnter(Collider other)
    {
        Toolbox box = (Toolbox)other.GetComponent(typeof(Toolbox));
        if (box != null) nearbyToolbox = box;

        StorageToolbox storageBox = (StorageToolbox)other.GetComponent(typeof(StorageToolbox));
        if (storageBox != null) nearbyStorageBox = storageBox;
    }

    private void OnTriggerExit(Collider other)
    {
        Toolbox box = (Toolbox)other.GetComponent(typeof(Toolbox));
        if (box != null && nearbyToolbox == box) nearbyToolbox = null;

        StorageToolbox storageBox = (StorageToolbox)other.GetComponent(typeof(StorageToolbox));
        if (storageBox != null && nearbyStorageBox == storageBox) nearbyStorageBox = null;
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
            currentToolPrefab = null;
        }
    }

    private System.Collections.IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }
}