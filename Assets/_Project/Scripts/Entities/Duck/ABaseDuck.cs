using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using Nguyen.Event;
using Unity.Netcode;
using System.Linq;
using Sirenix.OdinInspector;

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
    
    protected int flyAnimationHash = Animator.StringToHash(DuckAnimationString.FLY);
    protected int shootAnimationHash = Animator.StringToHash(DuckAnimationString.SHOOT);

    protected Rigidbody2D rb;
    protected Animator anim;
    protected Material hurtMatCopy;
    private SpriteRenderer spriteRenderer;

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
        UnSubscribeEvents();
    }
    #endregion

    #region Initialization
    public void InitializeStats()
    {
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
        if (value.isPressed && isFlying && canJump && (!infiniteStamina ? CurrentStamina.Value >= data.JumpStamina : true) && JumpCondition())
        {
            Jump();
        }
    }
    #endregion

    #region Movement

    public async UniTask StartFly() {
        if (!IsOwner) return;
        isFlying = true;
        canJump = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
        currentMoveSpeed = data.DefaultMoveSpeed;
        while (true && rb != null){
            rb.linearVelocity = isFlying ? new Vector2(currentMoveSpeed, rb.linearVelocity.y) : Vector2.zero;
            await UniTask.Yield();
        }
    }

    public void StopFlying()
    {
        isFlying = false;
        if (rb.bodyType == RigidbodyType2D.Dynamic) rb.bodyType = RigidbodyType2D.Kinematic;
    }

    public void ResumeFlying()
    {
        isFlying = true;
        if (rb.bodyType == RigidbodyType2D.Dynamic) rb.bodyType = RigidbodyType2D.Kinematic;
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
            PlayCollectSfx();
            // collision.TryGetComponent<NetworkObject>(out var networkObject);
            // if (networkObject.IsSpawned) networkObject.Despawn(true);
            Destroy(collision.gameObject);
            GameFlowManager.Instance.UpdateCoin(OwnerClientId, GameFlowManager.Instance.GetPlayerData(OwnerClientId).Value.Coin + 1);
        }
        if (!IsServer) return;
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
            OnBirdReachedFinish?.Invoke();
            GameManager.Instance.NotifyPlayerWin(OwnerClientId);
            StopFlying();
        }
    }

    private void TakeDamage(float damage)
    {
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
            PlayHurtSfx();
        }
        hurtCts?.Cancel();
        hurtCts?.Dispose();
        hurtCts = new();
        PlayHurtVfx(hurtCts.Token).Forget();
    }

    private void Die()
    {
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
        if (NetworkManager.Singleton.ConnectedClientsList.Count > 1)
        {
            var otherClient = NetworkManager.Singleton.ConnectedClientsList.FirstOrDefault(c => c.ClientId != OwnerClientId);
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

    #region 

    private void PlayHurtSfx()
    {
        SoundManager.Instance.PlaySFX(hurtSfx);
    }

    private void PlayCollectSfx()
    {
        SoundManager.Instance.PlaySFX(collectSfx);
    }

    #endregion

    #endregion
}