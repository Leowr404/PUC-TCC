using UnityEngine;

public class PlayerController3D : MonoBehaviour
{
    [Header("Configuracoes de Movimento")]
    public float moveSpeed = 8f;
    public float jumpForce = 10f;

    [Header("Ajuste de Gravidade")]
    public float fallMultiplier = 2.5f;
    public float lowJumpMultiplier = 2f;

    [Header("Mapeamento de Teclas")]
    public KeyCode leftKey;
    public KeyCode rightKey;
    public KeyCode jumpKey;
    public KeyCode actionKey;

    [Header("Detectador de Chao")]
    public Transform groundCheck;
    public LayerMask groundLayer;
    public float groundCheckRadius = 0.2f;

    [Header("Ponto de Encaixe da Ferramenta")]
    public Transform toolHoldPoint; // Objeto filho (mao do jogador)

    private BaseTool currentTool; // Ferramenta equipada no momento
    private Rigidbody rb;
    private bool isGrounded;
    private bool isAttachedToWall;
    private float horizontalInput;
    private Toolbox nearbyToolbox;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        horizontalInput = 0f;
        if (Input.GetKey(leftKey)) horizontalInput -= 1f;
        if (Input.GetKey(rightKey)) horizontalInput += 1f;

        if (groundCheck != null)
        {
            isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        }

        // --- COMUNICAO COM A FERRAMENTA EQUIPADA ---
        if (currentTool != null)
        {
            if (Input.GetKeyDown(actionKey)) currentTool.OnActionDown(this);
            if (Input.GetKey(actionKey)) currentTool.OnActionHold(this);
            if (Input.GetKeyUp(actionKey)) currentTool.OnActionUp(this);
        }

        // Interacao com a Caixa de Ferramentas
        if (Input.GetKeyDown(actionKey) && nearbyToolbox != null)
        {
            nearbyToolbox.Interact(this);
        }

        // Pulo
        if (Input.GetKeyDown(jumpKey) && (isGrounded || isAttachedToWall))
        {
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

        rb.linearVelocity = new Vector3(horizontalInput * moveSpeed, rb.linearVelocity.y, rb.linearVelocity.z);

        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !Input.GetKey(jumpKey))
        {
            rb.linearVelocity += Vector3.up * Physics.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    // Trava ou destrava o personagem na parede
    public void SetAttachedToWall(bool attach)
    {
        isAttachedToWall = attach;
        rb.isKinematic = attach;
    }

    // Equipa um novo objeto de ferramenta
    public void EquipToolPrefab(GameObject newToolPrefab)
    {
        // Destroi a ferramenta antiga se existir
        if (currentTool != null)
        {
            Destroy(currentTool.gameObject);
        }

        // Instancia a nova ferramenta na mao do jogador
        GameObject toolObj = Instantiate(newToolPrefab, toolHoldPoint.position, toolHoldPoint.rotation, toolHoldPoint);
        currentTool = toolObj.GetComponent<BaseTool>();
    }

    private void OnTriggerEnter(Collider other)
    {
        Toolbox box = other.GetComponent<Toolbox>();
        if (box != null) nearbyToolbox = box;
    }

    private void OnTriggerExit(Collider other)
    {
        Toolbox box = other.GetComponent<Toolbox>();
        if (box != null && nearbyToolbox == box) nearbyToolbox = null;
    }
}