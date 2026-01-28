using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public enum PathFacingDirection { Right, Left, Up, Down }

[System.Serializable]
public class Wave
{
    public string waveName = "New Wave";
    [Header("Path")]
    public Transform startPoint;
    public Transform endPoint;
    [Space]
    [Header("Wave Settings")]
    public GameObject entityPrefab;
    [Range(1, 100)] public int spawnCount = 5;
    public EntitySpawnTemplate.FormationType formation = EntitySpawnTemplate.FormationType.Circle;
    public List<Transform> customFormationPoints;
    public float formationScale = 2f;
    public float spawnInterval = 0.5f;
    public EntitySpawnTemplate.MovementType movement = EntitySpawnTemplate.MovementType.Linear;
    public float speed = 3f;
    public bool facePathDirection = false;
    public PathFacingDirection pathFacing = PathFacingDirection.Right;
    public bool loop = false;

    [Header("Next Wave Trigger")]
    public TriggerCondition triggerForNext = TriggerCondition.OnWaveClear;
    [Tooltip("Delay in seconds for 'AfterDelay' condition from this wave's start.")]
    public float nextWaveDelay = 1.0f;
}

public enum TriggerCondition
{
    [Tooltip("Start the next wave as soon as this wave's movement is complete.")]
    OnWaveMovementFinished,
    [Tooltip("Start the next wave only when all entities from this wave are destroyed.")]
    OnWaveClear,
    [Tooltip("Start the next wave after a specific delay from this wave's start.")]
    AfterDelay,
    [Tooltip("Start the next wave at the same time as this one.")]
    WithThis,
}


public class EntitySpawnTemplate : MonoBehaviour
{
    [Header("General Settings")]
    [SerializeField] private FacingDirection facingDirection = FacingDirection.Right;
    [Header("Movement References")]
    [SerializeField] private Transform homingTarget;   // For Homing movement
    [SerializeField] private Transform orbitCenter;    // For Orbit movement

    [Header("Pooling")]
    [SerializeField] private int initialPoolSizePerPrefab = 10;
    [SerializeField] private int maxPoolSizePerPrefab = 100;
    [SerializeField] private bool autoExpand = true;

    public List<Wave> waves = new List<Wave>();

    [Header("DEBUG")]
    public string currentWaveName;

    public Action<List<GameObject>> OnEntitiesSpawned;
    public UnityEvent OnSequenceCompleted;
    public UnityEvent OnStartSpawn;

    // Pooling system for multiple prefabs
    private readonly Dictionary<GameObject, Queue<GameObject>> _inactivePools = new();
    private readonly HashSet<GameObject> _activeEntities = new();
    private readonly Dictionary<int, List<GameObject>> _activeWaves = new(); // waveIndex -> entities

    void Awake()
    {
        InitializePools();
    }

    private void InitializePools()
    {
        foreach (var wave in waves)
        {
            if (wave.entityPrefab != null && !_inactivePools.ContainsKey(wave.entityPrefab))
            {
                _inactivePools[wave.entityPrefab] = new Queue<GameObject>();
                ExpandPool(wave.entityPrefab, initialPoolSizePerPrefab);
            }
        }
    }

    private void ExpandPool(GameObject prefab, int count)
    {
        if (!_inactivePools.TryGetValue(prefab, out var pool)) return;

        int currentTotal = pool.Count + CountActive(prefab);
        if (currentTotal + count > maxPoolSizePerPrefab)
        {
            count = Mathf.Max(0, maxPoolSizePerPrefab - currentTotal);
            if (count == 0)
            {
                Debug.LogWarning($"Pool for {prefab.name} reached max size ({maxPoolSizePerPrefab})!", this);
                return;
            }
        }

        for (int i = 0; i < count; i++)
        {
            var go = Instantiate(prefab, transform);
            go.SetActive(false);
            // Store prefab info for later
            var identifier = go.AddComponent<PoolIdentifier>();
            identifier.Prefab = prefab;
            pool.Enqueue(go);
        }
    }

    private int CountActive(GameObject prefab)
    {
        int count = 0;
        foreach (var entity in _activeEntities)
        {
            if (entity.GetComponent<PoolIdentifier>()?.Prefab == prefab)
            {
                count++;
            }
        }
        return count;
    }

    public async UniTask StartSequence()
    {
        OnStartSpawn?.Invoke();
        _activeWaves.Clear();

        List<UniTask> parallelTasks = new List<UniTask>();

        for (int i = 0; i < waves.Count; i++)
        {
            var currentWave = waves[i];
            var waveTask = SpawnWaveAsync(currentWave, i);
            currentWaveName = currentWave.waveName;

            // Decide how to proceed based on the trigger condition for the *next* wave
            if (i < waves.Count - 1)
            {
                var nextTrigger = currentWave.triggerForNext;
                switch (nextTrigger)
                {
                    case TriggerCondition.WithThis:
                        // Don't await, let it run in parallel with the next one
                        parallelTasks.Add(waveTask);
                        break;
                    case TriggerCondition.AfterDelay:
                        // Run the task, but don't wait for it to finish. Just wait for the delay.
                        waveTask.Forget();
                        await UniTask.Delay(TimeSpan.FromSeconds(currentWave.nextWaveDelay));
                        break;
                    case TriggerCondition.OnWaveMovementFinished:
                        // Wait for all parallel tasks so far, plus the current one
                        await UniTask.WhenAll(parallelTasks);
                        parallelTasks.Clear();
                        await waveTask;
                        break;
                    case TriggerCondition.OnWaveClear:
                        // Wait for all parallel tasks so far, plus the current one
                        await UniTask.WhenAll(parallelTasks);
                        parallelTasks.Clear();
                        await waveTask; // Wait for movement to finish first
                        await WaitUntilWaveCleared(i); // Then wait for entities to be cleared
                        break;
                }
            }
            else // This is the last wave
            {
                await UniTask.WhenAll(parallelTasks); // Wait for any remaining parallel tasks
                await waveTask; // Wait for the last wave to finish
            }
        }
        
        OnSequenceCompleted?.Invoke();
        Debug.Log("Wave sequence completed.");
    }

    private async UniTask SpawnWaveAsync(Wave wave, int waveIndex)
    {
        if (wave.startPoint == null || wave.endPoint == null)
        {
            Debug.LogError($"Wave '{wave.waveName}' is missing a StartPoint or EndPoint.", this);
            return;
        }
        if (wave.entityPrefab == null)
        {
            Debug.LogError($"Wave '{wave.waveName}' has no entity prefab assigned.", this);
            return;
        }

        var pool = _inactivePools[wave.entityPrefab];
        if (pool.Count < wave.spawnCount)
        {
            if (autoExpand)
            {
                ExpandPool(wave.entityPrefab, wave.spawnCount - pool.Count);
            }
            else
            {
                Debug.LogError($"Not enough entities in pool for wave '{wave.waveName}'. Available: {pool.Count}", this);
                return;
            }
        }

        IFormationTemplate formTemplate = GetFormationTemplate(wave.formation);
        if (wave.formation == FormationType.SequentialLine)
        {
            await SpawnSequentialLine(wave, waveIndex);
            return;
        }
        
        IMovementBehavior moveBehavior = GetMovementBehavior(wave.movement, wave.formationScale);

        List<Vector3> offsets;
        int count = wave.spawnCount;

        if (wave.formation == FormationType.Multiple)
        {
            offsets = new List<Vector3>();
            if (wave.customFormationPoints != null)
            {
                foreach (var point in wave.customFormationPoints)
                {
                    if (point != null)
                    {
                        // The offset is the world position of the point relative to the wave's start point
                        offsets.Add(point.position - wave.startPoint.position);
                    }
                }
            }
            count = offsets.Count; // Override spawn count with the number of custom points
        }
        else
        {
            formTemplate = GetFormationTemplate(wave.formation);
            offsets = formTemplate.GetLocalOffsets(count, wave.formationScale);
        }

        if (pool.Count < count)
        {
            if (autoExpand)
            {
                ExpandPool(wave.entityPrefab, count - pool.Count);
            }
            else
            {
                Debug.LogError($"Not enough entities in pool for wave '{wave.waveName}'. Available: {pool.Count}", this);
                return;
            }
        }

        bool useParent = !(wave.movement == MovementType.Sine);
        GameObject parent = null;
        List<GameObject> spawned = new List<GameObject>();

        if (useParent)
        {
            parent = new GameObject($"{wave.waveName} Group");
            parent.transform.position = wave.startPoint.position;
            parent.transform.SetParent(transform, false);
            if (wave.facePathDirection)
            {
                Vector3 direction = (wave.endPoint.position - wave.startPoint.position).normalized;
                if (direction != Vector3.zero)
                {
                    switch (wave.pathFacing)
                    {
                        case PathFacingDirection.Right:
                            parent.transform.right = direction;
                            break;
                        case PathFacingDirection.Left:
                            parent.transform.right = -direction;
                            break;
                        case PathFacingDirection.Up:
                            parent.transform.up = direction;
                            break;
                        case PathFacingDirection.Down:
                            parent.transform.up = -direction;
                            break;
                    }
                }
            }
        }

        for (int i = 0; i < count && i < offsets.Count && pool.Count > 0; i++)
        {
            var entity = pool.Dequeue();
            if (entity == null) continue;
            Vector3 worldPos = wave.startPoint.position + offsets[i];

            if (useParent)
            {
                entity.transform.SetParent(parent.transform, false);
                entity.transform.localPosition = offsets[i];
            }
            else
            {
                entity.transform.position = worldPos;
            }

            if (!wave.facePathDirection)
            {
                SetFacingDirection(entity.transform);
            }
            entity.SetActive(true);
            _activeEntities.Add(entity);
            spawned.Add(entity);
        }

        _activeWaves[waveIndex] = spawned;
        OnEntitiesSpawned?.Invoke(spawned);

        UniTask moveTask;
        if (useParent)
        {
            moveTask = new LinearMovement().Move(parent, wave.startPoint.position, wave.endPoint.position, wave.speed)
                .ContinueWith(() => {
                    foreach (var ent in spawned)
                    {
                        if (ent == null) continue;
                        ent.transform.SetParent(transform, false);
                        ReturnEntity(ent);
                    }
                    if(parent != null) Destroy(parent);
                });
        }
        else
        {
            var tasks = new List<UniTask>();
            foreach (var ent in spawned)
            {
                Vector3 startPos = ent.transform.position;
                tasks.Add(moveBehavior.Move(ent, startPos, wave.endPoint.position, wave.speed));
            }
            moveTask = UniTask.WhenAll(tasks).ContinueWith(() => {
                foreach (var ent in spawned) ReturnEntity(ent);
            });
        }

        await moveTask;

        if (wave.loop)
        {
            await UniTask.NextFrame();
            SpawnWaveAsync(wave, waveIndex).Forget();
        }
    }


    private async UniTask SpawnSequentialLine(Wave wave, int waveIndex)
    {
        if (wave.startPoint == null || wave.endPoint == null)
        {
            Debug.LogError($"SequentialLine Wave '{wave.waveName}' is missing a StartPoint or EndPoint.", this);
            return;
        }
        if (wave.entityPrefab == null)
        {
            Debug.LogError($"Wave '{wave.waveName}' has no entity prefab assigned.", this);
            return;
        }

        var pool = _inactivePools[wave.entityPrefab];
        if (pool.Count < wave.spawnCount)
        {
            if (autoExpand)
            {
                ExpandPool(wave.entityPrefab, wave.spawnCount - pool.Count);
            }
            else
            {
                Debug.LogError($"Not enough entities in pool for wave '{wave.waveName}'. Available: {pool.Count}", this);
                return;
            }
        }

        IMovementBehavior moveBehavior = GetMovementBehavior(wave.movement, wave.formationScale);
        var spawnedEntities = new List<GameObject>();
        var movementTasks = new List<UniTask>();

        for (int i = 0; i < wave.spawnCount; i++)
        {
            if (pool.Count == 0)
            {
                Debug.LogWarning($"Pool ran out of entities for wave '{wave.waveName}' after spawning {i} of {wave.spawnCount}.", this);
                break;
            }

            var entity = pool.Dequeue();
            if (entity == null) continue;

            entity.transform.position = wave.startPoint.position;

            if (wave.facePathDirection)
            {
                Vector3 direction = (wave.endPoint.position - wave.startPoint.position).normalized;
                if (direction != Vector3.zero)
                {
                    switch (wave.pathFacing)
                    {
                        case PathFacingDirection.Right:
                            entity.transform.right = direction;
                            break;
                        case PathFacingDirection.Left:
                            entity.transform.right = -direction;
                            break;
                        case PathFacingDirection.Up:
                            entity.transform.up = direction;
                            break;
                        case PathFacingDirection.Down:
                            entity.transform.up = -direction;
                            break;
                    }
                }
            }
            else
            {
                SetFacingDirection(entity.transform);
            }
            
            entity.SetActive(true);
            _activeEntities.Add(entity);
            spawnedEntities.Add(entity);

            // Start movement for this single entity and add its task to the list
            UniTask moveTask = moveBehavior.Move(entity, entity.transform.position, wave.endPoint.position, wave.speed)
                .ContinueWith(() => ReturnEntity(entity));
            movementTasks.Add(moveTask);

            // Wait for the interval before spawning the next entity
            if (i < wave.spawnCount - 1)
            {
                await UniTask.Delay(TimeSpan.FromSeconds(wave.spawnInterval), cancellationToken: this.GetCancellationTokenOnDestroy());
            }
        }

        _activeWaves[waveIndex] = spawnedEntities;
        OnEntitiesSpawned?.Invoke(spawnedEntities);

        // Wait for all individual movement tasks to complete
        await UniTask.WhenAll(movementTasks);
    }


    private async UniTask WaitUntilWaveCleared(int waveIndex)
    {
        if (!_activeWaves.ContainsKey(waveIndex)) return;

        await UniTask.WaitUntil(() => {
            // This check relies on entities being destroyed or deactivated externally.
            // We remove null (destroyed) entities from the list.
            _activeWaves[waveIndex].RemoveAll(e => e == null || !e.activeInHierarchy);
            return _activeWaves[waveIndex].Count == 0;
        }, cancellationToken: this.GetCancellationTokenOnDestroy());
        
        Debug.Log($"Wave {waveIndex} cleared.");
    }

    private IFormationTemplate GetFormationTemplate(FormationType formation) => formation switch
    {
        FormationType.Single => new SingleFormation(),
        FormationType.Circle => new CircleFormation(),
        FormationType.V => new VFormation(),
        _ => new SingleFormation()
    };

    private IMovementBehavior GetMovementBehavior(MovementType movement, float formationScale) => movement switch
    {
        MovementType.Linear => new LinearMovement(),
        MovementType.Sine => new SineMovement(),
        MovementType.Homing => new HomingMovement { target = homingTarget },
        MovementType.Orbit => new OrbitMovement { center = orbitCenter, radius = formationScale },
        _ => new LinearMovement()
    };

    private void SetFacingDirection(Transform t)
    {
        Vector3 s = t.localScale;
        s.x = facingDirection == FacingDirection.Left ? -Mathf.Abs(s.x) : Mathf.Abs(s.x);
        t.localScale = s;
    }

    private void ReturnEntity(GameObject ent)
    {
        if (ent == null || !_activeEntities.Contains(ent)) return;
        
        ent.SetActive(false);
        _activeEntities.Remove(ent);

        var identifier = ent.GetComponent<PoolIdentifier>();
        if (identifier != null && _inactivePools.TryGetValue(identifier.Prefab, out var pool))
        {
            pool.Enqueue(ent);
        }
        else
        {
            // If prefab not found in pools (should not happen), destroy it.
            Destroy(ent);
        }
    }

    public enum FacingDirection { Left, Right }
    public enum FormationType { Single, Circle, V, Multiple, SequentialLine }
    public enum MovementType { Linear, Sine, Homing, Orbit }
}

// Helper component to identify the original prefab of a pooled object
public class PoolIdentifier : MonoBehaviour
{
    public GameObject Prefab;
}