using UnityEngine;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
using Nguyen.Event;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody2D))]
public abstract class ABaseDuck : MonoBehaviour {
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
    protected bool canJump = true;
    protected bool isFlying;
    protected float currentHp;
    protected float currentMoveSpeed;
    protected float currentStamina;
    protected float jumpTimeCount;
    protected bool canAttack = true;

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

    public bool IsFlying { get => isFlying; }
    public float CurrentStamina {
        get
        {
            return currentStamina;
        }
        set
        {
            value = Mathf.Clamp(value, 0, data.Stamina);
            currentStamina = value;
            InvokeStaminaEvent();
        }
    }
    public float CurrentHP { 
        get
        {
            return currentHp;
        }
        set
        {
            value = Mathf.Clamp(value, 0, data.HP);
            currentHp = value;
            InvokeHpEvent();
        } 
    }
    public bool IsInvincible => isInvincible;
    public bool CanAttack { get => canAttack; set => canAttack = value; }

    void Awake() {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        playerInput ??= GetComponent<PlayerInput>();
        StopFlying();
    }

    private void Start() {
        hurtMatCopy = new(hurtMat);
    }

    protected virtual void Update()
    {
        TryToShoot();
    }

    void OnDestroy()
    {
        refillStaminaCts?.Cancel();
        refillStaminaCts?.Dispose();
        hurtCts?.Cancel();
        hurtCts?.Dispose();
    }

    public void Setup()
    {
        CurrentStamina = data.Stamina;
        CurrentHP = data.HP;
    }

    public async UniTask StartFly() {
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

    public void OnJump(InputValue value)
    {
        if (!isFlying) return;
        if (value.isPressed && canJump && (!infiniteStamina ? CurrentStamina >= data.JumpStamina : true) && JumpCondition())
        {
            Jump();
            JumpDelay().Forget();
            refillStaminaCts?.Cancel();
            refillStaminaCts = new();
            HandleStaminaUsage(refillStaminaCts.Token).Forget();
        }
    }

    protected virtual bool JumpCondition(){return true;}

    private void TryToShoot()
    {
        if (canAttack && playerInput.actions[ATTACK_ACTION].IsInProgress())
        {
            Attack();
            ShootDelay().Forget();
        }
    }

    protected virtual void Attack()
    {
        if (bulletPrefab == null) return;
        var bullet = Instantiate(bulletPrefab, shootPosition.position, shootPosition.rotation);
        anim.SetTrigger(shootAnimationHash);
        bullet.OnShootedEnemy += enemy =>
        {
            CurrentStamina += enemy.RefillStaminaForPlayer;
        };
    }

    private async UniTask ShootDelay()
    {
        canAttack = false;
        await UniTask.Delay(TimeSpan.FromSeconds(data.ShootDelay));
        canAttack = true;
    }

    private void Jump() {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, data.JumpForce);
        if (data.FlappingSfx.Count > 0) SoundManager.Instance.PlaySFX(data.FlappingSfx[UnityEngine.Random.Range(0, data.FlappingSfx.Count)], 0.4f);
        anim.SetTrigger(flyAnimationHash);
    }

    private async UniTask JumpDelay()
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

    private async UniTask HandleStaminaUsage(CancellationToken cancelToken)
    {
        if (!infiniteStamina)
            CurrentStamina -= data.JumpStamina;

        // 1. Kiểm tra hủy ngay lập tức khi bắt đầu task
        if (cancelToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(data.RefillStaminaDelay), cancellationToken: cancelToken);

            while (CurrentStamina < data.Stamina)
            {
                // 2. Kiểm tra hủy trước mỗi lần lặp
                if (cancelToken.IsCancellationRequested)
                {
                    return;
                }
                CurrentStamina += Time.deltaTime * data.RefillStaminaSpeed;
                await UniTask.Yield(PlayerLoopTiming.Update, cancelToken);
            }
            CurrentStamina = data.Stamina;
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
        // hurtVfx.gameObject.SetActive(true);
        // var hurtDuration = 0.2f;
        // for (var i = 0; i < 2; i++)
        // {
        //     hurtVfx.alpha = 0;
        //     hurtVfx.DOFade(1, hurtDuration/2).SetEase(Ease.OutFlash).OnComplete(() =>
        //     {
        //         hurtVfx.DOFade(0, hurtDuration/2).SetEase(Ease.InFlash);
        //     });
        //     await UniTask.Delay(TimeSpan.FromSeconds(hurtDuration), cancellationToken: token);
        // }
        // await UniTask.Yield();
        // hurtVfx.gameObject.SetActive(false);

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
        isFlying = false;
        rb.bodyType = RigidbodyType2D.Kinematic;  
        rb.linearVelocity = Vector2.zero;
    }

    private void InvokeStaminaEvent()
    {
        staminaEvent.RaiseEvent(Mathf.Clamp01(CurrentStamina / data.Stamina));
    }

    private void InvokeHpEvent()
    {
        hpEvent.RaiseEvent(Mathf.Clamp01(CurrentHP / data.HP));
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
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

        CurrentHP -= damage;
        if (CurrentHP <= 0) Die();
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
    }

    private async UniTask StartInvincible(float duration)
    {
        isInvincible = true;
        await UniTask.Delay(TimeSpan.FromSeconds(duration));
        isInvincible = false;
    }
}