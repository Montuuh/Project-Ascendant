using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.3.2.4 — runtime run state. Serialized to run-current.dat after every node (§9.8.1).
    // Mutated heavily at runtime. Created at run-start; loaded from save on continue.
    //
    // Box (live PokemonInstances): NOT stored here. PokemonInstanceFactory owns the pool;
    // SaveSystem serializes species-by-id + per-instance data separately. Epic 9 wires this.
    [CreateAssetMenu(menuName = "Project Ascendant/Runtime/Run State")]
    public class RunStateSO : ScriptableObject
    {
        [Header("Seed & Position")]
        // Per §9.7 — run seed for all 5 RNG streams.
        public int RunSeed;
        public int CurrentRegionIndex;
        public int CurrentLayerIndex;

        [Header("Active Team")]
        // Per §9.3.2.4 — indices into Box (managed by PokemonInstanceFactory at runtime).
        public List<int> ActiveTeamIndices;

        // Per §3.3.1 — 0-2 index into ActiveTeamIndices.
        public int LeadIndex;

        [Header("Inventory")]
        public List<RelicSO> HeldRelics;
        public List<ConsumableSO> Inventory;
        public int PokeDollars;
        public List<BadgeSO> EarnedBadges;

        // Per §7.7.1 + Epic 9 Task 9.6 — Region Shop "special slot" purchases. Held Items are
        // equipped to Pokémon from Map View (§8.4, Loadout/Epic 13); TMs are applied once the
        // §5.10 move pool lands (gap #36). Owned + saved here in the meantime.
        public List<HeldItemSO> OwnedHeldItems;
        public List<TMSO> OwnedTMs;
        // Per §5.3.2 / Task 10.5 — Evolution Items that unlock item-gated branches. None ship in the VS.
        public List<EvolutionItemSO> OwnedEvolutionItems;

        // Per §6.3.2 / Task 11.3 — Trainer (meta) XP accrued THIS run; committed to MetaProgressionSO at
        // run-end (§6.3.4), then discarded with the run. Distinct from per-Pokémon in-run XP (§5.2).
        public int TrainerXPEarnedThisRun;
        // Per §2.1.7 / Task 11.4 — run-summary tallies (combats cleared, evolutions triggered this run).
        public int CombatsClearedThisRun;
        public int EvolutionsThisRun;

        [Header("Active Modifiers")]
        public List<RegionModifierSO> ActiveRegionModifiers;
        public LeagueBoonSO ActiveBoon;
        // Per §6.8 / Task 11.6 — difficulty modifiers chosen at run start (VS: 0-1; Hub upgrade raises cap).
        public List<DifficultyModifierSO> ActiveDifficultyModifiers;

        [Header("Event Flags")]
        // Per §9.3.2.4 — key/value flags for event tracking.
        // Unity cannot serialize Dictionary<string,int>; StringIntPair list used instead.
        public List<StringIntPair> EventFlags;

        [Header("Determinism")]
        // Per §9.7.4 — recorded input log for deterministic replay.
        // Populated by InputLogRecorder during the run.
        public InputLog RecordedInputs;
    }
}
