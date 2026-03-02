using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages game flow for Multiplayer NGO system.
/// Handles scene loading, player tracking, and game state.
/// </summary>
public class GameFlowManager : NetworkBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [Header("Game Settings")]
    [SerializeField] private int minPlayersToStart = 2;
    private delegate void PlayerDataModifier(ref PlayerNetworkData data);

    // Game state
    public NetworkVariable<GameState> CurrentGameState = new(
        GameState.InMenu,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    // Player tracking
    public NetworkList<PlayerNetworkData> PlayerList;

    // Events
    public event Action<GameState> OnGameStateChanged;
    public event Action<ulong> OnPlayerDied;
    public event Action<ulong> OnPlayerWon;
    public event Action OnGameStarted;
    public event Action OnGameEnded;
    public event Action<NetworkListEvent<PlayerNetworkData>> OnPlayerListChanged;

    public int PlayerCount => PlayerList?.Count ?? 0;

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

        PlayerList = new NetworkList<PlayerNetworkData>();
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
        CurrentGameState.OnValueChanged += HandleGameStateChanged;
        PlayerList.OnListChanged += HandlePlayerListChanged;

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            // Add server/host player
            AddPlayer(NetworkManager.Singleton.LocalClientId);
        }
    }

    public override void OnNetworkDespawn()
    {
        CurrentGameState.OnValueChanged -= HandleGameStateChanged;
        PlayerList.OnListChanged -= HandlePlayerListChanged;

        if (IsServer && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    #endregion

    #region Player Management

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        if (clientId == NetworkManager.Singleton.LocalClientId) return;
        AddPlayer(clientId);
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        RemovePlayer(clientId);
    }

    private void AddPlayer(ulong clientId)
    {
        // Check if already exists
        for (int i = 0; i < PlayerList.Count; i++)
        {
            if (PlayerList[i].ClientId == clientId) return;
        }

        var playerData = new PlayerNetworkData
        {
            ClientId = clientId,
            PlayerName = $"Player {clientId}",
            Coin = 0,
            IsAlive = true
        };

        PlayerList.Add(playerData);
        foreach(var id in NetworkManager.Singleton.ConnectedClients.Keys)
        {
            NetworkManager.Singleton.ConnectedClients.TryGetValue(id, out var c);
        }
    }

    private void RemovePlayer(ulong clientId)
    {
        for (int i = 0; i < PlayerList.Count; i++)
        {
            if (PlayerList[i].ClientId == clientId)
            {
                PlayerList.RemoveAt(i);
                break;
            }
        }
    }

    public PlayerNetworkData? GetPlayerData(ulong clientId)
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

    #region Game Flow (Server)

    /// <summary>
    /// Start the game - Called by MatchmakingManager after match found
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void StartGameServerRpc()
    {
        if (!IsServer) return;

        if (CurrentGameState.Value != GameState.InMenu && CurrentGameState.Value != GameState.WaitingInLobby) return;
        if (PlayerList.Count < minPlayersToStart) return;

        CurrentGameState.Value = GameState.Loading;

        // Reset all players
        for (int i = 0; i < PlayerList.Count; i++)
        {
            var data = PlayerList[i];
            data.Coin = 0;
            data.IsAlive = true;
            PlayerList[i] = data;
        }

        // Load Level - Gửi RPC đến tất cả clients
        LoadMultiplayerLevelClientRpc();
    }

    public void OnLevelLoaded()
    {
        CurrentGameState.Value = GameState.Playing;
        NotifyGameStartedClientRpc();
    }

    /// <summary>
    /// Called when a player dies
    /// </summary>
    public void NotifyPlayerDied(ulong clientId)
    {
        if (!IsServer) return;
        UpdateNetworkPlayerData(clientId, (ref PlayerNetworkData data) => data.IsAlive = false);

        OnPlayerDied?.Invoke(clientId);
        CheckGameOver();
    }

    /// <summary>
    /// Add score to player
    /// </summary>
    public void AddScore(ulong clientId, int score)
    {
        if (!IsServer) return;
        UpdateNetworkPlayerData(clientId, (ref PlayerNetworkData data) => data.Coin += score);
    }

    private void UpdateNetworkPlayerData(ulong clientId, PlayerDataModifier updateAction)
    {
        int index = GetPlayerIndex(clientId);
        if (index >= 0)
        {
            var data = PlayerList[index];
            updateAction(ref data);
            PlayerList[index] = data;
        }
    }

    public void CheckGameOver()
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
        Debug.Log("Alive Players Count: " + alivePlayers);

        if (alivePlayers <= 1 && PlayerList.Count > 1)
        {
            // Game over

            // Declare winner
            // if (alivePlayers == 1)
            // {
            //     OnPlayerWon?.Invoke(lastAlivePlayer);
            //     DeclareWinnerClientRpc(lastAlivePlayer);
            //     Debug.Log("Winner");
            // }
            if (alivePlayers == 0)
            {
                // if (IsServer) GameFlowManager.Instance.CleanupPlayersServerRpc();
                DeclareWinnerClientRpc(); // No winner
                CurrentGameState.Value = GameState.GameOver;
                Debug.Log("Game Over");
            }
            OnGameEnded?.Invoke();
            // Update Data
            UpdateCoinDataClientRpc();
        }
    }

    [ClientRpc]
    public void UpdateCoinDataClientRpc()
    {
        DataManager.Instance.SaveCurrency(ConstantString.COIN, DataManager.Instance.GetCurrency(ConstantString.COIN) + GetPlayerData(OwnerClientId).Value.Coin);
    }

    /// <summary>
    /// Return to main menu
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ReturnToMenuServerRpc()
    {
        if (!IsServer) return;

        CurrentGameState.Value = GameState.InMenu;

        // Reset players
        for (int i = 0; i < PlayerList.Count; i++)
        {
            var data = PlayerList[i];
            data.IsAlive = true;
            data.Coin = 0;
            PlayerList[i] = data;
        }

        // TODO: Load main menu
    }

    /// <summary>
    /// Disconnect from network and return to menu
    /// </summary>
    public void Disconnect()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
        }

        // TODO: Return to main menu
    }

    #endregion

    #region Client RPCs

    [ClientRpc]
    private void LoadMultiplayerLevelClientRpc()
    {
        // Gọi GameFacade để load level trên mỗi client
        GameFacade.Instance.LoadMultiplayerLevel().Forget();
    }

    public void CleanupLevel()
    {
        var levelPrefab = GameFacade.Instance.CurrentLevelPrefab;
        if (levelPrefab != null)
        {
            Destroy(levelPrefab);
        }

        // Thông báo cho GameFacade cleanup local
        GameFacade.Instance.CleanupLevelLocal();
    }

    [ServerRpc]
    public void CleanupPlayersServerRpc()
    {
        foreach(var player in PlayerList)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(player.ClientId, out var netObj))
            {
                netObj.Despawn(true);
            }
            else
            {
                Destroy(GameFacade.Instance.ActiveDuck);
            }
        }
    }

    [ClientRpc]
    private void NotifyGameStartedClientRpc() => OnGameStarted?.Invoke();

    [ClientRpc]
    private void DeclareWinnerClientRpc()
    {
        var id = NetworkManager.Singleton.LocalClientId;
        bool isWin = true;
        if (id != ulong.MaxValue)
        {
            OnPlayerWon?.Invoke(id);
            isWin = false;
        }
        OnGameEnded?.Invoke();
        ABasePopup popup = null;
        UIManager.Instance.TryGetPopup(Popup.LevelResult, out popup);
        if (popup != null)
        {
            var data = GetPlayerData(id);
            LevelResultPopup levelResult = (LevelResultPopup)popup;
            levelResult.Setup(isWin, data.Value.Coin);
            UIManager.Instance.OpenPopup(Popup.LevelResult).Forget();
        }
    }

    #endregion

    #region Event Handlers

    private void HandleGameStateChanged(GameState oldValue, GameState newValue) => OnGameStateChanged?.Invoke(newValue);

    private void HandlePlayerListChanged(NetworkListEvent<PlayerNetworkData> changeEvent)
    {
        OnPlayerListChanged?.Invoke(changeEvent);
    }

    #endregion
}

#region Data Structures

/// <summary>
/// Network-serializable player data for game flow tracking
/// </summary>
[Serializable]
public struct PlayerNetworkData : INetworkSerializable, IEquatable<PlayerNetworkData>
{
    public ulong ClientId;
    public Unity.Collections.FixedString64Bytes PlayerName;
    public int Coin;
    public bool IsAlive;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref Coin);
        serializer.SerializeValue(ref IsAlive);
    }

    public bool Equals(PlayerNetworkData other)
    {
        return ClientId == other.ClientId
            && PlayerName == other.PlayerName
            && Coin == other.Coin
            && IsAlive == other.IsAlive;
    }

    public override string ToString()
    {
        return $"[Player {ClientId}] {PlayerName} | Score: {Coin} | Alive: {IsAlive}";
    }
}

public enum GameState
{
    InMenu,
    WaitingInLobby,
    Loading,
    Playing,
    GameOver
}

#endregion