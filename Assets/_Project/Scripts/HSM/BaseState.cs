using UnityEngine;

namespace Bap.State_Machine
{
    public abstract class BaseState
    {
        protected PhasesLevel1 ctx;
        protected PhasefactoryLevel1 factory;
        protected BaseState currentSuperState;
        protected BaseState currentSubState;

        public bool IsRootState;

        public BaseState CurrentSubState => currentSubState; 
        public BaseState CurrentSuperState => currentSuperState;

        public BaseState(PhasesLevel1 ctx, PhasefactoryLevel1 factory, bool isRoot)
        {
            this.ctx = ctx;
            this.factory = factory;
            IsRootState = isRoot;
        }

        public void Init()
        {
            Enter();
            InitializeSubState();
        }

        public abstract void Enter();
        public abstract void Update();
        public abstract void Exit();
        protected abstract void CheckTransition();
        public virtual void InitializeSubState(){}

        public void SetSubState(BaseState subState)
        {
            if (subState != null)
            {
                currentSubState = subState;
                subState.SetSuperState(this);
            }
            else
            {
                Debug.LogError("Set SubState to null");
            }
        }

        protected void SetSuperState(BaseState superState)
        {
            if (superState != null)
            {
                currentSuperState = superState;
            }
            else
            {
                Debug.LogError("Set SuperState to null");
            }
        }

        public void UpdateStates()
        {
            Update();
            currentSubState?.UpdateStates();
            CheckTransition();
        }
    }
}