using UnityEngine;

public class TutorialUI : MonoBehaviour
{
    [Header("Painel do Tutorial")]
    public GameObject tutorialPanel; // Arraste o TutorialPanel aqui

    [Header("Configurações")]
    public bool pauseGameOnTutorial = true; // Pausa o tempo do jogo enquanto lê

    private bool tutorialActive = true;

    void Start()
    {
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }

        // Congela o tempo do jogo se a opção estiver marcada
        if (pauseGameOnTutorial)
        {
            Time.timeScale = 0f;
        }
    }

    void Update()
    {
        // Se o tutorial estiver ativo e o jogador pressionar qualquer tecla ou botão do mouse
        if (tutorialActive && (Input.anyKeyDown || Input.GetMouseButtonDown(0)))
        {
            CloseTutorial();
        }
    }

    public void CloseTutorial()
    {
        tutorialActive = false;

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }

        // Descongela o tempo do jogo
        if (pauseGameOnTutorial)
        {
            Time.timeScale = 1f;
        }
    }
}