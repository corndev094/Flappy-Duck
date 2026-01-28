using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class Bullet : MonoBehaviour 
{
    [SerializeField] private float damage = 1f;
    [SerializeField] private float shootSpeed = 5f;
    [SerializeField] private float destroyAfter = 3;

    public Action<ABaseEnemy> OnShootedEnemy;
    public float Damage { get => damage; set => damage = value; }
    public float ShootSpeed { get => shootSpeed; set => shootSpeed = value; }

    private CancellationTokenSource destroyCts;

    void Start()
    {
        destroyCts = new();
        DestroyAfterSeconds();
    }

    void Update()
    {
        transform.Translate(Vector3.right * Time.deltaTime * ShootSpeed, Space.Self);
    }

    private async void DestroyAfterSeconds()
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(destroyAfter), cancellationToken: destroyCts.Token);
            Destroy(gameObject);
        }
        catch (OperationCanceledException) { /* Expected */ }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.CompareTag("PhaseTrigger")) return;
        
        if (collision.TryGetComponent<ABaseEnemy>(out var enemy))
        {
            enemy.TakeDamage(Damage);
            OnShootedEnemy?.Invoke(enemy);
        }

        destroyCts.Cancel();
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        destroyCts?.Dispose();
    }
}