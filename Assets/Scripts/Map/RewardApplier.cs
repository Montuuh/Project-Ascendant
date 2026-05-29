using System.Collections.Generic;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Map
{
    // Per §7.12 + Epic 9 — applies a TrainerRewardBundle into the run state. Shared by the
    // Trainer (9.4), Elite (9.4), and Gym (9.8) node controllers so reward plumbing lives once.
    //
    // TrainerXP is intentionally NOT applied here: it is meta-progression, credited at run end
    // by Epic 10/11 (RunStateSO has no per-run XP field). The bundle still carries it for that
    // downstream consumer.
    public static class RewardApplier
    {
        public static void Apply(RunStateSO run, TrainerRewardBundle bundle)
        {
            if (run == null) return;

            run.PokeDollars += bundle.PokeDollars;

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
