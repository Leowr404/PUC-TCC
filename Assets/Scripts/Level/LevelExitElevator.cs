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

            // Se ambos os jogadores (2) estiverem dentro do elevador
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

        // 2. Faz a plataforma do elevador subir durante a duração configurada
        float timer = 0f;
        Transform targetTransform = elevatorPlatform != null ? elevatorPlatform : transform;

        while (timer < riseDuration)
        {
            targetTransform.Translate(Vector3.up * riseSpeed * Time.deltaTime, Space.World);
            timer += Time.deltaTime;
            yield return null;
        }

        // 3. Carrega automaticamente o próximo índice de cena configurado no Build Settings
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