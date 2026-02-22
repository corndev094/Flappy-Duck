using System.Collections.Generic;
using NaughtyAttributes;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class DuckController : NetworkBehaviour {
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
    }

    void OnValidate()
    {
        if (Application.isEditor && CurrentDuck != null)
        {
            CurrentDuck = null;
        }
    }

    [ServerRpc(RequireOwnership = false)]
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
            Debug.Log($"{serverParams.Receive.SenderClientId} owns duck {instance.OwnerClientId}");
            instance.gameObject.SetActive(true);
            return;
        }
        else
        {
            EDebug.LogError($"Does not contains type: {skin}");
            return;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
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