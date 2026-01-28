using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bap.State_Machine
{
    /// <summary>
    /// Caching all states as a factory
    /// </summary>
    public class PhasefactoryLevel1 : MonoBehaviour
    {
        [SerializeField] private PhasesLevel1 _ctx;
        private Dictionary<Type, BaseState> _states = new();

        private void Awake()
        {
            _ctx ??= GetComponent<PhasesLevel1>();
            try
            {
                _states[typeof(Phase1)] = (BaseState)Activator.CreateInstance(typeof(Phase1), _ctx, this, true);
                // _states[typeof(Phase2)] = (BaseState)Activator.CreateInstance(typeof(Phase2), _ctx, this, true);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[HFSM] Failed to initialize state factory: {ex.Message}");
                throw; // Rethrow to ensure the issue is noticed during development
            }
        }
        
        public BaseState GetState<T>() where T : BaseState
        {
            if (_states.TryGetValue(typeof(T), out var state))
            {
                return state;
            }
            
            throw new ArgumentException($"State type {typeof(T).Name} not registered");
        }

        public List<string> GetStateList()
        {
            return _states.Values.Select(state => state.GetType().Name + ". IsRoot: " + state.IsRootState).ToList();
        }
    }

    public enum PhaseLevel1 {Phase1, Phase2}
}