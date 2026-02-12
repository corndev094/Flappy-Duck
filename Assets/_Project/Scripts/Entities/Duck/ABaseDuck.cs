using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using Nguyen.Event;
using Unity.Netcode;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody2D))]
public abstract class ABaseDuck : NetworkBehaviour {
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
    protected bool isInvincible;
    protected float currentMoveSpeed;
    protected float jumpTimeCount;
    protected bool canAttack = true;

    //* Network Variable
    protected NetworkVariable<bool> canJump = new (true);
    protected NetworkVariable<bool> isFlying;
    protected NetworkVariable<float> currentHp;
    protected NetworkVariable<float> currentStamina;

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

    protected const string ATTACK_ACTION = "Attack";

    #region Property
    public NetworkVariable<bool> IsFlying { get => isFlying; }
    public NetworkVariable<float> CurrentStamina{
        get
        {
            return currentStamina;
        }
        set
        {
            currentStamina.Value = Mathf.Clamp(value.Value, 0, data.Stamina);
        }
    }
    public NetworkVariable<float> CurrentHP{ 
        get
        {
            return currentHp;
        }
        set
        {
            value.Value = Mathf.Clamp(value.Value, 0, data.HP);
        } 
    }
    public bool IsInvincible => isInvincible;
    public bool CanAttack { get => canAttack; set => canAttack = value; }
    #endregion

    #region UnityEvent
    void Awake() {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        playerInput ??= GetComponent<PlayerInput>();
    }

    private void Start() {
        hurtMatCopy = new(hurtMat);
        StopFlying();
        SubcribeEvents();
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

    private void SubcribeEvents()
    {
        currentHp.OnValueChanged += InvokeHpEvent;
        currentStamina.OnValueChanged += InvokeStaminaEvent;
    }

    private void UnSubscribeEvents()
    {
        currentHp.OnValueChanged -= InvokeHpEvent;
        currentStamina.OnValueChanged -= InvokeStaminaEvent;
    }

    #region Input Control
    public void OnJump(InputValue value)
    {
        if (!IsOwner) return;
        if (value.isPressed && canJump.Value && (!infiniteStamina ? CurrentStamina.Value >= data.JumpStamina : true) && JumpCondition())
        {
            if (IsServer)
            {
                Jump();
            }
            else
            {
                JumpServerRpc();
            }
        }
    }
    #endregion

    public void Setup()
    {
        CurrentStamina = new(data.Stamina);
        CurrentHP = new(data.HP);
    }

    public async UniTask StartFly() {
        if (isFlying.Value) return;
        isFlying.Value = true;
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
        canJump.Value = true;
        while (isFlying.Value && rb.bodyType != RigidbodyType2D.Kinematic && rb.bodyType != RigidbodyType2D.Static){
            currentMoveSpeed = data.DefaultMoveSpeed;
            rb.linearVelocity = new Vector2(currentMoveSpeed, rb.linearVelocity.y);
            await UniTask.Yield();
        }
    }

    protected virtual bool JumpCondition(){return true;}

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

    [ServerRpc]
    private void JumpServerRpc()
    {
        Jump();
    }

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
        var bullet = Instantiate(bulletPrefab, shootPosition.position, shootPosition.rotation);
        bullet.GetComponent<NetworkObject>().Spawn();
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

    private async UniTask DelayJump()
    {
        canJump.Value = false;
        jumpTimeCount = 0;
        while (jumpTimeCount <= data.JumpDelay)
        {
            jumpTimeCount += Time.deltaTime;
            await UniTask.Yield();
        }
        canJump.Value = true;
    }

    private async UniTask HandleStaminaUsage(CancellationToken cancelToken)
    {
        if (!infiniteStamina)
            CurrentStamina.Value -= data.JumpStamina;

        // 1. Kiểm tra hủy ngay lập tức khi bắt đầu task
        if (cancelToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(data.RefillStaminaDelay), cancellationToken: cancelToken);

            while (CurrentStamina.Value < data.Stamina)
            {
                // 2. Kiểm tra hủy trước mỗi lần lặp
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
            // Đây là ngoại lệ mong đợi khi task bị hủy.
            // Không cần log lỗi hay re-throw.
        }
        // 3. Bắt các ngoại lệ khác không mong muốn
        catch (Exception ex)
        {
            EDebug.LogError($"An unexpected error occurred in HandleStaminaUsage: {ex}");
        }
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

    public void StopFlying()
    {
        isFlying.Value = false;
        rb.bodyType = RigidbodyType2D.Kinematic;  
        rb.linearVelocity = Vector2.zero;
    }

    private void InvokeStaminaEvent(float oldVlaue, float newValue)
    {
        staminaEvent.RaiseEvent(Mathf.Clamp01(CurrentStamina.Value / data.Stamina));
    }

    private void InvokeHpEvent(float oldHp, float newHp)
    {
        hpEvent.RaiseEvent(CurrentHP.Value);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer) return;
        if (collision.TryGetComponent<ABaseEnemy>(out var enemy))
        {
            OnBirdCollided?.Invoke(collision);
            TakeDamageServerRpc(enemy.Damage);
        }
        if (collision.gameObject.CompareTag("Finish"))
        {
            OnBirdReachedFinish?.Invoke();       
            StopFlying();
        }
    }

    [ServerRpc]
    private void TakeDamageServerRpc(float damage)
    {
        if (isInvincible) return;

        CurrentHP.Value-= damage;
        if (CurrentHP.Value <= 0) DieServerRpc();
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

    [ServerRpc]
    private void DieServerRpc()
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
}