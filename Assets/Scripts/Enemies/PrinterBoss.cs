using System.Collections;
using UnityEngine;

// Controla o comportamento, estados, ataques, mecânica do puzzle, animação e efeitos sonoros do PrinterBoss
public class PrinterBoss : MonoBehaviour
{
    // Estados possíveis da máquina de estados do boss
    public enum BossState { Patrol, Shooting, Reloading, Dash, StunnedByClip, Defeated }

    [Header("Estado Atual")]
    public BossState currentState = BossState.Patrol;

    [Header("Modelo 3D (Opcional)")]
    public Transform modelMesh; // Objeto visual filho que gira e exibe a animação
    [Tooltip("Ajuste fino de rotação em Y (ex: 90 ou -90) caso o modelo ande de lado.")]
    public float facingYawOffset = 90f;

    [Header("Animação")]
    public Animator animator; // Animator vinculado ao modelo do boss

    [Header("Movimentacao (Patrulha)")]
    public Transform leftPoint;
    public Transform rightPoint;
    public float moveSpeed = 3f;
    public bool startFacingRight = false;
    public float walkSoundInterval = 0.45f; // Intervalo mínimo entre sons de passo na patrulha
    public float maxSoundDistance = 20f; // Distância máxima em que os players ouvem o som de patrulha
    private bool movingRight;
    private float nextWalkSoundTime; // Timestamp que controla quando o próximo passo pode tocar

    [Header("Ataque Especial (Investida / Dash)")]
    public float dashSpeed = 10f;
    public float dashStunTime = 1.5f;
    public float upwardDashForce = 15f;
    private Vector3 dashTargetPosition; // Posição alvo capturada no início do dash

    [Header("Mecânica do Puzzle (Grampo e Sensor)")]
    public Transform paperTrayPoint; // Referência da gaveta de papel vulnerável ao grampo
    public Transform weakPointSensor; // Referência do sensor fraco atingido pelo laser
    public float reloadWindowDuration = 1f; // Duração da janela em que o grampo consegue acertar a gaveta
    private bool isRewindingPaper = false; // Verdadeiro apenas durante a janela vulnerável da recarga

    [Header("Configurações do Giro e Stun (Grampo)")]
    public float spinDuration = 2f;
    public float spinSpeed = 360f;
    public float vulnerableDuration = 2f; // Tempo parado e vulnerável ao laser após parar de girar

    [Header("Dano por Colisao Direta (Toque Normal)")]
    public float contactKnockback = 8f; // Força de empurrão em colisão normal (fora do dash)
    public float contactStunDuration = 1f;

    [Header("Visao e Deteccao por Area (Box)")]
    public Transform sightOrigin; // Ponto de origem da área de visão (usa o próprio transform se vazio)
    public Vector3 sightBoxSize = new Vector3(8f, 3f, 4f); // Dimensões da caixa de detecção (X, Y, Z)
    public Vector3 sightOffset = Vector3.zero; // Deslocamento adicional da caixa de visão
    public LayerMask playerLayer; // Layer usada para identificar os jogadores
    public LayerMask obstacleLayer;

    [Header("Ataque de Papel")]
    public GameObject paperProjectilePrefab;
    public Transform firePoint;
    public int papersPerBurst = 5;
    public float fireRate = 0.2f;
    public float projectileSpeed = 12f;

    [Header("Recarga")]
    public float reloadTime = 3f;
    private int burstCount = 0; // Conta quantas rajadas foram feitas até o próximo dash

    private Collider bossCollider;
    private Rigidbody rb;
    private AudioSource reloadAudioSource; // Fonte dedicada ao som de recarga para evitar sobreposição/vazamento
    private AudioSource shootAudioSource; // Fonte dedicada ao tiro de papel para cortar o disparo anterior
    private PlayerController3D targetPlayerForDash; // Guarda o jogador que acertou o grampo

    void Start()
    {
        bossCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();

        // Trava rotação e mantém cinemático para movimentação controlada por código/corrotinas
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.isKinematic = true;
        }

        // Cria e configura o AudioSource exclusivo da recarga no grupo de efeitos do mixer
        reloadAudioSource = gameObject.AddComponent<AudioSource>();
        reloadAudioSource.playOnAwake = false;
        reloadAudioSource.spatialBlend = 0f;
        ConfigureEffectMixerGroup(reloadAudioSource);

        // Cria e configura o AudioSource exclusivo do tiro para cortar o som anterior ao atirar de novo
        shootAudioSource = gameObject.AddComponent<AudioSource>();
        shootAudioSource.playOnAwake = false;
        shootAudioSource.spatialBlend = 0f;
        ConfigureEffectMixerGroup(shootAudioSource);

        movingRight = startFacingRight;
        ApplyFacingVisual();
    }

    // Vincula um AudioSource local ao grupo "Efeitos" do MixerPrincipal, se disponível
    private void ConfigureEffectMixerGroup(AudioSource source)
    {
        if (source == null || AudioManager.instancia == null) return;

        var mixer = Resources.Load<UnityEngine.Audio.AudioMixer>("MixerPrincipal");
        var grupos = mixer != null ? mixer.FindMatchingGroups("Efeitos") : null;
        if (grupos != null && grupos.Length > 0)
        {
            source.outputAudioMixerGroup = grupos[0];
        }
    }

    void Update()
    {
        UpdateAnimationState();

        // Executa apenas a lógica correspondente ao estado atual do boss
        switch (currentState)
        {
            case BossState.Patrol:
                PatrolMovement();
                CheckForPlayerArea();
                break;

            case BossState.Shooting:
                break;

            case BossState.Reloading:
                break;

            case BossState.Dash:
                break;

            case BossState.StunnedByClip:
                break;

            case BossState.Defeated:
                break;
        }
    }

    // Mantém a animação de caminhada rodando apenas quando o boss está em patrulha
    private void UpdateAnimationState()
    {
        if (animator == null) return;
        animator.speed = (currentState == BossState.Patrol) ? 1f : 0f;
    }

    // Verifica se há ao menos um player dentro do raio sonoro configurado
    private bool IsAnyPlayerWithinSoundRange()
    {
        Collider[] playersInRange = Physics.OverlapSphere(transform.position, maxSoundDistance, playerLayer);
        return playersInRange.Length > 0;
    }

    // Toca o som de recarga garantindo que não haja sobreposição com uma recarga anterior
    private void PlayReloadSound()
    {
        if (AudioManager.instancia == null || AudioManager.instancia.bossRecarregando == null) return;

        StopReloadSound();
        reloadAudioSource.clip = AudioManager.instancia.bossRecarregando;
        reloadAudioSource.volume = AudioManager.instancia.GetSavedVolume(AudioManager.SFXVolumeKey);
        reloadAudioSource.Play();
    }

    // Interrompe imediatamente o som de recarga caso esteja tocando
    private void StopReloadSound()
    {
        if (reloadAudioSource != null && reloadAudioSource.isPlaying)
        {
            reloadAudioSource.Stop();
        }
    }

    // Toca o som do tiro de papel cortando o clipe anterior caso ainda esteja ativo
    private void PlayShootSound()
    {
        if (AudioManager.instancia == null || AudioManager.instancia.bossAtirando == null) return;

        if (shootAudioSource.isPlaying)
        {
            shootAudioSource.Stop();
        }

        shootAudioSource.clip = AudioManager.instancia.bossAtirando;
        shootAudioSource.volume = AudioManager.instancia.GetSavedVolume(AudioManager.SFXVolumeKey);
        shootAudioSource.Play();
    }

    // Move o boss entre leftPoint e rightPoint e dispara o som de passo respeitando intervalo e distância
    void PatrolMovement()
    {
        if (leftPoint == null || rightPoint == null) return;

        Transform targetPoint = movingRight ? rightPoint : leftPoint;

        Vector3 targetPosition = new Vector3(targetPoint.position.x, transform.position.y, targetPoint.position.z);
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

        // Só toca o passo quando o intervalo e a duração do clipe já passaram e há player próximo
        if (Time.time >= nextWalkSoundTime && IsAnyPlayerWithinSoundRange())
        {
            AudioClip walkClip = AudioManager.instancia != null ? AudioManager.instancia.bossAndando : null;
            if (walkClip != null)
            {
                AudioManager.instancia.PlaySFX(walkClip);
                float clipDuration = walkClip.length;
                nextWalkSoundTime = Time.time + Mathf.Max(walkSoundInterval, clipDuration);
            }
        }

        // Inverte o sentido ao chegar próximo do ponto limite
        if (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                             new Vector3(targetPoint.position.x, 0, targetPoint.position.z)) < 0.2f)
        {
            movingRight = !movingRight;
            ApplyFacingVisual();
        }
    }

    // Rotaciona o modelo visual no eixo Y somando o offset de alinhamento do modelo
    void ApplyFacingVisual()
    {
        Transform targetTransform = modelMesh != null ? modelMesh : transform;
        float baseYaw = movingRight ? 90f : -90f;
        float finalYaw = baseYaw + facingYawOffset;

        Vector3 currentEuler = targetTransform.localEulerAngles;
        targetTransform.localEulerAngles = new Vector3(currentEuler.x, finalYaw, currentEuler.z);
    }

    // Retorna o vetor de direção frontal atual do boss
    private Vector3 GetFacingDirection()
    {
        return movingRight ? Vector3.right : Vector3.left;
    }

    // Calcula o centro da caixa de visão à frente do boss
    private Vector3 GetSightBoxCenter()
    {
        Vector3 baseOrigin = sightOrigin != null ? sightOrigin.position : transform.position;
        Vector3 forwardDir = GetFacingDirection();

        Vector3 center = baseOrigin + forwardDir * (sightBoxSize.x / 2f);
        center += new Vector3(sightOffset.x * (movingRight ? 1f : -1f), sightOffset.y, sightOffset.z);

        return center;
    }

    // Detecta jogadores na área de visão sem obstáculos e escolhe entre rajada de papel ou dash
    void CheckForPlayerArea()
    {
        Vector3 center = GetSightBoxCenter();
        Vector3 halfExtents = sightBoxSize / 2f;

        Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, Quaternion.identity, playerLayer);

        foreach (Collider hit in hitColliders)
        {
            if (bossCollider != null && hit == bossCollider) continue;

            Vector3 origin = sightOrigin != null ? sightOrigin.position : transform.position;
            Vector3 dirToPlayer = (hit.transform.position - origin).normalized;
            float distToPlayer = Vector3.Distance(origin, hit.transform.position);

            if (!Physics.Raycast(origin, dirToPlayer, distToPlayer, obstacleLayer))
            {
                Debug.Log("JOGADOR DETECTADO NA ÁREA DE VISÃO! Iniciando ataque.");

                if (burstCount >= 2)
                {
                    dashTargetPosition = new Vector3(hit.transform.position.x, transform.position.y, hit.transform.position.z);
                    StartCoroutine(DoDashAttack());
                }
                else
                {
                    StartCoroutine(ShootBurstRoutine());
                }
                break;
            }
        }
    }

    // Executa uma rajada de tiros de papel e abre a janela vulnerável de recarga/rebobinagem
    IEnumerator ShootBurstRoutine()
    {
        StopReloadSound();
        currentState = BossState.Shooting;

        for (int i = 0; i < papersPerBurst; i++)
        {
            ShootPaper();
            yield return new WaitForSeconds(fireRate);
        }

        currentState = BossState.Reloading;
        burstCount++;
        PlayReloadSound();

        // Abre a janela em que acertar a gaveta com o grampo atordoa o boss
        isRewindingPaper = true;
        Debug.Log("Impressora começou a rebobinar o papel! Gaveta vulnerável.");

        yield return new WaitForSeconds(reloadWindowDuration);
        isRewindingPaper = false;

        // Aguarda o restante do tempo de recarga e encerra o áudio de recarga ao sair do estado
        yield return new WaitForSeconds(Mathf.Max(0f, reloadTime - reloadWindowDuration));

        StopReloadSound();

        if (currentState == BossState.Reloading)
        {
            currentState = BossState.Patrol;
        }
    }

    // Instancia um projétil de papel na direção em que o boss está olhando e toca o som do tiro
    void ShootPaper()
    {
        if (paperProjectilePrefab == null) return;

        Transform spawnPoint = firePoint != null ? firePoint : transform;
        Vector3 shootDirection = GetFacingDirection();

        GameObject paper = Instantiate(paperProjectilePrefab, spawnPoint.position, Quaternion.LookRotation(shootDirection));
        Rigidbody paperRb = paper.GetComponent<Rigidbody>();

        if (paperRb != null)
        {
            paperRb.linearVelocity = shootDirection * projectileSpeed;
        }

        PlayShootSound();
    }

    // Chamado quando o projétil de grampo acerta a gaveta durante a janela de rebobinagem
    public void OnHitByPaperClip(PlayerController3D attackingPlayer)
    {
        if (currentState == BossState.Reloading && isRewindingPaper)
        {
            Debug.Log("Grampo acertou a gaveta! Boss travou, perdeu colisão e começou a girar.");
            isRewindingPaper = false;
            targetPlayerForDash = attackingPlayer;

            StopReloadSound();
            AudioManager.instancia?.PlaySFX(AudioManager.instancia.bossDanoGrampo);

            Feedback.TremerCamera(0.3f, 0.3f);
            Feedback.Vibrar(attackingPlayer, 0.5f, 0.2f);

            StopAllCoroutines();
            StartCoroutine(ClipHitStunRoutine());
        }
    }

    // Chamado quando o laser atinge o ponto fraco enquanto o boss está vulnerável
    public void OnHitByLaserWeakPoint()
    {
        if (currentState == BossState.StunnedByClip)
        {
            Debug.Log("Ponto fraco atingido pelo Laser! A impressora foi destruída!");
            currentState = BossState.Defeated;
            StopReloadSound();
            Feedback.TremerCamera(0.6f, 0.6f);
            Feedback.VibrarTodos(1f, 0.5f);
            Destroy(gameObject);
        }
    }

    // Executa a investida horizontal até o ponto alvo e retorna à patrulha após o tempo de pausa
    IEnumerator DoDashAttack()
    {
        StopReloadSound();
        currentState = BossState.Dash;
        burstCount = 0;

        while (Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z),
                                new Vector3(dashTargetPosition.x, 0, dashTargetPosition.z)) > 0.3f)
        {
            transform.position = Vector3.MoveTowards(transform.position, dashTargetPosition, dashSpeed * Time.deltaTime);
            yield return null;
        }

        yield return new WaitForSeconds(dashStunTime);

        currentState = BossState.Patrol;
    }

    // Desativa colisão, gira o boss e deixa o ponto fraco vulnerável ao laser por um tempo limitado
    IEnumerator ClipHitStunRoutine()
    {
        currentState = BossState.StunnedByClip;

        if (bossCollider != null)
        {
            bossCollider.enabled = false;
        }

        float currentSpinTime = 0f;
        while (currentSpinTime < spinDuration)
        {
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);
            currentSpinTime += Time.deltaTime;
            yield return null;
        }

        Debug.Log("Boss parou de girar. Vulnerável por 2 segundos!");
        float stopTimer = vulnerableDuration;
        while (stopTimer > 0f)
        {
            if (currentState == BossState.Defeated) yield break;
            stopTimer -= Time.deltaTime;
            yield return null;
        }

        // Restaura colisão e retorna à patrulha caso não tenha sido derrotado pelo laser
        if (bossCollider != null)
        {
            bossCollider.enabled = true;
        }
        currentState = BossState.Patrol;
        burstCount = 0;
    }

    // Aplica dano, repulsão e solta a ferramenta do player ao colidir com o boss
    private void OnCollisionEnter(Collision collision)
    {
        PlayerController3D player = collision.gameObject.GetComponent<PlayerController3D>();

        if (player != null)
        {
            Vector3 hitDirection = (player.transform.position - transform.position).normalized;

            if (currentState == BossState.Dash)
            {
                hitDirection.x *= 0.3f;
                hitDirection.z *= 0.3f;
                hitDirection.y = 1.5f;

                player.TakeDamage(hitDirection.normalized * upwardDashForce, contactStunDuration);
            }
            else
            {
                hitDirection.y = 0.4f;
                player.TakeDamage(hitDirection * contactKnockback, contactStunDuration);
            }

            player.DropCurrentTool();
        }
    }

    // Garante que o áudio de recarga seja interrompido se o objeto for destruído
    void OnDestroy()
    {
        StopReloadSound();
    }

    // Desenha a caixa de visão no editor quando o boss está selecionado
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 center = GetSightBoxCenter();
        Gizmos.DrawWireCube(center, sightBoxSize);
    }
}