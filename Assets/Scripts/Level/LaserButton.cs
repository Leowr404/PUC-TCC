using UnityEngine;

public class LaserButton : MonoBehaviour
{
    [Header("Conexao com a Porta")]
    public Door targetDoor;                  // Arraste a porta no Inspector

    [Header("Feedback Visual")]
    public Renderer buttonRenderer;          // Opcional: Renderer para mudar de cor
    public Color activatedColor = Color.green;

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
            // Cria uma instancia do material para mudar a cor sem alterar outros objetos
            buttonMaterial = buttonRenderer.material;
        }
    }

    public void ActivateByLaser()
    {
        if (isActivated) return;

        isActivated = true;

        // Muda a cor se houver um Renderer/Material disponível
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
            Debug.LogWarning("Nenhuma porta associada a este botão de laser!", this);
        }
    }

    public void ResetButton()
    {
        isActivated = false;

        if (targetDoor != null)
        {
            targetDoor.CloseDoor();
        }
    }
}