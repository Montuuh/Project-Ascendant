using System.Collections.Generic;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Map
{
    // Per §7.12 + Epic 9 — applies a TrainerRewardBundle into the run state. Shared by the
    // Trainer (9.4), Elite (9.4), and Gym (9.8) node controllers so reward plumbing lives once.
    //
    // Per §6.3.2 / Task 11.4 — TrainerXP (meta) is ACCRUED here into RunStateSO.TrainerXPEarnedThisRun
    // each cleared combat, and COMMITTED to MetaProgressionSO at run-end (RunEndService, §6.3.4).
    // ⚠ Per-encounter bundle.TrainerXP values trace to §7.12 seeding; §6.3.2 lists a slightly different
    // flat table (Elite 5 vs §7.12 25). Using the node-authored bundle value; flagged for systems-designer.
    public static class RewardApplier
    {
        public static void Apply(RunStateSO run, TrainerRewardBundle bundle, EconomyConfigSO economy = null)
        {
            if (run == null) return;

            // §8.3.3 Coin Pouch — all ₽ drops ×multiplier.
            int pokeDollars = bundle.PokeDollars;
            if (economy != null && ProjectAscendant.Combat.RelicResolver.Holds(run.HeldRelics, "coin_pouch"))
                pokeDollars = UnityEngine.Mathf.FloorToInt(pokeDollars * economy.CoinPouchPokeDollarMultiplier);
            run.PokeDollars += pokeDollars;
            run.TrainerXPEarnedThisRun += bundle.TrainerXP; // §6.3.2 — meta XP accrual (committed at run-end)
            run.CombatsClearedThisRun += 1;                 // §2.1.7 — run-summary tally

            if (bundle.RelicDrops != null && bundle.RelicDrops.Count > 0)
                (run.HeldRelics ??= new List<RelicSO>()).AddRange(bundle.RelicDrops);

            if (bundle.ConsumableDrops != null && bundle.ConsumableDrops.Count > 0)
                (run.Inventory ??= new List<ConsumableSO>()).AddRange(bundle.ConsumableDrops);

            // Per §4.4.5 — Gym Badges become run-wide active modifiers (§4.4.5.1 Boulder Badge etc.).
            if (bundle.BadgeAwards != null && bundle.BadgeAwards.Count > 0)
                (run.EarnedBadges ??= new List<BadgeSO>()).AddRange(bundle.BadgeAwards);
        }
    }
}
