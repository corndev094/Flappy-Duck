using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

public enum EffectType : byte
{
    CoinCollect,
    PlayerHurt,
    PlayerDeath,
    PlayerJump,
    BulletShoot,
    BulletExplosion,
    FinishReached
}

public class EffectManager : NetworkSingleton<EffectManager>
{
    [System.Serializable]
    public struct EffectConfig
    {
        public string Name;
        public EffectType Type;
        public GameObject VfxPrefab;
        public AudioClip SfxClip;
        [Range(0f, 1f)] public float SfxVolume;
        public bool RandomSfxPitch;
        public float VfxDestroyDelay;
    }

    [Header("Configurations")]
    [SerializeField] private List<EffectConfig> effectConfigs = new List<EffectConfig>();

    private Dictionary<EffectType, EffectConfig> configMap = new Dictionary<EffectType, EffectConfig>();

    protected override void Awake()
    {
        base.Awake();
        InitializeMap();
    }

    private void InitializeMap()
    {
        configMap.Clear();
        foreach (var config in effectConfigs)
        {
            if (!configMap.ContainsKey(config.Type))
            {
                configMap.Add(config.Type, config);
            }
            else
            {
                Debug.LogWarning($"Duplicate EffectConfig for Type {config.Type} in EffectManager!");
            }
        }
    }

    [Button]
    public void Test() => PlayEffectLocal(EffectType.CoinCollect, transform.position);

    /// <summary>
    /// Plays an effect locally on the client/host instantly.
    /// Best for lag-free local player feedback.
    /// </summary
    public void PlayEffectLocal(EffectType type, Vector3 position, Quaternion rotation = default)
    {
        if (!configMap.TryGetValue(type, out var config))
        {
            // Retry map initialization in case configs were modified or not initialized
            InitializeMap();
            if (!configMap.TryGetValue(type, out config))
            {
                Debug.LogWarning($"Effect type {type} is not configured in EffectManager!");
                return;
            }
        }

        // 1. Play SFX
        if (config.SfxClip != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySFX(config.SfxClip, config.SfxVolume, config.RandomSfxPitch);
        }

        // 2. Spawn VFX
        if (config.VfxPrefab != null)
        {
            var vfxInstance = Instantiate(config.VfxPrefab, position, rotation == default ? Quaternion.identity : rotation);
            if (config.VfxDestroyDelay > 0)
            {
                Destroy(vfxInstance, config.VfxDestroyDelay);
            }
        }
    }

    /// <summary>
    /// Plays an effect and synchronizes it across all connected clients.
    /// Uses ServerRpc and ClientRpc to broadcast.
    /// </summary>
    /// <param name="type">The type of effect to play</param>
    /// <param name="position">World position</param>
    /// <param name="rotation">World rotation</param>
    /// <param name="triggerClientId">Optional OwnerClientId who triggered this, to avoid double playing locally</param>
    public void PlayEffectNetworked(EffectType type, Vector3 position, Quaternion rotation = default, ulong triggerClientId = ulong.MaxValue)
    {
        if (IsServer)
        {
            PlayEffectClientRpc(type, position, rotation, triggerClientId);
        }
        else
        {
            PlayEffectServerRpc(type, position, rotation, triggerClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayEffectServerRpc(EffectType type, Vector3 position, Quaternion rotation, ulong triggerClientId)
    {
        PlayEffectClientRpc(type, position, rotation, triggerClientId);
    }

    [ClientRpc]
    private void PlayEffectClientRpc(EffectType type, Vector3 position, Quaternion rotation, ulong triggerClientId)
    {
        // Skip playing again on the client who originally triggered it locally to avoid duplicate SFX/VFX
        ulong localClientId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : ulong.MaxValue;
        if (triggerClientId == localClientId && triggerClientId != ulong.MaxValue)
        {
            return;
        }

        PlayEffectLocal(type, position, rotation);
    }
}
