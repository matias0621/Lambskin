using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI statusLabel;
    
    [Header("Lobby Settings")]
    public float spacing = 12.5f;
    public Vector3 startPosition = new Vector3(50, 10, 40);
    public string gameSceneName = "StageTest";

    // Diccionario para trackear si cada jugador está listo
    // En Unity, usamos el componente PlayerInput como clave única
    private Dictionary<InputSystem_Actions, bool> readyPlayers = new Dictionary<InputSystem_Actions, bool>();

    private void Start()
    {
        UpdateLobbyText();
    }

    // --- LOGICA DE JOIN (Llamada por el Player Input Manager) ---
    
    public void OnPlayerJoined(PlayerInput playerInput)
    {
        // 1. Posicionar al jugador según el índice
        int spawnIndex = readyPlayers.Count;
        Vector3 targetPos = startPosition + new Vector3(spacing * spawnIndex, 0, 0);
        playerInput.transform.position = targetPos;

        // 2. Inicializar estado de "Ready"
        readyPlayers.Add(playerInput.GetComponent<InputSystem_Actions>(), false);
        
        // 3. Configurar el evento de "Ready" del jugador
        // Asumimos que en tu Input Actions hay una acción llamada "Ready"
        playerInput.actions["Ready"].performed += ctx => TogglePlayerReady(playerInput);

        UpdateLobbyText();
        Debug.Log($"Jugador unido. Total: {readyPlayers.Count}");
    }

    public void OnPlayerLeft(PlayerInput playerInput)
    {
        var inputActions = playerInput.GetComponent<InputSystem_Actions>();
        if (readyPlayers.ContainsKey(inputActions))
        {
            readyPlayers.Remove(inputActions);
        }
        UpdateLobbyText();
    }

    // --- LOGICA DE READY ---

    public void TogglePlayerReady(PlayerInput playerInput)
    {
        var inputActions = playerInput.GetComponent<InputSystem_Actions>();
        readyPlayers[inputActions] = !readyPlayers[inputActions];
        
        // Feedback visual: Buscamos un componente en el jugador que maneje su UI individual
        // (Similar al Label3D de tu script de Godot)
        var feedback = playerInput.GetComponentInChildren<TextMeshPro>();
        if (feedback != null)
        {
            feedback.text = readyPlayers[inputActions] ? "✅" : "";
        }

        CheckAllReady();
    }

    private void UpdateLobbyText()
    {
        if (readyPlayers.Count < 2)
            statusLabel.text = "Esperando jugadores (Mínimo 2)...";
        else
            statusLabel.text = "¡Presionen Ready para comenzar!";
    }

    private void CheckAllReady()
    {
        if (readyPlayers.Count < 2) return;

        bool allReady = true;
        foreach (var status in readyPlayers.Values)
        {
            if (!status)
            {
                allReady = false;
                break;
            }
        }

        if (allReady)
        {
            StartGame();
        }
    }

    private void StartGame()
    {
        statusLabel.text = "Iniciando partida...";
        
        // Guardar datos en una clase estática (como tu Global en Godot)
        // GlobalData.PlayerCount = readyPlayers.Count;
        
        SceneManager.LoadScene(gameSceneName);
    }
}
