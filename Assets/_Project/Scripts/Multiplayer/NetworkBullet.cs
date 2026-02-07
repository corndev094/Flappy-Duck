using System;
using Unity.Netcode;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

/// <summary>
/// Network-synchronized bullet that is spawned by server and synced to all clients.
/// Server handles collision detection, clients just render.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class NetworkBullet : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float damage = 1f;
    [SerializeField] private float shootSpeed = 10f;
    [SerializeField] private float destroyAfterSeconds = 3f;

    [Header("Visual")]
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private ParticleSystem hitEffect;

    // Network synced position for smooth interpolation
    public NetworkVariable<Vector3> NetworkPosition = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<Quaternion> NetworkRotation = new(writePerm: NetworkVariableWritePermission.Server);

    // Owner info (who shot this bullet)
    public new NetworkVariable<ulong> OwnerClientId = new(writePerm: NetworkVariableWritePermission.Server);

    public float Damage => damage;
    public float ShootSpeed { get => shootSpeed; set => shootSpeed = value; }

    // Events
    public event Action<ABaseEnemy> OnHitEnemy;
    public event Action OnDestroyed;

    private CancellationTokenSource destroyCts;
    private Rigidbody2D rb;

    #region Network Lifecycle

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        rb = GetComponent<Rigidbody2D>();

        if (IsServer)
        {
            // Server controls the bullet
            StartDestroyTimer();
        }
        else
        {
            // Client: disable physics, just interpolate
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.simulated = false;
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        destroyCts?.Cancel();
        destroyCts?.Dispose();
        base.OnNetworkDespawn();
    }

    #endregion

    #region Update

    private void Update()
    {
        if (IsServer)
        {
            // Server moves the bullet
            transform.Translate(Vector3.right * Time.deltaTime * shootSpeed, Space.Self);

            // Update network position
            NetworkPosition.Value = transform.position;
            NetworkRotation.Value = transform.rotation;
        }
        else
        {
            // Clients interpolate to network position
            transform.position = Vector3.Lerp(transform.position, NetworkPosition.Value, Time.deltaTime * 20f);
            transform.rotation = Quaternion.Lerp(transform.rotation, NetworkRotation.Value, Time.deltaTime * 20f);
        }
    }

    #endregion

    #region Server Logic

    /// <summary>
    /// Initialize bullet with owner and direction. Call this after spawning on server.
    /// </summary>
    public void Initialize(ulong ownerClientId, Vector3 position, Quaternion rotation, float customDamage = -1)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[NetworkBullet] Initialize should only be called on server");
            return;
        }

        OwnerClientId.Value = ownerClientId;
        transform.position = position;
        transform.rotation = rotation;
        NetworkPosition.Value = position;
        NetworkRotation.Value = rotation;

        if (customDamage > 0)
        {
            damage = customDamage;
        }
    }

    private void StartDestroyTimer()
    {
        destroyCts = new CancellationTokenSource();
        DestroyAfterDelay(destroyCts.Token).Forget();
    }

    private async UniTaskVoid DestroyAfterDelay(CancellationToken token)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(destroyAfterSeconds), cancellationToken: token);
            
            if (IsServer && IsSpawned)
            {
                DespawnBullet();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when destroyed early
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        // Ignore collision with owner player
        if (collision.CompareTag("Player"))
        {
            var playerController = collision.GetComponent<PlayerNetworkController>();
            if (playerController != null && playerController.OwnerClientId == OwnerClientId.Value)
            {
                return; // Don't hit yourself
            }
        }

        // Ignore phase triggers
        if (collision.CompareTag("PhaseTrigger")) return;

        // Hit enemy
        if (collision.TryGetComponent<ABaseEnemy>(out var enemy))
        {
            enemy.TakeDamage(damage);
            OnHitEnemy?.Invoke(enemy);

            // Notify owner about hit (for stamina refill, score, etc.)
            NotifyHitClientRpc(OwnerClientId.Value, enemy.RefillStaminaForPlayer);
        }

        // Hit other player (PvP)
        if (collision.TryGetComponent<PlayerNetworkController>(out var player))
        {
            player.ServerTakeDamage(damage);
        }

        // Spawn hit effect on all clients
        PlayHitEffectClientRpc(transform.position);

        // Destroy bullet
        DespawnBullet();
    }

    private void DespawnBullet()
    {
        if (!IsServer || !IsSpawned) return;

        destroyCts?.Cancel();
        OnDestroyed?.Invoke();
        
        GetComponent<NetworkObject>().Despawn();
    }

    #endregion

    #region Client RPCs

    [ClientRpc]
    private void NotifyHitClientRpc(ulong ownerClientId, float staminaRefill)
    {
        // Only the bullet owner cares about this
        if (NetworkManager.Singleton.LocalClientId != ownerClientId) return;

        // Find local player and refill stamina
        var controllers = FindObjectsByType<PlayerNetworkController>(FindObjectsSortMode.None);
        foreach (var controller in controllers)
        {
            if (controller.IsOwner)
            {
                // Note: Stamina refill should be handled by server in real implementation
                // This is just for visual feedback
                Debug.Log($"[NetworkBullet] Hit enemy! Stamina refill: {staminaRefill}");
                break;
            }
        }
    }

    [ClientRpc]
    private void PlayHitEffectClientRpc(Vector3 position)
    {
        if (hitEffect != null)
        {
            var effect = Instantiate(hitEffect, position, Quaternion.identity);
            effect.Play();
            Destroy(effect.gameObject, effect.main.duration);
        }
    }

    #endregion

    #region Static Factory

    /// <summary>
    /// Spawns a network bullet from the server.
    /// </summary>
    public static NetworkBullet SpawnBullet(GameObject bulletPrefab, Vector3 position, Quaternion rotation, ulong ownerClientId)
    {
        if (!NetworkManager.Singleton.IsServer)
        {
            Debug.LogError("[NetworkBullet] SpawnBullet must be called on server");
            return null;
        }

        var bulletGO = Instantiate(bulletPrefab, position, rotation);
        var networkObject = bulletGO.GetComponent<NetworkObject>();
        var bullet = bulletGO.GetComponent<NetworkBullet>();

        if (networkObject == null || bullet == null)
        {
            Debug.LogError("[NetworkBullet] Bullet prefab missing NetworkObject or NetworkBullet component");
            Destroy(bulletGO);
            return null;
        }

        networkObject.Spawn();
        bullet.Initialize(ownerClientId, position, rotation);

        return bullet;
    }

    #endregion
}
