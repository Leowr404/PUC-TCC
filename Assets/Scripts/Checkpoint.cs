using UnityEngine;
using UnityEngine.SceneManagement;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] int id;                  // único dentro da fase: 0, 1, 2...
    [SerializeField] Transform[] pontosSpawn; // um ponto por player (P1, P2)

    public int Id => id;
    bool ativado;

    public Transform PontoSpawn(int indicePlayer) =>
        pontosSpawn[Mathf.Clamp(indicePlayer, 0, pontosSpawn.Length - 1)];

    void OnTriggerEnter(Collider other)
    {
        if (ativado || !other.CompareTag("Player")) return;
        ativado = true;
        SaveSystem.SalvarCheckpoint(SceneManager.GetActiveScene().name, id);
    }
}
