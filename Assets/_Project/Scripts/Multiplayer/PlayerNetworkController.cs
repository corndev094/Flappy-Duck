using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using System;

/// <summary>
/// Network controller for player duck. Handles input synchronization,
/// position interpolation, and server-authoritative movement.
/// Attach this to the Duck prefab alongside NetworkObject.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerNetworkController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private ABaseDuck duckController;
    [SerializeField] private PlayerInput playerInput;

    [Header("Network Settings")]
    [SerializeField] private float interpolationSpeed = 15f;
    [SerializeField] private float jumpForce = 8f;

    // NetworkVariables - Server authoritative
    public NetworkVariable<Vector3> NetworkPosition = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<float> NetworkRotation = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<float> NetworkHealth = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<float> NetworkStamina = new(writePerm: NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsFlying = new(writePerm: NetworkVariableWritePermission.Server);

    // Events
    public event Action OnPlayerDied;
    public event Action<float> OnHealthChanged;
    public event Action<float> OnStaminaChanged;

    private Rigidbody2D rb;
    private bool jumpInputPressed;
    private bool attackInputPressed;

    #region Lifecycle

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        if (duckController == null)
            duckController = GetComponent<ABaseDuck>();
        
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Subscribe to NetworkVariable changes
        NetworkHealth.OnValueChanged += HandleHealthChanged;
        NetworkStamina.OnValueChanged += HandleStaminaChanged;

        if (IsOwner)
        {
            SetupOwnerPlayer();
        }
        else
        {
            SetupRemotePlayer();
        }

        if (IsServer)
        {
            SetupServerPlayer();
        }
    }

    public override void OnNetworkDespawn()
    {
        NetworkHealth.OnValueChanged -= HandleHealthChanged;
        NetworkStamina.OnValueChanged -= HandleStaminaChanged;
        base.OnNetworkDespawn();
    }

    #endregion

    #region Setup

    private void SetupOwnerPlayer()
    {
        // Enable input for owner
        if (playerInput != null)
            playerInput.enabled = true;

        // Setup camera to follow this player
        if (Camera.main != null)
        {
            var camController = Camera.main.GetComponent<CameraController>();
            if (camController != null)
                camController.Target = transform;
        }

        // Initialize duck on owner (visual feedback)
        if (duckController != null)
        {
            duckController.Setup();
        }

        Debug.Log($"[Owner] Player spawned: ClientId={OwnerClientId}");
    }

    private void SetupRemotePlayer()
    {
        // Disable input for non-owners
        if (playerInput != null)
            playerInput.enabled = false;

        // Disable physics for remote players (server controls position)
        rb.bodyType = RigidbodyType2D.Kinematic;

        Debug.Log($"[Remote] Player spawned: ClientId={OwnerClientId}");
    }

    private void SetupServerPlayer()
    {
        // Server initializes authoritative state
        if (duckController != null)
        {
            duckController.Setup();
            NetworkHealth.Value = duckController.CurrentHP;
            NetworkStamina.Value = duckController.CurrentStamina;
        }

        NetworkPosition.Value = transform.position;
        NetworkRotation.Value = rb.rotation;
        IsFlying.Value = false;
    }

    #endregion

    #region Update Loop

    private void Update()
    {
        if (IsOwner)
        {
            HandleOwnerInput();
        }
        else
        {
            InterpolateRemotePlayer();
        }
    }

    private void FixedUpdate()
    {
        if (IsServer)
        {
            UpdateServerState();
        }
    }

    private void HandleOwnerInput()
    {
        // Send jump input to server
        if (jumpInputPressed)
        {
            SubmitJumpServerRpc();
            jumpInputPressed = false;
        }

        // Send attack input to server
        if (attackInputPressed)
        {
            SubmitAttackServerRpc();
            attackInputPressed = false;
        }
    }

    private void InterpolateRemotePlayer()
    {
        // Smoothly interpolate position for remote players
        transform.position = Vector3.Lerp(
            transform.position,
            NetworkPosition.Value,
            Time.deltaTime * interpolationSpeed
        );

        // Interpolate rotation
        float currentRotation = rb.rotation;
        float targetRotation = NetworkRotation.Value;
        rb.rotation = Mathf.LerpAngle(currentRotation, targetRotation, Time.deltaTime * interpolationSpeed);
    }

    private void UpdateServerState()
    {
        // Server updates NetworkVariables with authoritative state
        NetworkPosition.Value = rb.position;
        NetworkRotation.Value = rb.rotation;

        if (duckController != null)
        {
            NetworkHealth.Value = duckController.CurrentHP;
            NetworkStamina.Value = duckController.CurrentStamina;
        }
    }

    #endregion

    #region Input Handlers (Called by Input System)

    public void OnJump(InputValue value)
    {
        if (!IsOwner) return;
        
        if (value.isPressed)
        {
            jumpInputPressed = true;
        }
    }

    public void OnAttack(InputValue value)
    {
        if (!IsOwner) return;

        if (value.isPressed)
        {
            attackInputPressed = true;
        }
    }

    #endregion

    #region Server RPCs

    [ServerRpc]
    private void SubmitJumpServerRpc()
    {
        if (!IsFlying.Value) return;

        // Server applies jump force
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);

        // Notify all clients about jump (for animation/VFX)
        OnJumpClientRpc();
    }

    [ServerRpc]
    private void SubmitAttackServerRpc()
    {
        if (duckController == null || !duckController.CanAttack) return;

        // Server spawns bullet
        SpawnBulletOnServer();

        // Notify clients for animation
        OnAttackClientRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestStartFlyingServerRpc()
    {
        if (IsFlying.Value) return;

        IsFlying.Value = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        
        StartFlyingClientRpc();
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestStopFlyingServerRpc()
    {
        IsFlying.Value = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;

        StopFlyingClientRpc();
    }

    #endregion

    #region Client RPCs

    [ClientRpc]
    private void OnJumpClientRpc()
    {
        // Play jump animation on all clients
        if (duckController != null)
        {
            var anim = duckController.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetTrigger(DuckAnimationString.FLY);
            }
        }
    }

    [ClientRpc]
    private void OnAttackClientRpc()
    {
        // Play attack animation on all clients
        if (duckController != null)
        {
            var anim = duckController.GetComponent<Animator>();
            if (anim != null)
            {
                anim.SetTrigger(DuckAnimationString.SHOOT);
            }
        }
    }

    [ClientRpc]
    private void StartFlyingClientRpc()
    {
        if (duckController != null && IsOwner)
        {
            duckController.StartFly().Forget();
        }
    }

    [ClientRpc]
    private void StopFlyingClientRpc()
    {
        if (duckController != null)
        {
            duckController.StopFlying();
        }
    }

    [ClientRpc]
    private void OnPlayerDiedClientRpc()
    {
        OnPlayerDied?.Invoke();
        
        if (duckController != null)
        {
            duckController.StopFlying();
            duckController.gameObject.SetActive(false);
        }
    }

    #endregion

    #region Server Logic

    private void SpawnBulletOnServer()
    {
        // This would spawn a NetworkBullet
        // Implementation depends on your bullet prefab setup
        // var bullet = Instantiate(bulletPrefab, shootPosition.position, shootPosition.rotation);
        // bullet.GetComponent<NetworkObject>().Spawn();
    }

    /// <summary>
    /// Called by server when this player takes damage
    /// </summary>
    public void ServerTakeDamage(float damage)
    {
        if (!IsServer) return;

        NetworkHealth.Value -= damage;
        
        if (NetworkHealth.Value <= 0)
        {
            ServerPlayerDied();
        }
    }

    private void ServerPlayerDied()
    {
        if (!IsServer) return;

        IsFlying.Value = false;
        OnPlayerDiedClientRpc();

        // Notify MatchFlowManager
        if (MatchFlowManager.Instance != null)
        {
            MatchFlowManager.Instance.OnPlayerDied(OwnerClientId);
        }
    }

    #endregion

    #region Event Handlers

    private void HandleHealthChanged(float oldValue, float newValue)
    {
        OnHealthChanged?.Invoke(newValue);
    }

    private void HandleStaminaChanged(float oldValue, float newValue)
    {
        OnStaminaChanged?.Invoke(newValue);
    }

    #endregion

    #region Collision (Server Only)

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;

        if (collision.TryGetComponent<ABaseEnemy>(out var enemy))
        {
            ServerTakeDamage(enemy.Damage);
        }

        if (collision.CompareTag("Finish"))
        {
            RequestStopFlyingServerRpc();
            // Handle level complete
            if (MatchFlowManager.Instance != null)
            {
                MatchFlowManager.Instance.OnPlayerReachedFinish(OwnerClientId);
            }
        }
    }

    #endregion
}
