using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelExitElevator : MonoBehaviour
{
    [Header("Configuração do Elevador")]
    public float riseSpeed = 3f;              // Velocidade de subida do elevador
    public float riseDuration = 2f;           // Tempo (em segundos) que o elevador fica subindo
    public Transform elevatorPlatform;        // Objeto visual/físico do elevador que vai subir

    private HashSet<GameObject> playersInElevator = new HashSet<GameObject>();
    private bool isElevatorActivated = false;

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

    private void OnTriggerExit(Collider other)
    {
        if (isElevatorActivated) return;

        PlayerController3D player = other.GetComponent<PlayerController3D>();
        if (player != null)
        {
            playersInElevator.Remove(player.gameObject);
        }
    }

    private IEnumerator StartElevatorSequence()
    {
        isElevatorActivated = true;

        // CALCULA O BÔNUS DE VELOCIDADE
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.CalculateAndAddSpeedBonus();
        }

        // 1. Esconde o modelo visual de ambos os jogadores
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

        // 2. Faz o elevador subir
        float timer = 0f;
        Transform targetTransform = elevatorPlatform != null ? elevatorPlatform : transform;

        while (timer < riseDuration)
        {
            targetTransform.Translate(Vector3.up * riseSpeed * Time.deltaTime, Space.World);
            timer += Time.deltaTime;
            yield return null;
        }

        // 3. Reseta o cronômetro para a próxima fase e carrega a nova cena
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