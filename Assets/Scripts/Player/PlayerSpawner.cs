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
    [SerializeField] private int maxPlayers = NetworkSessionRequest.MaxPlayers;

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
            NetworkMatchController matchController = NetworkMatchController.Instance;
            if (matchController != null && !matchController.CanAcceptNewPlayer())
            {
                Debug.LogWarning($"[PlayerSpawner] Jugador {player} no puede entrar: sala llena o partida en curso.");
                return;
            }

            if (GetSpawnedPlayerCount() >= maxPlayers)
            {
                Debug.LogWarning($"[PlayerSpawner] Jugador {player} no puede entrar: maximo de {maxPlayers} jugadores alcanzado.");
                return;
            }

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
            
            var index = Mathf.Abs(player.AsIndex) % spawnPoints.Length;
            Debug.Log($"[PlayerSpawner] 📍 Spawneando jugador {player} en spawn point {index} (posición: {spawnPoints[index].position})");
            
            var spawnedPlayer = Runner.Spawn(
                playerPrefab, 
                spawnPoints[index].position, 
                Quaternion.identity, 
                player  // Este jugador tendrá Input Authority sobre este objeto
            );
            
            if (spawnedPlayer != null)
            {
                Runner.SetPlayerObject(player, spawnedPlayer);

                PlayerMovement playerMovement = spawnedPlayer.GetComponent<PlayerMovement>();
                if (playerMovement != null)
                {
                    playerMovement.InitializeForPlayer(player);
                }

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

    private int GetSpawnedPlayerCount()
    {
        int count = 0;
        PlayerMovement[] players = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);

        foreach (PlayerMovement player in players)
        {
            if (player != null && player.HasOwner)
            {
                count++;
            }
        }

        return count;
    }
}
