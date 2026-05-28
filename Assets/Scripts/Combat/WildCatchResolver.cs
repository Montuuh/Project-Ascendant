using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §7.3.4 (LOCKED) + Epic 8 Task 8.1.4 — deterministic Pokéball catch
    // evaluation. No RNG: outcomes are a pure function of (wild HP fraction,
    // wild primary status, effect thresholds). Pillar 1 (Telegraphed tactics).
    //
    // Outcomes per §7.3.4.1:
    //   HP ≤ 0                           → FailedFainted (recruit is lost)
    //   HP ≥ threshold, no status        → FailedHighHP  (ball consumed, fight on)
    //   HP <  threshold, no status       → Caught
    //   any HP + any primary status      → Caught        (when CatchWithAnyStatus)
    //
    // MaxHP is computed from species base + level growth, matching
    // PokemonInstanceFactory.ComputeMaxHP and IntentScorer.HPFraction.
    public static class WildCatchResolver
    {
        public enum CatchAttempt { Caught, FailedHighHP, FailedFainted }

        // Pure evaluator. Null guards return FailedFainted as the safest
        // degenerate default (no ball is "spent" deciding nothing happens —
        // caller is responsible for consumption regardless of outcome).
        public static CatchAttempt Evaluate(PokemonInstance wild,
                                            CatchConsumableEffectSO effect)
        {
            if (wild == null) return CatchAttempt.FailedFainted;
            if (effect == null) return CatchAttempt.FailedHighHP;
            if (wild.CurrentHP <= 0) return CatchAttempt.FailedFainted;

            // Status-expanded catch window — §7.3.4.1 step 5c.
            if (effect.CatchWithAnyStatus &&
                wild.PrimaryStatus != StatusCondition.None)
                return CatchAttempt.Caught;

            float maxHP = ComputeMaxHP(wild);
            if (maxHP <= 0) return CatchAttempt.FailedHighHP;
            float fraction = wild.CurrentHP / maxHP;
            if (fraction < effect.CatchThresholdPercent)
                return CatchAttempt.Caught;
            return CatchAttempt.FailedHighHP;
        }

        // Convenience predicate. Same logic as Evaluate but boolean-shaped
        // for hot-path callers in CombatController.TryPlayConsumable.
        public static bool IsCatchable(PokemonInstance wild,
                                       CatchConsumableEffectSO effect)
            => Evaluate(wild, effect) == CatchAttempt.Caught;

        private static float ComputeMaxHP(PokemonInstance p)
        {
            if (p == null || p.Species == null) return 0f;
            int max = p.Species.BaseStats.BaseHP;
            if (p.Species.GrowthCurve != null)
                max += p.Species.GrowthCurve.GetHPAt(p.Level);
            return max;
        }
    }
}
