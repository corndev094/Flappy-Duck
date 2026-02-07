using Unity.Netcode;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

/// <summary>
/// Server-authoritative pipe spawner for multiplayer.
/// Only the server spawns pipes, which are then synced to all clients.
/// </summary>
public class NetworkPipeSpawner : NetworkBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject pipePrefab;
    
    [Header("Spawn Settings")]
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float pipeOffsetY = 1f;
    [SerializeField] private float minY = -1f;
    [SerializeField] private float maxY = 3f;
    [SerializeField] private float pipeSpacing = 5f;
    [SerializeField] private float initialXPosition = 7f;

    [Header("Cleanup")]
    [SerializeField] private float destroyAfterX = -15f;

    // Network synced spawn position for deterministic spawning
    public NetworkVariable<float> CurrentXPos = new(7f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsSpawning = new(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private CancellationTokenSource spawnCts;

    #region Network Lifecycle

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            CurrentXPos.Value = initialXPosition;
        }

        // Subscribe to game state changes
        if (MatchFlowManager.Instance != null)
        {
            MatchFlowManager.Instance.OnGameStateChanged += HandleGameStateChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        StopSpawning();
        
        if (MatchFlowManager.Instance != null)
        {
            MatchFlowManager.Instance.OnGameStateChanged -= HandleGameStateChanged;
        }

        base.OnNetworkDespawn();
    }

    #endregion

    #region Game State

    private void HandleGameStateChanged(GameState newState)
    {
        if (!IsServer) return;

        switch (newState)
        {
            case GameState.Playing:
                StartSpawning();
                break;
            case GameState.GameOver:
            case GameState.Lobby:
                StopSpawning();
                break;
        }
    }

    #endregion

    #region Spawning Control

    /// <summary>
    /// Start spawning pipes (Server only)
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void StartSpawningServerRpc()
    {
        StartSpawning();
    }

    /// <summary>
    /// Stop spawning pipes (Server only)
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void StopSpawningServerRpc()
    {
        StopSpawning();
    }

    private void StartSpawning()
    {
        if (!IsServer) return;
        if (IsSpawning.Value) return;

        IsSpawning.Value = true;
        spawnCts = new CancellationTokenSource();
        SpawnLoop(spawnCts.Token).Forget();

        Debug.Log("[NetworkPipeSpawner] Started spawning pipes");
    }

    private void StopSpawning()
    {
        if (!IsServer) return;

        IsSpawning.Value = false;
        spawnCts?.Cancel();
        spawnCts?.Dispose();
        spawnCts = null;

        Debug.Log("[NetworkPipeSpawner] Stopped spawning pipes");
    }

    private async UniTaskVoid SpawnLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested && IsSpawning.Value)
        {
            try
            {
                await UniTask.Delay((int)(spawnInterval * 1000), cancellationToken: token);
                
                if (token.IsCancellationRequested) break;
                
                SpawnPipe();
                CurrentXPos.Value += pipeSpacing;
            }
            catch (System.OperationCanceledException)
            {
                break;
            }
        }
    }

    #endregion

    #region Pipe Spawning

    private void SpawnPipe()
    {
        if (!IsServer) return;
        if (pipePrefab == null)
        {
            Debug.LogError("[NetworkPipeSpawner] Pipe prefab is not assigned!");
            return;
        }

        // Generate random values on server
        float randomY = Random.Range(minY, maxY);
        float randomOffset = Random.Range(0, pipeOffsetY);
        Vector3 spawnPosition = new Vector3(CurrentXPos.Value, randomY, transform.position.z);

        // Check if prefab has NetworkObject
        var prefabNetworkObject = pipePrefab.GetComponent<NetworkObject>();
        
        if (prefabNetworkObject != null)
        {
            // Spawn as NetworkObject (synced automatically)
            SpawnNetworkPipe(spawnPosition, randomOffset);
        }
        else
        {
            // Spawn locally and sync via RPC
            SpawnLocalPipeWithRpc(spawnPosition, randomOffset);
        }
    }

    private void SpawnNetworkPipe(Vector3 position, float offset)
    {
        GameObject pipeInstance = Instantiate(pipePrefab, position, Quaternion.identity);
        
        var networkObject = pipeInstance.GetComponent<NetworkObject>();
        networkObject.Spawn();

        // Setup pipe offset
        var pipe = pipeInstance.GetComponent<IPipe>();
        if (pipe != null)
        {
            pipe.Setup(offset);
            
            // Sync offset to clients
            var networkPipe = pipeInstance.GetComponent<NetworkPipe>();
            if (networkPipe != null)
            {
                networkPipe.SetOffset(offset);
            }
        }

        // Schedule cleanup
        SchedulePipeCleanup(pipeInstance).Forget();
    }

    private void SpawnLocalPipeWithRpc(Vector3 position, float offset)
    {
        // Spawn on server
        GameObject pipeInstance = Instantiate(pipePrefab, position, Quaternion.identity);
        
        var pipe = pipeInstance.GetComponent<IPipe>();
        pipe?.Setup(offset);

        // Tell clients to spawn
        SpawnPipeClientRpc(position, offset);

        // Schedule cleanup
        SchedulePipeCleanup(pipeInstance).Forget();
    }

    [ClientRpc]
    private void SpawnPipeClientRpc(Vector3 position, float offset)
    {
        if (IsServer) return; // Server already spawned

        GameObject pipeInstance = Instantiate(pipePrefab, position, Quaternion.identity);
        
        var pipe = pipeInstance.GetComponent<IPipe>();
        pipe?.Setup(offset);

        // Client-side cleanup (based on position)
        CleanupPipeWhenOffscreen(pipeInstance).Forget();
    }

    #endregion

    #region Cleanup

    private async UniTaskVoid SchedulePipeCleanup(GameObject pipe)
    {
        if (pipe == null) return;

        var token = this.GetCancellationTokenOnDestroy();

        try
        {
            // Wait until pipe is off screen
            while (pipe != null && pipe.transform.position.x > destroyAfterX)
            {
                await UniTask.Delay(500, cancellationToken: token);
            }

            if (pipe != null)
            {
                var networkObject = pipe.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned)
                {
                    networkObject.Despawn();
                }
                else
                {
                    Destroy(pipe);
                }
            }
        }
        catch (System.OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    private async UniTaskVoid CleanupPipeWhenOffscreen(GameObject pipe)
    {
        if (pipe == null) return;

        var token = this.GetCancellationTokenOnDestroy();

        try
        {
            while (pipe != null && pipe.transform.position.x > destroyAfterX)
            {
                await UniTask.Delay(500, cancellationToken: token);
            }

            if (pipe != null)
            {
                Destroy(pipe);
            }
        }
        catch (System.OperationCanceledException)
        {
            // Expected on shutdown
        }
    }

    /// <summary>
    /// Clears all spawned pipes (Server only)
    /// </summary>
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ClearAllPipesServerRpc()
    {
        if (!IsServer) return;

        // Find all pipes with NetworkObject
        var networkPipes = FindObjectsByType<NetworkPipe>(FindObjectsSortMode.None);
        foreach (var pipe in networkPipes)
        {
            var netObj = pipe.GetComponent<NetworkObject>();
            if (netObj != null && netObj.IsSpawned)
            {
                netObj.Despawn();
            }
        }

        // Also clear non-network pipes
        ClearLocalPipesClientRpc();

        CurrentXPos.Value = initialXPosition;
    }

    [ClientRpc]
    private void ClearLocalPipesClientRpc()
    {
        var pipes = FindObjectsByType<Pipe>(FindObjectsSortMode.None);
        foreach (var pipe in pipes)
        {
            if (pipe.GetComponent<NetworkObject>() == null)
            {
                Destroy(pipe.gameObject);
            }
        }
    }

    #endregion
}

/// <summary>
/// NetworkBehaviour component for pipes that need to be synced.
/// Add this to pipe prefab alongside NetworkObject.
/// </summary>
public class NetworkPipe : NetworkBehaviour, IPipe
{
    [SerializeField] private Transform abovePipe;
    [SerializeField] private Transform underPipe;

    public NetworkVariable<float> PipeOffset = new(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private Vector3 abovePipeInitialPos;
    private Vector3 underPipeInitialPos;

    private void Awake()
    {
        if (abovePipe != null) abovePipeInitialPos = abovePipe.localPosition;
        if (underPipe != null) underPipeInitialPos = underPipe.localPosition;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Apply offset when spawned
        PipeOffset.OnValueChanged += OnOffsetChanged;
        
        if (PipeOffset.Value != 0)
        {
            UpdatePipePositions(PipeOffset.Value);
        }
    }

    public override void OnNetworkDespawn()
    {
        PipeOffset.OnValueChanged -= OnOffsetChanged;
        base.OnNetworkDespawn();
    }

    private void OnOffsetChanged(float oldValue, float newValue)
    {
        UpdatePipePositions(newValue);
    }

    public void Setup(float offset)
    {
        if (IsServer)
        {
            PipeOffset.Value = offset;
        }
        UpdatePipePositions(offset);
    }

    public void SetOffset(float offset)
    {
        if (IsServer)
        {
            PipeOffset.Value = offset;
        }
    }

    private void UpdatePipePositions(float offset)
    {
        if (abovePipe != null)
            abovePipe.localPosition = abovePipeInitialPos + new Vector3(0, offset, 0);
        
        if (underPipe != null)
            underPipe.localPosition = underPipeInitialPos - new Vector3(0, offset, 0);
    }
}
