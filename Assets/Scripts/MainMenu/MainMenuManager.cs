using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("Painéis")]
    [SerializeField] GameObject painelPrincipal;
    [SerializeField] GameObject painelLevels;
    [SerializeField] GameObject painelSettings;
    [SerializeField] GameObject painelCreditos;
    [SerializeField] GameObject popupNovoJogo;

    [Header("Botões")]
    [SerializeField] GameObject botaoContinuar;
    [SerializeField] Button[] botoesLevels;   // na ordem: Level 1, Level 2...

    [Header("Cenas")]
    [SerializeField] string[] cenasLevels = { "Level1" }; // mesmo tamanho e ordem de botoesLevels

    void Start()
    {
        Time.timeScale = 1f;
        popupNovoJogo.SetActive(false);
        botaoContinuar.SetActive(SaveSystem.TemSave);
        AbrirPainel(painelPrincipal);
    }

    void AbrirPainel(GameObject painel)
    {
        painelPrincipal.SetActive(painel == painelPrincipal);
        painelLevels.SetActive(painel == painelLevels);
        painelSettings.SetActive(painel == painelSettings);
        painelCreditos.SetActive(painel == painelCreditos);
    }

    // ---------- Jogar / Novo jogo ----------
    public void Jogar()
    {
        if (SaveSystem.TemSave) popupNovoJogo.SetActive(true); // pergunta antes de apagar
        else IniciarNovoJogo();
    }

    public void ConfirmarNovoJogo()   // botão "Sim"
    {
        SaveSystem.ApagarProgresso();
        IniciarNovoJogo();
    }

    public void CancelarNovoJogo() => popupNovoJogo.SetActive(false); // botão "Não"

    void IniciarNovoJogo()
    {
        SaveSystem.CarregarDoCheckpoint = false;
        SceneManager.LoadScene(cenasLevels[0]);
    }

    // ---------- Continuar ----------
    public void Continuar()
    {
        if (!SaveSystem.TemSave) return;
        SaveSystem.CarregarDoCheckpoint = true;
        SceneManager.LoadScene(SaveSystem.LevelSalvo);
    }

    // ---------- Levels ----------
    public void AbrirLevels()
    {
        for (int i = 0; i < botoesLevels.Length; i++)
            botoesLevels[i].interactable = SaveSystem.LevelDesbloqueado(i + 1);

        AbrirPainel(painelLevels);
    }

    public void CarregarLevel(int numero)  // no OnClick, digite 1, 2, 3...
    {
        if (!SaveSystem.LevelDesbloqueado(numero)) return;
        SaveSystem.CarregarDoCheckpoint = false;
        SceneManager.LoadScene(cenasLevels[numero - 1]);
    }

    // ---------- Outros ----------
    public void AbrirSettings() => AbrirPainel(painelSettings);
    public void AbrirCreditos() => AbrirPainel(painelCreditos);
    public void Voltar() => AbrirPainel(painelPrincipal);
    public void Sair() => Application.Quit();
}

