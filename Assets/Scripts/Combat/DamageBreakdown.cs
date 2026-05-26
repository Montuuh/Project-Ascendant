namespace ProjectAscendant.Combat
{
    // Per §4.1.1 + Epic 4 Task 4.2.1 — full damage-calculation result, exposed for
    // hover preview UI, breakdown panel, and test assertions.
    //
    // Field order is intentional: it mirrors the GDD §4.1.1 presentation order
    // (Power → Crit → STAB → TypeEff → Range → Final). Per OPEN G3, this ordering
    // is presentational only — floor() runs once at the end and multiplication is
    // commutative, so the spec's "Crit before STAB/TypeEff" rule has no
    // numerical effect. The struct order documents the intended UI breakdown.
    public readonly struct DamageBreakdown
    {
        public readonly int Power;
        public readonly int EffectiveAttack;
        public readonly int EffectiveDefense;

        // Pre-modifier base before Crit/STAB/TypeEff are applied. Per OPEN G2,
        // RangeModifier is bundled into BaseDamage (treated as intrinsic to the
        // move's power profile, applied alongside Power/Atk/Def/Divisor).
        public readonly double BaseDamage;

        public readonly double CritMultiplier;
        public readonly double StabMultiplier;
        public readonly double TypeEffectiveness;
        public readonly double RangeModifier;

        // Floored final integer damage. Per Task 4.2.4, floor() runs once, at the end.
        public readonly int Final;

        public readonly bool IsCrit;
        public readonly bool HasStab;

        public DamageBreakdown(
            int power,
            int effectiveAttack,
            int effectiveDefense,
            double baseDamage,
            double critMultiplier,
            double stabMultiplier,
            double typeEffectiveness,
            double rangeModifier,
            int final,
            bool isCrit,
            bool hasStab)
        {
            Power = power;
            EffectiveAttack = effectiveAttack;
            EffectiveDefense = effectiveDefense;
            BaseDamage = baseDamage;
            CritMultiplier = critMultiplier;
            StabMultiplier = stabMultiplier;
            TypeEffectiveness = typeEffectiveness;
            RangeModifier = rangeModifier;
            Final = final;
            IsCrit = isCrit;
            HasStab = hasStab;
        }
    }
}
