namespace Multiplayer
{
    using System;
    using System.Collections.Generic;
    using Unity.Netcode;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// Manages game flow for Multiplayer NGO system.
    /// Handles scene loading, player tracking, and game state.
    /// </summary>
    public class GameFlowManager : NetworkBehaviour
    {
        public static GameFlowManager Instance { get; private set; }

        [Header("Scene Names")]
        [SerializeField] private string gameSceneName = "GameScene";
        [SerializeField] private string menuSceneName = "MainMenu";

        [Header("Game Settings")]
        [SerializeField] private int minPlayersToStart = 2;

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
            base.OnNetworkSpawn();

            CurrentGameState.OnValueChanged += HandleGameStateChanged;
            PlayerList.OnListChanged += HandlePlayerListChanged;

            if (IsServer)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
                NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

                if (NetworkManager.Singleton.SceneManager != null)
                {
                    // NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadComplete;
                }

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

                if (NetworkManager.Singleton.SceneManager != null)
                {
                    // NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnSceneLoadComplete;
                }
            }

            base.OnNetworkDespawn();
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
                Score = 0,
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
                data.Score = 0;
                data.IsAlive = true;
                PlayerList[i] = data;
            }

            // Load game scene
            if (NetworkManager.Singleton.SceneManager == null)
            {
                Debug.LogError("[GameFlowManager] SceneManager is null!");
            }
        }

        private void OnSceneLoadComplete(string sceneName, LoadSceneMode loadSceneMode,
            List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (!IsServer) return;

            if (sceneName == gameSceneName)
            {
                CurrentGameState.Value = GameState.Playing;
                OnGameStarted?.Invoke();
                NotifyGameStartedClientRpc();
            }
        }

        /// <summary>
        /// Called when a player dies
        /// </summary>
        public void NotifyPlayerDied(ulong clientId)
        {
            if (!IsServer) return;

            int index = GetPlayerIndex(clientId);
            if (index >= 0)
            {
                var data = PlayerList[index];
                data.IsAlive = false;
                PlayerList[index] = data;
            }

            OnPlayerDied?.Invoke(clientId);
            CheckGameOver();
        }

        /// <summary>
        /// Add score to player
        /// </summary>
        public void AddScore(ulong clientId, int score)
        {
            if (!IsServer) return;

            int index = GetPlayerIndex(clientId);
            if (index >= 0)
            {
                var data = PlayerList[index];
                data.Score += score;
                PlayerList[index] = data;
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
                // Game over
                CurrentGameState.Value = GameState.GameOver;

                if (alivePlayers == 1)
                {
                    OnPlayerWon?.Invoke(lastAlivePlayer);
                    DeclareWinnerClientRpc(lastAlivePlayer);
                }
                else
                {
                    DeclareWinnerClientRpc(ulong.MaxValue); // No winner
                }

                OnGameEnded?.Invoke();
            }
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
                data.Score = 0;
                PlayerList[i] = data;
            }

            NetworkManager.Singleton.SceneManager.LoadScene(menuSceneName, LoadSceneMode.Single);
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

            SceneManager.LoadScene(menuSceneName);
        }

        #endregion

        #region Client RPCs

        [Rpc(SendTo.NotServer)]
        private void NotifyGameStartedClientRpc() => OnGameStarted?.Invoke();

        [Rpc(SendTo.NotServer)]
        private void DeclareWinnerClientRpc(ulong winnerId)
        {
            if (winnerId != ulong.MaxValue)
                OnPlayerWon?.Invoke(winnerId);
            OnGameEnded?.Invoke();
        }

        #endregion

        #region Event Handlers

        private void HandleGameStateChanged(GameState oldValue, GameState newValue) => OnGameStateChanged?.Invoke(newValue);

        private void HandlePlayerListChanged(NetworkListEvent<PlayerNetworkData> changeEvent) { }

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
        public int Score;
        public bool IsAlive;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref PlayerName);
            serializer.SerializeValue(ref Score);
            serializer.SerializeValue(ref IsAlive);
        }

        public bool Equals(PlayerNetworkData other)
        {
            return ClientId == other.ClientId;
        }

        public override string ToString()
        {
            return $"[Player {ClientId}] {PlayerName} | Score: {Score} | Alive: {IsAlive}";
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
}
