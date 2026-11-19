using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Controla a saída da fase pelo elevador quando ambos os jogadores entram e carrega a próxima cena
public class LevelExitElevator : MonoBehaviour
{
    [Header("Configuração do Elevador")]
    public float riseSpeed = 3f; // Velocidade vertical de subida do elevador
    public float riseDuration = 2f; // Duração da subida em segundos antes de trocar de cena
    public Transform elevatorPlatform; // Plataforma/objeto que sobe; usa o próprio transform se vazio

    private HashSet<GameObject> playersInElevator = new HashSet<GameObject>(); // Jogadores atualmente dentro do gatilho
    private bool isElevatorActivated = false; // Evita disparar a sequência mais de uma vez

    // Registra a entrada do jogador e inicia a subida quando os 2 players estiverem no elevador
    private void OnTriggerEnter(Collider other)
    {
        if (isElevatorActivated) return;

        PlayerController3D player = other.GetComponent<PlayerController3D>();
        if (player != null)
        {
            playersInElevator.Add(player.gameObject);

            if (playersInElevator.Count >= 2)
            {
                StartCoroutine(StartElevatorSequence());
            }
        }
    }

    // Remove o jogador da contagem caso ele saia antes do elevador ativar
    private void OnTriggerExit(Collider other)
    {
        if (isElevatorActivated) return;

        PlayerController3D player = other.GetComponent<PlayerController3D>();
        if (player != null)
        {
            playersInElevator.Remove(player.gameObject);
        }
    }

    // Executa bônus, oculta os jogadores, toca os áudios do elevador, sobe e carrega a próxima fase
    private IEnumerator StartElevatorSequence()
    {
        isElevatorActivated = true;

        // Toca o efeito sonoro do elevador subindo e troca para a música de elevador
        if (AudioManager.instancia != null)
        {
            AudioManager.instancia.PlaySFX(AudioManager.instancia.elevadorSubindo);
            AudioManager.instancia.PlayMusic(AudioManager.instancia.musicaElevador);
        }

        // Calcula e adiciona o bônus de tempo antes de encerrar a fase
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.CalculateAndAddSpeedBonus();
        }

        // Esconde os renderers e trava a física de ambos os jogadores durante a subida
        foreach (GameObject playerObj in playersInElevator)
        {
            Renderer[] renderers = playerObj.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                r.enabled = false;
            }

            Rigidbody rb = playerObj.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
            }
        }

        // Move o elevador para cima durante riseDuration segundos
        float timer = 0f;
        Transform targetTransform = elevatorPlatform != null ? elevatorPlatform : transform;

        while (timer < riseDuration)
        {
            targetTransform.Translate(Vector3.up * riseSpeed * Time.deltaTime, Space.World);
            timer += Time.deltaTime;
            yield return null;
        }

        // Reseta o cronômetro da fase e carrega a próxima cena configurada no Build Settings
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetLevelTimer();
        }

        int nextSceneIndex = SceneManager.GetActiveScene().buildIndex + 1;

        if (nextSceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(nextSceneIndex);
        }
        else
        {
            Debug.Log("Esta é a última fase! Não há mais cenas no Build Settings.");
        }
    }
}