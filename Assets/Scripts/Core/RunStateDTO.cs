using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.8 + VS gap #43 — flat, JsonUtility-safe snapshot of RunStateSO. Every nested SO
    // reference is stored as its stable string ID instead of an unstable instanceID, so a save
    // round-trips across sessions and builds. Capture(run) on save; Rebuild(registry) on load.
    // Value-type fields (seed, position, tallies) and plain-serializable types (EventFlags,
    // RecordedInputs) are copied directly — only UnityEngine.Object refs need ID indirection.
    [Serializable]
    public sealed class RunStateDTO
    {
        // Seed & position
        public int RunSeed;
        public int CurrentRegionIndex;
        public int CurrentLayerIndex;
        public int CurrentLaneIndex;
        public int CurrentNodeIndexInLane;

        // Active team
        public List<int> ActiveTeamIndices;
        public int LeadIndex;

        // Inventory (SO refs → stable IDs)
        public List<string> HeldRelicIds;
        public List<string> InventoryIds;
        public int PokeDollars;
        public List<string> EarnedBadgeIds;
        public List<string> OwnedHeldItemIds;
        public List<string> OwnedTMIds;
        public List<string> OwnedEvolutionItemIds;

        // Run tallies
        public int TrainerXPEarnedThisRun;
        public int CombatsClearedThisRun;
        public int EvolutionsThisRun;

        // Active modifiers (SO refs → stable IDs)
        public List<string> ActiveRegionModifierIds;
        public string ActiveBoonId;
        public List<string> ActiveDifficultyModifierIds;

        // Event flags + determinism log (plain serializable; copied directly)
        public List<StringIntPair> EventFlags;
        public InputLog RecordedInputs;

        // Capture a live RunStateSO into a persistable DTO (no registry needed — IDs are read
        // directly off each SO's identity field).
        public static RunStateDTO Capture(RunStateSO run)
        {
            RunStateDTO dto = new();
            if (run == null) return dto;

            dto.RunSeed            = run.RunSeed;
            dto.CurrentRegionIndex = run.CurrentRegionIndex;
            dto.CurrentLayerIndex  = run.CurrentLayerIndex;
            dto.CurrentLaneIndex   = run.CurrentLaneIndex;
            dto.CurrentNodeIndexInLane = run.CurrentNodeIndexInLane;

            dto.ActiveTeamIndices  = run.ActiveTeamIndices != null ? new List<int>(run.ActiveTeamIndices) : null;
            dto.LeadIndex          = run.LeadIndex;

            dto.HeldRelicIds          = Ids(run.HeldRelics,          so => so.Id);
            dto.InventoryIds          = Ids(run.Inventory,           so => so.Id);
            dto.PokeDollars           = run.PokeDollars;
            dto.EarnedBadgeIds        = Ids(run.EarnedBadges,        so => so.BadgeId);
            dto.OwnedHeldItemIds      = Ids(run.OwnedHeldItems,      so => so.Id);
            dto.OwnedTMIds            = Ids(run.OwnedTMs,            so => so.Id);
            dto.OwnedEvolutionItemIds = Ids(run.OwnedEvolutionItems, so => so.Id);

            dto.TrainerXPEarnedThisRun = run.TrainerXPEarnedThisRun;
            dto.CombatsClearedThisRun  = run.CombatsClearedThisRun;
            dto.EvolutionsThisRun      = run.EvolutionsThisRun;

            dto.ActiveRegionModifierIds     = Ids(run.ActiveRegionModifiers,     so => so.ModifierId);
            dto.ActiveBoonId                = run.ActiveBoon != null ? run.ActiveBoon.BoonId : null;
            dto.ActiveDifficultyModifierIds = Ids(run.ActiveDifficultyModifiers, so => so.ModifierId);

            dto.EventFlags     = run.EventFlags != null ? new List<StringIntPair>(run.EventFlags) : null;
            dto.RecordedInputs = run.RecordedInputs;

            return dto;
        }

        // Rebuild a live RunStateSO, resolving every stored ID back to its authored asset via the
        // registry. Unknown/null IDs are dropped (logged by the registry) rather than crashing the
        // load — a missing item is recoverable; a forfeited run is not.
        public RunStateSO Rebuild(RunContentRegistry registry)
        {
            RunStateSO run = ScriptableObject.CreateInstance<RunStateSO>();
            ApplyTo(run, registry);
            return run;
        }

        // Overwrite an EXISTING RunStateSO in place. Used by the resume path so the live SO already
        // registered in Services (and referenced by RunController/Loadout) keeps its identity.
        public void ApplyTo(RunStateSO run, RunContentRegistry registry)
        {
            if (run == null) return;

            run.RunSeed            = RunSeed;
            run.CurrentRegionIndex = CurrentRegionIndex;
            run.CurrentLayerIndex  = CurrentLayerIndex;
            run.CurrentLaneIndex   = CurrentLaneIndex;
            run.CurrentNodeIndexInLane = CurrentNodeIndexInLane;

            run.ActiveTeamIndices  = ActiveTeamIndices != null ? new List<int>(ActiveTeamIndices) : null;
            run.LeadIndex          = LeadIndex;

            run.HeldRelics          = Resolve(HeldRelicIds,          id => registry?.ResolveRelic(id));
            run.Inventory           = Resolve(InventoryIds,          id => registry?.ResolveConsumable(id));
            run.PokeDollars         = PokeDollars;
            run.EarnedBadges        = Resolve(EarnedBadgeIds,        id => registry?.ResolveBadge(id));
            run.OwnedHeldItems      = Resolve(OwnedHeldItemIds,      id => registry?.ResolveHeldItem(id));
            run.OwnedTMs            = Resolve(OwnedTMIds,            id => registry?.ResolveTM(id));
            run.OwnedEvolutionItems = Resolve(OwnedEvolutionItemIds, id => registry?.ResolveEvolutionItem(id));

            run.TrainerXPEarnedThisRun = TrainerXPEarnedThisRun;
            run.CombatsClearedThisRun  = CombatsClearedThisRun;
            run.EvolutionsThisRun      = EvolutionsThisRun;

            run.ActiveRegionModifiers     = Resolve(ActiveRegionModifierIds,     id => registry?.ResolveRegionModifier(id));
            run.ActiveBoon                = registry?.ResolveBoon(ActiveBoonId);
            run.ActiveDifficultyModifiers = Resolve(ActiveDifficultyModifierIds, id => registry?.ResolveDifficultyModifier(id));

            run.EventFlags     = EventFlags != null ? new List<StringIntPair>(EventFlags) : null;
            run.RecordedInputs = RecordedInputs;
        }

        // Map an SO list → ID list, preserving order. Null entries map to null (skipped on rebuild).
        private static List<string> Ids<T>(IReadOnlyList<T> src, Func<T, string> idOf) where T : UnityEngine.Object
        {
            if (src == null) return null;
            List<string> ids = new(src.Count);
            for (int i = 0; i < src.Count; i++)
                ids.Add(src[i] != null ? idOf(src[i]) : null);
            return ids;
        }

        // Map an ID list → SO list, dropping nulls/unknowns. Preserves order of resolved entries.
        private static List<T> Resolve<T>(List<string> ids, Func<string, T> resolver) where T : UnityEngine.Object
        {
            if (ids == null) return null;
            List<T> list = new(ids.Count);
            for (int i = 0; i < ids.Count; i++)
            {
                T so = resolver(ids[i]);
                if (so != null) list.Add(so);
            }
            return list;
        }
    }
}
