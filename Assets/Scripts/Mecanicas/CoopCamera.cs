using DG.Tweening;
using UnityEngine;

// Controla a câmera em jogos coop local mantendo ambos os jogadores no enquadramento e aplicando tremor (shake)
public class CoopCamera : MonoBehaviour
{
    public static CoopCamera instancia; 

    [Header("Jogadores")]
    public Transform player1; 
    public Transform player2; 

    [Header("Limites do Zoom (FOV)")]
    public float minFOV = 40f; // Campo de visão mínimo (mais zoom quando estão próximos)
    public float maxFOV = 85f; // Campo de visão máximo (menos zoom quando estão afastados)

    [Header("Distancia dos Jogadores")]
    public float minDistance = 2f;  // Distância a partir da qual o zoom out começa
    public float maxDistance = 25f; // Distância de referência para atingir o FOV máximo

    [Header("Limite de Afastamento Entre Players")]
    [Tooltip("Distância horizontal máxima permitida entre Player 1 e Player 2 antes de bloquear o avanço.")]
    public float maxHorizontalSeparation = 25f;

    [Header("Suavizacao")]
    public float smoothTime = 0.2f; // Tempo de suavização do movimento da câmera
    public Vector3 offset = new Vector3(0f, 2f, -10f); // Deslocamento relativo ao ponto médio dos jogadores

    private Camera cam;
    private Vector3 velocity;

    // Posição base calculada antes do shake para evitar conflito com o SmoothDamp
    private Vector3 basePosition;
    private Vector3 shakeOffset;
    private Tween shakeTween;
    private float shakeForce;

    void Awake()
    {
        instancia = this;
    }

    void Start()
    {
        cam = GetComponent<Camera>();
        basePosition = transform.position;
    }

    // Dispara tremor na câmera; um tremor mais forte substitui outro ativo mais fraco
    public void Shake(float force, float duration)
    {
        if (shakeTween != null && shakeTween.IsActive() && force < shakeForce) return;

        shakeTween?.Kill();
        shakeForce = force;
        shakeOffset = Vector3.zero;
        shakeTween = DOTween.Shake(() => shakeOffset, x => shakeOffset = x, duration,
                                   new Vector3(force, force, 0f), 20, 90f, true)
            .SetUpdate(true) // Permite tremer mesmo em timeScale reduzido/pausado
            .SetLink(gameObject)
            .OnComplete(() => shakeOffset = Vector3.zero);
    }

    void LateUpdate()
    {
        if (player1 == null || player2 == null)
            return;

        MoveCamera();
        ZoomCamera();
        ApplyShake();
    }

    // Soma o offset do tremor sobre a posição base da câmera
    void ApplyShake()
    {
        transform.position = basePosition + shakeOffset;
    }

    // Move a câmera suavemente para o ponto médio entre os dois jogadores
    void MoveCamera()
    {
        Vector3 centerPoint = GetCenterPoint();
        Vector3 targetPosition = centerPoint + offset;
        basePosition = Vector3.SmoothDamp(basePosition, targetPosition, ref velocity, smoothTime);
    }

    // Ajusta o FOV de acordo com a maior distância horizontal ou vertical entre os jogadores
    void ZoomCamera()
    {
        float distanceX = Mathf.Abs(player1.position.x - player2.position.x);
        float distanceY = Mathf.Abs(player1.position.y - player2.position.y);

        float currentDistance = Mathf.Max(distanceX, distanceY);

        float t = Mathf.InverseLerp(minDistance, maxDistance, currentDistance);
        float targetFOV = Mathf.Lerp(minFOV, maxFOV, t);

        if (cam != null)
        {
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 3f);
        }
    }

    // Retorna -1 se o player estiver bloqueado de ir para a esquerda,
    // +1 se estiver bloqueado de ir para a direita, ou 0 se não houver bloqueio
    public int GetBlockedHorizontalDirection(Transform player)
    {
        if (player1 == null || player2 == null || player == null) return 0;

        Transform other = player == player1 ? player2 : player1;
        float deltaX = player.position.x - other.position.x;

        // Só bloqueia quando atinge ou ultrapassa a distância horizontal máxima permitida
        if (Mathf.Abs(deltaX) < maxHorizontalSeparation) return 0;

        if (deltaX < 0f) return -1;

        return 1;
    }

    // Calcula o ponto médio exato entre os dois jogadores
    Vector3 GetCenterPoint()
    {
        return (player1.position + player2.position) / 2f;
    }
}