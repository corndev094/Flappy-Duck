using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class DuckController : NetworkBehaviour {
    [Header("Duck Prefab")]
    [SerializeField] private ABaseDuck normalDuck;
    [SerializeField] private ABaseDuck ramboDuck;

    private Dictionary<DuckSkinID, ABaseDuck> duckPrefabList;
    [Space]
    [ReadOnly] public ABaseDuck CurrentDuck;

    void Awake()
    {
        duckPrefabList = new()
        {
            {DuckSkinID.Normal, normalDuck},
            {DuckSkinID.Rambo, ramboDuck}
        };
    }

    void OnValidate()
    {
        if (Application.isEditor && CurrentDuck != null)
        {
            CurrentDuck = null;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void GetDuckServerRpc(DuckSkinID skin, RpcParams serverParams = default)
    {
        ReleaseDuckForClient(serverParams.Receive.SenderClientId);

        if (duckPrefabList.TryGetValue(skin, out var duck))
        {
            if (duck == null)
            {
                EDebug.LogError($"Duck prefab with type {skin} is null");
                return;
            }
            var instance = Instantiate(duck, transform.position, Quaternion.identity);
            NetworkObject netObj = instance.GetComponent<NetworkObject>();
            netObj.transform.position = new Vector2(0, Random.Range(-1f, 1f));
            netObj.SpawnAsPlayerObject(serverParams.Receive.SenderClientId);
            CurrentDuck = instance;
            instance.InitializeStats();
            return;
        }
        else
        {
            EDebug.LogError($"Does not contains type: {skin}");
            return;
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void ReleaseDuckServerRpc(RpcParams serverParams = default)
    {
        ReleaseDuckForClient(serverParams.Receive.SenderClientId);
    }

    private void ReleaseDuckForClient(ulong clientId)
    {
        var nm = NetworkManager.Singleton;
        if (nm != null && nm.ConnectedClients.TryGetValue(clientId, out var client) && client.PlayerObject != null)
        {
            client.PlayerObject.Despawn(true);
            if (CurrentDuck != null && CurrentDuck.NetworkObject == client.PlayerObject)
            {
                CurrentDuck = null;
            }
            return;
        }

        if (CurrentDuck != null && CurrentDuck.OwnerClientId == clientId)
        {
            if (CurrentDuck.TryGetComponent<NetworkObject>(out var netObj))
                netObj.Despawn(true);
            else
                Destroy(CurrentDuck);
            CurrentDuck = null;
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
