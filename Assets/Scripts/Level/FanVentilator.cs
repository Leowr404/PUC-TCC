using System.Collections;
using UnityEngine;

public class FanVentilator : MonoBehaviour
{
    [Header("Configuracoes do Vento")]
    public bool alwaysOn = false;           // Se verdadeiro, o vento fica ligado o todo o tempo
    public float activeDuration = 2.0f;     // Tempo ligado ao ser ativado pelo botão (2 segundos)
    public float windForce = 20f;           // Força para lançar o inimigo para fora
    public AudioSource windAudioSource;     // Som do vento soprando

    [Header("Rotacao da Helice")]
    public Transform propellerMesh;         // O objeto visual da hélice que vai rodar (se for este próprio objeto, deixe vazio)
    public float rotationSpeed = 720f;      // Velocidade de rotação em graus por segundo

    [Header("Dano ao Jogador")]
    public float playerKnockbackForce = 5f; // Pequeno empurrão caso o jogador encoste

    private bool isActive = false;
    private Collider windTriggerCollider;

    void Start()
    {
        windTriggerCollider = GetComponent<Collider>();
        if (windTriggerCollider != null)
        {
            windTriggerCollider.isTrigger = true;
            windTriggerCollider.enabled = alwaysOn;
        }

        isActive = alwaysOn;
        if (alwaysOn && windAudioSource != null)
        {
            windAudioSource.Play();
        }
    }

    void Update()
    {
        // Se o ventilador estiver ligado, faz a hélice girar no eixo verde (Vector3.up)
        if (isActive)
        {
            Transform targetRot = propellerMesh != null ? propellerMesh : transform;
            targetRot.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }

    // Chamado pelo botão para ligar o ventilador por 2 segundos
    public void ActivateFan()
    {
        if (alwaysOn || isActive) return;
        StartCoroutine(FanRoutine());
    }

    IEnumerator FanRoutine()
    {
        isActive = true;
        if (windTriggerCollider != null) windTriggerCollider.enabled = true;
        if (windAudioSource != null) windAudioSource.Play();

        // Fica ligado pelo tempo estipulado
        yield return new WaitForSeconds(activeDuration);

        if (!alwaysOn)
        {
            isActive = false;
            if (windTriggerCollider != null) windTriggerCollider.enabled = false;
            if (windAudioSource != null) windAudioSource.Stop();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isActive) return;

        // 1. Se encostar no Inimigo
        PatrolEnemy enemy = other.GetComponent<PatrolEnemy>();
        if (enemy != null)
        {
            Vector3 blowDirection = transform.up;
            enemy.GetBlownAway(blowDirection * windForce);
            return;
        }

        // 2. Se encostar no Jogador (aplica apenas o knockback sem perder a ferramenta)
        PlayerController3D player = other.GetComponent<PlayerController3D>();
        if (player != null)
        {
            Vector3 pushDirection = transform.up + Vector3.up * 0.2f;
            player.TakeWindKnockback(pushDirection * playerKnockbackForce, 0.3f);
        }
    }
}