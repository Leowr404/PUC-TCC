using UnityEngine;

public class LevelSettings : MonoBehaviour
{
    [Header("Configuração desta Fase")]
    public float targetTimeForThisLevel = 60f; // Tempo ideal em segundos para ESTA fase
    public int maxSpeedBonusForThisLevel = 500; // Bônus máximo para ESTA fase

    void Start()
    {
        // Envia as configurações desta fase para o ScoreManager assim que a cena abre
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ApplyLevelSettings(targetTimeForThisLevel, maxSpeedBonusForThisLevel);
        }
    }
}