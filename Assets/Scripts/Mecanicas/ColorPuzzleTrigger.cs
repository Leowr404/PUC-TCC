using UnityEngine;

public class ColorPuzzleTrigger : MonoBehaviour
{
    [Header("Conexao")]
    public ColorPuzzleManager puzzleManager;

    [Header("Configuracao de Ativacao")]
    public string projectileTag = "Projectile";

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
        if (puzzleManager != null)
        {
            puzzleManager.NotifyLaserPressed();
            puzzleManager.StartPuzzleSequence();
        }
        else
        {
            Debug.LogWarning("ColorPuzzleManager não está atribuído no Botão X!", this);
        }
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