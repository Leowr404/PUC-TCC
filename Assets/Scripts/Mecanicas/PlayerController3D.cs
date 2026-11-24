using UnityEngine;
using UnityEngine.InputSystem;

// Controla movimentação, pulo, agachar, interação, ferramentas, dano e limite de afastamento da CoopCamera
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
    private LaserPointer currentLaserTool;
    private GameObject currentToolPrefab;
    private Transform selfTransform;
    private Rigidbody rb;
    private CapsuleCollider capsuleCollider;

    private bool isGrounded;
    private bool isAttachedToWall;
    private float horizontalInput;
    private bool isWalkingSoundPlaying;
    private bool isBlockedByCamera;

    private Toolbox nearbyToolbox;
    private StorageToolbox nearbyStorageBox;
    private ConnectedStorageBox nearbyConnectedBox;
    private bool isStunned = false;

    private float originalColliderHeight;
    private Vector3 originalColliderCenter;

    private float targetYRotation = 90f;
    private KeyCode kLeft, kRight, kJump, kInteract, kCrouch;

    private static readonly Quaternion rightFacing = Quaternion.Euler(0f, 90f, 0f);
    private static readonly Quaternion leftFacing = Quaternion.Euler(0f, -90f, 0f);

    void Start()
    {
        selfTransform = transform;
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();

        if (capsuleCollider != null)
        {
            originalColliderHeight = capsuleCollider.height;
            originalColliderCenter = capsuleCollider.center;
        }

        SetupKeyboardKeys();
        targetYRotation = selfTransform.eulerAngles.y;
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

    private bool IsLaserLockingMovement()
    {
        return currentLaserTool != null && currentLaserTool.IsLockingMovement;
    }

    void Update()
    {
        if (isStunned)
        {
            StopWalkSound();
            return;
        }

        bool laserLock = IsLaserLockingMovement();

        horizontalInput = 0f;
        if (!laserLock)
        {
            if (Input.GetKey(kLeft)) horizontalInput -= 1f;
            if (Input.GetKey(kRight)) horizontalInput += 1f;

            if (Input.GetKeyDown(kJump) && (isGrounded || isAttachedToWall))
            {
                ExecuteJump();
            }

            if (Input.GetKeyDown(kCrouch) && !isAttachedToWall)
            {
                ToggleCrouch();
            }
        }

        if (Input.GetKeyDown(kInteract))
        {
            ExecuteInteract();
        }

        if (laserLock)
        {
            horizontalInput = 0f;
            isBlockedByCamera = false;
            StopWalkSound();

            if (isCrouching)
            {
                ToggleCrouch();
            }
            return;
        }

        if (horizontalInput != 0f && !isAttachedToWall)
        {
            targetYRotation = horizontalInput > 0f ? 90f : -90f;
        }

        UpdateWalkSound();
    }

    private void UpdateWalkSound()
    {
        bool shouldWalk = isGrounded && !isAttachedToWall && !isBlockedByCamera && Mathf.Abs(horizontalInput) > 0.01f;

        if (shouldWalk && !isWalkingSoundPlaying)
        {
            AudioManager audio = AudioManager.instancia;
            if (audio != null && audio.andar != null)
            {
                audio.PlaySFX(audio.andar, true);
                isWalkingSoundPlaying = true;
            }
        }
        else if (!shouldWalk && isWalkingSoundPlaying)
        {
            StopWalkSound();
        }
    }

    private void StopWalkSound()
    {
        if (!isWalkingSoundPlaying) return;

        AudioManager.instancia?.StopLoopingSFX();
        isWalkingSoundPlaying = false;
    }

    private void ExecuteJump()
    {
        StopWalkSound();

        if (isCrouching) ToggleCrouch();

        SetAttachedToWall(false);
        isGrounded = false;

        if (rb != null)
        {
            Vector3 vel = rb.linearVelocity;
            vel.y = 0f;
            rb.linearVelocity = vel;
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    private void ExecuteInteract()
    {
        if (nearbyToolbox != null)
        {
            nearbyToolbox.Interact(this);
            return;
        }
        if (nearbyStorageBox != null)
        {
            nearbyStorageBox.Interact(this);
            return;
        }
        if (nearbyConnectedBox != null)
        {
            nearbyConnectedBox.Interact(this);
            return;
        }

        if (currentTool != null)
        {
            currentTool.OnActionDown(this);
        }
    }

    public void PlayPushSound()
    {
        AudioManager audio = AudioManager.instancia;
        if (audio != null && audio.empurrar != null)
        {
            audio.PlaySFX(audio.empurrar);
        }
    }

    public void OnJump(InputValue value)
    {
        if (!IsLaserLockingMovement() && value.isPressed && (isGrounded || isAttachedToWall))
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
        if (!IsLaserLockingMovement() && value.isPressed && !isAttachedToWall)
        {
            ToggleCrouch();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        ContactPoint[] contacts = collision.contacts;
        for (int i = 0; i < contacts.Length; i++)
        {
            if (contacts[i].normal.y > 0.3f)
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
        if (isAttachedToWall || rb == null) return;

        selfTransform.rotation = Mathf.Approximately(targetYRotation, 90f) ? rightFacing : leftFacing;

        float currentSpeed = isCrouching ? crouchSpeed : moveSpeed;
        float vx = horizontalInput * currentSpeed;
        isBlockedByCamera = false;

        CoopCamera coopCam = CoopCamera.instancia;
        if (coopCam != null)
        {
            int blockedDir = coopCam.GetBlockedHorizontalDirection(selfTransform);
            if ((blockedDir < 0 && vx < 0f) || (blockedDir > 0 && vx > 0f))
            {
                vx = 0f;
                isBlockedByCamera = true;
            }
        }

        Vector3 vel = rb.linearVelocity;
        vel.x = vx;

        if (vel.y < 0f)
        {
            vel.y += Physics.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (vel.y > 0f && !Input.GetKey(kJump))
        {
            vel.y += Physics.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
        }

        rb.linearVelocity = vel;
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
        if (rb != null)
        {
            rb.isKinematic = attach;
        }

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
        currentTool = toolObj.GetComponent<BaseTool>();
        currentLaserTool = currentTool as LaserPointer;

        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.DeductToolCost();
        }
    }

    public GameObject GetCurrentToolPrefab() => currentToolPrefab;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Toolbox box)) nearbyToolbox = box;
        if (other.TryGetComponent(out StorageToolbox storageBox)) nearbyStorageBox = storageBox;
        if (other.TryGetComponent(out ConnectedStorageBox connectedBox)) nearbyConnectedBox = connectedBox;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Toolbox box) && nearbyToolbox == box) nearbyToolbox = null;
        if (other.TryGetComponent(out StorageToolbox storageBox) && nearbyStorageBox == storageBox) nearbyStorageBox = null;
        if (other.TryGetComponent(out ConnectedStorageBox connectedBox) && nearbyConnectedBox == connectedBox) nearbyConnectedBox = null;
    }

    public void TakeDamage(Vector3 knockback, float duration)
    {
        if (isStunned) return;

        StopWalkSound();

        if (isCrouching) ToggleCrouch();

        DropCurrentTool();

        SetAttachedToWall(false);
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(knockback, ForceMode.Impulse);
        }

        AudioManager audio = AudioManager.instancia;
        if (audio != null && audio.Hit != null)
        {
            audio.PlaySFX(audio.Hit);
        }

        Feedback.TremerCamera(0.2f, 0.25f);
        Feedback.Vibrar(this, 0.7f, 0.25f);

        StartCoroutine(StunRoutine(duration));
    }

    public void TakeWindKnockback(Vector3 knockback, float duration)
    {
        if (isStunned) return;

        StopWalkSound();

        if (isCrouching) ToggleCrouch();

        SetAttachedToWall(false);
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(knockback, ForceMode.Impulse);
        }

        Feedback.Vibrar(this, 0.3f, 0.15f);

        StartCoroutine(StunRoutine(duration));
    }

    public void DropCurrentTool()
    {
        if (currentTool != null)
        {
            Destroy(currentTool.gameObject);
            currentTool = null;
            currentLaserTool = null;
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