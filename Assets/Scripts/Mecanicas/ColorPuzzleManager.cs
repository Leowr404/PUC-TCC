using System.Collections;
using UnityEngine;

public class ColorPuzzleManager : MonoBehaviour
{
    public enum ColorOption { White, Blue, Red, Yellow, Green }

    [Header("Configuracao dos Botoes na Parede (3 Botoes)")]
    public ColorButtonController[] wallButtons = new ColorButtonController[3];

    [Header("Referencia da Porta")]
    public GameObject targetDoor; // A porta que vai sumir quando acertar

    [Header("Estado do Puzzle")]
    private ColorOption[] correctSequence = new ColorOption[3];
    private ColorOption[] playerSequence = new ColorOption[3];
    private bool isShowingSequence = false;
    private bool puzzleSolved = false;

    void Start()
    {
        // Inicializa a sequência do jogador como Branca
        for (int i = 0; i < playerSequence.Length; i++)
        {
            playerSequence[i] = ColorOption.White;
        }
    }

    // Chamado pelo Botão X
    public void StartPuzzleSequence()
    {
        if (isShowingSequence || puzzleSolved) return;

        StartCoroutine(ShowSequenceRoutine());
    }

    IEnumerator ShowSequenceRoutine()
    {
        isShowingSequence = true;

        // 1. Sorteia 3 cores entre as 4 disponíveis (Azul, Vermelho, Amarelo, Verde)
        // Usamos valores de 1 a 4 para ignorar o Branco (0) na sequência secreta
        for (int i = 0; i < 3; i++)
        {
            correctSequence[i] = (ColorOption)Random.Range(1, 5);
        }

        // 2. Mostra as cores nos botões da parede
        for (int i = 0; i < wallButtons.Length; i++)
        {
            wallButtons[i].SetColor(correctSequence[i]);
        }

        // 3. Fica aceso por 2 segundos (conforme o requisito 5)
        yield return new WaitForSeconds(2f);

        // 4. Voltam a ser brancos para o jogador tentar adivinhar
        for (int i = 0; i < wallButtons.Length; i++)
        {
            wallButtons[i].SetColor(ColorOption.White);
        }

        isShowingSequence = false;
    }

    // Chamado quando o jogador interage/clica em um dos 3 botões da parede para mudar a cor dele
    public void CycleButtonColor(int buttonIndex)
    {
        if (isShowingSequence || puzzleSolved) return;

        // Cicla a cor do jogador: Branco -> Azul -> Vermelho -> Amarelo -> Verde -> Branco
        int nextColorIndex = ((int)playerSequence[buttonIndex] + 1) % 5;
        playerSequence[buttonIndex] = (ColorOption)nextColorIndex;

        // Atualiza o visual do botão na parede
        wallButtons[buttonIndex].SetColor(playerSequence[buttonIndex]);

        // Verifica se o jogador acertou a ordem completa
        CheckCompletion();
    }

    void CheckCompletion()
    {
        bool isCorrect = true;
        for (int i = 0; i < 3; i++)
        {
            if (playerSequence[i] != correctSequence[i])
            {
                isCorrect = false;
                break;
            }
        }

        if (isCorrect)
        {
            puzzleSolved = true;
            Debug.Log("Puzzle resolvido! A porta sumiu.");

            if (targetDoor != null)
            {
                targetDoor.SetActive(false); // Some com a porta
            }
        }
    }
}