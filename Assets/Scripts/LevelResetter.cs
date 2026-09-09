using UnityEngine;
using UnityEngine.SceneManagement;

// Gerencia o reinicio do nivel para evitar soft-locks
public class LevelResetter : MonoBehaviour
{
    [Header("Configuracao de Atalho")]
    public KeyCode resetKey = KeyCode.R;

    void Update()
    {
        // Se a tecla R for pressionada, recarrega a cena atual
        if (Input.GetKeyDown(resetKey))
        {
            ResetLevel();
        }
    }

    public void ResetLevel()
    {
        // Pega o indice da cena ativa no momento e recarrega
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }
}