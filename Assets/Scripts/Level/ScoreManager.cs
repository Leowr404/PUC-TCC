using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("Configurações de Salário")]
    public int startingSalary = 1000;       // Salário inicial do jogo
    public int currentSalary;               // Salário acumulado
    private int salaryAtStartOfLevel;       // Guarda o salário acumulado no início da cena atual
    public int toolCost = 50;               // Custo cobrado ao pegar qualquer ferramenta

    [Header("Configuração da Fase Atual (Atualizado via LevelSettings)")]
    public float targetTimeSeconds = 60f;
    public int maxSpeedBonus = 500;

    [Header("Interface UI")]
    public TextMeshProUGUI salaryText;

    private float levelTimer = 0f;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            currentSalary = startingSalary;
            salaryAtStartOfLevel = startingSalary;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetLevelTimer();

        // Reconecta o componente de texto da nova cena
        salaryText = FindFirstObjectByType<TextMeshProUGUI>();
        UpdateUI();
    }

    void Update()
    {
        levelTimer += Time.deltaTime;
    }

    // Chamado pelo LevelSettings ao iniciar a cena
    public void ApplyLevelSettings(float targetTime, int speedBonus)
    {
        targetTimeSeconds = targetTime;
        maxSpeedBonus = speedBonus;
    }

    // Restaura o salário para o valor consolidado no início desta fase
    public void ResetToLevelStartSalary()
    {
        currentSalary = salaryAtStartOfLevel;
        UpdateUI();
    }

    public void ResetLevelTimer()
    {
        levelTimer = 0f;
    }

    public void DeductToolCost()
    {
        currentSalary -= toolCost;
        if (currentSalary < 0) currentSalary = 0;
        UpdateUI();
    }

    // Calcula o bônus dinâmico e consolida o novo saldo para a próxima fase
    public int CalculateAndAddSpeedBonus()
    {
        // Cálculo do Bônus:
        // Se terminar em 0s -> 100% do bônus.
        // Se terminar no targetTimeSeconds -> 0% do bônus.
        float remainingTimeFactor = Mathf.Clamp01((targetTimeSeconds - levelTimer) / targetTimeSeconds);
        int bonusGained = Mathf.RoundToInt(maxSpeedBonus * remainingTimeFactor);

        currentSalary += bonusGained;

        // Salva este novo total acumulado para ser o "Checkpoint" de dinheiro da próxima fase!
        salaryAtStartOfLevel = currentSalary;

        UpdateUI();

        return bonusGained;
    }

    public void UpdateUI()
    {
        if (salaryText != null)
        {
            salaryText.text = $"Salário: ${currentSalary}";
        }
    }
}