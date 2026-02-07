using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// Manages Unity Relay service for P2P multiplayer connections.
/// Handles authentication, relay allocation, and connection setup.
/// </summary>
public class RelayManager : Singleton<RelayManager>
{
    [Header("Settings")]
    [SerializeField] private int maxPlayers = 4;
    [SerializeField] private string relayRegion = null; // null = auto select best region

    // Connection state
    public bool IsConnecting { get; private set; }
    public bool IsConnected => NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient;
    public bool IsHost => NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
    public string CurrentJoinCode { get; private set; }

    // Events
    public event Action OnConnecting;
    public event Action<string> OnHostCreated; // joinCode
    public event Action OnClientJoined;
    public event Action<string> OnConnectionFailed; // error message
    public event Action OnDisconnected;

    // Authentication state
    public bool IsAuthenticated => AuthenticationService.Instance?.IsSignedIn ?? false;
    public string PlayerId => AuthenticationService.Instance?.PlayerId ?? string.Empty;

    #region Public API

    /// <summary>
    /// Creates a new relay host and returns the join code.
    /// </summary>
    public async Task<string> CreateHost()
    {
        if (IsConnecting)
        {
            Debug.LogWarning("[RelayManager] Already connecting...");
            return null;
        }

        IsConnecting = true;
        OnConnecting?.Invoke();

        try
        {
            await AuthenticateAsync();

            Debug.Log($"[RelayManager] Creating relay allocation for {maxPlayers} players...");
            
            Allocation allocation = string.IsNullOrEmpty(relayRegion) 
                ? await RelayService.Instance.CreateAllocationAsync(maxPlayers)
                : await RelayService.Instance.CreateAllocationAsync(maxPlayers, relayRegion);
            
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            CurrentJoinCode = joinCode;

            // Configure transport
            ConfigureTransport(allocation);

            // Start host
            if (!NetworkManager.Singleton.StartHost())
            {
                throw new Exception("Failed to start NetworkManager host");
            }

            Debug.Log($"[RelayManager] Host created successfully! Join Code: {joinCode}");
            OnHostCreated?.Invoke(joinCode);
            
            return joinCode;
        }
        catch (Exception e)
        {
            string errorMsg = $"Failed to create host: {e.Message}";
            Debug.LogError($"[RelayManager] {errorMsg}");
            OnConnectionFailed?.Invoke(errorMsg);
            return null;
        }
        finally
        {
            IsConnecting = false;
        }
    }

    /// <summary>
    /// Joins an existing relay using the provided join code.
    /// </summary>
    public async Task<bool> JoinGame(string joinCode)
    {
        if (IsConnecting)
        {
            Debug.LogWarning("[RelayManager] Already connecting...");
            return false;
        }

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            OnConnectionFailed?.Invoke("Join code cannot be empty");
            return false;
        }

        IsConnecting = true;
        OnConnecting?.Invoke();

        try
        {
            await AuthenticateAsync();

            Debug.Log($"[RelayManager] Joining relay with code: {joinCode}");
            
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim().ToUpper());
            CurrentJoinCode = joinCode;

            // Configure transport
            ConfigureTransport(joinAllocation);

            // Start client
            if (!NetworkManager.Singleton.StartClient())
            {
                throw new Exception("Failed to start NetworkManager client");
            }

            Debug.Log("[RelayManager] Client connected successfully!");
            OnClientJoined?.Invoke();
            
            return true;
        }
        catch (RelayServiceException e)
        {
            string errorMsg = e.Reason switch
            {
                RelayExceptionReason.JoinCodeNotFound => "Invalid join code. Please check and try again.",
                RelayExceptionReason.AllocationNotFound => "Room no longer exists.",
                RelayExceptionReason.RegionNotFound => "Region not available.",
                _ => $"Relay error: {e.Message}"
            };
            
            Debug.LogError($"[RelayManager] {errorMsg}");
            OnConnectionFailed?.Invoke(errorMsg);
            return false;
        }
        catch (Exception e)
        {
            string errorMsg = $"Failed to join: {e.Message}";
            Debug.LogError($"[RelayManager] {errorMsg}");
            OnConnectionFailed?.Invoke(errorMsg);
            return false;
        }
        finally
        {
            IsConnecting = false;
        }
    }

    /// <summary>
    /// Disconnects from current session and shuts down NetworkManager.
    /// </summary>
    public void Disconnect()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            NetworkManager.Singleton.Shutdown();
            Debug.Log("[RelayManager] Disconnected from network");
        }
        
        CurrentJoinCode = null;
        OnDisconnected?.Invoke();
    }

    /// <summary>
    /// Ensures Unity Services are initialized and user is authenticated.
    /// </summary>
    public async Task AuthenticateAsync()
    {
        // Initialize Unity Services
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            Debug.Log("[RelayManager] Initializing Unity Services...");
            
            var options = new InitializationOptions();
            
            // For testing: use unique profile per instance
            #if UNITY_EDITOR
            if (ParrelSync.ClonesManager.IsClone())
            {
                string customArgument = ParrelSync.ClonesManager.GetArgument();
                options.SetProfile($"Clone_{customArgument}");
            }
            #endif
            
            await UnityServices.InitializeAsync(options);
        }

        // Sign in anonymously if not signed in
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("[RelayManager] Signing in anonymously...");
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"[RelayManager] Signed in as: {AuthenticationService.Instance.PlayerId}");
        }
    }

    /// <summary>
    /// Gets available relay regions.
    /// </summary>
    public async Task<System.Collections.Generic.List<Region>> GetAvailableRegions()
    {
        try
        {
            await AuthenticateAsync();
            return await RelayService.Instance.ListRegionsAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayManager] Failed to get regions: {e.Message}");
            return new System.Collections.Generic.List<Region>();
        }
    }

    #endregion

    #region Private Helpers

    private void ConfigureTransport(Allocation allocation)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            throw new Exception("UnityTransport component not found on NetworkManager");
        }

        transport.SetRelayServerData(
            allocation.RelayServer.IpV4,
            (ushort)allocation.RelayServer.Port,
            allocation.AllocationIdBytes,
            allocation.Key,
            allocation.ConnectionData,
            allocation.ConnectionData, // Host uses same connection data
            isSecure: true
        );
    }

    private void ConfigureTransport(JoinAllocation joinAllocation)
    {
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        if (transport == null)
        {
            throw new Exception("UnityTransport component not found on NetworkManager");
        }

        transport.SetRelayServerData(
            joinAllocation.RelayServer.IpV4,
            (ushort)joinAllocation.RelayServer.Port,
            joinAllocation.AllocationIdBytes,
            joinAllocation.Key,
            joinAllocation.ConnectionData,
            joinAllocation.HostConnectionData, // Client uses host's connection data
            isSecure: true
        );
    }

    #endregion

    #region Editor Support

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    private void OnApplicationQuit()
    {
        Disconnect();
    }

    #endregion
}

#if UNITY_EDITOR
// Stub for ParrelSync when not installed
namespace ParrelSync
{
    public static class ClonesManager
    {
        public static bool IsClone()
        {
            // Check if this is a ParrelSync clone by looking for the clone marker
            string projectPath = UnityEngine.Application.dataPath;
            return projectPath.Contains("_clone_");
        }

        public static string GetArgument()
        {
            // Extract clone number from path
            string projectPath = UnityEngine.Application.dataPath;
            int index = projectPath.LastIndexOf("_clone_");
            if (index >= 0 && index + 7 < projectPath.Length)
            {
                return projectPath.Substring(index + 7, 1);
            }
            return "0";
        }
    }
}
#endif
