using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(NetworkRunner))]
public class NetworkSessionManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static NetworkSessionManager Instance { get; private set; }

    [Header("Session")]
    [SerializeField] private int maxPlayers = NetworkSessionRequest.MaxPlayers;
    [SerializeField] private int mainMenuSceneIndex = NetworkSessionRequest.MainMenuSceneIndex;

    private NetworkRunner _runner;
    private bool _isStarting;
    private bool _isReturningToMenu;
    private static bool _multiPeerBootstrapStarted;

    public NetworkRunner Runner => _runner;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        _runner = GetComponent<NetworkRunner>();
        _runner.ProvideInput = true;
    }

    private async void Start()
    {
        if (_isStarting)
        {
            return;
        }

        _isStarting = true;

        if (NetworkSessionRequest.IsMultiPeerTest)
        {
            if (_multiPeerBootstrapStarted)
            {
                // Este es uno de los runners clonados por FusionBootstrap.
                // Debe registrar sus callbacks, pero no iniciar otro bootstrap.
                EnsureRunnerComponents();
                MatchHudUI.EnsureExists();
                return;
            }

            _multiPeerBootstrapStarted = true;
            _isStarting = false;
            StartCoroutine(StartMultiPeerTest());
            return;
        }

        if (!NetworkSessionRequest.HasPendingRequest)
        {
            Debug.LogWarning("[NetworkSessionManager] No habia solicitud de menu. Iniciando sala de desarrollo.");
            NetworkSessionRequest.Set(GameMode.Host, "LambskinDev");
        }

        EnsureRunnerComponents();

        var sceneManager = GetComponent<NetworkSceneManagerDefault>();
        var args = new StartGameArgs
        {
            GameMode = NetworkSessionRequest.RequestedMode,
            SessionName = NetworkSessionRequest.SessionName,
            PlayerCount = maxPlayers,
            IsOpen = true,
            IsVisible = true,
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = sceneManager
        };

        NetworkSessionRequest.SetStatus($"Conectando a {NetworkSessionRequest.SessionName}...");
        Debug.Log($"[NetworkSessionManager] Iniciando {args.GameMode} en sala {args.SessionName}");

        var result = await _runner.StartGame(args);
        _isStarting = false;

        if (!result.Ok)
        {
            NetworkSessionRequest.SetStatus($"No se pudo conectar: {result.ShutdownReason}");
            Debug.LogError($"[NetworkSessionManager] Error al iniciar sesion: {result.ShutdownReason}");
            LoadMainMenu();
            return;
        }

        NetworkSessionRequest.ClearPending();
        NetworkSessionRequest.SetStatus($"Conectado a {args.SessionName}");
        MatchHudUI.EnsureExists();
    }

    private IEnumerator StartMultiPeerTest()
    {
        // Este objeto pertenece a la escena y sirve como prefab para FusionBootstrap.
        // El manager debe permanecer activo: Fusion clonará también este componente y
        // cada peer necesita registrar sus callbacks de red.
        NetworkRunner runnerPrefab = _runner;
        if (runnerPrefab == null)
        {
            NetworkSessionRequest.SetStatus("No se pudo preparar el runner de prueba.");
            yield break;
        }

        Fusion.FusionBootstrap bootstrap = null;
        Fusion.FusionBootstrap[] bootstraps = Resources.FindObjectsOfTypeAll<Fusion.FusionBootstrap>();
        foreach (Fusion.FusionBootstrap candidate in bootstraps)
        {
            if (candidate.gameObject.scene == SceneManager.GetActiveScene())
            {
                bootstrap = candidate;
                break;
            }
        }

        if (bootstrap == null || runnerPrefab == null)
        {
            NetworkSessionRequest.SetStatus("No se pudo preparar el modo Multi-Peer.");
            Debug.LogError("[NetworkSessionManager] Falta FusionBootstrap o el runner de prueba.");
            yield break;
        }

        bootstrap.RunnerPrefab = runnerPrefab;
        bootstrap.StartMode = Fusion.FusionBootstrap.StartModes.Manual;
        bootstrap.AutoClients = NetworkSessionRequest.MaxPlayers - 1;
        bootstrap.DefaultRoomName = NetworkSessionRequest.SessionName;
        bootstrap.gameObject.SetActive(true);

        // Esperar a que FusionBootstrap complete su inicialización de escena.
        yield return null;
        bootstrap.StartHostPlusClients(NetworkSessionRequest.MaxPlayers - 1);
        NetworkSessionRequest.ClearPending();
        NetworkSessionRequest.SetStatus("Prueba online: host + 3 clientes virtuales");
    }

    private void EnsureRunnerComponents()
    {
        if (GetComponent<NetworkSceneManagerDefault>() == null)
        {
            gameObject.AddComponent<NetworkSceneManagerDefault>();
        }

        if (GetComponent<NetworkInputProvider>() == null)
        {
            gameObject.AddComponent<NetworkInputProvider>();
        }

        if (GetComponent<NetworkMatchController>() == null)
        {
            gameObject.AddComponent<NetworkMatchController>();
        }

        _runner.RemoveCallbacks(this);
        _runner.AddCallbacks(this);
    }

    public async void ReturnToMenu(string status = "Volviendo al menu...")
    {
        if (_isReturningToMenu)
        {
            return;
        }

        _isReturningToMenu = true;
        NetworkSessionRequest.SetStatus(status);

        if (_runner != null)
        {
            await _runner.Shutdown();
        }

        LoadMainMenu();
    }

    private void LoadMainMenu()
    {
        if (SceneManager.GetActiveScene().buildIndex != mainMenuSceneIndex)
        {
            SceneManager.LoadScene(mainMenuSceneIndex);
        }
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"[NetworkSessionManager] Shutdown: {shutdownReason}");

        if (!_isReturningToMenu && !_isStarting)
        {
            NetworkSessionRequest.SetStatus($"Sesion cerrada: {shutdownReason}");
            LoadMainMenu();
        }
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        NetworkSessionRequest.SetStatus($"Desconectado: {reason}");
        LoadMainMenu();
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        NetworkSessionRequest.SetStatus($"Conexion fallida: {reason}");
        LoadMainMenu();
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
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
}
