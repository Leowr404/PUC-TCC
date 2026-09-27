using UnityEngine;

// Ferramenta que dispara clipes de papel ao apertar o botao de acao
public class PaperClipLauncherTool : BaseTool
{
    [Header("Configuracoes do Lancador de Clipes")]
    public GameObject clipPrefab;   // Prefab do clipe de papel
    public Transform firePoint;     // Ponto de saida do disparo (bico da arma)
    public float fireRate = 0.3f;   // Cadência de tiro (ajuste para ser mais rápido ou lento)

    private float nextFireTime = 0f;

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
        if (clipPrefab != null && firePoint != null)
        {
            Instantiate(clipPrefab, firePoint.position, firePoint.rotation);
        }
    }
}