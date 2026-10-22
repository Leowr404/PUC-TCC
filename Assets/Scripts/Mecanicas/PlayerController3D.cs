using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController3D : MonoBehaviour
{
    public enum PlayerIndex { Player1, Player2 }

    [Header("Identificacao do Jogador")]
    public PlayerIndex playerIndex = PlayerIndex.Player1;

    [Header("Configuracoes de Movimento")]
    public float moveSpeed = 8f;
    public float crouchSpeed = 4f;
    public float jumpForce = 10f;

    [Header("Configuracoes de Agachar")]
    [Range(0.2f, 0.9f)]
    public float crouchHeightRatio = 0.5f;
    public bool isCrouching = false;

    [Header("Ajuste de Gravidade")]
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;

    [Header("Ponto de Encaixe da Ferramenta")]
    public Transform toolHoldPoint;

    private BaseTool currentTool;
    private GameObject currentToolPrefab;
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;
    private bool isGrounded;
    private bool isAttachedToWall;
    private float horizontalInput;

    private Toolbox nearbyToolbox;
    private StorageToolbox nearbyStorageBox;
    private bool isStunned = false;

    private float originalColliderHeight;
    private Vector3 originalColliderCenter;

    // Teclas do Teclado
    private KeyCode kLeft, kRight, kJump, kInteract, kCrouch, kCrouchAlt;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        if (capsuleCollider != null)
        {
            originalColliderHeight = capsuleCollider.height;
            originalColliderCenter = capsuleCollider.center;
        }

        SetupKeyboardKeys();
    }

    void SetupKeyboardKeys()
    {
        if (playerIndex == PlayerIndex.Player1)
        {
            // Player 1: A/D (Mover), W (Pulo), S (Interagir), C (Agachar)
            kLeft = KeyCode.A;
            kRight = KeyCode.D;
            kJump = KeyCode.W;
            kInteract = KeyCode.S;
            kCrouch = KeyCode.C;
            kCrouchAlt = KeyCode.C;
        }
        else
        {
            // Player 2: Setas Esq/Direita (Mover), Seta Cima (Pulo), Seta Baixo (Interagir), 0 (Agachar)
            kLeft = KeyCode.LeftArrow;
            kRight = KeyCode.RightArrow;
            kJump = KeyCode.UpArrow;
            kInteract = KeyCode.DownArrow;
            kCrouch = KeyCode.Alpha0;
            kCrouchAlt = KeyCode.Keypad0; // Suporta tanto o 0 do teclado numérico quanto o superior
        }
    }

    void Update()
    {
        if (isStunned) return;

        // 1. Processa teclado local configurado por P1 / P2
        ProcessKeyboardInput();

        // 2. Trava de movimento se estiver mirando com o Laser
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

        // 3. Rotação do Personagem
        if (horizontalInput != 0 && !isAttachedToWall)
        {
            transform.rotation = Quaternion.Euler(0, horizontalInput > 0 ? 90f : -90f, 0);
        }
    }

    private void ProcessKeyboardInput()
    {
        // Movimento Horizontal
        horizontalInput = 0f;
        if (Input.GetKey(kLeft)) horizontalInput -= 1f;
        if (Input.GetKey(kRight)) horizontalInput += 1f;

        // Pulo
        if (Input.GetKeyDown(kJump) && (isGrounded || isAttachedToWall))
        {
            ExecuteJump();
        }

        // Agachar (Suporta a tecla principal e alternativa do 0)
        if ((Input.GetKeyDown(kCrouch) || Input.GetKeyDown(kCrouchAlt)) && !isAttachedToWall)
        {
            ToggleCrouch();
        }

        // Interação e Uso de Ferramentas
        if (Input.GetKeyDown(kInteract))
        {
            ExecuteInteract();
        }
    }

    private void ExecuteJump()
    {
        if (isCrouching) ToggleCrouch();

        SetAttachedToWall(false);
        isGrounded = false;

        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    private void ExecuteInteract()
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

        if (currentTool != null)
        {
            currentTool.OnActionDown(this);
        }
    }

    // --- MÉTODOS DE EVENTO (Para uso com Gamepads) ---
    public void OnMove(InputValue value)
    {
        Vector2 move = value.Get<Vector2>();
        if (Mathf.Abs(move.x) > 0.1f) horizontalInput = move.x;
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed && (isGrounded || isAttachedToWall))
        {
            ExecuteJump();
        }
    }

    public void OnInteract(InputValue value)
    {
        if (value.isPressed)
        {
            ExecuteInteract();
        }
    }

    public void OnSprint(InputValue value)
    {
        if (value.isPressed && !isAttachedToWall)
        {
            ToggleCrouch();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        foreach (ContactPoint contact in collision.contacts)
        {
            if (contact.normal.y > 0.3f)
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
        else if (rb.linearVelocity.y > 0 && !Input.GetKey(kJump))
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

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.DeductToolCost();
        }
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