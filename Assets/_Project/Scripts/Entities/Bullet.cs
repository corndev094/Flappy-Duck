using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class Bullet : NetworkBehaviour 
{
    [SerializeField] private float damage = 1f;
    [SerializeField] private float shootSpeed = 5f;
    [SerializeField] private float destroyAfter = 3;
    [SerializeField] private LayerMask mask;
    

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
            DespawnOrDestroy();
        }
        catch (OperationCanceledException) { /* Expected */ }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.CompareTag("PhaseTrigger")) return;
        if (!IsServer) return;
        
        if (collision.TryGetComponent<ABaseEnemy>(out var enemy))
        {
            enemy.TakeDamage(Damage);
            OnShootedEnemy?.Invoke(enemy);
        }

        destroyCts.Cancel();
        DespawnOrDestroy();
    }

    private void DespawnOrDestroy()
    {
        if (IsServer && TryGetComponent<NetworkObject>(out var networkObject) && networkObject.IsSpawned)
        {
            networkObject.Despawn(true);
            return;
        }

        if (!IsSpawned)
        {
            Destroy(gameObject);
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        destroyCts?.Dispose();
    }
}
