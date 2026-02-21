using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// Manages matchmaking with Unity Lobby + Relay.
/// Supports both quick join and filtered matchmaking.
/// NO LOBBY UI - Auto-starts game after match found.
/// </summary>
[DefaultExecutionOrder(-1000)]
public class MatchmakingManager : NetworkBehaviour
{
    public static MatchmakingManager Instance { get; private set; }

    [Header("Matchmaking Configs")]
    [SerializeField] private int maxPlayer = 4;
    [SerializeField] private int minPlayer = 2;

    [Header("Auto-Start Settings")]
    [SerializeField] private float matchFoundDelay = 2f; // Delay before starting game
    [SerializeField] private float gracePeriod = 1; // Wait time after min players for more to join
    [SerializeField] private float hostWaitTimeout = 30f; // Max wait time for players
    [SerializeField] private int minimumPlayersToStart = 2;
    [SerializeField] private float connectionTimeout = 20f; // Client connection timeout (increased from 10s)
    [SerializeField] private float hostStartDelay = 2f; // Delay before host starts listening (give relay time)

    // Lobby Data Keys
    private const string KEY_JOIN_CODE = "joinCode";
    private const string KEY_GAME_MODE = "S1";

    public Lobby CurrentLobby { get; private set; }
    public bool IsMatchingInProgress { get; private set; }
    public bool IsLobbyHost { get; private set; }
    public bool IsInLobby => CurrentLobby != null;

    // Events
    public event Action<Lobby> OnJoinedLobby;
    public event Action OnLeftLobby;
    public event Action<MatchingResult> OnMatchmakingCompleted;
    public event Action<string> OnMatchmakingFailed;

    // Host timeout tracking
    private float hostWaitTimer;
    private bool isWaitingForPlayers;
    private bool isInGracePeriod;
    private float gracePeriodTimer;

    // Services initialization
    private bool isServicesReady;

    // Lobby heartbeat
    private bool isHeartbeatRunning;
    private const float HEARTBEAT_INTERVAL = 15f; // Send heartbeat every 15 seconds

    #region Unity Lifecycle

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeServices().Forget();
    }

    void Update()
    {
        HandleHostWaitTimeout();
    }

    private void OnApplicationQuit()
    {
        // Critical: Clean up lobby when application quits
        // This prevents "ghost" lobbies that clients can find but can't join
        StopLobbyHeartbeat();

        if (IsLobbyHost && CurrentLobby != null)
        {
            // Fire-and-forget: Best effort to delete lobby
            // Can't await in OnApplicationQuit, but service will attempt deletion
            try
            {
                LobbyService.Instance.DeleteLobbyAsync(CurrentLobby.Id);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Matchmaking] Failed to cleanup lobby: {e.Message}");
            }
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this)
        {
            Instance = null;
            StopLobbyHeartbeat();
            LeaveLobby().Forget();
        }
    }

    #endregion

    #region Initialization

    private async UniTaskVoid InitializeServices()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            isServicesReady = true;
            Debug.Log($"[Matchmaking] Initialized as {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Matchmaking] Init failed: {e.Message}");
            isServicesReady = false;
        }
    }

    private async UniTask WaitForServicesReady()
    {
        if (isServicesReady) return;

        float timeout = 10f;
        float elapsed = 0f;

        while (!isServicesReady && elapsed < timeout)
        {
            await UniTask.Delay(100);
            elapsed += 0.1f;
        }

        if (!isServicesReady)
        {
            throw new Exception("Services initialization timeout. Please restart the application.");
        }
    }

    #endregion

    #region Host Timeout Management

    private void HandleHostWaitTimeout()
    {
        if (!IsLobbyHost || !isWaitingForPlayers) return;

        hostWaitTimer += Time.deltaTime;

        // Validate NetworkManager exists
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[Matchmaking] NetworkManager.Singleton is null! Please add NetworkManager to the scene.");
            OnMatchmakingFailed?.Invoke("NetworkManager not found");
            OnMatchmakingCompleted?.Invoke(MatchingResult.Failed);
            isWaitingForPlayers = false;
            LeaveLobby().Forget();
            return;
        }

        // Get current player count (prefer GameFlowManager if available)
        int currentPlayers;
        if (GameFlowManager.Instance != null)
        {
            currentPlayers = GameFlowManager.Instance.PlayerCount;
        }
        else
        {
            currentPlayers = NetworkManager.Singleton.ConnectedClientsIds.Count;
        }

        if (currentPlayers >= minimumPlayersToStart)
        {
            if (!isInGracePeriod)
            {
                isInGracePeriod = true;
                gracePeriodTimer = 0f;
            }
            else
            {
                gracePeriodTimer += Time.deltaTime;

                if (currentPlayers >= maxPlayer || gracePeriodTimer >= gracePeriod)
                {
                    isWaitingForPlayers = false;
                    isInGracePeriod = false;
                    StartGameAfterDelay().Forget();
                    return;
                }
            }
        }
        else if (hostWaitTimer >= hostWaitTimeout)
        {
            Debug.Log($"[Matchmaking] Timeout: No players joined");
            OnMatchmakingFailed?.Invoke("No players found");
            OnMatchmakingCompleted?.Invoke(MatchingResult.Timeout);

            isWaitingForPlayers = false;
            isInGracePeriod = false;
            LeaveLobby().Forget();
        }
    }

    #endregion

    #region Lobby Heartbeat

    /// <summary>
    /// Start sending heartbeat pings to keep lobby alive
    /// Unity Lobbies require heartbeat every 15-30 seconds or they become stale
    /// </summary>
    private async UniTaskVoid StartLobbyHeartbeat()
    {
        if (!IsLobbyHost || CurrentLobby == null)
        {
            Debug.LogWarning("[Matchmaking] Cannot start heartbeat: Not host or no lobby");
            return;
        }

        if (isHeartbeatRunning)
        {
            Debug.LogWarning("[Matchmaking] Heartbeat already running");
            return;
        }

        isHeartbeatRunning = true;

        while (isHeartbeatRunning && CurrentLobby != null)
        {
            try
            {
                await UniTask.Delay((int)(HEARTBEAT_INTERVAL * 1000));
                
                if (!isHeartbeatRunning || CurrentLobby == null)
                    break;

                await LobbyService.Instance.SendHeartbeatPingAsync(CurrentLobby.Id);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Matchmaking] Heartbeat failed: {e.Message}");
                // Continue trying - lobby might still be valid
            }
        }
    }

    /// <summary>
    /// Stop the heartbeat loop
    /// </summary>
    private void StopLobbyHeartbeat()
    {
        isHeartbeatRunning = false;
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validate that all required components are present
    /// </summary>
    private bool ValidateNetworkSetup(out string errorMessage)
    {
        // Check NetworkManager
        if (NetworkManager.Singleton == null)
        {
            errorMessage = "NetworkManager not found in scene.\n\n" +
                            "SETUP INSTRUCTIONS:\n" +
                            "1. Add a NetworkManager GameObject to your Bootstrap scene\n" +
                            "2. Add UnityTransport component to it\n" +
                            "3. Ensure NetworkManager is spawned before matchmaking starts";
            Debug.LogError($"[Matchmaking] {errorMessage}");
            return false;
        }

        // Check UnityTransport
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            errorMessage = "UnityTransport component not found on NetworkManager.\n\n" +
                            "SETUP INSTRUCTIONS:\n" +
                            "1. Select the NetworkManager GameObject in your scene\n" +
                            "2. Add Component -> Netcode -> UnityTransport\n" +
                            "3. Set it as the active transport in NetworkManager";
            Debug.LogError($"[Matchmaking] {errorMessage}");
            return false;
        }

        // Check GameFlowManager (warning only, not critical)
        if (GameFlowManager.Instance == null)
        {
            Debug.LogWarning("[Matchmaking] GameFlowManager.Instance is null.\n\n" +
                            "RECOMMENDED SETUP:\n" +
                            "1. Add a GameFlowManager GameObject to your Bootstrap scene\n" +
                            "2. Add NetworkObject component to it\n" +
                            "3. GameFlowManager handles scene loading and game state\n" +
                            "Fallback: Will attempt direct scene loading if missing.");
        }

        errorMessage = null;
        return true;
    }

    #endregion

    #region Quick Matchmaking API

    /// <summary>
    /// Start quick matchmaking for any player join in a specific online map
    /// </summary>
    public async UniTask<MatchingResult> StartQuickMatchmaking()
    {
        if (IsMatchingInProgress)
        {
            Debug.LogWarning("[Matchmaking] Already in progress");
            return MatchingResult.Cancelled;
        }

        // Validate network setup
        if (!ValidateNetworkSetup(out string validationError))
        {
            OnMatchmakingFailed?.Invoke(validationError);
            OnMatchmakingCompleted?.Invoke(MatchingResult.Failed);
            return MatchingResult.Failed;
        }

        IsMatchingInProgress = true;

        try
        {
            await WaitForServicesReady();

            // Try quick join
            CurrentLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            IsLobbyHost = false;
            await JoinAsClient();

            OnJoinedLobby?.Invoke(CurrentLobby);
            OnMatchmakingCompleted?.Invoke(MatchingResult.Success);
            return MatchingResult.Success;
        }
        catch (Exception e)
        {
            // Clean up partial join
            if (CurrentLobby != null)
            {
                Debug.LogWarning($"[Matchmaking] Join failed: {e.Message}");
                await LeaveLobby();
            }

            // Create new lobby
            try
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                {
                    NetworkManager.Singleton.Shutdown();
                    await UniTask.Delay(500);
                }

                await CreateLobbyAndHost();

                OnJoinedLobby?.Invoke(CurrentLobby);
                OnMatchmakingCompleted?.Invoke(MatchingResult.Success);
                return MatchingResult.Success;
            }
            catch (Exception hostEx)
            {
                Debug.LogError($"[Matchmaking] Failed: {hostEx.Message}");
                OnMatchmakingFailed?.Invoke(hostEx.Message);
                OnMatchmakingCompleted?.Invoke(MatchingResult.Failed);
                return MatchingResult.Failed;
            }
        }
        finally
        {
            IsMatchingInProgress = false;
        }
    }

    #endregion

    #region Filter Matchmaking API

    /// <summary>
    /// Start matchmaking with specific conditions
    /// </summary>
    public async UniTask<MatchingResult> StartMatchmaking()
    {
        if (IsMatchingInProgress)
        {
            Debug.LogWarning("[Matchmaking] Already in progress");
            return MatchingResult.Cancelled;
        }

        // Validate network setup
        if (!ValidateNetworkSetup(out string validationError))
        {
            OnMatchmakingFailed?.Invoke(validationError);
            OnMatchmakingCompleted?.Invoke(MatchingResult.Failed);
            return MatchingResult.Failed;
        }

        IsMatchingInProgress = true;

        try
        {
            await WaitForServicesReady();

            var lobbies = await FindMatchingLobbies();

            if (lobbies.Count > 0)
            {
                foreach (var lobby in lobbies)
                {
                    try
                    {
                        CurrentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id);
                        IsLobbyHost = false;
                        await JoinAsClient();

                        OnJoinedLobby?.Invoke(CurrentLobby);
                        OnMatchmakingCompleted?.Invoke(MatchingResult.Success);
                        return MatchingResult.Success;
                    }
                    catch (Exception joinEx)
                    {
                        Debug.LogWarning($"[Matchmaking] Failed to join lobby: {joinEx.Message}");

                        if (CurrentLobby != null)
                            await LeaveLobby();

                        await UniTask.Delay(1000);
                        continue;
                    }
                }
            }

            // Create new lobby as host
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
                await UniTask.Delay(500);
            }

            await CreateLobbyAndHost();

            OnJoinedLobby?.Invoke(CurrentLobby);
            OnMatchmakingCompleted?.Invoke(MatchingResult.Success);
            return MatchingResult.Success;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Matchmaking] Failed: {e.Message}");
            OnMatchmakingFailed?.Invoke(e.Message);
            OnMatchmakingCompleted?.Invoke(MatchingResult.Failed);
            return MatchingResult.Failed;
        }
        finally
        {
            IsMatchingInProgress = false;
        }
    }

    /// <summary>
    /// Query for available lobbies and filter out stale ones
    /// Returns list of fresh lobbies (updated within last 30 seconds)
    /// </summary>
    private async UniTask<List<Lobby>> FindMatchingLobbies()
    {
        var queryOptions = new QueryLobbiesOptions
        {
            Count = 10,
            Filters = new List<QueryFilter>
            {
                new(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
            },
            Order = new List<QueryOrder>
            {
                new(asc: false, field: QueryOrder.FieldOptions.AvailableSlots)
            }
        };

        try
        {
            var response = await LobbyService.Instance.QueryLobbiesAsync(queryOptions);

            const int STALE_THRESHOLD_SECONDS = 30;
            var freshLobbies = response.Results.Where(lobby =>
            {
                var secondsSinceUpdate = (DateTime.UtcNow - lobby.LastUpdated).TotalSeconds;
                bool isRecentlyUpdated = secondsSinceUpdate < STALE_THRESHOLD_SECONDS;
                bool hasActivePlayers = lobby.Players != null && lobby.Players.Count > 0;
                bool hasSlots = lobby.AvailableSlots > 0;
                return isRecentlyUpdated && hasActivePlayers && hasSlots;
            }).ToList();

            return freshLobbies;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogWarning($"[Matchmaking] Query failed: {e.Message}");
            return new List<Lobby>();
        }
    }

    #endregion

    #region Lobby Creation & Joining API

    private async UniTask CreateLobbyAndHost()
    {
        // NetworkManager and UnityTransport already validated in ValidateNetworkSetup()
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // Create Relay allocation
        var allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayer);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        // Create lobby data
        var lobbyData = new Dictionary<string, DataObject>
        {
            { KEY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, joinCode) },
        };

        var createOptions = new CreateLobbyOptions
        {
            IsPrivate = false,
            Data = lobbyData,
            Player = CreatePlayerData()
        };

        CurrentLobby = await LobbyService.Instance.CreateLobbyAsync(
            $"Match_{UnityEngine.Random.Range(0, 1000)}",
            maxPlayer,
            createOptions
        );

        IsLobbyHost = true;
        Debug.Log($"[Matchmaking] Created lobby: {CurrentLobby.Id}, JoinCode: {joinCode}");

        // Setup Relay transport
        transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));

        // Start host
        NetworkManager.Singleton.StartHost();

        // Wait for host to be fully ready
        await UniTask.Delay((int)(hostStartDelay * 1000));

        // Verify host is listening
        if (!NetworkManager.Singleton.IsListening || !NetworkManager.Singleton.IsServer)
        {
            throw new Exception("Host failed to start listening. Please try again.");
        }

        Debug.Log("[Matchmaking] Host ready, waiting for players...");

        // Start lobby heartbeat to keep lobby alive
        StartLobbyHeartbeat().Forget();

        // Start waiting for players (with timeout)
        isWaitingForPlayers = true;
        isInGracePeriod = false;
        hostWaitTimer = 0f;
        gracePeriodTimer = 0f;
    }

    private async UniTask JoinAsClient()
    {
        // NetworkManager and UnityTransport already validated in ValidateNetworkSetup()
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // Refresh lobby data to ensure we have the join code (member-only visibility)
        if (!CurrentLobby.Data.ContainsKey(KEY_JOIN_CODE) || string.IsNullOrEmpty(CurrentLobby.Data[KEY_JOIN_CODE].Value))
        {
            CurrentLobby = await LobbyService.Instance.GetLobbyAsync(CurrentLobby.Id);
        }

        if (!CurrentLobby.Data.ContainsKey(KEY_JOIN_CODE))
        {
            throw new Exception("Join code not found in lobby data. Lobby may not be properly configured.");
        }

        string joinCode = CurrentLobby.Data[KEY_JOIN_CODE].Value;
        
        // Small delay to give host time to setup
        await UniTask.Delay(1000);
        
        // Try to join Relay allocation
        Unity.Services.Relay.Models.JoinAllocation joinAlloc;
        try
        {
            joinAlloc = await RelayService.Instance.JoinAllocationAsync(joinCode);
        }
        catch (Unity.Services.Relay.RelayServiceException relayEx)
        {
            // Specific error for stale/invalid join code
            if (relayEx.Message.Contains("join code not found") || relayEx.Message.Contains("Not Found"))
            {
                throw new Exception($"Relay allocation not found. Host may have disconnected. (Join code: {joinCode})");
            }
            else
            {
                throw new Exception($"Failed to join Relay: {relayEx.Message}");
            }
        }

        transport.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAlloc, "dtls"));

        NetworkManager.Singleton.StartClient();

        // Wait for connection with timeout
        float elapsed = 0f;
        while (!NetworkManager.Singleton.IsConnectedClient && elapsed < connectionTimeout)
        {
            await UniTask.Delay(100);
            elapsed += 0.1f;
        }

        if (!NetworkManager.Singleton.IsConnectedClient)
        {
            // Log detailed error info
            Debug.LogError($"[Matchmaking] Connection timeout after {connectionTimeout}s");
            Debug.LogError($"[Matchmaking] NetworkManager.IsClient: {NetworkManager.Singleton.IsClient}");
            Debug.LogError($"[Matchmaking] NetworkManager.IsConnectedClient: {NetworkManager.Singleton.IsConnectedClient}");
            Debug.LogError($"[Matchmaking] NetworkManager.IsListening: {NetworkManager.Singleton.IsListening}");
            
            throw new Exception($"Failed to connect to host after {connectionTimeout} seconds. Host may not be ready or network issues.");
        }

        Debug.Log("[Matchmaking] Client connected successfully!");
        // Note: Host will trigger game start via HandleHostWaitTimeout()
        // Clients don't call StartGameAfterDelay() - only host controls game start
    }

    private Player CreatePlayerData()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, $"Player_{UnityEngine.Random.Range(1000, 9999)}") },
            }
        };
    }

    #endregion

    #region Auto-Start Game

    private async UniTaskVoid StartGameAfterDelay()
    {
        await UniTask.Delay((int)(matchFoundDelay * 1000));

        if (!IsLobbyHost) return;

        // Verify player count hasn't dropped during the delay
        int currentPlayers = GameFlowManager.Instance != null
            ? GameFlowManager.Instance.PlayerCount
            : NetworkManager.Singleton.ConnectedClientsIds.Count;

        if (currentPlayers < minimumPlayersToStart)
        {
            Debug.Log("[Matchmaking] Player dropped, resuming wait...");
            isWaitingForPlayers = true;
            isInGracePeriod = false;
            return;
        }

        // Close lobby to new players
        await CloseLobbyToNewPlayers();

        // Start game
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.StartGameServerRpc();
        }
        else
        {
            Debug.LogError("[Matchmaking] Cannot start game: GameFlowManager unavailable");
        }
    }

    #endregion

    #region Cancel Matchmaking

    /// <summary>
    /// Cancel ongoing matchmaking
    /// </summary>
    public async UniTask CancelMatchmaking()
    {
        if (!IsMatchingInProgress && !isWaitingForPlayers) return;

        await LeaveLobby();

        IsMatchingInProgress = false;
        isWaitingForPlayers = false;
        hostWaitTimer = 0f;

        OnMatchmakingCompleted?.Invoke(MatchingResult.Cancelled);
    }

    #endregion

    #region Lobby Management

    /// <summary>
    /// Close lobby to prevent new players from joining
    /// </summary>
    private async UniTask CloseLobbyToNewPlayers()
    {
        if (CurrentLobby == null || !IsLobbyHost) return;

        StopLobbyHeartbeat();

        try
        {
            await LobbyService.Instance.UpdateLobbyAsync(CurrentLobby.Id, new UpdateLobbyOptions { IsPrivate = true });
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Matchmaking] Failed to close lobby: {e.Message}");
        }
    }

    public async UniTask LeaveLobby()
    {
        if (CurrentLobby == null) return;

        // Stop heartbeat first
        StopLobbyHeartbeat();

        try
        {
            string playerId = AuthenticationService.Instance.PlayerId;

            if (IsLobbyHost)
                await LobbyService.Instance.DeleteLobbyAsync(CurrentLobby.Id);
            else
                await LobbyService.Instance.RemovePlayerAsync(CurrentLobby.Id, playerId);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Matchmaking] Error leaving lobby: {e.Message}");
        }
        finally
        {
            CurrentLobby = null;
            IsLobbyHost = false;
            isWaitingForPlayers = false;
            isInGracePeriod = false;
            hostWaitTimer = 0f;
            gracePeriodTimer = 0f;
            OnLeftLobby?.Invoke();

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }
    }

    #endregion
}

#region Data Structures

public enum MatchingResult
{
    Success,
    Failed,
    Cancelled,
    Timeout
}

#endregion