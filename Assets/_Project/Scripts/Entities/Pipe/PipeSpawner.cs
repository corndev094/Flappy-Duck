using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

public class PipeSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject pipePrefab;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float pipeOffsetY = 1f;
    [SerializeField] private float minY = -1f;
    [SerializeField] private float maxY = 3f;
    [SerializeField] private float pipeSpacing = 5f;

    private float currentXPos;

    private void Start()
    {
        if (!IsServer) return; // Only run on the server
        StartSpawn().Forget();
    }

    private async UniTaskVoid StartSpawn()
    {
        currentXPos = 7;
        while (gameObject.activeInHierarchy)
        {
            await UniTask.Delay((int)(spawnInterval * 1000), cancellationToken: this.GetCancellationTokenOnDestroy());
            if (this == null || !enabled) return;

            SpawnPipeServerRpc();
            currentXPos += pipeSpacing;
        }
    }

    [ServerRpc]
    private void SpawnPipeServerRpc()
    {
        float randomY = Random.Range(minY, maxY);
        float randomOffset = Random.Range(0, pipeOffsetY);
        Vector3 spawnPosition = new(currentXPos, randomY, transform.position.z);
        
        GameObject pipeInstance = Instantiate(pipePrefab, spawnPosition, Quaternion.identity);
        pipeInstance.GetComponent<NetworkObject>().Spawn();
        
        IPipe pipe = pipeInstance.GetComponent<IPipe>();
        if (pipe != null)
        {
            pipe.Setup(randomOffset);
        }
        else
        {
            Debug.LogError("Pipe prefab does not have a component that implements IPipe.", pipeInstance);
        }
    }
}