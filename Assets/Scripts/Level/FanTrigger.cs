using UnityEngine;

public class FanTrigger : MonoBehaviour
{
    [Header("Conexao")]
    public FanVentilator targetFan; 

    [Header("Configuracao de Ativacao")]
    public string projectileTag = "Projectile";

    // Permite ativar por colisão (ex: tiro do PaperClipProjectile)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(projectileTag) || other.GetComponent<PaperClipProjectile>() != null)
        {
            TriggerFan();
        }
    }

    // Permite ativar pelo laser ou outra mecânica diretamente, se necessário
    public void ActivateByLaser()
    {
        TriggerFan();
    }

    private void TriggerFan()
    {
        if (targetFan != null)
        {
            targetFan.ActivateFan();
        }
        else
        {
            Debug.LogWarning("Nenhum FanVentilator associado a este gatilho!", this);
        }
    }
}