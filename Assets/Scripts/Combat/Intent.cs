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
        Buff,        // attacker raises its OWN stat (self-target) — e.g. Harden
        Stall,       // attacker applies a defensive/heal effect to itself (self-target)
        Status,      // applies AppliedStatus to TargetSlot's occupant
        Debuff,      // lowers a stat of TargetSlot's occupant (the player) — e.g. Growl
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
        public Stat BuffStat;              // stat raised (Buff) or lowered (Debuff)
        public StatusCondition AppliedStatus; // only meaningful when Kind == Status
        public IntentReveal Reveal;

        // Per §4.3.2 + Pillar 2 — the Lead is a POSITION, not a fixed Pokémon. An intent
        // telegraphed at the Lead must re-resolve to WHOEVER is the Lead at Resolution time, so a
        // manual swap (or SF/SB) can pull a tank into the hit — that's the whole point of "every
        // swap is a decision". Backstrike / fixed-bench intents leave this false (they target a
        // specific position). TargetSlot still holds the Lead's slot at declaration time for AI
        // scoring; EffectiveTargetSlot is the source of truth for resolution + display.
        public bool TargetsLead;

        // The slot this intent actually hits given the current Lead.
        public int EffectiveTargetSlot(int currentLeadIndex) =>
            TargetsLead ? currentLeadIndex : TargetSlot;

        // Per §4.3.2 — display label component. The slot index is the source
        // of truth; the occupant lookup happens at render time.
        public bool TargetsSlot =>
            Kind == IntentKind.Attack ||
            Kind == IntentKind.Backstrike ||
            Kind == IntentKind.Status ||
            Kind == IntentKind.Debuff;

        // Per §4.3.2 — Buff/Stall act on the attacker itself; the UI renders
        // these as "→ Self" rather than a player slot.
        public bool TargetsSelf =>
            Kind == IntentKind.Buff ||
            Kind == IntentKind.Stall;
    }
}
