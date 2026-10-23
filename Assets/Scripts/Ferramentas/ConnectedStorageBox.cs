using UnityEngine;

public class ConnectedStorageBox : MonoBehaviour
{
    [Header("Item Armazenado Atual (Deixe vazio para começar vazia)")]
    public GameObject storedToolPrefab;

    [Header("Conexão")]
    [Tooltip("Arraste a outra caixa ligada a esta aqui no Inspector")]
    public ConnectedStorageBox partnerBox;

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
            GameObject playerTool = player.GetCurrentToolPrefab();

            if (playerTool != null)
            {
                // Salva a referencia da ferramenta na caixa
                storedToolPrefab = playerTool;

                // REMOVE O ITEM DA MÃO DO JOGADOR
                player.DropCurrentTool();

                UpdateVisuals();

                // SE A CAIXA PARCEIRA TAMBÉM ESTIVER CHEIA, ELAS TROCAM OS ITENS DE LUGAR!
                if (partnerBox != null && partnerBox.storedToolPrefab != null)
                {
                    SwapItemsWithPartner();
                }
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

    // Método que realiza a troca de itens entre as duas caixas conectadas
    private void SwapItemsWithPartner()
    {
        // Guarda temporariamente o item da caixa parceira
        GameObject tempTool = partnerBox.storedToolPrefab;

        // A caixa parceira recebe o item desta caixa
        partnerBox.storedToolPrefab = this.storedToolPrefab;

        // Esta caixa recebe o item que estava na parceira
        this.storedToolPrefab = tempTool;

        // Atualiza o visual de ambas as caixas na tela
        this.UpdateVisuals();
        partnerBox.UpdateVisuals();
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