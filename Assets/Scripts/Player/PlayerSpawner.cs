using Fusion;
using UnityEngine;

/// <summary>
/// Spawner de jugadores para Photon Fusion 2.
/// Solo el servidor/host spawneará los jugadores cuando se unan a la partida.
/// </summary>
public class PlayerSpawner : SimulationBehaviour, IPlayerJoined
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform[] spawnPoints;

    private void Awake()
    {
        // Validaciones al iniciar
        if (playerPrefab == null)
        {
            Debug.LogError("[PlayerSpawner] ❌ Error: No hay prefab de jugador asignado!");
        }
        
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("[PlayerSpawner] ❌ Error: No hay spawn points asignados!");
        }
        else
        {
            Debug.Log($"[PlayerSpawner] ✅ Configurado con {spawnPoints.Length} spawn points");
        }
    }

    public void PlayerJoined(PlayerRef player)
    {
        Debug.Log($"[PlayerSpawner] 🎮 PlayerJoined llamado para {player}. IsServer: {Runner.IsServer}, IsClient: {Runner.IsClient}");
        
        // Solo el servidor/host spawnea jugadores
        if (Runner.IsServer)
        {
            if (playerPrefab == null)
            {
                Debug.LogError("[PlayerSpawner] ❌ No se puede spawnear: playerPrefab es null");
                return;
            }
            
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogError("[PlayerSpawner] ❌ No se puede spawnear: no hay spawn points");
                return;
            }
            
            var index = player.AsIndex % spawnPoints.Length;
            Debug.Log($"[PlayerSpawner] 📍 Spawneando jugador {player} en spawn point {index} (posición: {spawnPoints[index].position})");
            
            var spawnedPlayer = Runner.Spawn(
                playerPrefab, 
                spawnPoints[index].position, 
                Quaternion.identity, 
                player  // Este jugador tendrá Input Authority sobre este objeto
            );
            
            if (spawnedPlayer != null)
            {
                Debug.Log($"[PlayerSpawner] ✅ Jugador {player} spawneado exitosamente: {spawnedPlayer.name}");
            }
            else
            {
                Debug.LogError($"[PlayerSpawner] ❌ Error al spawnear jugador {player}");
            }
        }
        else
        {
            Debug.Log($"[PlayerSpawner] ⏭️ No soy servidor, saltando spawn para {player}");
        }
    }
}
