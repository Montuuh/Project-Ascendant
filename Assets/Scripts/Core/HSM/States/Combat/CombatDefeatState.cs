namespace ProjectAscendant.Core
{
    // Per §9.5.2 — all three Pokémon fainted; run ends.
    // Fires GameOver externally; RunState.HandleEvent transitions to GameOverState.
    // Epic 4 adds: Trauma stack application, run-end summary construction.
    public sealed class CombatDefeatState : GameStateNode
    {
        public CombatDefeatState(NodeState node) { /* node ref reserved for Epic 4 defeat logic */ }
    }
}
