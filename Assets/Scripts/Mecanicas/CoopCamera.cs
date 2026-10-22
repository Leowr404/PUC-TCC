using UnityEngine;

// Controla a camera em jogos coop local mantendo ambos os jogadores no enquadramento
public class CoopCamera : MonoBehaviour
{
    [Header("Jogadores")]
    public Transform player1;
    public Transform player2;

    [Header("Limites do Zoom (FOV)")]
    public float minFOV = 40f;  // Zoom maximo (jogadores muito proximos)
    public float maxFOV = 85f;  // Zoom minimo (jogadores muito distantes)

    [Header("Distancia dos Jogadores")]
    public float minDistance = 2f;  // Distancia minima para comecar a dar zoom out
    public float maxDistance = 25f; // Distancia maxima entre os dois jogadores para o zoom limite

    [Header("Suavizacao")]
    public float smoothTime = 0.2f;
    public Vector3 offset = new Vector3(0f, 2f, -10f); // Posicao base da camera (Z determina a distancia inicial)

    private Camera cam;
    private Vector3 velocity;

    void Start()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (player1 == null || player2 == null)
            return;

        MoveCamera();
        ZoomCamera();
    }

    void MoveCamera()
    {
        // Encontra o ponto medio exato entre os dois jogadores
        Vector3 centerPoint = GetCenterPoint();

        // Posicao desejada da camera mantendo o offset de profundidade
        Vector3 targetPosition = centerPoint + offset;

        // Move suavemente ate a posicao final
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }

    void ZoomCamera()
    {
        // Calcula a maior distancia ocupada entre eles (seja na horizontal X ou vertical Y)
        float distanceX = Mathf.Abs(player1.position.x - player2.position.x);
        float distanceY = Mathf.Abs(player1.position.y - player2.position.y);

        // Pega a maior distancia para garantir que ambos fiquem dentro do enquadramento
        float currentDistance = Mathf.Max(distanceX, distanceY);

        // Interpola o FOV de acordo com a distancia real dos jogadores
        float t = Mathf.InverseLerp(minDistance, maxDistance, currentDistance);
        float targetFOV = Mathf.Lerp(minFOV, maxFOV, t);

        // Aplica o zoom suavemente
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * 3f);
    }

    Vector3 GetCenterPoint()
    {
        return (player1.position + player2.position) / 2f;
    }
}