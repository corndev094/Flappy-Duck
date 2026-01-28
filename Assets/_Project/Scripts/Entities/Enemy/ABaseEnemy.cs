using UnityEngine;

public abstract class ABaseEnemy : MonoBehaviour
{
    [SerializeField] protected BaseEnemyData data;
    [SerializeField] protected float refillStaminaForPlayer = 1;
    [SerializeField] private bool isInvincible;
    [SerializeField] protected float currentHp;

    public float RefillStaminaForPlayer => refillStaminaForPlayer;
    public float CurrentHP
    {
        get
        {
            return currentHp;
        }
        private set
        {
            value = Mathf.Clamp(value, 0, data.HP);
            currentHp = value;
        }
    }
    public float Damage => data.Damage;
    public bool IsInvincible => isInvincible; 

    void Awake()
    {
        currentHp = data.HP;
    }

    public void TakeDamage(float damage)
    {
        if (isInvincible) return;

        CurrentHP -= damage;
        if (CurrentHP <= 0) Die();
    }

    private void Die()
    {
        Destroy(this.gameObject);
    }

    void Reset()
    {
        gameObject.tag = "Enemy";
    }
}