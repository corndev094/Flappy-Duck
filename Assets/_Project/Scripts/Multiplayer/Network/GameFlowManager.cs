using System;
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
            var nm = NetworkManager.Singleton;
            if (nm == null)
            {
                EDebug.LogError("NetworkManager.Singleton is null in GameFlowManager.OnNetworkSpawn");
                return;
            }

            // New network session: always reset state from previous match/session.
            CurrentGameState.Value = GameState.InMenu;
            ClearPlayerList();
            nm.OnClientConnectedCallback += OnClientConnected;
            nm.OnClientDisconnectCallback += OnClientDisconnected;

            // Add server/host player
            AddPlayer(nm.LocalClientId);
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
        // if (clientId == NetworkManager.Singleton.LocalClientId) return;
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

    private void ClearPlayerList()
    {
        PlayerList.Clear();
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

    public PlayerNetworkData? GetOfflinePlayerData()
    {
        ulong clientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : OwnerClientId;
        int index = GetPlayerIndex(clientId);
        if (index >= 0)
        {
            var data = PlayerList[index];
            return data;
        }
        return null;
    }

    public int GetPlayerIndex(ulong clientId)
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

        // Send load level RPC to all clients
        LoadMultiplayerLevelClientRpc();
    }

    public void OnLevelLoaded()
    {
        if (!IsServer) return;
        CurrentGameState.Value = GameState.Playing;
        NotifyGameStartedClientRpc();
    }

    public void NotifyPlayerDied(ulong clientId)
    {
        if (!IsServer) return;
        UpdateAliveStatus(clientId, false);

        OnPlayerDied?.Invoke(clientId);
        CheckGameOver();
    }

    public void NotifyPlayerFinished(ulong clientId)
    {
        if (!IsServer || CurrentGameState.Value != GameState.Playing) return;
        if (GetPlayerIndex(clientId) < 0) return;

        OnPlayerWon?.Invoke(clientId);
        DeclareWinnerClientRpc(clientId);
        CurrentGameState.Value = GameState.GameOver;
        OnGameEnded?.Invoke();
        UpdateCoinDataClientRpc();
        Debug.Log($"Winner: {clientId}");
    }

    public void UpdateCoin(ulong clientId, int coin)
    {
        if (!IsServer) return;
        UpdateNetworkPlayerData(clientId, (ref PlayerNetworkData data) => data.Coin = coin);
    }

    public void UpdateAliveStatus(ulong clientId, bool isAlive)
    {
        if (!IsServer) return;
        UpdateNetworkPlayerData(clientId, (ref PlayerNetworkData data) => data.IsAlive = isAlive);
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

    public void ResetAllPlayerStats()
    {
        if (!IsServer) return;
        for (int i = 0; i < PlayerList.Count; i++)
        {
            var player = PlayerList[i];
            UpdateCoin(player.ClientId, 0);
            UpdateAliveStatus(player.ClientId, true);
            Debug.Log($"Reset Player {player.ClientId}: {player.Coin} - {player.IsAlive}");
        }
    }

    public void CheckGameOver()
    {
        if (!IsServer || CurrentGameState.Value != GameState.Playing) return;
        int alivePlayers = 0;

        for (int i = 0; i < PlayerList.Count; i++)
        {
            if (PlayerList[i].IsAlive)
            {
                alivePlayers++;
            }
        }
        Debug.Log("Alive Players Count: " + alivePlayers);

        if (alivePlayers == 0 && PlayerList.Count > 0)
        {
            DeclareWinnerClientRpc(ulong.MaxValue); // No winner
            CurrentGameState.Value = GameState.GameOver;
            OnGameEnded?.Invoke();
            UpdateCoinDataClientRpc();
            Debug.Log("All players lost");
        }
    }

    // public void ReturnToMenuServerRpc()
    // {
    //     if (!IsServer) return;

    //     // Reset players
    //     for (int i = 0; i < PlayerList.Count; i++)
    //     {
    //         var data = PlayerList[i];
    //         data.IsAlive = true;
    //         data.Coin = 0;
    //         PlayerList[i] = data;
    //     }

    //     // TODO: Load main menu
    // }

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
    public void UpdateCoinDataClientRpc()
    {
        var localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : OwnerClientId;
        var playerData = GetPlayerData(localClientId);
        if (!playerData.HasValue) return;

        DataManager.Instance.SaveCurrency(ConstantString.COIN, DataManager.Instance.GetCurrency(ConstantString.COIN) + playerData.Value.Coin);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void LoadMultiplayerLevelClientRpc()
    {
        GameFacade.Instance.LoadMultiplayerLevel().Forget();
    }

    /// <summary>
    /// Despawn all player-owned NetworkObjects (ducks) on the server.
    /// Uses SpawnManager to find objects by owner, not by ClientId key.
    /// </summary>
    // [ServerRpc]
    // public void CleanupPlayersServerRpc()
    // {
    //     var spawnedObjects = new List<NetworkObject>(NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values);
    //     foreach (var netObj in spawnedObjects)
    //     {
    //         if (netObj != null && netObj.IsPlayerObject)
    //         {
    //             netObj.Despawn(true);
    //         }
    //     }
    // }

    [ClientRpc]
    private void NotifyGameStartedClientRpc() => OnGameStarted?.Invoke();

    [ClientRpc]
    private void DeclareWinnerClientRpc(ulong winnerClientId)
    {
        var localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : OwnerClientId;
        bool hasWinner = winnerClientId != ulong.MaxValue;
        bool isWin = hasWinner && localClientId == winnerClientId;
        if (hasWinner)
        {
            OnPlayerWon?.Invoke(winnerClientId);
        }
        OnGameEnded?.Invoke();
        ABasePopup popup = null;
        UIManager.Instance.TryGetPopup(Popup.LevelResult, out popup);
        if (popup != null)
        {
            var data = GetPlayerData(localClientId);
            if (!data.HasValue) return;

            LevelResultPopup levelResult = (LevelResultPopup)popup;
            levelResult.Setup(isWin, data.Value.Coin);
            UIManager.Instance.OpenPopup(Popup.Leaderboard).Forget();
        }
    }

    #endregion

    #region Event Handlers

    private void HandleGameStateChanged(GameState oldValue, GameState newValue) => OnGameStateChanged?.Invoke(newValue);

    private void HandlePlayerListChanged(NetworkListEvent<PlayerNetworkData> changeEvent)
    {
        OnPlayerListChanged?.Invoke(changeEvent);

        switch (changeEvent.Type)
        {
            case NetworkListEvent<PlayerNetworkData>.EventType.Add:
                Debug.Log($"Player added: {changeEvent.Value.PlayerName}");
                break;
            case NetworkListEvent<PlayerNetworkData>.EventType.Remove:
                Debug.Log($"Player removed: {changeEvent.Value.PlayerName}");
                break;
            case NetworkListEvent<PlayerNetworkData>.EventType.RemoveAt:
                Debug.Log($"Player removed at index: {changeEvent.Index}");
                break;
            case NetworkListEvent<PlayerNetworkData>.EventType.Value:
                Debug.Log($"Player updated: {changeEvent.Value.PlayerName}, Coin: {changeEvent.Value.Coin}, IsAlive: {changeEvent.Value.IsAlive}");
                break;
            case NetworkListEvent<PlayerNetworkData>.EventType.Clear:
                Debug.Log("All players cleared");
                break;
        }
    }

    #endregion
}

#region Data Structures


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
        return $"[Player {ClientId}] {PlayerName} | Coin: {Coin} | Alive: {IsAlive}";
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
