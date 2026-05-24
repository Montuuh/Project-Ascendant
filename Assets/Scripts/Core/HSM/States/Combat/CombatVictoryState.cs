namespace ProjectAscendant.Core
{
    // Per §9.5.2 — combat won; awards XP / loot, then fires NodeComplete externally.
    // Epic 4 adds: XP grant, loot roll, evolution check → EvolutionTriggered if needed.
    // Transition to MapView happens via NodeState.HandleEvent(NodeComplete).
    public sealed class CombatVictoryState : GameStateNode
    {
        public CombatVictoryState(NodeState node) { /* node ref reserved for Epic 4 loot logic */ }
    }
}
