using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

// Controla o comportamento, estados, ataques, mecânica do puzzle, animação e efeitos sonoros do PrinterBoss
public class PrinterBoss : MonoBehaviour
{
    public enum BossState { Patrol, Shooting, Reloading, Dash, StunnedByClip, Defeated }

    [Header("Estado Atual")]
    public BossState currentState = BossState.Patrol;

    [Header("Modelo 3D (Opcional)")]
    public Transform modelMesh;
    [Tooltip("Ajuste fino de rotação em Y (ex: 90 ou -90) caso o modelo ande de lado.")]
    public float facingYawOffset = 90f;

    [Header("Animação")]
    public Animator animator;

    [Header("Movimentacao (Patrulha)")]
    public Transform leftPoint;
    public Transform rightPoint;
    public float moveSpeed = 3f;
    public bool startFacingRight = false;
    public float walkSoundInterval = 0.45f;
    public float maxSoundDistance = 20f;
    private bool movingRight;
    private float nextWalkSoundTime;

    [Header("Ataque Especial (Investida / Dash)")]
    public float dashSpeed = 10f;
    public float dashStunTime = 1.5f;
    public float upwardDashForce = 15f;
    private Vector3 dashTargetPosition;

    [Header("Mecânica do Puzzle (Grampo e Sensor)")]
    public Transform paperTrayPoint;
    public Transform weakPointSensor;
    public float reloadWindowDuration = 1f;
    private bool isRewindingPaper = false;

    [Header("Configurações do Giro e Stun (Grampo)")]
    public float spinDuration = 2f;
    public float spinSpeed = 360f;
    public float vulnerableDuration = 2f;

    [Header("Dano por Colisao Direta (Toque Normal)")]
    public float contactKnockback = 8f;
    public float contactStunDuration = 1f;

    [Header("Visao e Deteccao por Area (Box)")]
    public Transform sightOrigin;
    public Vector3 sightBoxSize = new Vector3(8f, 3f, 4f);
    public Vector3 sightOffset = Vector3.zero;
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;

    [Header("Ataque de Papel")]
    public GameObject paperProjectilePrefab;
    public Transform firePoint;
    public int papersPerBurst = 5;
    public float fireRate = 0.2f;
    public float projectileSpeed = 12f;

    [Header("Recarga")]
    public float reloadTime = 3f;
    private int burstCount = 0;

    private Transform selfTransform;
    private Collider bossCollider;
    private Rigidbody rb;
    private AudioSource reloadAudioSource;
    private AudioSource shootAudioSource;
    private AudioMixerGroup effectMixerGroup;
    private PlayerController3D targetPlayerForDash;

    // Buffers estáticos reutilizáveis para evitar alocação em consultas físicas
    private static readonly Collider[] soundRangeBuffer = new Collider[4];
    private static readonly Collider[] sightBoxBuffer = new Collider[4];
    private static readonly RaycastHit[] obstacleHitBuffer = new RaycastHit[1];

    void Start()
    {
        selfTransform = transform;
        bossCollider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeRotation;
            rb.isKinematic = true;
        }

        CacheEffectMixerGroup();

        reloadAudioSource = CreateDedicatedAudioSource("ReloadAudio", reloadAudioSource);
        shootAudioSource = CreateDedicatedAudioSource("ShootAudio", shootAudioSource);

        movingRight = startFacingRight;
        ApplyFacingVisual();
    }

    // Busca e armazena em cache o grupo "Efeitos" apenas uma vez no Start
    private void CacheEffectMixerGroup()
    {
        var mixer = Resources.Load<AudioMixer>("MixerPrincipal");
        if (mixer == null) return;

        var grupos = mixer.FindMatchingGroups("Efeitos");
        if (grupos != null && grupos.Length > 0)
        {
            effectMixerGroup = grupos[0];
        }
    }

    private AudioSource CreateDedicatedAudioSource(string sourceName, AudioSource existing)
    {
        AudioSource source = existing != null ? existing : gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 0f;
        if (effectMixerGroup != null && source.outputAudioMixerGroup == null)
        {
            source.outputAudioMixerGroup = effectMixerGroup;
        }
        return source;
    }

    void Update()
    {
        UpdateAnimationState();

        if (currentState == BossState.Patrol)
        {
            PatrolMovement();
            CheckForPlayerArea();
        }
    }

    private void UpdateAnimationState()
    {
        if (animator == null) return;
        float targetSpeed = (currentState == BossState.Patrol) ? 1f : 0f;
        if (!Mathf.Approximately(animator.speed, targetSpeed))
        {
            animator.speed = targetSpeed;
        }
    }

    private bool IsAnyPlayerWithinSoundRange()
    {
        int count = Physics.OverlapSphereNonAlloc(selfTransform.position, maxSoundDistance, soundRangeBuffer, playerLayer, QueryTriggerInteraction.Ignore);
        return count > 0;
    }

    private void PlayReloadSound()
    {
        AudioManager audio = AudioManager.instancia;
        if (audio == null || audio.bossRecarregando == null) return;

        StopReloadSound();
        reloadAudioSource.clip = audio.bossRecarregando;
        reloadAudioSource.volume = audio.GetSavedVolume(AudioManager.SFXVolumeKey);
        reloadAudioSource.Play();
    }

    private void StopReloadSound()
    {
        if (reloadAudioSource != null && reloadAudioSource.isPlaying)
        {
            reloadAudioSource.Stop();
        }
    }

    private void PlayShootSound()
    {
        AudioManager audio = AudioManager.instancia;
        if (audio == null || audio.bossAtirando == null) return;

        if (shootAudioSource.isPlaying)
        {
            shootAudioSource.Stop();
        }

        shootAudioSource.clip = audio.bossAtirando;
        shootAudioSource.volume = audio.GetSavedVolume(AudioManager.SFXVolumeKey);
        shootAudioSource.Play();
    }

    void PatrolMovement()
    {
        if (leftPoint == null || rightPoint == null) return;

        Transform targetPoint = movingRight ? rightPoint : leftPoint;
        Vector3 pos = selfTransform.position;
        Vector3 targetPosition = new Vector3(targetPoint.position.x, pos.y, targetPoint.position.z);

        selfTransform.position = Vector3.MoveTowards(pos, targetPosition, moveSpeed * Time.deltaTime);

        if (Time.time >= nextWalkSoundTime && IsAnyPlayerWithinSoundRange())
        {
            AudioManager audio = AudioManager.instancia;
            AudioClip walkClip = audio != null ? audio.bossAndando : null;
            if (walkClip != null)
            {
                audio.PlaySFX(walkClip);
                nextWalkSoundTime = Time.time + Mathf.Max(walkSoundInterval, walkClip.length);
            }
        }

        float dx = Mathf.Abs(selfTransform.position.x - targetPoint.position.x);
        float dz = Mathf.Abs(selfTransform.position.z - targetPoint.position.z);
        if (dx < 0.2f && dz < 0.2f)
        {
            movingRight = !movingRight;
            ApplyFacingVisual();
        }
    }

    void ApplyFacingVisual()
    {
        Transform targetTransform = modelMesh != null ? modelMesh : selfTransform;
        float baseYaw = movingRight ? 90f : -90f;
        float finalYaw = baseYaw + facingYawOffset;

        Vector3 currentEuler = targetTransform.localEulerAngles;
        targetTransform.localEulerAngles = new Vector3(currentEuler.x, finalYaw, currentEuler.z);
    }

    private Vector3 GetFacingDirection() => movingRight ? Vector3.right : Vector3.left;

    private Vector3 GetSightBoxCenter()
    {
        Vector3 baseOrigin = sightOrigin != null ? sightOrigin.position : selfTransform.position;
        Vector3 forwardDir = GetFacingDirection();

        Vector3 center = baseOrigin + forwardDir * (sightBoxSize.x * 0.5f);
        center.x += sightOffset.x * (movingRight ? 1f : -1f);
        center.y += sightOffset.y;
        center.z += sightOffset.z;

        return center;
    }

    void CheckForPlayerArea()
    {
        Vector3 center = GetSightBoxCenter();
        Vector3 halfExtents = sightBoxSize * 0.5f;

        int hitCount = Physics.OverlapBoxNonAlloc(center, halfExtents, sightBoxBuffer, Quaternion.identity, playerLayer, QueryTriggerInteraction.Ignore);
        if (hitCount == 0) return;

        Vector3 origin = sightOrigin != null ? sightOrigin.position : selfTransform.position;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = sightBoxBuffer[i];
            if (hit == null || (bossCollider != null && hit == bossCollider)) continue;

            Vector3 toPlayer = hit.transform.position - origin;
            float distToPlayer = toPlayer.magnitude;
            if (distToPlayer <= 0.001f) continue;

            Vector3 dirToPlayer = toPlayer / distToPlayer;
            int obstacleCount = Physics.RaycastNonAlloc(origin, dirToPlayer, obstacleHitBuffer, distToPlayer, obstacleLayer, QueryTriggerInteraction.Ignore);

            if (obstacleCount == 0)
            {
                if (burstCount >= 2)
                {
                    dashTargetPosition = new Vector3(hit.transform.position.x, selfTransform.position.y, hit.transform.position.z);
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

    IEnumerator ShootBurstRoutine()
    {
        StopReloadSound();
        currentState = BossState.Shooting;

        for (int i = 0; i < papersPerBurst; i++)
        {
            ShootPaper();
            if (fireRate > 0f)
            {
                yield return new WaitForSeconds(fireRate);
            }
        }

        currentState = BossState.Reloading;
        burstCount++;
        PlayReloadSound();

        isRewindingPaper = true;
        if (reloadWindowDuration > 0f)
        {
            yield return new WaitForSeconds(reloadWindowDuration);
        }
        isRewindingPaper = false;

        float remainingReload = Mathf.Max(0f, reloadTime - reloadWindowDuration);
        if (remainingReload > 0f)
        {
            yield return new WaitForSeconds(remainingReload);
        }

        StopReloadSound();

        if (currentState == BossState.Reloading)
        {
            currentState = BossState.Patrol;
        }
    }

    void ShootPaper()
    {
        if (paperProjectilePrefab == null) return;

        Transform spawnPoint = firePoint != null ? firePoint : selfTransform;
        Vector3 shootDirection = GetFacingDirection();

        GameObject paper = Instantiate(paperProjectilePrefab, spawnPoint.position, Quaternion.LookRotation(shootDirection));
        if (paper.TryGetComponent(out Rigidbody paperRb))
        {
            paperRb.linearVelocity = shootDirection * projectileSpeed;
        }

        PlayShootSound();
    }

    public void OnHitByPaperClip(PlayerController3D attackingPlayer)
    {
        if (currentState == BossState.Reloading && isRewindingPaper)
        {
            isRewindingPaper = false;
            targetPlayerForDash = attackingPlayer;

            StopReloadSound();
            AudioManager audio = AudioManager.instancia;
            if (audio != null && audio.bossDanoGrampo != null)
            {
                audio.PlaySFX(audio.bossDanoGrampo);
            }

            Feedback.TremerCamera(0.3f, 0.3f);
            Feedback.Vibrar(attackingPlayer, 0.5f, 0.2f);

            StopAllCoroutines();
            StartCoroutine(ClipHitStunRoutine());
        }
    }

    public void OnHitByLaserWeakPoint()
    {
        if (currentState == BossState.StunnedByClip)
        {
            currentState = BossState.Defeated;
            StopReloadSound();
            Feedback.TremerCamera(0.6f, 0.6f);
            Feedback.VibrarTodos(1f, 0.5f);
            Destroy(gameObject);
        }
    }

    IEnumerator DoDashAttack()
    {
        StopReloadSound();
        currentState = BossState.Dash;
        burstCount = 0;

        while (true)
        {
            Vector3 pos = selfTransform.position;
            float dx = dashTargetPosition.x - pos.x;
            float dz = dashTargetPosition.z - pos.z;
            if (dx * dx + dz * dz <= 0.09f) break; // 0.3f ao quadrado

            selfTransform.position = Vector3.MoveTowards(pos, dashTargetPosition, dashSpeed * Time.deltaTime);
            yield return null;
        }

        if (dashStunTime > 0f)
        {
            yield return new WaitForSeconds(dashStunTime);
        }

        currentState = BossState.Patrol;
    }

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
            selfTransform.Rotate(Vector3.up, spinSpeed * Time.deltaTime);
            currentSpinTime += Time.deltaTime;
            yield return null;
        }

        float stopTimer = vulnerableDuration;
        while (stopTimer > 0f)
        {
            if (currentState == BossState.Defeated) yield break;
            stopTimer -= Time.deltaTime;
            yield return null;
        }

        if (bossCollider != null)
        {
            bossCollider.enabled = true;
        }
        currentState = BossState.Patrol;
        burstCount = 0;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.TryGetComponent(out PlayerController3D player)) return;

        Vector3 hitDirection = (player.transform.position - selfTransform.position).normalized;

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

    void OnDestroy()
    {
        StopReloadSound();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(GetSightBoxCenter(), sightBoxSize);
    }
}