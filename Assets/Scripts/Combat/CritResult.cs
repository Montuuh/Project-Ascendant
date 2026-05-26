namespace ProjectAscendant.Combat
{
    // Per §4.1.3 + Epic 4 Task 4.4.4 — full crit-resolution result for the
    // damage path (IsCrit feeds MoveContext) and the UI hover panel.
    public readonly struct CritResult
    {
        // Final roll: true if this move crits.
        public readonly bool IsCrit;

        // Final stacked-and-clamped probability in [0, 1]. For AlwaysCrit moves
        // this is 1.0 regardless of input bonuses (RNG is short-circuited).
        public readonly float ResolvedChance;

        // True if Move.AlwaysCrit was set. Exposed for the UI hover panel.
        public readonly bool IsAlwaysCrit;

        // True if (CombatTempBonus + PermanentPassiveBonus) > 0 — i.e. the attacker
        // has at least one crit-chance source stacked.
        public readonly bool HasChanceBonus;

        // Per §4.1.3 UI redundancy flag — both AlwaysCrit AND a chance bonus are
        // active simultaneously; the chance bonus has no effect on this move.
        // Surface this on the card hover so the player knows the buff is wasted.
        public readonly bool IsRedundant;

        public CritResult(bool isCrit, float resolvedChance, bool isAlwaysCrit, bool hasChanceBonus, bool isRedundant)
        {
            IsCrit = isCrit;
            ResolvedChance = resolvedChance;
            IsAlwaysCrit = isAlwaysCrit;
            HasChanceBonus = hasChanceBonus;
            IsRedundant = isRedundant;
        }
    }
}
