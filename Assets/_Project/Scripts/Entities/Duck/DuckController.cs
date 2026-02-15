using System.Collections.Generic;
using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class DuckController : MonoBehaviour {
    [Header("Duck Prefab")]
    [SerializeField] private ABaseDuck normalDuck;
    [SerializeField] private ABaseDuck ramboDuck;

    private Dictionary<SkinID, ABaseDuck> duckPrefabList;
    [Space]
    [ReadOnly] public ABaseDuck CurrentDuck;

    void Awake()
    {
        duckPrefabList = new()
        {
            {SkinID.Normal, normalDuck},
            {SkinID.Rambo, ramboDuck}
        };
        CurrentDuck = normalDuck;
    }

    void OnValidate()
    {
        if (Application.isEditor && CurrentDuck != null)
        {
            CurrentDuck = null;
        }
    }

    [ServerRpc]
    public void GetDuckServerRpc(SkinID skin, ServerRpcParams serverParams = default)
    {
        if (duckPrefabList.TryGetValue(skin, out var duck))
        {
            if (duck == null)
            {
                EDebug.LogError($"Duck prefab with type {skin} is null");
                return;
            }
            var instance = Instantiate(duck, transform.position, Quaternion.identity);
            instance.GetComponent<NetworkObject>().SpawnWithOwnership(serverParams.Receive.SenderClientId);
            instance.gameObject.SetActive(true);
            CurrentDuck = instance;
            return;
        }
        else
        {
            EDebug.LogError($"Does not contains type: {skin}");
            return;
        }
    }
    [ServerRpc]
    public void ReleaseDuckServerRpc()
    {
        if (CurrentDuck != null)
        {
            if (CurrentDuck.TryGetComponent<NetworkObject>(out var netObj))
                netObj.Despawn(true);
            else
                Destroy(CurrentDuck);
        }
    }

    public void DisableAll()
    {
        foreach (var duck in duckPrefabList.Values)
        {
            duck.gameObject.SetActive(false);
        }
    }
}