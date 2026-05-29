using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §4.4.3 + Epic 8 Task 8.4 — derives a boss/Elite Pokémon's current
    // phase from its live HP fraction. Stateless by design: there is no stored
    // "current phase" on the instance — phase is a pure function of CurrentHP
    // and PhaseCount, recomputed every IntentPhase. This keeps phase logic
    // deterministic and replay-safe (Engineering Pillar 3) and means a heal
    // back above a threshold correctly returns the boss to the earlier phase.
    //
    // Phase model (§4.4.3 standard templates):
    //   • PhaseCount <= 1  → always Phase 1 (ordinary wild/trainer Pokémon).
    //   • PhaseCount == 2  → Phase 1 (HP > P2) / Phase 2 (HP <= P2).  (Elite, §7.5.1)
    //   • PhaseCount >= 3  → adds Phase 3 (HP <= P3).  (Gym ace, Task 8.5)
    //
    // Thresholds live on BattleConfigSO (BossPhase2HPThreshold = 0.5,
    // BossPhase3HPThreshold = 0.2) — universal across all boss-tier Pokémon.
    //
    // Phase BEHAVIOUR wired in Task 8.4 is the Phase-2 aggression bias only
    // (IntentScorer.Context.PhaseAggressive). Phase-3 last-stand (cooldown
    // reset, no-cooldown signature, Sturdy) and mid-fight evolution are
    // deferred to Task 8.5, which builds on this same tracker.
    public static class BossPhaseTracker
    {
        // Returns 1, 2, or 3. Defensive: 1 for null inputs or single-phase
        // Pokémon, so callers can invoke this unconditionally on any enemy.
        public static int CurrentPhase(PokemonInstance enemy, BattleConfigSO config)
        {
            if (enemy == null || config == null) return 1;
            int maxPhases = enemy.PhaseCount <= 1 ? 1 : enemy.PhaseCount;
            if (maxPhases <= 1) return 1;

            float hp = HPFraction(enemy);
            if (maxPhases >= 3 && hp <= config.BossPhase3HPThreshold) return 3;
            if (hp <= config.BossPhase2HPThreshold) return 2;
            return 1;
        }

        // Per §4.4.3 — Phase 2 and Phase 3 are both "aggressive"; Phase 1 is the
        // setup/standard phase. Convenience predicate for the intent scorer.
        public static bool IsAggressivePhase(PokemonInstance enemy, BattleConfigSO config)
            => CurrentPhase(enemy, config) >= 2;

        // Mirrors IntentScorer.HPFraction / StatusEffectManager.ComputeDoTDamage:
        // MaxHP = Species.BaseStats.BaseHP + GrowthCurve.GetHPAt(Level). Kept
        // local to avoid a tested-file change; see the shared-helper TODO.
        // TODO: centralise HP-fraction math (4 call sites) in a CombatMath util.
        private static float HPFraction(PokemonInstance p)
        {
            if (p == null || p.Species == null) return 1f;
            int max = p.Species.BaseStats.BaseHP;
            if (p.Species.GrowthCurve != null)
                max += p.Species.GrowthCurve.GetHPAt(p.Level);
            if (max <= 0) return 1f;
            float frac = (float)p.CurrentHP / max;
            if (frac < 0f) return 0f;
            if (frac > 1f) return 1f;
            return frac;
        }
    }
}
