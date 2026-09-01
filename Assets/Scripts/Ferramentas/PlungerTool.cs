using UnityEngine;

// Script especifico da ferramenta Desentupidor
public class PlungerTool : BaseTool
{
    [Header("Configuracoes do Desentupidor")]
    public Transform wallCheck;
    public LayerMask wallLayer;
    public float wallCheckDistance = 0.5f;

    public override void OnActionHold(PlayerController3D player)
    {
        bool touchingWall = Physics.Raycast(wallCheck.position, player.transform.forward, wallCheckDistance, wallLayer);

        if (touchingWall)
        {
            player.SetAttachedToWall(true);
        }
        else
        {
            player.SetAttachedToWall(false);
        }
    }

    public override void OnActionUp(PlayerController3D player)
    {
        // Soltou o botao de acao, desgrupa da parede
        player.SetAttachedToWall(false);
    }
}