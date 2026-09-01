using UnityEngine;

// Ferramenta que dispara rolos de papel higienico ao apertar o botao de acao
public class ToiletPaperLauncherTool : BaseTool
{
    [Header("Configuracoes do Lancador")]
    public GameObject paperPrefab; // Prefab do rolo de papel
    public Transform firePoint;     // Ponto de saida do disparo (bico da arma)
    public float fireRate = 0.5f;   // Tempo de espera entre disparos

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
        if (paperPrefab != null && firePoint != null)
        {
            Instantiate(paperPrefab, firePoint.position, firePoint.rotation);
        }
    }
}