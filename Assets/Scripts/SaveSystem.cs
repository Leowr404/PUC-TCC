using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    const string KEY_LEVEL = "Save_Level";
    const string KEY_CHECKPOINT = "Save_Checkpoint";
    const string KEY_DESBLOQUEADOS = "Levels_Desbloqueados";

    // Ligado só pelo botão Continuar, para o level saber que deve spawnar no checkpoint.
    public static bool CarregarDoCheckpoint;

    // ---------- Progresso (apagado ao iniciar novo jogo) ----------
    public static bool TemSave => PlayerPrefs.HasKey(KEY_LEVEL);
    public static string LevelSalvo => PlayerPrefs.GetString(KEY_LEVEL, "");
    public static int CheckpointSalvo => PlayerPrefs.GetInt(KEY_CHECKPOINT, -1);

    public static void SalvarCheckpoint(string cena, int checkpointId)
    {
        PlayerPrefs.SetString(KEY_LEVEL, cena);
        PlayerPrefs.SetInt(KEY_CHECKPOINT, checkpointId);
        PlayerPrefs.Save();
    }

    public static void ApagarProgresso()
    {
        PlayerPrefs.DeleteKey(KEY_LEVEL);
        PlayerPrefs.DeleteKey(KEY_CHECKPOINT);
        PlayerPrefs.Save();
    }

    // ---------- Levels desbloqueados (nunca apagados pelo novo jogo) ----------
    // Level 1 sempre liberado. Números começam em 1.
    public static int LevelsDesbloqueados => PlayerPrefs.GetInt(KEY_DESBLOQUEADOS, 1);

    public static bool LevelDesbloqueado(int numero) => numero <= LevelsDesbloqueados;

    public static void DesbloquearLevel(int numero)
    {
        if (numero <= LevelsDesbloqueados) return;
        PlayerPrefs.SetInt(KEY_DESBLOQUEADOS, numero);
        PlayerPrefs.Save();
    }
}
