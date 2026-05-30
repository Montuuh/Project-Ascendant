namespace ProjectAscendant.Core
{
    // Per §9.5.1 — Active node being resolved.
    // The run-layer RunController (Map assembly) owns node-type behaviour and drives this state
    // coarsely via GameEvents (NodeConfirmed → here; NodeComplete → MapView; RunEnded → RunEnd).
    // OnEnter parks in CombatState (a harmless stub for non-combat nodes — CombatStartState just
    // waits for CombatBegin). Per-node-type HSM sub-states + the live combat screen are Epic 13.
    public sealed class NodeState : GameStateNode
    {
        public readonly CombatState Combat;

        private readonly RunState _run;

        public NodeState(RunState run)
        {
            _run   = run;
            Combat = new CombatState(this);
        }

        // Default entry is CombatState; the combat screen wires real combat in Epic 13.
        public override void OnEnter() { EnterChild(Combat); }

        public override void HandleEvent(GameEvent evt)
        {
            if (evt.Type == GameEventType.NodeComplete)
                TransitionTo(_run.MapView);
            // Per §7.13 — a Gym victory ends the run from inside the node (run-end transition,
            // Task 8.5.9). MapView also handles RunEnded; NodeState handles the in-node case.
            else if (evt.Type == GameEventType.RunEnded)
                TransitionTo(_run.RunEnd);
            else
                base.HandleEvent(evt);
        }
    }
}
