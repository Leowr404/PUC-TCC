using UnityEngine;

// Ferramenta que dispara clipes de papel ao apertar o botao de acao
public class PaperClipLauncherTool : BaseTool
{
    [Header("Configuracoes do Lancador de Clipes")]
    public GameObject clipPrefab;   // Prefab do clipe de papel
    public Transform firePoint;     // Ponto de saida do disparo (bico da arma)
    public float fireRate = 0.3f;   // Cadência de tiro (ajuste para ser mais rápido ou lento)

    private float nextFireTime = 0f;
    private Transform selfTransform;

    void Awake()
    {
        selfTransform = transform;
    }

    public override void OnActionDown(PlayerController3D player)
    {
        if (Time.time >= nextFireTime)
        {
            Shoot();
            nextFireTime = Time.time + fireRate;
        }
    }

    void Shoot()
    {
        Transform spawn = firePoint != null ? firePoint : selfTransform;
        if (clipPrefab == null || spawn == null) return;

        Instantiate(clipPrefab, spawn.position, spawn.rotation);

        AudioManager audio = AudioManager.instancia;
        if (audio != null && audio.atirarGrampo != null)
        {
            audio.PlaySFX(audio.atirarGrampo);
        }
    }
}