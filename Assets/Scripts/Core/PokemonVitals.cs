namespace ProjectAscendant.Core
{
    // Per §2.4.2 / §4.x / §6.2 + Epic 9 — canonical HP computation for a PokemonInstance.
    //
    // Establishes the single shared home the existing "shared-helper TODO" calls for. CombatController,
    // WildCatchResolver, IntentScorer, BossPhaseTracker, and PokemonInstanceFactory each compute MaxHP
    // inline today (and the factory's stub `BaseHP + level*2` even disagrees with combat's
    // `BaseHP + GrowthCurve`). Those sites should migrate here — tracked as tech-debt; not refactored
    // in this task to keep the combat suite untouched.
    public static class PokemonVitals
    {
        // MaxHP = Species.BaseStats.BaseHP + GrowthCurve.GetHPAt(Level). Mirrors CombatController.
        public static int MaxHP(PokemonInstance p)
        {
            if (p == null || p.Species == null) return 1;
            int max = p.Species.BaseStats.BaseHP;
            if (p.Species.GrowthCurve != null)
                max += p.Species.GrowthCurve.GetHPAt(p.Level);
            return max <= 0 ? 1 : max;
        }

        // Per §2.4.2 / §6.2 — Trauma-adjusted Max HP = MaxHP × (1 − penaltyPercent/100 × min(stacks, cap)).
        // Linear per §2.4.2 (`MaxHP × (1 - TraumaPenalty)`) and §2.6 (−25% at the 5-stack soft cap).
        // Integer math throughout (no float literals — PA0001). Floored at 1.
        public static int EffectiveMaxHP(PokemonInstance p, EconomyConfigSO economy)
        {
            int max = MaxHP(p);
            if (p == null || economy == null) return max;

            int stacks = p.TraumaStacks;
            if (stacks <= 0) return max;
            int capped = stacks < economy.TraumaStackCap ? stacks : economy.TraumaStackCap;

            int effPercent = 100 - economy.TraumaStackPenaltyPercent * capped;
            if (effPercent < 0) effPercent = 0;

            int eff = max * effPercent / 100;
            return eff < 1 ? 1 : eff;
        }
    }
}
