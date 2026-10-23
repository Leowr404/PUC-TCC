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
    private ConnectedStorageBox nearbyConnectedBox;
    private bool isStunned = false;

    private float originalColliderHeight;
    private Vector3 originalColliderCenter;

    // Teclas do Teclado
    private KeyCode kLeft, kRight, kJump, kInteract, kCrouch;

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
            kLeft = KeyCode.A;
            kRight = KeyCode.D;
            kJump = KeyCode.W;
            kInteract = KeyCode.E;
            kCrouch = KeyCode.S;
        }
        else
        {
            kLeft = KeyCode.LeftArrow;
            kRight = KeyCode.RightArrow;
            kJump = KeyCode.UpArrow;
            kInteract = KeyCode.RightShift;
            kCrouch = KeyCode.DownArrow;
        }
    }

    void Update()
    {
        if (isStunned) return;

        ProcessKeyboardInput();

        // Verifica se a caneta laser está ativa e travando o movimento/ações
        LaserPointer laserTool = currentTool as LaserPointer;
        bool isLaserActive = laserTool != null && laserTool.IsLockingMovement;

        if (isLaserActive)
        {
            horizontalInput = 0f;
            // Se o player estiver agachado e ativar o laser, ele levanta automaticamente
            if (isCrouching)
            {
                ToggleCrouch();
            }
            return;
        }

        if (horizontalInput != 0 && !isAttachedToWall)
        {
            transform.rotation = Quaternion.Euler(0, horizontalInput > 0 ? 90f : -90f, 0);
        }
    }

    private void ProcessKeyboardInput()
    {
        horizontalInput = 0f;
        if (Input.GetKey(kLeft)) horizontalInput -= 1f;
        if (Input.GetKey(kRight)) horizontalInput += 1f;

        LaserPointer laserTool = currentTool as LaserPointer;
        bool isLaserActive = laserTool != null && laserTool.IsLockingMovement;

        // Pulo bloqueado se o laser estiver ativo
        if (!isLaserActive && Input.GetKeyDown(kJump) && (isGrounded || isAttachedToWall))
        {
            ExecuteJump();
        }

        // Agachar bloqueado se o laser estiver ativo
        if (!isLaserActive && Input.GetKeyDown(kCrouch) && !isAttachedToWall)
        {
            ToggleCrouch();
        }

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
        else if (nearbyConnectedBox != null)
        {
            nearbyConnectedBox.Interact(this);
            return;
        }

        if (currentTool != null)
        {
            currentTool.OnActionDown(this);
        }
    }

    public void OnMove(InputValue value)
    {
        Vector2 move = value.Get<Vector2>();
        if (Mathf.Abs(move.x) > 0.1f) horizontalInput = move.x;
    }

    public void OnJump(InputValue value)
    {
        LaserPointer laserTool = currentTool as LaserPointer;
        bool isLaserActive = laserTool != null && laserTool.IsLockingMovement;

        if (!isLaserActive && value.isPressed && (isGrounded || isAttachedToWall))
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
        LaserPointer laserTool = currentTool as LaserPointer;
        bool isLaserActive = laserTool != null && laserTool.IsLockingMovement;

        // Gamepad Sprint/Crouch bloqueado se o laser estiver ativo
        if (!isLaserActive && value.isPressed && !isAttachedToWall)
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
        Toolbox box = other.GetComponent<Toolbox>();
        if (box != null) nearbyToolbox = box;

        StorageToolbox storageBox = other.GetComponent<StorageToolbox>();
        if (storageBox != null) nearbyStorageBox = storageBox;

        ConnectedStorageBox connectedBox = other.GetComponent<ConnectedStorageBox>();
        if (connectedBox != null) nearbyConnectedBox = connectedBox;
    }

    private void OnTriggerExit(Collider other)
    {
        Toolbox box = other.GetComponent<Toolbox>();
        if (box != null && nearbyToolbox == box) nearbyToolbox = null;

        StorageToolbox storageBox = other.GetComponent<StorageToolbox>();
        if (storageBox != null && nearbyStorageBox == storageBox) nearbyStorageBox = null;

        ConnectedStorageBox connectedBox = other.GetComponent<ConnectedStorageBox>();
        if (connectedBox != null && nearbyConnectedBox == connectedBox) nearbyConnectedBox = null;
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