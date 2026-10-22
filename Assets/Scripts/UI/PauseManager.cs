using UnityEngine;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Pause para co-op local: Time.timeScale = 0 pausa os dois jogadores juntos.
public class PauseManager : MonoBehaviour
{
    [SerializeField] GameObject painelPause;
    [SerializeField] GameObject painelSettings;
    [SerializeField] string cenaMenu = "MainMenu";
    [Tooltip("Desmarque se o script do player já chama AlternarPause().")]
    [SerializeField] bool lerTeclaPause = true;

    public static bool Pausado { get; private set; }

    CursorLockMode cursorAntes;
    bool cursorVisivelAntes;

    bool SettingsAberto { get { return painelSettings != null && painelSettings.activeSelf; } }

    void Start()
    {
        if (painelPause != null) painelPause.SetActive(false);
        if (painelSettings != null) painelSettings.SetActive(false);
    }

    void Update()
    {
        if (lerTeclaPause && ApertouPause()) AlternarPause();
    }

    static bool ApertouPause()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) return true;
        foreach (var g in Gamepad.all)
            if (g.startButton.wasPressedThisFrame) return true;
        return false;
#else
        return Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.JoystickButton7);
#endif
    }

    public void AlternarPause()
    {
        if (Pausado && SettingsAberto) { FecharSettings(); return; } // Esc no Settings volta pro pause
        if (Pausado) Despausar(); else Pausar();
    }

    public void Pausar()
    {
        if (Pausado) return;
        Pausado = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;
        cursorAntes = Cursor.lockState;
        cursorVisivelAntes = Cursor.visible;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (painelPause != null) painelPause.SetActive(true);
    }

    public void Despausar()
    {
        if (!Pausado) return;
        Pausado = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = cursorAntes;
        Cursor.visible = cursorVisivelAntes;
        if (painelSettings != null) painelSettings.SetActive(false);
        if (painelPause != null) painelPause.SetActive(false);
    }

    public void AbrirSettings()
    {
        if (painelSettings == null) return;
        if (painelPause != null) painelPause.SetActive(false);
        painelSettings.SetActive(true);
    }

    public void FecharSettings()
    {
        if (painelSettings != null) painelSettings.SetActive(false);
        if (Pausado && painelPause != null) painelPause.SetActive(true);
    }

    public void VoltarAoMenu()
    {
        Despausar();
        SceneManager.LoadScene(cenaMenu);
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Pausado = false;
    }
}
