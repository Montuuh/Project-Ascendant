using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §4.1.3 + Epic 4 Task 4.4.2 — input bundle for CritResolver.
    // The resolver is pure math; combat assembles bonuses from runtime state
    // (active consumables + attacker's selected branch) and hands them in here.
    //
    // CombatTempBonus and PermanentPassiveBonus are both in [0, 1] — the resolver
    // clamps the sum, so callers may pass over-range values defensively.
    public readonly struct CritInputs
    {
        // Per §4.1.3 source 1 — move-level AlwaysCrit (independent of chance stacking).
        public readonly MoveSO Move;

        // Per §4.1.3 source 2 — Consumable temporary boost. Sourced from
        // CritResolver.GatherConsumableBonus over the active consumables.
        public readonly float CombatTempBonus;

        // Per §4.1.3 source 3 — Evolution offensive-path passive. Sourced from
        // CritResolver.GatherPassiveBonus over the attacker's selected branch.
        public readonly float PermanentPassiveBonus;

        public CritInputs(MoveSO move, float combatTempBonus, float permanentPassiveBonus)
        {
            Move = move;
            CombatTempBonus = combatTempBonus;
            PermanentPassiveBonus = permanentPassiveBonus;
        }
    }
}
