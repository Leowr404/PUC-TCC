using UnityEngine;
using UnityEngine.SceneManagement;

public class SpawnNoCheckpoint : MonoBehaviour
{
    [SerializeField] Checkpoint[] checkpoints;
    [SerializeField] Transform[] players; // P1, P2

    void Start()
    {
        if (!SaveSystem.CarregarDoCheckpoint) return;
        SaveSystem.CarregarDoCheckpoint = false;

        if (SaveSystem.LevelSalvo != SceneManager.GetActiveScene().name) return;

        foreach (Checkpoint cp in checkpoints)
        {
            if (cp.Id != SaveSystem.CheckpointSalvo) continue;

            for (int i = 0; i < players.Length; i++)
                Teleportar(players[i], cp.PontoSpawn(i));
            return;
        }
    }

    void Teleportar(Transform player, Transform destino)
    {
        // CharacterController ignora mudança de posição se estiver ligado
        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;

        player.SetPositionAndRotation(destino.position, destino.rotation);

        if (cc) cc.enabled = true;
    }
}
