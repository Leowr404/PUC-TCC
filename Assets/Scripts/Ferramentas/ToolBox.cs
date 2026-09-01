using UnityEngine;

public class Toolbox : MonoBehaviour
{
    [Header("Ferramenta que esta caixa contem")]
    public GameObject toolPrefab; // Arraste o Prefab da ferramenta aqui

    public void Interact(PlayerController3D player)
    {
        if (toolPrefab != null)
        {
            player.EquipToolPrefab(toolPrefab);
        }
    }
}