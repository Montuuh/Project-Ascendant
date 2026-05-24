namespace ProjectAscendant.Core
{
    // Per §9.5 — Typed event dispatched to the HSM via GameStateMachine.HandleEvent.
    // Distinct from EventBus/GameEventSO events — these drive state-machine transitions only.
    public readonly struct GameEvent
    {
        public readonly GameEventType Type;

        public GameEvent(GameEventType type) { Type = type; }

        public static readonly GameEvent None = new GameEvent(GameEventType.None);
    }

    public enum GameEventType
    {
        None,

        // ── Top-level run control ───────────────────────────────────────────────
        StartNewRun,        // MainMenu / Hub   → RunState
        ReturnToHub,        // RunState         → HubState
        GameOver,           // RunState / any   → GameOverState

        // ── Map navigation ──────────────────────────────────────────────────────
        NodeConfirmed,      // MapView          → NodeState
        NodeComplete,       // NodeState        → MapView

        // ── Combat lifecycle ────────────────────────────────────────────────────
        CombatBegin,            // CombatStart      → TurnLoop
        DrawComplete,           // DrawPhase        → IntentPhase
        IntentRevealComplete,   // IntentPhase      → ActionPhase
        PlayerEndedTurn,        // ActionPhase      → ResolutionPhase
        ResolutionComplete,     // ResolutionPhase  → TurnEnd
        TurnEndComplete,        // TurnEnd          → DrawPhase (next turn)
        CombatVictory,          // TurnLoop         → CombatVictory
        CombatDefeat,           // TurnLoop         → CombatDefeat

        // ── Evolution ───────────────────────────────────────────────────────────
        EvolutionTriggered, // MapView → EvolutionScreen
        EvolutionComplete,  // EvolutionScreen → MapView

        // ── Run end ─────────────────────────────────────────────────────────────
        RunEnded,           // MapView → RunEndState
    }
}
