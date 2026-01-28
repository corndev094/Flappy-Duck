using System;
using System.Collections.Generic;
using System.Threading;
using Bap.State_Machine;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class PhasesLevel1 : APhase {
    [SerializeField] private ABaseDuck bird;
    private BaseState _currentSuperState;
    private BaseState _currentSubState;
    
    [Header("References")]
    [SerializeField] private PhasefactoryLevel1 _factory;

    [Header("Phase Data")]
    [SerializeField] private List<EntitySpawnTemplate> phaseSpawners = new();
    
    [Header("Debug")]
    public List<string> StateList;
    public string CurrentSuperStateString;
    public string CurrentSubStateString;
    public float StartTime { get; private set; }
    public float ElapseTime { get; private set; }
    
    private EntitySpawnTemplate currentWave;
    private readonly List<GameObject> _activeEnemies = new();
    private CancellationTokenSource _phaseCts;
    private CancellationTokenSource _elapseTimeCts;
    private UniTask _currentWaveTask;

    public PhasefactoryLevel1 PhasefactoryLevel1 { get => _factory; }
    public BaseState CurrentSuperState
    {
        get => _currentSuperState;
        private set
        {
            _currentSuperState = value;
            CurrentSuperStateString = _currentSuperState.GetType().Name;
        }
    }
    public BaseState CurrentSubState
    {
        get => _currentSubState;
        private set
        {
            _currentSubState = value;
            CurrentSubStateString = _currentSubState.GetType().Name;
        }
    }

    private void Awake()
    {
        _factory ??= GetComponent<PhasefactoryLevel1>();
        if (phaseSpawners == null) return;
        
        foreach (var spawner in phaseSpawners)
        {
            if (spawner != null)
            {
                spawner.OnEntitiesSpawned += HandleEntitiesSpawned;
            }
        }
    }

    private void OnDestroy()
    {
        _phaseCts?.Cancel();
        _phaseCts?.Dispose();
        _elapseTimeCts?.Cancel();
        _elapseTimeCts?.Dispose();

        if (phaseSpawners == null) return;
        
        foreach (var spawner in phaseSpawners)
        {
            if (spawner != null)
            {
                spawner.OnEntitiesSpawned -= HandleEntitiesSpawned;
            }
        }
    }

    [ContextMenu("StartPhase")]
    public override void StartPhase()
    {
        StateList = _factory.GetStateList();
        SetCurrentSuperState(_factory.GetState<Phase1>());
        CurrentSuperState.Init();
        _elapseTimeCts = new();
        UpdateElapseTime(_elapseTimeCts).Forget();
    }

    private void Update()
    {
        CurrentSuperState?.UpdateStates();
    }

    private async UniTask UpdateElapseTime(CancellationTokenSource token)
    {
        StartTime = Time.time;
        while (true)
        {
            ElapseTime += Time.deltaTime;
            if (token.IsCancellationRequested) break;
            await UniTask.Yield();
        }
    }

    private void HandleEntitiesSpawned(List<GameObject> spawnedEntities)
    {
        _activeEnemies.AddRange(spawnedEntities);
    }
    
    public void TriggerPhase(int phaseIndex)
    {
        if (phaseIndex < 0 || phaseIndex >= phaseSpawners.Count || phaseSpawners[phaseIndex] == null)
        {
            Debug.LogError($"Invalid phase index or spawner not set for index {phaseIndex}", this);
            return;
        }
        
        currentWave = phaseSpawners[phaseIndex];
        _currentWaveTask = phaseSpawners[phaseIndex].StartSequence();
        Debug.Log($"Phase {phaseIndex + 1} triggered.");
    }

    public void StopCurrentPhase()
    {
        _phaseCts = new();
        _phaseCts?.Cancel();
        for (int i = _activeEnemies.Count - 1; i >= 0; i--)
        {
            if (_activeEnemies[i] != null)
            {
                Destroy(_activeEnemies[i]);
            }
        }
        _activeEnemies.Clear();
    }

    public bool AreWaveFinish()
    {
        // A wave is finished if its task is no longer running.
        return _currentWaveTask.Status != UniTaskStatus.Pending;
    }

    public bool AreAllEnemiesDefeated()
    {
        // Remove any null entries (destroyed enemies) from the list
        _activeEnemies.RemoveAll(item => item == null);
        
        // If the list is empty, all enemies that were spawned have been defeated.
        return _activeEnemies.Count == 0;
    }

    public void TransitionRootState(BaseState from, BaseState to)
    {
        if(to == null || from == null)
        {
            EDebug.LogError($"State {from.GetType().Name} or {to.GetType().Name} is null");
            return;
        }

        if (!from.IsRootState || !to.IsRootState)
        {
            EDebug.LogError($"Transition state fail. State {from.GetType().Name} and {to.GetType().Name} is not root state ");
            return;
        }
        
        from.Exit();
        from.CurrentSubState?.Exit();
        SetCurrentSuperState(to);
        to.Enter();
    }

    public void TransitionSubState(BaseState from, BaseState to)
    {
        if(to == null || from == null)
        {
            EDebug.LogError($"State {from.GetType().Name} or {to.GetType().Name} is null");
            return;
        }

        if (from.IsRootState || to.IsRootState)
        {
            EDebug.LogError($"Transition state fail. State {from.GetType().Name} and {to.GetType().Name} is not subState");
        }

        from.Exit();
        SetCurrentSubState(to);
        to.Enter();
    }
    
    public void SetCurrentSuperState(BaseState state)
    {
        if (state != null)
        {
            CurrentSuperState = state;
        }
        else
        {
            //TODO: Switch to empty state to avoid crashing game
            EDebug.LogError($"[HFSM] {CurrentSuperState.GetType().Name} switchs to null state");
        }
    }
    public void SetCurrentSubState(BaseState state)
    {
        if (state != null)
        {
            CurrentSubState = state;
        }
        else
        {
            //TODO: Switch to empty state to avoid crashing game
            EDebug.LogError($"[HFSM] {CurrentSubState.GetType().Name} switchs to null state");
        }
    }
}