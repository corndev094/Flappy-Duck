using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using _Project.Scripts.CustomPropertyAttribute;

public class Bullet : NetworkBehaviour 
{
    [SerializeField] private float damage = 1f;
    [SerializeField] private float shootSpeed = 5f;
    [SerializeField] private float destroyAfter = 3;
    [SerializeField, Tag] private string targetTag;

    public Action<ABaseEnemy> OnShootedEnemy;
    public float Damage { get => damage; set => damage = value; }
    public float ShootSpeed { get => shootSpeed; set => shootSpeed = value; }

    private CancellationTokenSource destroyCts;
    private bool isSpent = false;

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
        if (collision.CompareTag("PhaseTrigger")) return;
        if (!IsTarget(collision.gameObject.tag)) return;
        
        if (isSpent) return;
        isSpent = true;

        if (!IsServer)
        {
            DisableVisualsAndPhysics();
            return;
        }
        
        if (collision.TryGetComponent<ABaseEnemy>(out var enemy))
        {
            enemy.TakeDamage(Damage);
            OnShootedEnemy?.Invoke(enemy);
        }
        else if (collision.TryGetComponent<ABaseDuck>(out var player))
        {
            player.TakeDamage(Damage);
        }

        destroyCts.Cancel();
        DespawnOrDestroy();
    }

    private bool IsTarget(string objectTag)
    {
        if (string.IsNullOrEmpty(targetTag)) return true;

        string[] tags = targetTag.Split(',');
        foreach (var t in tags)
        {
            if (objectTag == t.Trim()) return true;
        }
        return false;
    }

    private void DisableVisualsAndPhysics()
    {
        if (TryGetComponent<Collider2D>(out var col))
        {
            col.enabled = false;
        }
        if (TryGetComponent<SpriteRenderer>(out var sr))
        {
            sr.enabled = false;
        }
        
        var childSrs = GetComponentsInChildren<SpriteRenderer>();
        foreach (var child in childSrs)
        {
            child.enabled = false;
        }
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
