using UnityEngine;

public class LaserPlatform : MonoBehaviour
{
    [Header("Posição de Encaixe")]
    public Transform standPoint;

    private PlayerController3D activePlayer = null;

    public void Interact(PlayerController3D player)
    {
        if (activePlayer == player)
        {
            ExitPlatform();
            return;
        }

        GameObject currentTool = player.GetCurrentToolPrefab();
        if (currentTool != null)
        {
            EnterPlatform(player);
        }
    }

    private void EnterPlatform(PlayerController3D player)
    {
        activePlayer = player;

        if (standPoint != null)
        {
            player.transform.position = standPoint.position;
            player.transform.rotation = standPoint.rotation;
        }

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }

    public void ExitPlatform()
    {
        if (activePlayer != null)
        {
            Rigidbody rb = activePlayer.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
            }
            activePlayer = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController3D player = other.GetComponent<PlayerController3D>();
        if (player != null)
        {
            // Se precisar registrar a plataforma próxima no player, ajuste se necessário
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController3D player = other.GetComponent<PlayerController3D>();
        if (player != null && activePlayer == player)
        {
            ExitPlatform();
        }
    }
}