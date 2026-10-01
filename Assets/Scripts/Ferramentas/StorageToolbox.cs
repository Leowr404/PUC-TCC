using UnityEngine;

public class StorageToolbox : MonoBehaviour
{
    [Header("Item Armazenado Atual (Deixe vazio para começar vazia)")]
    public GameObject storedToolPrefab;

    [Header("Feedback Visual")]
    public MeshRenderer boxRenderer;      // O MeshRenderer da caixa para mudar de cor
    public Color emptyColor = Color.gray; // Cor da caixa quando estiver vazia
    public Color fullColor = Color.green; // Cor da caixa quando tiver um item

    [Header("Modelo 3D Visual (Opcional)")]
    public GameObject visualToolDisplay; // Um objeto filho na caixa para mostrar o item repousando

    void Start()
    {
        UpdateVisuals();
    }

    public void Interact(PlayerController3D player)
    {
        // CASO 1: A caixa esta VAZIA -> Guarda a ferramenta do jogador
        if (storedToolPrefab == null)
        {
            // Pega a ferramenta que o jogador tem na mao no momento
            GameObject playerTool = player.GetCurrentToolPrefab();

            if (playerTool != null)
            {
                // Salva a referencia da ferramenta na caixa
                storedToolPrefab = playerTool;

                // Destroi o item da mao do jogador
                player.DropCurrentTool();

                UpdateVisuals();
            }
        }
        // CASO 2: A caixa esta CHEIA -> Entrega a ferramenta para o jogador
        else
        {
            // Equipamento vai para a mao do player
            player.EquipToolPrefab(storedToolPrefab);

            // Reseta a caixa para vazia
            storedToolPrefab = null;

            UpdateVisuals();
        }
    }

    // Atualiza a cor ou o modelo da caixa
    void UpdateVisuals()
    {
        bool hasItem = storedToolPrefab != null;

        // Altera a cor do material da caixa se o MeshRenderer foi atribuido
        if (boxRenderer != null)
        {
            boxRenderer.material.color = hasItem ? fullColor : emptyColor;
        }

        // Mostra/Esconde o modelo visual do item em cima/dentro da caixa
        if (visualToolDisplay != null)
        {
            visualToolDisplay.SetActive(hasItem);
        }
    }
}