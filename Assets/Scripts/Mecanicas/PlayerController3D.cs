using UnityEngine;
using UnityEngine.InputSystem;

// Controla movimentação, pulo, agachar, interação, ferramentas, dano e limite de afastamento da CoopCamera
public class PlayerController3D : MonoBehaviour
{
    public enum PlayerIndex { Player1, Player2 }

    [Header("Identificacao do Jogador")]
    public PlayerIndex playerIndex = PlayerIndex.Player1; // Define controles e identidade do jogador

    [Header("Configuracoes de Movimento")]
    public float moveSpeed = 8f;   // Velocidade horizontal padrão
    public float crouchSpeed = 4f; // Velocidade horizontal enquanto agachado
    public float jumpForce = 10f;  // Impulso vertical aplicado ao pular

    [Header("Configuracoes de Agachar")]
    [Range(0.2f, 0.9f)]
    public float crouchHeightRatio = 0.5f; // Proporção da altura do CapsuleCollider ao agachar
    public bool isCrouching = false;       // Estado atual de agachamento

    [Header("Ajuste de Gravidade")]
    public float fallMultiplier = 2.5f; // Multiplicador de gravidade na queda para deixar o salto mais firme
    public float lowJumpMultiplier = 2f;  // Multiplicador ao soltar o botão de pulo cedo

    [Header("Ponto de Encaixe da Ferramenta")]
    public Transform toolHoldPoint; // Local onde a ferramenta equipada é instanciada

    private BaseTool currentTool;
    private GameObject currentToolPrefab;
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
        targetYRotation = transform.eulerAngles.y;
    }

    // Configura as teclas padrão de teclado para Player 1 ou Player 2
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
        if (isStunned)
        {
            StopWalkSound();
            return;
        }

        ProcessKeyboardInput();

        LaserPointer laserTool = currentTool as LaserPointer;
        bool isLaserActive = laserTool != null && laserTool.IsLockingMovement;

        // Trava deslocamento horizontal e agachamento enquanto o laser está bloqueando o movimento
        if (isLaserActive)
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

        if (horizontalInput != 0 && !isAttachedToWall)
        {
            targetYRotation = horizontalInput > 0 ? 90f : -90f;
        }

        UpdateWalkSound();
    }

    // Toca ou interrompe o som contínuo de passos conforme estado no chão e movimento efetivo
    private void UpdateWalkSound()
    {
        bool shouldWalk = isGrounded && !isAttachedToWall && !isBlockedByCamera && Mathf.Abs(horizontalInput) > 0.01f;

        if (shouldWalk && !isWalkingSoundPlaying)
        {
            if (AudioManager.instancia != null && AudioManager.instancia.andar != null)
            {
                AudioManager.instancia.PlaySFX(AudioManager.instancia.andar, true);
                isWalkingSoundPlaying = true;
            }
        }
        else if (!shouldWalk && isWalkingSoundPlaying)
        {
            StopWalkSound();
        }
    }

    // Para o áudio em loop de passos se estiver ativo
    private void StopWalkSound()
    {
        if (!isWalkingSoundPlaying) return;

        AudioManager.instancia?.StopLoopingSFX();
        isWalkingSoundPlaying = false;
    }

    // Lê entradas de teclado para andar, pular, agachar e interagir
    private void ProcessKeyboardInput()
    {
        horizontalInput = 0f;
        if (Input.GetKey(kLeft)) horizontalInput -= 1f;
        if (Input.GetKey(kRight)) horizontalInput += 1f;

        LaserPointer laserTool = currentTool as LaserPointer;
        bool isLaserActive = laserTool != null && laserTool.IsLockingMovement;

        if (!isLaserActive && Input.GetKeyDown(kJump) && (isGrounded || isAttachedToWall))
        {
            ExecuteJump();
        }

        if (!isLaserActive && Input.GetKeyDown(kCrouch) && !isAttachedToWall)
        {
            ToggleCrouch();
        }

        if (Input.GetKeyDown(kInteract))
        {
            ExecuteInteract();
        }
    }

    // Aplica impulso vertical de pulo e reseta velocidade Y anterior
    private void ExecuteJump()
    {
        StopWalkSound();

        if (isCrouching) ToggleCrouch();

        SetAttachedToWall(false);
        isGrounded = false;

        Vector3 velocity = rb.linearVelocity;
        velocity.y = 0f;
        rb.linearVelocity = velocity;

        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
    }

    // Interage com caixas próximas ou aciona a ferramenta equipada
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

    // Toca efeito sonoro de empurrar
    public void PlayPushSound()
    {
        AudioManager.instancia?.PlaySFX(AudioManager.instancia.empurrar);
    }

    // Callback do Player Input para pulo
    public void OnJump(InputValue value)
    {
        LaserPointer laserTool = currentTool as LaserPointer;
        bool isLaserActive = laserTool != null && laserTool.IsLockingMovement;

        if (!isLaserActive && value.isPressed && (isGrounded || isAttachedToWall))
        {
            ExecuteJump();
        }
    }

    // Callback do Player Input para interação
    public void OnInteract(InputValue value)
    {
        if (value.isPressed)
        {
            ExecuteInteract();
        }
    }

    // Callback do Player Input para agachar
    public void OnSprint(InputValue value)
    {
        LaserPointer laserTool = currentTool as LaserPointer;
        bool isLaserActive = laserTool != null && laserTool.IsLockingMovement;

        if (!isLaserActive && value.isPressed && !isAttachedToWall)
        {
            ToggleCrouch();
        }
    }

    // Verifica contato com chão através de normais voltadas para cima
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

    // Aplica rotação, velocidade horizontal (com bloqueio na borda de afastamento da câmera) e gravidade customizada
    void FixedUpdate()
    {
        if (isAttachedToWall) return;

        transform.rotation = Quaternion.Euler(0, targetYRotation, 0);

        float currentSpeed = isCrouching ? crouchSpeed : moveSpeed;
        float vx = horizontalInput * currentSpeed;
        isBlockedByCamera = false;

        // Impede que o jogador ultrapasse o limite de afastamento horizontal em relação ao outro player
        if (CoopCamera.instancia != null)
        {
            int blockedDir = CoopCamera.instancia.GetBlockedHorizontalDirection(transform);
            if ((blockedDir < 0 && vx < 0f) || (blockedDir > 0 && vx > 0f))
            {
                vx = 0f;
                isBlockedByCamera = true;
            }
        }

        rb.linearVelocity = new Vector3(vx, rb.linearVelocity.y, rb.linearVelocity.z);

        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !Input.GetKey(kJump))
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    // Alterna entre altura normal e altura reduzida do collider ao agachar
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

    // Prende ou solta o jogador de uma parede (modo cinemático quando preso)
    public void SetAttachedToWall(bool attach)
    {
        isAttachedToWall = attach;
        rb.isKinematic = attach;

        if (attach && isCrouching)
        {
            ToggleCrouch();
        }
    }

    // Instancia e equipa uma ferramenta a partir de um prefab
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

    // Detecta aproximação de caixas interativas
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

    // Aplica dano, knockback, feedback audiovisual e atordoamento temporário
    public void TakeDamage(Vector3 knockback, float duration)
    {
        if (isStunned) return;

        StopWalkSound();

        if (isCrouching) ToggleCrouch();

        DropCurrentTool();

        SetAttachedToWall(false);
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(knockback, ForceMode.Impulse);

        AudioManager.instancia?.PlaySFX(AudioManager.instancia.Hit);
        Feedback.TremerCamera(0.2f, 0.25f);
        Feedback.Vibrar(this, 0.7f, 0.25f);

        StartCoroutine(StunRoutine(duration));
    }

    // Aplica empurrão de vento sem tocar som de dano padrão
    public void TakeWindKnockback(Vector3 knockback, float duration)
    {
        if (isStunned) return;

        StopWalkSound();

        if (isCrouching) ToggleCrouch();

        SetAttachedToWall(false);
        rb.linearVelocity = Vector3.zero;
        rb.AddForce(knockback, ForceMode.Impulse);

        Feedback.Vibrar(this, 0.3f, 0.15f);

        StartCoroutine(StunRoutine(duration));
    }

    // Remove e limpa a ferramenta equipada atualmente
    public void DropCurrentTool()
    {
        if (currentTool != null)
        {
            Destroy(currentTool.gameObject);
            currentTool = null;
            currentToolPrefab = null;
        }
    }

    // Mantém o jogador atordoado pelo tempo especificado
    private System.Collections.IEnumerator StunRoutine(float duration)
    {
        isStunned = true;
        yield return new WaitForSeconds(duration);
        isStunned = false;
    }
}