using UnityEngine;

public class Door : MonoBehaviour
{
    [Header("Estado Inicial")]
    public bool isOpen = false;

    [Header("Modo de Desaparecimento")]
    [Tooltip("Se true, desativa o GameObject inteiro. Se false, apenas esconde o visual e desativa a colisão.")]
    public bool hideEntireObject = true;

    private Renderer doorRenderer;
    private Collider doorCollider;

    void Start()
    {
        doorRenderer = GetComponent<Renderer>();
        doorCollider = GetComponent<Collider>();

        // Sincroniza o estado inicial
        UpdateDoorState();
    }

    public void OpenDoor()
    {
        isOpen = true;
        UpdateDoorState();
    }

    public void CloseDoor()
    {
        isOpen = false;
        UpdateDoorState();
    }

    public void ToggleDoor()
    {
        isOpen = !isOpen;
        UpdateDoorState();
    }

    private void UpdateDoorState()
    {
        if (hideEntireObject)
        {
            // Simplesmente ativa/desativa o objeto no jogo
            gameObject.SetActive(!isOpen);
        }
        else
        {
            // Mantém o objeto ativo, mas esconde a malha e desativa a colisão
            if (doorRenderer != null) doorRenderer.enabled = !isOpen;
            if (doorCollider != null) doorCollider.enabled = !isOpen;
        }
    }
}