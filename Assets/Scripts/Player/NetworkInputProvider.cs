using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

/// <summary>
/// Proveedor de input para Photon Fusion 2.
/// Captura el input local y lo envía a través de la red.
/// </summary>
public class NetworkInputProvider : SimulationBehaviour, INetworkRunnerCallbacks
{
    private NetworkInputData _inputData;

    private void OnEnable()
    {
        if (Runner != null)
        {
            Runner.AddCallbacks(this);
        }
    }

    private void OnDisable()
    {
        if (Runner != null)
        {
            Runner.RemoveCallbacks(this);
        }
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Resetear el input
        _inputData.movementInput = Vector2.zero;
        _inputData.attackPressed = false;

        // Obtener input del jugador local desde teclado y gamepad.
        // Gamepad.current puede existir aunque el stick esté quieto, así que no debe
        // impedir que WASD funcione.
        var gamepad = Gamepad.current;
        var keyboard = Keyboard.current;

        Vector2 move = Vector2.zero;
        bool attackPressed = false;

        if (keyboard != null)
        {
            if (keyboard.wKey.isPressed) move.y += 1;
            if (keyboard.sKey.isPressed) move.y -= 1;
            if (keyboard.aKey.isPressed) move.x -= 1;
            if (keyboard.dKey.isPressed) move.x += 1;
            attackPressed |= keyboard.spaceKey.isPressed || keyboard.eKey.isPressed;
        }

        if (gamepad != null)
        {
            Vector2 gamepadMove = gamepad.leftStick.ReadValue();
            if (gamepadMove.sqrMagnitude > move.sqrMagnitude)
            {
                move = gamepadMove;
            }

            attackPressed |= gamepad.buttonSouth.isPressed;
        }

        _inputData.movementInput = move.sqrMagnitude > 1f ? move.normalized : move;
        _inputData.attackPressed = attackPressed;

        input.Set(_inputData);
    }

    #region INetworkRunnerCallbacks - Callbacks no utilizados
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
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
    #endregion
}
