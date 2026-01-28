using Bap.State_Machine;
using UnityEngine;

public class Phase2 : BaseState
{
    public Phase2(PhasesLevel1 ctx, PhasefactoryLevel1 factory, bool isRoot) : base(ctx, factory, isRoot)
    {
    }

    public override void Enter()
    {
        // Start the second phase of enemies
        ctx.TriggerPhase(1);
    }

    public override void Exit()
    {
        ctx.StopCurrentPhase();
    }

    public override void Update()
    {
        // In a real game, you might have logic specific to this phase
    }

    protected override void CheckTransition()
    {
        // Condition to move to Phase 3 or end the level
        // if (ctx.AreAllEnemiesDefeated())
        // {
        //     // Transition to next phase
        // }
    }
}
