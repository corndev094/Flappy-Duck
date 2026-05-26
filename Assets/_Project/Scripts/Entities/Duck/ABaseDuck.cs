using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using Nguyen.Event;
using Unity.Netcode;
using System.Linq;
using Sirenix.OdinInspector;
using DG.Tweening;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody2D))]
public abstract class ABaseDuck : NetworkBehaviour {
    
    #region Fields
    [Header("References")]
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] protected DuckBaseData data;
    [Space, SerializeField] protected PlayerInput playerInput;
    [SerializeField] private Material hurtMat;

    [Header("Shooting")]
    [SerializeField] protected Transform shootPosition;
    [SerializeField] protected Bullet bulletPrefab;

    [Header("Configurations")]
    [SerializeField] private float invincibleDuration = 0.3f;
    [SerializeField] private float hurtFlashDuration = 0.3f;
    [SerializeField] private float duckCollisionFlyDelay = 0.12f;
    [SerializeField] private float duckCollisionShakeDuration = 0.12f;
    [SerializeField] private float duckCollisionShakeStrength = 0.06f;
    [SerializeField] private float duckCollisionCooldown = 0.25f;

    [Header("Events")]
    [SerializeField] private FloatEventChannelSO staminaEvent;
    [SerializeField] private FloatEventChannelSO hpEvent;

    [Header("Audio")]
    [SerializeField] private AudioClip hurtSfx;
    [SerializeField] private AudioClip collectSfx;

    [SerializeField] private bool infiniteStamina;
    protected float currentMoveSpeed;
    protected float jumpTimeCount;
    [ReadOnly] protected bool isInvincible;
    [ReadOnly] protected bool canAttack = true;
    [ReadOnly] protected bool isFlying;
    [ReadOnly] protected bool canJump = true;

    // Network Variables
    [SerializeField] protected NetworkVariable<float> currentHp = new(default, NetworkVariableReadPermission.Everyone ,NetworkVariableWritePermission.Server);
    [SerializeField] protected NetworkVariable<float> currentStamina = new(default, NetworkVariableReadPermission.Everyone ,NetworkVariableWritePermission.Server);
    [SerializeField] private NetworkVariable<Color> spriteColor = new(default, NetworkVariableReadPermission.Everyone ,NetworkVariableWritePermission.Owner);

    public event Action<Collider2D> OnBirdCollided;
    public event Action OnBirdReachedFinish;
    public event Action OnBirdDie;
    
    private CancellationTokenSource refillStaminaCts;
    private CancellationTokenSource hurtCts;
    private CancellationTokenSource flyCts;
    private bool isDead;
    private double lastDuckCollisionTime;
    
    protected int flyAnimationHash = Animator.StringToHash(DuckAnimationString.FLY);
    protected int shootAnimationHash = Animator.StringToHash(DuckAnimationString.SHOOT);

    protected Rigidbody2D rb;
    protected Animator anim;
    protected Material hurtMatCopy;
    private SpriteRenderer spriteRenderer;
    private Vector3 spriteDefaultLocalPosition;

    protected const string ATTACK_ACTION = "Attack";
    #endregion

    #region Properties
    public bool IsFlying { get => isFlying; }
    public bool CanJump { get => canJump; }
    public DuckBaseData Data => data;
    
    public NetworkVariable<float> CurrentStamina => currentStamina;
    public NetworkVariable<float> CurrentHP => currentHp;
    
    public bool IsInvincible => isInvincible;
    public bool CanAttack { get => canAttack; set => canAttack = value; }
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
        if (sprite != null) spriteDefaultLocalPosition = sprite.transform.localPosition;
    }

    public override void OnNetworkSpawn()
    {
        SubscribeEvents();
        if (hurtMat != null) hurtMatCopy = new(hurtMat);
        if (!IsOwner) return;
        // spriteColor.Value = new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f));
        // sprite.color = spriteColor.Value;
        rb.bodyType = RigidbodyType2D.Kinematic;
        StopFlying();
    }

    protected virtual void Update()
    {
        TryToShoot();
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        refillStaminaCts?.Cancel();
        refillStaminaCts?.Dispose();
        hurtCts?.Cancel();
        hurtCts?.Dispose();
        flyCts?.Cancel();
        UnSubscribeEvents();
    }
    #endregion

    #region Initialization
    public void InitializeStats()
    {
        isDead = false;
        currentHp.Value = data.HP;
        currentStamina.Value = data.Stamina;
    }

    private void SubscribeEvents()
    {
        currentHp.OnValueChanged += InvokeHpEvent;
        currentStamina.OnValueChanged += InvokeStaminaEvent;
    }

    private void UnSubscribeEvents()
    {
        currentHp.OnValueChanged -= InvokeHpEvent;
        currentStamina.OnValueChanged -= InvokeStaminaEvent;
    }
    #endregion

    #region Input Handling
    public void OnJump(InputValue value)
    {
        if (!IsOwner) return;
        bool touchPressed = Touchscreen.current?.press.wasPressedThisFrame ?? false;
        if ((value.isPressed || touchPressed) && isFlying && canJump && (!infiniteStamina ? CurrentStamina.Value >= data.JumpStamina : true) && JumpCondition())
        {
            Debug.Log("Jump input received");
            Jump();
        }
    }
    #endregion

    #region Movement

    public async UniTask StartFly() {
        if (!IsOwner) return;
        flyCts?.Cancel();
        var cts = new CancellationTokenSource();
        flyCts = cts;
        var token = cts.Token;

        isFlying = true;
        canJump = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
        currentMoveSpeed = data.DefaultMoveSpeed;
        try
        {
            while (!token.IsCancellationRequested && rb != null)
            {
                rb.linearVelocity = isFlying ? new Vector2(currentMoveSpeed, rb.linearVelocity.y) : Vector2.zero;
                await UniTask.Yield(PlayerLoopTiming.Update, token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when flight is paused, stopped, or duck is destroyed.
        }
        finally
        {
            if (ReferenceEquals(flyCts, cts))
            {
                flyCts = null;
            }
            cts.Dispose();
        }
    }

    public void StopFlying()
    {
        flyCts?.Cancel();
        isFlying = false;
        if (rb.bodyType == RigidbodyType2D.Dynamic) rb.bodyType = RigidbodyType2D.Kinematic;
        rb.linearVelocity = Vector2.zero;
    }

    public void ResumeFlying()
    {
        StartFly().Forget();
    }

    private void Jump() {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, data.JumpForce);
        if (data.FlappingSfx.Count > 0) SoundManager.Instance.PlaySFX(data.FlappingSfx[UnityEngine.Random.Range(0, data.FlappingSfx.Count)], 0.4f);
        if (anim.enabled) anim.SetTrigger(flyAnimationHash);
        DelayJump().Forget();
        ConsumeStaminaServerRpc();
    }

    protected virtual bool JumpCondition(){ return true; }

    private async UniTaskVoid DelayJump()
    {
        canJump = false;
        jumpTimeCount = 0;
        while (jumpTimeCount <= data.JumpDelay)
        {
            jumpTimeCount += Time.deltaTime;
            await UniTask.Yield();
        }
        canJump = true;
    }
    #endregion

    #region Combat
    private void TryToShoot()
    {
        if (!IsOwner) return;
        if (canAttack && playerInput.actions[ATTACK_ACTION].IsInProgress())
        {
            Attack();
            DelayShoot().Forget();
        }
    }

    protected virtual void Attack()
    {
        if (bulletPrefab == null) return;
        if (IsServer) SpawnBullet();
        else AttackServerRpc();
    }

    private void SpawnBullet()
    {
        Bullet bullet = null;
        if (IsServer)
        {
            bullet = Instantiate(bulletPrefab, shootPosition.position, shootPosition.rotation);
            bullet.GetComponent<NetworkObject>().Spawn();
        }
        if (anim.enabled) anim.SetTrigger(shootAnimationHash);
        bullet.OnShootedEnemy += enemy =>
        {
            currentStamina.Value = Mathf.Min(currentStamina.Value + enemy.RefillStaminaForPlayer, data.Stamina);
        };
    }

    [ServerRpc]
    private void AttackServerRpc()
    {
        SpawnBullet();
    }

    private async UniTask DelayShoot()
    {
        canAttack = false;
        await UniTask.Delay(TimeSpan.FromSeconds(data.ShootDelay));
        canAttack = true;
    }
    #endregion

    #region Stamina Management
    [ServerRpc]
    private void ConsumeStaminaServerRpc()
    {
        refillStaminaCts?.Cancel();
        refillStaminaCts = new();
        HandleStaminaUsage(refillStaminaCts.Token).Forget();
    }

    private async UniTask HandleStaminaUsage(CancellationToken cancelToken)
    {
        if (!infiniteStamina)
            currentStamina.Value = Mathf.Max(currentStamina.Value - data.JumpStamina, 0);

        if (cancelToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(data.RefillStaminaDelay), cancellationToken: cancelToken);

            while (currentStamina.Value < data.Stamina)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    return;
                }
                currentStamina.Value = Mathf.Min(currentStamina.Value + Time.deltaTime * data.RefillStaminaSpeed, data.Stamina);
                await UniTask.Yield(PlayerLoopTiming.Update, cancelToken);
            }
            currentStamina.Value = data.Stamina;
        }
        catch (System.OperationCanceledException)
        {
            // Expected exception when task is cancelled
        }
        catch (Exception ex)
        {
            EDebug.LogError($"An unexpected error occurred in HandleStaminaUsage: {ex}");
        }
    }
    #endregion

    #region Health & Damage
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Coin"))
        {
            // Client Side
            if (IsOwner)
            {
                EffectManager.Instance.PlayEffectLocal(EffectType.CoinCollect, transform.position);
            }

            // This prevents the coin from appearing collected but remaining due to network latency.
            if (collision.TryGetComponent<SpriteRenderer>(out var coinSprite))
            {
                coinSprite.enabled = false;
            }
            var childSprites = collision.GetComponentsInChildren<SpriteRenderer>();
            foreach (var s in childSprites)
            {
                s.enabled = false;
            }
            if (collision.TryGetComponent<Collider2D>(out var coinCollider))
            {
                coinCollider.enabled = false;
            }

            if (!IsServer) return;

            var playerData = GameFlowManager.Instance?.GetPlayerData(OwnerClientId);
            if (playerData.HasValue)
            {
                GameFlowManager.Instance.UpdateCoin(OwnerClientId, playerData.Value.Coin + 1);
            }

            if (collision.TryGetComponent<NetworkObject>(out var networkObject) && networkObject.IsSpawned)
            {
                networkObject.Despawn(true);
            }
            else
            {
                Destroy(collision.gameObject);
            }
            return;
        }
        if (!IsServer) return;
        if (collision.TryGetComponent<ABaseDuck>(out var otherDuck) && otherDuck != this)
        {
            HandleDuckCollision(otherDuck);
            return;
        }

        if (collision.TryGetComponent<ABaseEnemy>(out var enemy))
        {
            OnBirdCollided?.Invoke(collision);
            if (!isInvincible || (isInvincible && enemy.CompareTag("Border")))
            {
                TakeDamage(enemy.Damage);
            }
        }
        else if (collision.gameObject.CompareTag("Finish"))
        {
            Debug.Log($"Duck {OwnerClientId} reached the finish line!");
            OnBirdReachedFinish?.Invoke();
            GameManager.Instance.NotifyPlayerWin(OwnerClientId);
            StopFlying();
        }
    }

    private void HandleDuckCollision(ABaseDuck otherDuck)
    {
        if (!IsServer || isDead || otherDuck == null || otherDuck.isDead) return;

        double now = Time.timeAsDouble;
        if (now - lastDuckCollisionTime < duckCollisionCooldown) return;
        lastDuckCollisionTime = now;
        otherDuck.lastDuckCollisionTime = now;

        DuckCollisionClientRpc();
        otherDuck.DuckCollisionClientRpc();
    }

    [ClientRpc]
    private void DuckCollisionClientRpc()
    {
        ShakeOnDuckCollision();
        DelayFlyAfterDuckCollision().Forget();
    }

    private void ShakeOnDuckCollision()
    {
        var shakeTarget = sprite != null ? sprite.transform : transform;
        var defaultLocalPosition = sprite != null ? spriteDefaultLocalPosition : shakeTarget.localPosition;
        shakeTarget.DOKill();
        shakeTarget.localPosition = defaultLocalPosition;
        shakeTarget.DOShakePosition(duckCollisionShakeDuration, duckCollisionShakeStrength, vibrato: 8, randomness: 45f)
            .OnComplete(() => shakeTarget.localPosition = defaultLocalPosition)
            .SetLink(gameObject);
    }

    private async UniTask DelayFlyAfterDuckCollision()
    {
        if (!IsOwner || isDead || !isFlying) return;

        try
        {
            StopFlying();
            await UniTask.Delay(TimeSpan.FromSeconds(duckCollisionFlyDelay), cancellationToken: this.GetCancellationTokenOnDestroy());
            if (!isDead)
            {
                StartFly().Forget();
            }
        }
        catch (OperationCanceledException)
        {
            // Expected if duck despawns during the collision delay.
        }
    }

    private void TakeDamage(float damage)
    {
        if (isDead) return;
        currentHp.Value -= damage;
        if (currentHp.Value <= 0) Die();
        else
        {
            StartInvincible(invincibleDuration).Forget();
            // Delegate VFX to the owning client
            PlayHurtEffectsClientRpc();
        }
    }

    [ClientRpc]
    private void PlayHurtEffectsClientRpc()
    {
        if (IsOwner)
        {
            PrimeTween.Tween.ShakeLocalRotation(Camera.main.transform, data.TakeDamageCamShakeSettings);
            EffectManager.Instance.PlayEffectNetworked(EffectType.PlayerHurt, transform.position);
        }
        hurtCts?.Cancel();
        hurtCts?.Dispose();
        hurtCts = new();
        PlayHurtVfx(hurtCts.Token).Forget();
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Server-side logic
        GameManager.Instance.NotifyPlayerDied(OwnerClientId);
        DieClientRpc();
    }

    [ClientRpc]
    private void DieClientRpc()
    {
        StopFlying();
        gameObject.SetActive(false);

        if (!IsOwner) return;

        OnBirdDie?.Invoke();
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.ConnectedClientsList.Count > 1)
        {
            var otherClient = nm.ConnectedClientsList.FirstOrDefault(c => c.ClientId != OwnerClientId);
            if (otherClient?.PlayerObject != null)
                CameraController.Instance.Target = otherClient.PlayerObject.transform;
        }
        else
        {
            CameraController.Instance.Target = null;
            CameraController.Instance.enabled = false;
        }
    }

    private async UniTask StartInvincible(float duration)
    {
        isInvincible = true;
        await UniTask.Delay(TimeSpan.FromSeconds(duration));
        isInvincible = false;
    }

    private async UniTask PlayHurtVfx(CancellationToken token)
    {
        hurtMatCopy.SetFloat("FlashAmount", 1);
        var currentMat = sprite.material;
        try
        {
            for (var i = 0; i < 3; i++)
            {
                sprite.material = hurtMatCopy;
                await UniTask.Delay(TimeSpan.FromSeconds(hurtFlashDuration / 6), cancellationToken: token);
                sprite.material = currentMat;
                await UniTask.Delay(TimeSpan.FromSeconds(hurtFlashDuration / 6), cancellationToken: token);
            }
        }
        finally
        {
            sprite.material = currentMat;
        }
    }
    #endregion

    #region Event Invokers
    private void InvokeStaminaEvent(float oldVlaue, float newValue)
    {
        if (!IsOwner) return;
        staminaEvent.RaiseEvent(Mathf.Clamp01(CurrentStamina.Value / data.Stamina));
    }

    private void InvokeHpEvent(float oldValue, float newValue)
    {
        if (!IsOwner) return;
        hpEvent.RaiseEvent(Mathf.Clamp01(CurrentHP.Value / data.HP));
    }

    #endregion
}
