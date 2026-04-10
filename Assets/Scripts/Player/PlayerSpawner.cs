using Fusion;
using UnityEngine;

public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{

    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    public void PlayerJoined(PlayerRef player)
    {
        if (player == Runner.LocalPlayer)
        {
            var index = player.AsIndex % spawnPoints.Length;
            Runner.Spawn(playerPrefab, spawnPoints[index].position, Quaternion.identity, player);
        }
    }

}
