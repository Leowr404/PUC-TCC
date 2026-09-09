using UnityEngine;

// Ferramenta desentupidor: permite grudar/soltar caixas e paredes com um clique (Toggle)
public class PlungerTool : BaseTool
{
    [Header("Configuracoes do Desentupidor")]
    public Transform raycastPoint;       // Ponto de origem do raio
    public LayerMask interactableLayer;  // Camada que contem Paredes e Caixas
    public float rayDistance = 2.0f;     // Distancia de alcance do raio
    public float toggleCooldown = 1.0f;  // Intervalo obrigatorio entre acoes

    private PlayerController3D ownerPlayer;
    private MovableBox attachedBox;
    private float nextActionTime = 0f;   // Controla o tempo de espera do cooldown

    void Update()
    {
        if (raycastPoint != null && ownerPlayer != null)
        {
            Debug.DrawRay(raycastPoint.position, ownerPlayer.transform.forward * rayDistance, Color.red);
        }
    }

    public override void OnActionDown(PlayerController3D player)
    {
        ownerPlayer = player;

        // Se ainda nao passou 1 segundo desde a ultima acao, ignora o clique
        if (Time.time < nextActionTime) return;

        nextActionTime = Time.time + toggleCooldown;

        // 1. Se JA ESTA segurando uma caixa, solta ela
        if (attachedBox != null)
        {
            ReleaseAll(player);
            return;
        }

        if (raycastPoint == null) return;

        // 2. Se NAO ESTA segurando nada, tenta pegar/grudar
        RaycastHit hit;
        if (Physics.Raycast(raycastPoint.position, player.transform.forward, out hit, rayDistance, interactableLayer))
        {
            MovableBox box = hit.collider.GetComponent<MovableBox>();
            if (box != null)
            {
                attachedBox = box;
                attachedBox.AttachToTool(raycastPoint);
                return;
            }

            player.SetAttachedToWall(true);
        }
    }

    public override void OnActionHold(PlayerController3D player) { }
    public override void OnActionUp(PlayerController3D player) { }

    private void ReleaseAll(PlayerController3D player)
    {
        if (player != null)
        {
            player.SetAttachedToWall(false);
        }

        if (attachedBox != null)
        {
            attachedBox.DetachFromTool();
            attachedBox = null;
        }
    }

    private void OnDestroy()
    {
        if (attachedBox != null)
        {
            attachedBox.DetachFromTool();
            attachedBox = null;
        }

        if (ownerPlayer != null)
        {
            ownerPlayer.SetAttachedToWall(false);
        }
    }
}