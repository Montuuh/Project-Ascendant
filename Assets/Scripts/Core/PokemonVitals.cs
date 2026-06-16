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
        // MaxHP = (Species.BaseStats.BaseHP + GrowthCurve.GetHPAt(Level)) × MaxHPMultiplier.
        // The multiplier defaults to 1.0 (behaviour-preserving); the Iron Will difficulty
        // modifier (#44, §6.8.2) sets it >1 on enemy instances at combat build, so every
        // HP read that routes through here sees the scaled max. Floored at 1.
        public static int MaxHP(PokemonInstance p)
        {
            if (p == null || p.Species == null) return 1;
            int baseMax = p.Species.BaseStats.BaseHP;
            if (p.Species.GrowthCurve != null)
                baseMax += p.Species.GrowthCurve.GetHPAt(p.Level);
            if (baseMax <= 0) return 1;

            float mult = p.MaxHPMultiplier <= 0f ? 1f : p.MaxHPMultiplier;
            int scaled = (int)System.Math.Round(baseMax * mult, System.MidpointRounding.AwayFromZero);
            return scaled < 1 ? 1 : scaled;
        }

        // Per §2.4.2 / §6.2.1 (CL-017) — Trauma-adjusted Max HP via a TWO-ZONE penalty curve:
        // stacks 1..Zone1Count cost TraumaStackPenaltyPercent each (−5% → −25% at 5); stacks beyond
        // that up to TraumaStackCap cost TraumaZone2PenaltyPercent each (−10% → −75% at 10). Stacks
        // past the cap accrue no further penalty (soft cap). Equivalent to the old linear ladder when
        // TraumaStackCap == Zone1Count (zone 2 stays empty), so callers using the legacy cap are
        // behaviour-preserved. Integer math throughout (no float literals — PA0001). Floored at 1.
        //
        // Per §7.8.3.1 (CL-016) Trauma Resistance — `traumaPenaltyReduction` shaves that many percentage
        // points off EACH per-stack penalty (zone 1 → 5−R, zone 2 → 10−R, clamped ≥ 0). Defaults to 0 so
        // every existing caller is behaviour-preserved; the run/combat layer passes the active modifier's
        // reduction (RegionModifierResolver.TraumaPenaltyReduction).
        public static int EffectiveMaxHP(PokemonInstance p, EconomyConfigSO economy, int traumaPenaltyReduction = 0)
        {
            int max = MaxHP(p);
            if (p == null || economy == null) return max;

            int stacks = p.TraumaStacks;
            if (stacks <= 0) return max;

            int cap = economy.TraumaStackCap;
            int capped = stacks < cap ? stacks : cap;
            int z1Boundary = economy.TraumaZone1StackCount;
            int zone1 = capped < z1Boundary ? capped : z1Boundary;
            int zone2 = capped - zone1; // ≥ 0

            int reduction = traumaPenaltyReduction < 0 ? 0 : traumaPenaltyReduction;
            int z1Pen = economy.TraumaStackPenaltyPercent - reduction;
            int z2Pen = economy.TraumaZone2PenaltyPercent - reduction;
            if (z1Pen < 0) z1Pen = 0;
            if (z2Pen < 0) z2Pen = 0;

            int penalty = z1Pen * zone1 + z2Pen * zone2;
            int effPercent = 100 - penalty;
            if (effPercent < 0) effPercent = 0;

            int eff = max * effPercent / 100;
            return eff < 1 ? 1 : eff;
        }
    }
}
