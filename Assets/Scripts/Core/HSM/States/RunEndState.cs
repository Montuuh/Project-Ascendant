namespace ProjectAscendant.Core
{
    // Per §9.5.1 — run-complete screen (victory or defeat summary).
    // Transition back to Hub is driven externally: RunController publishes to EventBus →
    // external dispatcher calls GameStateMachine.HandleEvent(ReturnToHub) →
    // RunState.HandleEvent intercepts and transitions to HubState.
    public sealed class RunEndState : GameStateNode
    {
        public RunEndState(RunState run) { /* run ref reserved for Epic 11 summary logic */ }
    }
}
