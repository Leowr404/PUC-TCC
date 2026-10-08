using UnityEngine;

public class LaserButton : MonoBehaviour
{
    public enum ButtonType
    {
        Toggle, // Ativa no primeiro toque e permanece ativado
        Hold    // Precisa do laser apontado continuamente
    }

    [Header("Tipo de Botao")]
    public ButtonType buttonType = ButtonType.Toggle;

    [Header("Conexao com a Porta")]
    public Door targetDoor;                  // Arraste a porta no Inspector

    [Header("Feedback Visual")]
    public Renderer buttonRenderer;          // Opcional: Renderer para mudar de cor
    public Color activatedColor = Color.green;
    private Color originalColor;

    [Header("Configuracoes do Modo Hold")]
    [Tooltip("Tempo em segundos sem o laser ate o botao desativar (evita oscilacoes)")]
    public float holdResetDelay = 0.15f;
    private float lastTimeHitByLaser = 0f;

    [Header("Estado")]
    public bool isActivated = false;

    private Material buttonMaterial;

    void Start()
    {
        // Se nao atribuiu o Renderer no Inspector, busca no proprio objeto ou nos filhos
        if (buttonRenderer == null)
        {
            buttonRenderer = GetComponentInChildren<Renderer>();
        }

        if (buttonRenderer != null)
        {
            // Cria uma instancia do material para mudar a cor apenas deste objeto
            buttonMaterial = buttonRenderer.material;
            originalColor = buttonMaterial.color;
        }
    }

    void Update()
    {
        // No modo Hold, verifica se o laser parou de acertar o botao
        if (buttonType == ButtonType.Hold && isActivated)
        {
            if (Time.time - lastTimeHitByLaser > holdResetDelay)
            {
                DeactivateButton();
            }
        }
    }

    // Metodo chamado continuamento pelo LaserPointer enquanto o raio estiver colidindo
    public void ActivateByLaser()
    {
        lastTimeHitByLaser = Time.time;

        if (!isActivated)
        {
            isActivated = true;

            // Altera visual para cor de ativado
            if (buttonMaterial != null)
            {
                buttonMaterial.color = activatedColor;
            }

            // Abre a porta
            if (targetDoor != null)
            {
                targetDoor.OpenDoor();
            }
            else
            {
                Debug.LogWarning("Nenhuma porta associada a este botão!", this);
            }
        }
    }

    private void DeactivateButton()
    {
        isActivated = false;

        // Retorna a cor original
        if (buttonMaterial != null)
        {
            buttonMaterial.color = originalColor;
        }

        // Fecha/reaparece a porta
        if (targetDoor != null)
        {
            targetDoor.CloseDoor();
        }
    }

    public void ResetButton()
    {
        DeactivateButton();
    }
}