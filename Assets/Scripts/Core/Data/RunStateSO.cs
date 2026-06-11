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
        // Per gap #43 — lane + index-in-lane of the current node, paired with CurrentLayerIndex so a
        // resumed run lands on the EXACT node (the map itself is regenerated deterministically from
        // RunSeed). A (layer, lane) can hold more than one node, so IndexInLane disambiguates.
        public int CurrentLaneIndex;
        public int CurrentNodeIndexInLane;

        [Header("Active Team")]
        // Per §9.3.2.4 — indices into Box (managed by PokemonInstanceFactory at runtime).
        public List<int> ActiveTeamIndices;

        // Per §3.3.1 — 0-2 index into ActiveTeamIndices.
        public int LeadIndex;

        [Header("Inventory")]
        public List<RelicSO> HeldRelics;
        public List<ConsumableSO> Inventory;
        public int PokeDollars;
        // Per §7.3.4 (Option 1 scarcity) — counted Pokéball resource: starting stock + per-region grant
        // + shop purchases, decremented on each catch attempt. Gates whether the catch card appears.
        public int PokeballCount;
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
        // Per §7.8.3.1 (CL-016) — Region Modifiers are per-Region, non-accumulating: this list holds
        // 0..1 entries (the current Region's pick). Kept as a list for save-DTO compatibility; use
        // SetRegionModifier to enforce single-active.
        public List<RegionModifierSO> ActiveRegionModifiers;
        // Per §7.3.1 + CL-018 (Q21) — the biome Naturalist's Lens steers Wild-Area weighting toward this
        // Region (player-chosen; null → the sampler auto-surfaces the top non-primary eligible biome).
        // Transient/per-Region: cleared whenever the active modifier changes (SetRegionModifier) and at
        // run reset. (Save round-trip is a follow-up — biomes aren't in the ID registry yet.)
        public BiomeSO NaturalistLensBiome;
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

        // Per §9.8.6 (gap #45) — the 5 RNG stream cursors, snapshotted from RNGStreams before each
        // node-entry autosave so a resume continues each stream where it left off (encounters/loot/
        // mystery/combat don't re-roll). On resume only the 4 content cursors are restored; MapRNG
        // re-derives the map by replay (§9.8.6). Absent in pre-CL-022 saves → defaults to all-zero,
        // i.e. today's "re-roll from seed" behaviour (backward-compatible).
        public RNGCursors RngCursors;

        // Per gap #43 — reset this SO to a fresh run with the given seed, in place (preserving the
        // instance identity so live references — LoadoutManager, Services registration — stay valid).
        // Used by "New Run" from the Main Menu. Position, inventory, team, modifiers, and tallies all
        // clear; only the new seed remains.
        public void ResetToNewRun(int seed)
        {
            RunSeed = seed;
            CurrentRegionIndex = 0;
            CurrentLayerIndex = 0;
            CurrentLaneIndex = 0;
            CurrentNodeIndexInLane = 0;

            ActiveTeamIndices = null;
            LeadIndex = 0;

            HeldRelics = null;
            Inventory = null;
            PokeDollars = 0;
            PokeballCount = 0;
            EarnedBadges = null;
            OwnedHeldItems = null;
            OwnedTMs = null;
            OwnedEvolutionItems = null;

            TrainerXPEarnedThisRun = 0;
            CombatsClearedThisRun = 0;
            EvolutionsThisRun = 0;

            ActiveRegionModifiers = null;
            NaturalistLensBiome = null;
            ActiveBoon = null;
            ActiveDifficultyModifiers = null;

            EventFlags = null;
            RecordedInputs = null;
            RngCursors = default; // §9.8.6 — fresh run: streams re-seed from RunSeed, no saved cursors
        }

        // Per §7.8.3.1 (CL-016) — the single active Region Modifier (per-Region), or null.
        public RegionModifierSO ActiveRegionModifier =>
            ActiveRegionModifiers != null && ActiveRegionModifiers.Count > 0 ? ActiveRegionModifiers[0] : null;

        // Per §7.8.3.1 (CL-016) — set the current Region's modifier, replacing any previous (single-
        // active, non-accumulating). Pass null to clear. Called by the Region-start pick (run setup
        // for R1, City Reflection for R2/R3).
        public void SetRegionModifier(RegionModifierSO modifier)
        {
            if (ActiveRegionModifiers == null) ActiveRegionModifiers = new List<RegionModifierSO>();
            ActiveRegionModifiers.Clear();
            if (modifier != null) ActiveRegionModifiers.Add(modifier);
            // Per CL-018 (Q21) — the steered biome is per-Region; clear it when the modifier changes.
            // The pick flow re-sets NaturalistLensBiome afterward if the new pick is Naturalist's Lens.
            NaturalistLensBiome = null;
        }
    }
}
