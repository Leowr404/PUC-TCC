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
    private bool hasStartedPuzzle = false;
    private bool hasLaserBeenPressed = false;

    private static readonly WaitForSeconds sequenceDisplayWait = new WaitForSeconds(2f);

    void Start()
    {
        for (int i = 0; i < playerSequence.Length; i++)
        {
            playerSequence[i] = ColorOption.White;
            correctSequence[i] = (ColorOption)99;
        }
    }

    // Chame este método quando o jogador apertar/ativar o laser pelo menos 1 vez
    public void NotifyLaserPressed()
    {
        hasLaserBeenPressed = true;

        // Se o jogador já tinha montado a sequência certa antes de apertar o laser,
        // reavalia a conclusão agora que o laser foi ativado
        if (hasStartedPuzzle && !puzzleSolved)
        {
            CheckCompletion();
        }
    }

    // Chamado pelo Botão X
    public void StartPuzzleSequence()
    {
        if (isShowingSequence || puzzleSolved) return;

        hasStartedPuzzle = true;
        StartCoroutine(ShowSequenceRoutine());
    }

    IEnumerator ShowSequenceRoutine()
    {
        isShowingSequence = true;

        // 1. Sorteia 3 cores entre as 4 disponíveis (Azul, Vermelho, Amarelo, Verde)
        for (int i = 0; i < 3; i++)
        {
            correctSequence[i] = (ColorOption)Random.Range(1, 5);
        }

        // 2. Mostra as cores nos botões da parede
        int buttonCount = wallButtons != null ? Mathf.Min(3, wallButtons.Length) : 0;
        for (int i = 0; i < buttonCount; i++)
        {
            if (wallButtons[i] != null)
            {
                wallButtons[i].SetColor(correctSequence[i]);
            }
        }

        // 3. Fica aceso por 2 segundos
        yield return sequenceDisplayWait;

        // 4. Voltam a ser brancos para o jogador tentar adivinhar
        for (int i = 0; i < buttonCount; i++)
        {
            if (wallButtons[i] != null)
            {
                wallButtons[i].SetColor(ColorOption.White);
            }
        }

        isShowingSequence = false;
    }

    // Chamado quando o jogador interage/clica em um dos 3 botões da parede para mudar a cor dele
    public void CycleButtonColor(int buttonIndex)
    {
        if (isShowingSequence || puzzleSolved) return;
        if (buttonIndex < 0 || buttonIndex >= 3) return;

        int nextColorIndex = ((int)playerSequence[buttonIndex] + 1) % 5;
        playerSequence[buttonIndex] = (ColorOption)nextColorIndex;

        if (wallButtons != null && buttonIndex < wallButtons.Length && wallButtons[buttonIndex] != null)
        {
            wallButtons[buttonIndex].SetColor(playerSequence[buttonIndex]);
        }

        CheckCompletion();
    }

    void CheckCompletion()
    {
        // Só permite abrir a porta se o puzzle tiver sido iniciado E o laser tiver sido apertado ao menos 1 vez
        if (!hasStartedPuzzle || !hasLaserBeenPressed) return;

        for (int i = 0; i < 3; i++)
        {
            if (playerSequence[i] != correctSequence[i])
            {
                return;
            }
        }

        puzzleSolved = true;

        if (targetDoor != null)
        {
            targetDoor.SetActive(false);
        }
    }
}