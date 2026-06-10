using UnityEngine;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §7.3.4 (CL-014) + Epic 8 Task 8.1.4 — deterministic Pokéball catch via a 0–100 Catchability
    // gauge. No RNG: the gauge is a pure function of (wild HP fraction, wild primary status, effect
    // thresholds). Pillar 1 (Telegraphed tactics).
    //
    //   CatchThreshold = effect.CatchThresholdPercent (ball tier) + (hasStatus ? StatusCatchBonusPercent : 0)
    //   gauge          = clamp(0, 100, round(100 × (1 − HPfraction) / (1 − CatchThreshold)))
    //   Catch succeeds when gauge == 100  (i.e. HPfraction ≤ CatchThreshold).
    //
    // Outcomes per §7.3.4.1:
    //   HP ≤ 0                  → FailedFainted (recruit is lost)
    //   gauge == 100            → Caught
    //   gauge <  100            → FailedHighHP  (ball consumed, fight on)
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

            return Catchability(wild, effect) >= 100
                ? CatchAttempt.Caught
                : CatchAttempt.FailedHighHP;
        }

        // Per §7.3.4.1 (CL-014) — the deterministic 0–100 Catchability gauge for telegraph + resolution.
        // 0 = full HP / not catchable; 100 = catchable now (READY). Returns 0 on degenerate input.
        public static int Catchability(PokemonInstance wild, CatchConsumableEffectSO effect)
        {
            if (wild == null || effect == null || wild.CurrentHP <= 0) return 0;
            float maxHP = ComputeMaxHP(wild);
            if (maxHP <= 0f) return 0;
            float hpFraction = wild.CurrentHP / maxHP;

            float threshold = effect.CatchThresholdPercent;
            if (wild.PrimaryStatus != StatusCondition.None)
                threshold += effect.StatusCatchBonusPercent; // §7.3.4 — status expands the window
            if (threshold >= 1f) return 100;                 // threshold ≥ full HP → always catchable
            if (threshold < 0f) threshold = 0f;

            float gauge = 100f * (1f - hpFraction) / (1f - threshold);
            int rounded = Mathf.RoundToInt(gauge);
            if (rounded < 0) return 0;
            return rounded > 100 ? 100 : rounded;
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
