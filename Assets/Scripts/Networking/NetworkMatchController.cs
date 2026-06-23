using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

public enum NetworkMatchPhase
{
    Waiting = 0,
    Playing = 1,
    Finished = 2
}

public class NetworkMatchController : SimulationBehaviour, INetworkRunnerCallbacks
{
    public static NetworkMatchController Instance { get; private set; }

    [Header("Match Rules")]
    [SerializeField] private float humanTimeLimit = 60f;
    [SerializeField] private int minPlayers = 2;
    [SerializeField] private int maxPlayers = NetworkSessionRequest.MaxPlayers;
    [Range(0f, 1f)]
    [SerializeField] private float maskSuccessChance = 0.5f;

    private bool _callbacksRegistered;

    public NetworkRunner ActiveRunner => Runner;
    public float HumanTimeLimit => humanTimeLimit;
    public float MaskSuccessChance => maskSuccessChance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        RegisterCallbacks();
        MatchHudUI.EnsureExists();
    }

    private void OnDestroy()
    {
        if (_callbacksRegistered && Runner != null)
        {
            Runner.RemoveCallbacks(this);
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (Runner == null || !Runner.IsServer || GetPhase() != NetworkMatchPhase.Playing)
        {
            return;
        }

        TickHumanTimer();
    }

    private void RegisterCallbacks()
    {
        if (_callbacksRegistered || Runner == null)
        {
            return;
        }

        Runner.AddCallbacks(this);
        _callbacksRegistered = true;
    }

    public bool CanAcceptNewPlayer()
    {
        return GetPhase() == NetworkMatchPhase.Waiting && GetPlayersSnapshot(false).Count < maxPlayers;
    }

    public bool CanHostStart()
    {
        return Runner != null && Runner.IsServer && GetPhase() == NetworkMatchPhase.Waiting && GetPlayersSnapshot(false).Count >= minPlayers;
    }

    public void StartMatchByHost()
    {
        if (!CanHostStart())
        {
            Debug.LogWarning("[NetworkMatchController] No se puede iniciar: faltan jugadores o la partida ya empezo.");
            return;
        }

        List<PlayerMovement> players = GetPlayersSnapshot(false);

        if (Runner.SessionInfo != null)
        {
            Runner.SessionInfo.IsOpen = false;
            Runner.SessionInfo.IsVisible = false;
        }

        foreach (PlayerMovement player in players)
        {
            player.PrepareForMatch(NetworkMatchPhase.Playing);
            player.SetAsMonster();
            player.SetHumanTimer(0f);
        }

        PlayerMovement human = players[UnityEngine.Random.Range(0, players.Count)];
        human.SetAsHuman();
        human.SetHumanTimer(humanTimeLimit);

        Debug.Log($"[NetworkMatchController] Partida iniciada. Humano inicial: {human.GetDisplayName()}");
    }

    public bool TryResolveMaskHit(PlayerMovement owner, PlayerMovement target)
    {
        if (Runner == null || !Runner.IsServer || owner == null || target == null)
        {
            return false;
        }

        if (GetPhase() != NetworkMatchPhase.Playing || !owner.IsHuman || owner.IsEliminated || !target.CanBeConvertedByMask(owner))
        {
            return false;
        }

        bool converted = UnityEngine.Random.value < maskSuccessChance;

        if (!converted)
        {
            Debug.Log($"[NetworkMatchController] La mascara golpeo a {target.GetDisplayName()}, pero fallo la transformacion.");
            return true;
        }

        owner.SetAsMonster();
        owner.SetHumanTimer(0f);

        target.SetAsHuman();
        target.SetHumanTimer(humanTimeLimit);

        Debug.Log($"[NetworkMatchController] {target.GetDisplayName()} ahora es humano.");
        return true;
    }

    public NetworkMatchPhase GetPhase()
    {
        List<PlayerMovement> players = GetPlayersSnapshot(false);
        if (players.Count == 0)
        {
            return NetworkMatchPhase.Waiting;
        }

        return (NetworkMatchPhase)players[0].MatchState;
    }

    public float GetCurrentHumanTime()
    {
        PlayerMovement human = GetCurrentHuman();
        return human != null ? human.HumanTimeRemaining : 0f;
    }

    public PlayerMovement GetCurrentHuman()
    {
        List<PlayerMovement> players = GetPlayersSnapshot(false);
        foreach (PlayerMovement player in players)
        {
            if (player.IsHuman && !player.IsEliminated)
            {
                return player;
            }
        }

        return null;
    }

    public PlayerMovement GetLocalPlayer()
    {
        List<PlayerMovement> players = GetPlayersSnapshot(false);
        foreach (PlayerMovement player in players)
        {
            if (player.Object != null && player.Object.HasInputAuthority)
            {
                return player;
            }
        }

        return null;
    }

    public PlayerMovement GetWinner()
    {
        List<PlayerMovement> players = GetPlayersSnapshot(false);
        foreach (PlayerMovement player in players)
        {
            if (player.OwnerPlayer == player.WinnerPlayer)
            {
                return player;
            }
        }

        return null;
    }

    public List<PlayerMovement> GetPlayersSnapshot(bool aliveOnly)
    {
        PlayerMovement[] foundPlayers = FindObjectsByType<PlayerMovement>(FindObjectsSortMode.None);
        List<PlayerMovement> players = new List<PlayerMovement>();

        foreach (PlayerMovement player in foundPlayers)
        {
            if (player == null || !player.HasOwner)
            {
                continue;
            }

            if (aliveOnly && player.IsEliminated)
            {
                continue;
            }

            players.Add(player);
        }

        players.Sort((left, right) => left.OwnerPlayer.AsIndex.CompareTo(right.OwnerPlayer.AsIndex));
        return players;
    }

    private void TickHumanTimer()
    {
        PlayerMovement human = GetCurrentHuman();
        if (human == null)
        {
            SelectNextHumanOrFinish();
            return;
        }

        human.SetHumanTimer(Mathf.Max(0f, human.HumanTimeRemaining - Time.deltaTime));

        if (human.HumanTimeRemaining <= 0f)
        {
            Debug.Log($"[NetworkMatchController] El humano {human.GetDisplayName()} fue eliminado por tiempo.");
            human.SetEliminated();
            SelectNextHumanOrFinish();
        }
    }

    private void SelectNextHumanOrFinish()
    {
        List<PlayerMovement> alivePlayers = GetPlayersSnapshot(true);

        if (alivePlayers.Count <= 1)
        {
            PlayerRef winner = alivePlayers.Count == 1 ? alivePlayers[0].OwnerPlayer : PlayerRef.None;
            FinishMatch(winner);
            return;
        }

        foreach (PlayerMovement player in alivePlayers)
        {
            player.SetAsMonster();
            player.SetHumanTimer(0f);
        }

        PlayerMovement nextHuman = alivePlayers[UnityEngine.Random.Range(0, alivePlayers.Count)];
        nextHuman.SetAsHuman();
        nextHuman.SetHumanTimer(humanTimeLimit);
    }

    private void FinishMatch(PlayerRef winner)
    {
        List<PlayerMovement> players = GetPlayersSnapshot(false);
        foreach (PlayerMovement player in players)
        {
            player.SetMatchResult(winner);
            player.SetHumanTimer(0f);
        }

        Debug.Log($"[NetworkMatchController] Partida terminada. Ganador: {winner}");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer)
        {
            return;
        }

        NetworkObject playerObject = runner.GetPlayerObject(player);
        if (playerObject != null)
        {
            runner.Despawn(playerObject);
        }

        if (GetPhase() == NetworkMatchPhase.Playing)
        {
            SelectNextHumanOrFinish();
        }
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
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
