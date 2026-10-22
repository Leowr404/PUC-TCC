using UnityEngine;

public class ButtonTrigger : MonoBehaviour
{
    [Header("Conexao com a Porta")]
    public Door targetDoor;              // Arraste a porta correspondente aqui no Inspector

    [Header("Configuracoes do Botao")]
    public bool isToggle = false;        // Se true, apertar alterna (Abre/Fecha). Se false, só abre.
    public KeyCode interactKey = KeyCode.E; // Tecla para interagir (ou acionar por colisao)

    private bool isPlayerInside = false;

    void Update()
    {
        // Se o player estiver na area do botao e apertar a tecla de interacao
        if (isPlayerInside && Input.GetKeyDown(interactKey))
        {
            PressButton();
        }
    }

    public void PressButton()
    {
        if (targetDoor == null)
        {
            Debug.LogWarning("Nenhuma porta associada a este botao!", this);
            return;
        }

        if (isToggle)
        {
            targetDoor.ToggleDoor();
        }
        else
        {
            targetDoor.OpenDoor();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
        }
    }
}