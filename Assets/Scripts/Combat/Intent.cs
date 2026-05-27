using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §4.3.2 + Epic 4 Task 4.7 — intent kinds the AI can choose.
    // Targets POSITIONS (slots), not specific Pokémon (§4.3.2 load-bearing).
    public enum IntentKind
    {
        Attack,      // single-target damage to TargetSlot (Lead or bench)
        Cleave,      // damages all occupied non-fainted slots (§4.3.4.1)
        Backstrike,  // specific bench slot; fizzles on empty (§4.3.4.1)
        Buff,        // attacker raises its own stat by one stage
        Stall,       // attacker applies defensive effect to itself
        Status,      // applies AppliedStatus to TargetSlot's occupant
        Unknown      // hidden intent — ❓ displayed (§4.3.5)
    }

    // Per §4.3.5 — visibility tier. Hidden = ❓ display. Witnessed/Scouted/
    // Researched/AlwaysVisible all show the full intent to the player.
    public enum IntentReveal
    {
        Hidden,
        Witnessed,
        Scouted,
        Researched,
        AlwaysVisible
    }

    // Immutable per turn. Value-semantic struct so intents can be stored in
    // pooled lists / arrays without GC pressure.
    public struct Intent
    {
        public IntentKind Kind;
        public MoveSO Move;                // null only for Unknown
        public int TargetSlot;             // 0..2 for slot-targeted; -1 otherwise
        public Stat BuffStat;              // only meaningful when Kind == Buff
        public StatusCondition AppliedStatus; // only meaningful when Kind == Status
        public IntentReveal Reveal;

        // Per §4.3.2 — display label component. The slot index is the source
        // of truth; the occupant lookup happens at render time.
        public bool TargetsSlot =>
            Kind == IntentKind.Attack ||
            Kind == IntentKind.Backstrike ||
            Kind == IntentKind.Status;
    }
}
