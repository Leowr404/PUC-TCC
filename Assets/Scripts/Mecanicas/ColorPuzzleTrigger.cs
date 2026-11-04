using UnityEngine;

public class ColorPuzzleTrigger : MonoBehaviour
{
    [Header("Conexao")]
    public ColorPuzzleManager puzzleManager; 

    [Header("Configuracao de Ativacao")]
    public string projectileTag = "Projectile"; 

    // Permite ativar por colisão (ex: tiro do PaperClipProjectile)
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(projectileTag) || other.GetComponent<PaperClipProjectile>() != null)
        {
            TriggerPuzzle();
        }
    }

    // Permite ativar pelo laser ou outra mecânica diretamente
    public void ActivateByLaser()
    {
        TriggerPuzzle();
    }

    private void TriggerPuzzle()
    {
        if (puzzleManager != null)
        {
            puzzleManager.StartPuzzleSequence();
        }
        else
        {
            Debug.LogWarning("ColorPuzzleManager não está atribuído no Botão X!", this);
        }
    }
}