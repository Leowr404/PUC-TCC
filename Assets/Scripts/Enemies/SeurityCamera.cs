using UnityEngine;
using UnityEngine.SceneManagement;

public class SecurityCamera : MonoBehaviour
{
    [Header("Configurações do Ciclo")]
    public float activeTime = 3f;      // Tempo em segundos que a câmera fica LIGADA
    public float inactiveTime = 2f;    // Tempo em segundos que a câmera fica DESLIGADA
    public bool isActive = true;

    [Header("Configurações de Visão")]
    public float range = 15f;           // Alcance máximo do campo de visão
    public float visionAngle = 45f;     // Ângulo total da área de varredura
    public int rayCount = 10;           // Quantidade de raios para desenhar o cone
    public LayerMask targetMask;        // Layer correspondente aos Jogadores (Player)
    public LayerMask obstacleMask;      // Layer correspondente aos Obstáculos (Wall / Default)

    [Header("Componentes Visuais")]
    public LineRenderer visionCone;     // Referência ao Line Renderer
    public Material activeMaterial;     // Material quando a câmera estiver vigiando
    public Material inactiveMaterial;   // Material quando a câmera estiver desligada

    private float timer;

    void Start()
    {
        timer = activeTime;

        if (visionCone != null)
        {
            // Ativa o fechamento do contorno automaticamente
            visionCone.loop = true;
            visionCone.positionCount = rayCount + 2;
        }

        UpdateMaterial();
    }

    void Update()
    {
        HandleCycle();

        if (isActive)
        {
            DetectPlayer();
            DrawVisionCone();
        }
        else
        {
            if (visionCone != null)
            {
                visionCone.enabled = false;
            }
        }
    }

    void HandleCycle()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            isActive = !isActive;
            timer = isActive ? activeTime : inactiveTime;
            UpdateMaterial();
        }
    }

    void UpdateMaterial()
    {
        if (visionCone == null) return;

        visionCone.enabled = isActive;
        visionCone.material = isActive ? activeMaterial : inactiveMaterial;
    }

    void DetectPlayer()
    {
        // Encontra todos os colliders de jogadores dentro do raio de alcance
        Collider[] targetsInRadius = Physics.OverlapSphere(transform.position, range, targetMask);

        foreach (Collider target in targetsInRadius)
        {
            Vector3 dirToTarget = (target.transform.position - transform.position).normalized;

            // Verifica se o jogador está dentro do ângulo de visão
            if (Vector3.Angle(transform.forward, dirToTarget) < visionAngle / 2f)
            {
                float distToTarget = Vector3.Distance(transform.position, target.transform.position);

                // Dispara o Hitscan. Se NÃO atingir uma parede antes do player, o nível reseta
                if (!Physics.Raycast(transform.position, dirToTarget, distToTarget, obstacleMask))
                {
                    ResetLevel();
                }
            }
        }
    }

    void DrawVisionCone()
    {
        if (visionCone == null) return;

        visionCone.enabled = true;

        // Garante a quantidade correta de vértices (Origem + Pontas da varredura)
        visionCone.positionCount = rayCount + 2;

        // Ponto inicial do cone na posição da câmera
        visionCone.SetPosition(0, transform.position);

        float currentAngle = -visionAngle / 2f;
        float angleStep = visionAngle / rayCount;

        for (int i = 0; i <= rayCount; i++)
        {
            Vector3 dir = Quaternion.Euler(0, currentAngle, 0) * transform.forward;
            RaycastHit hit;

            Vector3 hitPoint;
            // Se o raio bater em uma parede (ObstacleMask), para na parede
            if (Physics.Raycast(transform.position, dir, out hit, range, obstacleMask))
            {
                hitPoint = hit.point;
            }
            else
            {
                hitPoint = transform.position + dir * range;
            }

            visionCone.SetPosition(i + 1, hitPoint);
            currentAngle += angleStep;
        }
    }

    void ResetLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void OnDrawGizmosSelected()
    {
        // Desenha a esfera de alcance no editor para facilitar o ajuste do alcance (Range)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, range);
    }
}