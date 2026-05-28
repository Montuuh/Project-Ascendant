using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §7.4.2 + Epic 8 Task 8.2.4 — the result of a defeated trainer fight.
    // Computed by TrainerBattleController on Outcome.Victory using LootRNG.
    //
    // Drop semantics (VS scope):
    //   • TrainerXP        — flat archetype-tier value (Epic 10 consumer);
    //                        defaults to 5 per §7.4.2 if archetype is silent.
    //   • PokeDollars      — TrainerArchetypeSO.BasePokeDollarReward.
    //   • RelicDrops       — 0–1 RelicSO from RelicLootTable (uniform pick).
    //   • ConsumableDrops  — 0–1 ConsumableSO from ConsumableLootTable.
    //
    // The 50/30/20 weighted item/relic/uncommon roll table from §7.4.2 is
    // post-VS — current archetype assets carry flat lists, and the bundle
    // reflects that. Refactor when the weighted roll lands.
    public struct TrainerRewardBundle
    {
        public int TrainerXP;
        public int PokeDollars;
        public List<RelicSO> RelicDrops;
        public List<ConsumableSO> ConsumableDrops;

        public static TrainerRewardBundle Empty => new()
        {
            TrainerXP = 0,
            PokeDollars = 0,
            RelicDrops = new List<RelicSO>(),
            ConsumableDrops = new List<ConsumableSO>(),
        };
    }
}
