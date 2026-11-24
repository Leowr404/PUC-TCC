using System.Collections;
using UnityEngine;

// Controla o ventilador que sopra inimigos e empurra jogadores quando ativo
public class FanVentilator : MonoBehaviour
{
    [Header("Configuracoes do Vento")]
    public bool alwaysOn = false;           // Se verdadeiro, o vento fica ligado o todo o tempo
    public float activeDuration = 2.0f;     // Tempo ligado ao ser ativado pelo botão (2 segundos)
    public float windForce = 20f;           // Força para lançar o inimigo para fora
    public AudioSource windAudioSource;     // Som do vento soprando

    [Header("Rotacao da Helice")]
    public Transform propellerMesh;         // Objeto visual da hélice que vai rodar (se vazio, usa este objeto)
    public float rotationSpeed = 720f;      // Velocidade de rotação em graus por segundo

    [Header("Dano ao Jogador")]
    public float playerKnockbackForce = 5f; // Pequeno empurrão caso o jogador encoste

    private bool isActive = false;
    private Collider windTriggerCollider;
    private Transform selfTransform;
    private Transform propellerTransform;
    private Coroutine activeRoutine;
    private WaitForSeconds cachedDurationWait;
    private float cachedDurationValue = -1f;

    void Start()
    {
        selfTransform = transform;
        propellerTransform = propellerMesh != null ? propellerMesh : selfTransform;
        windTriggerCollider = GetComponent<Collider>();

        if (windTriggerCollider != null)
        {
            windTriggerCollider.isTrigger = true;
            windTriggerCollider.enabled = alwaysOn;
        }

        isActive = alwaysOn;
        if (alwaysOn && windAudioSource != null && !windAudioSource.isPlaying)
        {
            windAudioSource.Play();
        }
    }

    void Update()
    {
        // Gira a hélice apenas quando o ventilador está ligado
        if (isActive)
        {
            propellerTransform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }

    // Chamado pelo botão para ligar o ventilador temporariamente
    public void ActivateFan()
    {
        if (alwaysOn || isActive) return;

        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
        }
        activeRoutine = StartCoroutine(FanRoutine());
    }

    private WaitForSeconds GetDurationWait()
    {
        float duration = Mathf.Max(0f, activeDuration);
        if (cachedDurationWait == null || !Mathf.Approximately(cachedDurationValue, duration))
        {
            cachedDurationValue = duration;
            cachedDurationWait = new WaitForSeconds(duration);
        }
        return cachedDurationWait;
    }

    IEnumerator FanRoutine()
    {
        isActive = true;
        if (windTriggerCollider != null) windTriggerCollider.enabled = true;
        if (windAudioSource != null && !windAudioSource.isPlaying) windAudioSource.Play();

        if (activeDuration > 0f)
        {
            yield return GetDurationWait();
        }

        if (!alwaysOn)
        {
            isActive = false;
            if (windTriggerCollider != null) windTriggerCollider.enabled = false;
            if (windAudioSource != null && windAudioSource.isPlaying) windAudioSource.Stop();
        }

        activeRoutine = null;
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isActive) return;

        // 1. Se encostar no Inimigo
        if (other.TryGetComponent(out PatrolEnemy enemy))
        {
            enemy.GetBlownAway(selfTransform.up * windForce);
            return;
        }

        // 2. Se encostar no Jogador (aplica apenas o knockback sem perder a ferramenta)
        if (other.TryGetComponent(out PlayerController3D player))
        {
            Vector3 pushDirection = selfTransform.up + Vector3.up * 0.2f;
            player.TakeWindKnockback(pushDirection * playerKnockbackForce, 0.3f);
        }
    }

    void OnDisable()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }
    }
}