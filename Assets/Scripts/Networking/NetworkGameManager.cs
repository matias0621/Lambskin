using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

/// <summary>
/// Controlador principal del NetworkRunner de Photon Fusion 2.
/// Este script inicia la sesión de red y gestiona la conexión.
/// </summary>
public class NetworkGameManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Network Settings")]
    [SerializeField] private NetworkRunner networkRunnerPrefab;
    [SerializeField] private GameMode gameMode = GameMode.AutoHostOrClient;
    [SerializeField] private string sessionName = "LambskinRoom";
    
    [Header("Scene Settings")]
    [SerializeField] private int gameSceneIndex = 1; // Índice de la escena de juego en Build Settings
    
    private NetworkRunner _runner;

    private void Start()
    {
        StartGame();
    }

    private async void StartGame()
    {
        Debug.Log("[NetworkGameManager] Iniciando sesión de red...");
        
        // Crear el NetworkRunner
        _runner = Instantiate(networkRunnerPrefab);
        _runner.name = "NetworkRunner";
        
        // Agregar callbacks
        _runner.AddCallbacks(this);
        
        // Configurar los argumentos de inicio
        var startGameArgs = new StartGameArgs()
        {
            GameMode = gameMode,
            SessionName = sessionName,
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        };

        Debug.Log($"[NetworkGameManager] Modo: {gameMode}, Sala: {sessionName}");
        
        // Iniciar el runner
        var result = await _runner.StartGame(startGameArgs);
        
        if (result.Ok)
        {
            Debug.Log($"[NetworkGameManager] ✅ Sesión iniciada exitosamente. IsServer: {_runner.IsServer}, IsClient: {_runner.IsClient}");
        }
        else
        {
            Debug.LogError($"[NetworkGameManager] ❌ Error al iniciar sesión: {result.ShutdownReason}");
        }
    }

    #region INetworkRunnerCallbacks
    
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[NetworkGameManager] 🎮 Jugador {player} se unió a la sesión");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[NetworkGameManager] 👋 Jugador {player} dejó la sesión");
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("[NetworkGameManager] ✅ Conectado al servidor");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"[NetworkGameManager] ⚠️ Desconectado del servidor: {reason}");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"[NetworkGameManager] ❌ Falló conexión: {reason}");
    }

    // Callbacks no utilizados
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) 
    {
        Debug.Log($"[NetworkGameManager] Apagando NetworkRunner: {shutdownReason}");
    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    
    #endregion
}
