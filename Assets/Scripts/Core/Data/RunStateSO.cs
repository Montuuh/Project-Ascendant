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

        [Header("Active Modifiers")]
        public List<RegionModifierSO> ActiveRegionModifiers;
        public LeagueBoonSO ActiveBoon;

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
