namespace ProjectAscendant.Core
{
    // Per §9.5.2 — Combat sub-machine root.
    // Owns CombatStart → TurnLoop → CombatVictory / CombatDefeat sub-states.
    public sealed class CombatState : GameStateNode
    {
        public readonly CombatStartState   CombatStart;
        public readonly TurnLoopState      TurnLoop;
        public readonly CombatVictoryState Victory;
        public readonly CombatDefeatState  Defeat;

        public CombatState(NodeState node)
        {
            // Sibling refs are accessed only at HandleEvent time, not at construction —
            // so forward references (e.g., CombatStart referencing TurnLoop) are safe.
            CombatStart = new CombatStartState(this);
            TurnLoop    = new TurnLoopState(this);
            Victory     = new CombatVictoryState(node);
            Defeat      = new CombatDefeatState(node);
        }

        public override void OnEnter() { EnterChild(CombatStart); }
    }
}
