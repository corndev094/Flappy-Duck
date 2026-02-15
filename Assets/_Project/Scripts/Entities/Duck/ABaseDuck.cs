using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using Nguyen.Event;
using Unity.Netcode;
using System.Collections;
using System.Threading.Tasks;
using NaughtyAttributes;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody2D))]
public abstract class ABaseDuck : NetworkBehaviour {
    
    #region Fields
    [Header("References")]
    [SerializeField] protected DuckBaseData data;
    [Space, SerializeField] protected PlayerInput playerInput;
    [SerializeField] private CanvasGroup hurtVfx;
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

    [SerializeField] private bool infiniteStamina;
    protected float currentMoveSpeed;
    protected float jumpTimeCount;
    [ReadOnly] protected bool isInvincible;
    [ReadOnly] protected bool canAttack = true;
    [ReadOnly] protected bool isFlying;
    [ReadOnly] protected bool canJump = true;

    // Network Variables
    [SerializeField] protected NetworkVariable<float> currentHp = new();
    [SerializeField] protected NetworkVariable<float> currentStamina = new();
    [SerializeField] private NetworkVariable<Color> spriteColor = new();

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
    
    public NetworkVariable<float> CurrentStamina{
        get { return currentStamina; }
        set { currentStamina.Value = Mathf.Clamp(value.Value, 0, data.Stamina); }
    }
    
    public NetworkVariable<float> CurrentHP{ 
        get { return currentHp; }
        set { value.Value = Mathf.Clamp(value.Value, 0, data.HP); } 
    }
    
    public bool IsInvincible => isInvincible;
    public bool CanAttack { get => canAttack; set => canAttack = value; }
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (playerInput == null) playerInput = GetComponent<PlayerInput>();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;
        hurtMatCopy = new(hurtMat);
        SetColorServerRpc();
        StopFlying();
        SubscribeEvents();
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
    public void Setup()
    {
        CurrentStamina = new(data.Stamina);
        CurrentHP = new(data.HP);
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
        if (value.isPressed && canJump && (!infiniteStamina ? CurrentStamina.Value >= data.JumpStamina : true) && JumpCondition())
        {
            Jump();
        }
    }
    #endregion

    #region Movement

    public async UniTask StartFly() {
        if (!IsOwner) return;
        if (isFlying) return;
        isFlying = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
        canJump = true;
        while (isFlying && rb.bodyType != RigidbodyType2D.Kinematic && rb.bodyType != RigidbodyType2D.Static){
            currentMoveSpeed = data.DefaultMoveSpeed;
            rb.linearVelocity = new Vector2(currentMoveSpeed, rb.linearVelocity.y);
            await UniTask.Yield();
        }
    }

    public void StopFlying()
    {
        isFlying = false;
        rb.bodyType = RigidbodyType2D.Kinematic;  
        rb.linearVelocity = Vector2.zero;
    }

    private void Jump() {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, data.JumpForce);
        if (data.FlappingSfx.Count > 0) SoundManager.Instance.PlaySFX(data.FlappingSfx[UnityEngine.Random.Range(0, data.FlappingSfx.Count)], 0.4f);
        anim.SetTrigger(flyAnimationHash);
        DelayJump().Forget();
        refillStaminaCts?.Cancel();
        refillStaminaCts = new();
        HandleStaminaUsage(refillStaminaCts.Token).Forget();
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
        anim.SetTrigger(shootAnimationHash);
        bullet.OnShootedEnemy += enemy =>
        {
            CurrentStamina.Value += enemy.RefillStaminaForPlayer;
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
    private async UniTask HandleStaminaUsage(CancellationToken cancelToken)
    {
        if (!infiniteStamina)
            CurrentStamina.Value -= data.JumpStamina;

        if (cancelToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(data.RefillStaminaDelay), cancellationToken: cancelToken);

            while (CurrentStamina.Value < data.Stamina)
            {
                if (cancelToken.IsCancellationRequested)
                {
                    return;
                }
                CurrentStamina.Value += Time.deltaTime * data.RefillStaminaSpeed;
                await UniTask.Yield(PlayerLoopTiming.Update, cancelToken);
            }
            CurrentStamina.Value = data.Stamina;
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
        if (!IsServer) return;
        if (collision.TryGetComponent<ABaseEnemy>(out var enemy))
        {
            OnBirdCollided?.Invoke(collision);
            TakeDamage(enemy.Damage);
        }
        if (collision.gameObject.CompareTag("Finish"))
        {
            OnBirdReachedFinish?.Invoke();       
            StopFlying();
        }
    }

    private void TakeDamage(float damage)
    {
        if (isInvincible) return;

        CurrentHP.Value -= damage;
        if (CurrentHP.Value <= 0) Die();
        else
        {
            PrimeTween.Tween.ShakeLocalRotation(Camera.main.transform, data.TakeDamageCamShakeSettings);
            hurtCts?.Cancel();
            hurtCts?.Dispose();
            hurtCts = new();
            StartInvincible(invincibleDuration).Forget();
            PlayHurtVfx(hurtCts.Token).Forget();
        }
    }

    private void Die()
    {
        StopFlying();
        gameObject.SetActive(false);
        OnBirdDie?.Invoke();
        GameManager.Instance.NotifyPlayerDied(OwnerClientId);
    }

    private async UniTask StartInvincible(float duration)
    {
        isInvincible = true;
        await UniTask.Delay(TimeSpan.FromSeconds(duration));
        isInvincible = false;
    }

    private async UniTask PlayHurtVfx(CancellationToken token)
    {
        var sprite = GetComponent<SpriteRenderer>();
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
        staminaEvent.RaiseEvent(Mathf.Clamp01(CurrentStamina.Value / data.Stamina));
    }

    private void InvokeHpEvent(float oldHp, float newHp)
    {
        hpEvent.RaiseEvent(Mathf.Clamp01(CurrentHP.Value / data.HP));
    }

    [ServerRpc]
    private void SetColorServerRpc()
    {
        spriteColor.Value = new Color(UnityEngine.Random.Range(0, 1), UnityEngine.Random.Range(0, 1), UnityEngine.Random.Range(0, 1));
    }
    #endregion
}