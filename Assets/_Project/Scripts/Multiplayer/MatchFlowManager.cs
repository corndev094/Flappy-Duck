using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages the multiplayer match flow: lobby, game start, player tracking, scoring.
/// This is a NetworkBehaviour singleton that persists across scenes.
/// </summary>
public class MatchFlowManager : NetworkBehaviour
{
    public static MatchFlowManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private int minPlayersToStart = 2;
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string lobbySceneName = "LobbyScene";
    [SerializeField] private string menuSceneName = "MainMenu";

    // Network synced player list
    public NetworkList<PlayerData> PlayerList;

    // Game state
    public NetworkVariable<GameState> CurrentGameState = new(GameState.Lobby, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Events
    public event Action<PlayerData> OnPlayerJoined;
    public event Action<ulong> OnPlayerLeft;
    public event Action<GameState> OnGameStateChanged;
    public event Action<ulong, int> OnPlayerScoreChanged;
    public event Action<ulong> OnPlayerDiedEvent;
    public event Action OnAllPlayersReady;
    public event Action<ulong> OnPlayerWon;

    public int PlayerCount => PlayerList?.Count ?? 0;
    public bool AllPlayersReady
    {
        get
        {
            if (PlayerList == null || PlayerList.Count < minPlayersToStart) return false;
            foreach (var player in PlayerList)
            {
                if (!player.IsReady) return false;
            }
            return true;
        }
    }

    #region Unity Lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        PlayerList = new NetworkList<PlayerData>();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #endregion

    #region Network Lifecycle

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Subscribe to state changes
        CurrentGameState.OnValueChanged += HandleGameStateChanged;
        PlayerList.OnListChanged += HandlePlayerListChanged;

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
            
            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadComplete;
            }

            // Add host player
            AddPlayer(NetworkManager.Singleton.LocalClientId);
        }

        Debug.Log($"[MatchFlowManager] Network spawned. IsServer: {IsServer}, IsHost: {IsHost}");
    }

    public override void OnNetworkDespawn()
    {
        CurrentGameState.OnValueChanged -= HandleGameStateChanged;
        PlayerList.OnListChanged -= HandlePlayerListChanged;

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
            
            if (NetworkManager.Singleton.SceneManager != null)
            {
                NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadComplete;
            }
        }

        base.OnNetworkDespawn();
    }

    #endregion

    #region Player Management

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        
        Debug.Log($"[Server] Client connected: {clientId}");
        
        // Don't add host again (already added in OnNetworkSpawn)
        if (clientId == NetworkManager.Singleton.LocalClientId) return;
        
        AddPlayer(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        
        Debug.Log($"[Server] Client disconnected: {clientId}");
        RemovePlayer(clientId);
    }

    private void AddPlayer(ulong clientId)
    {
        // Check if already exists
        for (int i = 0; i < PlayerList.Count; i++)
        {
            if (PlayerList[i].ClientId == clientId) return;
        }

        var playerData = new PlayerData(clientId);
        PlayerList.Add(playerData);
        
        Debug.Log($"[Server] Added player: {playerData}");
    }

    private void RemovePlayer(ulong clientId)
    {
        for (int i = 0; i < PlayerList.Count; i++)
        {
            if (PlayerList[i].ClientId == clientId)
            {
                PlayerList.RemoveAt(i);
                OnPlayerLeft?.Invoke(clientId);
                break;
            }
        }
    }

    public PlayerData? GetPlayerData(ulong clientId)
    {
        for (int i = 0; i < PlayerList.Count; i++)
        {
            if (PlayerList[i].ClientId == clientId)
            {
                return PlayerList[i];
            }
        }
        return null;
    }

    private int GetPlayerIndex(ulong clientId)
    {
        for (int i = 0; i < PlayerList.Count; i++)
        {
            if (PlayerList[i].ClientId == clientId)
            {
                return i;
            }
        }
        return -1;
    }

    #endregion

    #region Lobby Actions (Client -> Server)

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerReadyServerRpc(bool isReady, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        int index = GetPlayerIndex(clientId);
        
        if (index >= 0)
        {
            var data = PlayerList[index];
            data.IsReady = isReady;
            PlayerList[index] = data;

            Debug.Log($"[Server] Player {clientId} ready: {isReady}");

            // Check if all players ready
            if (AllPlayersReady)
            {
                OnAllPlayersReady?.Invoke();
                AllPlayersReadyClientRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerSkinServerRpc(SkinID skin, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        int index = GetPlayerIndex(clientId);
        
        if (index >= 0)
        {
            var data = PlayerList[index];
            data.SelectedSkin = skin;
            PlayerList[index] = data;

            Debug.Log($"[Server] Player {clientId} selected skin: {skin}");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerNameServerRpc(string playerName, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        int index = GetPlayerIndex(clientId);
        
        if (index >= 0)
        {
            var data = PlayerList[index];
            data.PlayerName = playerName;
            PlayerList[index] = data;

            Debug.Log($"[Server] Player {clientId} set name: {playerName}");
        }
    }

    #endregion

    #region Game Flow (Server)

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void StartGameServerRpc()
    {
        if (!IsServer) return;
        
        if (CurrentGameState.Value != GameState.Lobby)
        {
            Debug.LogWarning("[Server] Cannot start game: Not in lobby state");
            return;
        }

        if (PlayerList.Count < minPlayersToStart)
        {
            Debug.LogWarning($"[Server] Cannot start game: Need at least {minPlayersToStart} players");
            return;
        }

        Debug.Log("[Server] Starting game...");
        CurrentGameState.Value = GameState.Starting;

        // Reset all players
        for (int i = 0; i < PlayerList.Count; i++)
        {
            var data = PlayerList[i];
            data.Score = 0;
            data.IsAlive = true;
            PlayerList[i] = data;
        }

        // Load game scene
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    private void OnSceneLoadComplete(string sceneName, LoadSceneMode loadSceneMode, 
        List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer) return;

        Debug.Log($"[Server] Scene load complete: {sceneName}. Clients completed: {clientsCompleted.Count}, Timed out: {clientsTimedOut.Count}");

        if (sceneName == gameSceneName)
        {
            CurrentGameState.Value = GameState.Playing;
            
            // Notify all clients to start flying
            StartPlayingClientRpc();
        }
    }

    public void OnPlayerDied(ulong clientId)
    {
        if (!IsServer) return;

        int index = GetPlayerIndex(clientId);
        if (index >= 0)
        {
            var data = PlayerList[index];
            data.IsAlive = false;
            PlayerList[index] = data;
        }

        OnPlayerDiedEvent?.Invoke(clientId);
        PlayerDiedClientRpc(clientId);

        // Check if game over (all players dead or only one alive)
        CheckGameOver();
    }

    public void OnPlayerReachedFinish(ulong clientId)
    {
        if (!IsServer) return;

        // Award score
        AddScoreToPlayer(clientId, 100);

        // Check for winner
        CheckGameOver();
    }

    public void AddScoreToPlayer(ulong clientId, int score)
    {
        if (!IsServer) return;

        int index = GetPlayerIndex(clientId);
        if (index >= 0)
        {
            var data = PlayerList[index];
            data.Score += score;
            PlayerList[index] = data;

            OnPlayerScoreChanged?.Invoke(clientId, data.Score);
            PlayerScoreChangedClientRpc(clientId, data.Score);
        }
    }

    private void CheckGameOver()
    {
        if (!IsServer || CurrentGameState.Value != GameState.Playing) return;

        int alivePlayers = 0;
        ulong lastAlivePlayer = 0;

        for (int i = 0; i < PlayerList.Count; i++)
        {
            if (PlayerList[i].IsAlive)
            {
                alivePlayers++;
                lastAlivePlayer = PlayerList[i].ClientId;
            }
        }

        if (alivePlayers <= 1 && PlayerList.Count > 1)
        {
            // Game over - we have a winner (or everyone died)
            CurrentGameState.Value = GameState.GameOver;
            
            if (alivePlayers == 1)
            {
                OnPlayerWon?.Invoke(lastAlivePlayer);
                GameOverClientRpc(lastAlivePlayer);
            }
            else
            {
                GameOverClientRpc(ulong.MaxValue); // No winner
            }
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ReturnToLobbyServerRpc()
    {
        if (!IsServer) return;

        CurrentGameState.Value = GameState.Lobby;

        // Reset ready state
        for (int i = 0; i < PlayerList.Count; i++)
        {
            var data = PlayerList[i];
            data.IsReady = false;
            data.IsAlive = true;
            PlayerList[i] = data;
        }

        NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, LoadSceneMode.Single);
    }

    public void Disconnect()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        
        // Load menu scene locally
        SceneManager.LoadScene(menuSceneName);
    }

    #endregion

    #region Client RPCs

    [ClientRpc]
    private void AllPlayersReadyClientRpc()
    {
        OnAllPlayersReady?.Invoke();
    }

    [ClientRpc]
    private void StartPlayingClientRpc()
    {
        Debug.Log("[Client] Game started - Begin playing!");
        
        // Find local player and start flying
        var localPlayer = FindLocalPlayerController();
        if (localPlayer != null)
        {
            localPlayer.RequestStartFlyingServerRpc();
        }
    }

    [ClientRpc]
    private void PlayerDiedClientRpc(ulong clientId)
    {
        OnPlayerDiedEvent?.Invoke(clientId);
    }

    [ClientRpc]
    private void PlayerScoreChangedClientRpc(ulong clientId, int newScore)
    {
        OnPlayerScoreChanged?.Invoke(clientId, newScore);
    }

    [ClientRpc]
    private void GameOverClientRpc(ulong winnerId)
    {
        if (winnerId == ulong.MaxValue)
        {
            Debug.Log("[Client] Game Over - No winner!");
        }
        else
        {
            Debug.Log($"[Client] Game Over - Player {winnerId} wins!");
            OnPlayerWon?.Invoke(winnerId);
        }
    }

    #endregion

    #region Event Handlers

    private void HandleGameStateChanged(GameState oldValue, GameState newValue)
    {
        Debug.Log($"[MatchFlow] Game state changed: {oldValue} -> {newValue}");
        OnGameStateChanged?.Invoke(newValue);
    }

    private void HandlePlayerListChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        switch (changeEvent.Type)
        {
            case NetworkListEvent<PlayerData>.EventType.Add:
                OnPlayerJoined?.Invoke(changeEvent.Value);
                break;
            case NetworkListEvent<PlayerData>.EventType.Remove:
            case NetworkListEvent<PlayerData>.EventType.RemoveAt:
                OnPlayerLeft?.Invoke(changeEvent.Value.ClientId);
                break;
        }
    }

    #endregion

    #region Helpers

    private PlayerNetworkController FindLocalPlayerController()
    {
        var controllers = FindObjectsByType<PlayerNetworkController>(FindObjectsSortMode.None);
        foreach (var controller in controllers)
        {
            if (controller.IsOwner)
            {
                return controller;
            }
        }
        return null;
    }

    #endregion
}

public enum GameState
{
    Lobby,
    Starting,
    Playing,
    GameOver
}
