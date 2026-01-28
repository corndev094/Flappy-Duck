using Bap.State_Machine;
using UnityEngine;

public class Phase1 : BaseState
{
    // A timer to prevent checking for completion every single frame.
    private float _checkTimer;
    private const float CHECK_INTERVAL = 0.5f; // Check every 1 second.
    
    public Phase1(PhasesLevel1 ctx, PhasefactoryLevel1 factory, bool isRoot) : base(ctx, factory, isRoot)
    {
    }

    public override void Enter()
    {
        _checkTimer = 0f;
        ctx.TriggerPhase(0);
    }

    public override void Exit()
    {
        // This is called when we transition away from this state.
        // Depending on game design, you might not want to stop the phase here,
        // but it's good for cleanup if the state is exited unexpectedly.
        ctx.StopCurrentPhase();
    }

    public override void Update()
    {
        // Use a timer to avoid checking the enemy list every frame, which is inefficient.
        _checkTimer += Time.deltaTime;
        if (_checkTimer >= CHECK_INTERVAL)
        {
            _checkTimer = 0f;
            CheckTransition();
        }
    }

    protected override void CheckTransition()
    {
        // Once all enemies spawned in this phase are defeated, transition to the next phase.
        // if (ctx.AreWaveFinish())
        // {
        //     // This assumes Phase2 is a root state, based on the existing HSM structure.
        //     // If it were a sub-state, you would use ctx.TransitionSubState.
        //     ctx.TransitionRootState(this, factory.GetState<Phase2>());
        // }
    }
}
