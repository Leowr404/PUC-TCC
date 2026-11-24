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

    private readonly HashSet<GameObject> playersInElevator = new HashSet<GameObject>();
    private bool isElevatorActivated = false;
    private Coroutine elevatorRoutine;
    private Transform selfTransform;

    void Awake()
    {
        selfTransform = transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isElevatorActivated) return;

        if (other.TryGetComponent(out PlayerController3D player))
        {
            playersInElevator.Add(player.gameObject);

            if (playersInElevator.Count >= 2 && elevatorRoutine == null)
            {
                elevatorRoutine = StartCoroutine(StartElevatorSequence());
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (isElevatorActivated) return;

        if (other.TryGetComponent(out PlayerController3D player))
        {
            playersInElevator.Remove(player.gameObject);
        }
    }

    private IEnumerator StartElevatorSequence()
    {
        isElevatorActivated = true;

        AudioManager audio = AudioManager.instancia;
        if (audio != null)
        {
            if (audio.elevadorSubindo != null)
            {
                audio.PlaySFX(audio.elevadorSubindo);
            }
            if (audio.musicaElevador != null)
            {
                audio.PlayMusic(audio.musicaElevador);
            }
        }

        ScoreManager score = ScoreManager.Instance;
        if (score != null)
        {
            score.CalculateAndAddSpeedBonus();
        }

        foreach (GameObject playerObj in playersInElevator)
        {
            if (playerObj == null) continue;

            Renderer[] renderers = playerObj.GetComponentsInChildren<Renderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = false;
                }
            }

            if (playerObj.TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = true;
            }
        }

        float timer = 0f;
        Transform targetTransform = elevatorPlatform != null ? elevatorPlatform : selfTransform;
        float duration = Mathf.Max(0f, riseDuration);

        while (timer < duration)
        {
            targetTransform.Translate(Vector3.up * (riseSpeed * Time.deltaTime), Space.World);
            timer += Time.deltaTime;
            yield return null;
        }

        if (score != null)
        {
            score.ResetLevelTimer();
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

        elevatorRoutine = null;
    }

    void OnDisable()
    {
        if (elevatorRoutine != null)
        {
            StopCoroutine(elevatorRoutine);
            elevatorRoutine = null;
        }
    }
}