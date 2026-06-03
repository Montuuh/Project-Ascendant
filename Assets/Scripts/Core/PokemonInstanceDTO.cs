using System;
using System.Collections.Generic;

namespace ProjectAscendant.Core
{
    // Per §9.8 + VS gap #43 — JsonUtility-safe snapshot of a live PokemonInstance (the run team/Box).
    // Every SO reference is stored as its stable ID and re-resolved via RunContentRegistry on load.
    // Captures the durable between-node state only; purely combat-scoped fields (MoveCooldowns) and
    // boss-encounter runtime (PhaseCount/Sturdy/MidFight*) are inert outside combat and rebuilt fresh
    // by the encounter controllers, so they are intentionally not persisted.
    [Serializable]
    public sealed class PokemonInstanceDTO
    {
        public string SpeciesId;
        public int Level;
        public int CurrentHP;
        public int CurrentXP;
        public int TraumaStacks;

        public List<string> CurrentMoveIds;
        public string MasteryMoveId;
        public List<string> LearnedMoveIds;

        public string AbilityId;
        public string HeldItemId;

        public List<StatStageEntry> StatStages;

        public StatusCondition PrimaryStatus;
        public StatusCondition SecondaryStatus;
        public int PrimaryStatusTurnsRemaining;
        public int SecondaryStatusTurnsRemaining;

        public EvolutionStage CurrentStage;
        public string SelectedBranchId;

        // ── Capture ──────────────────────────────────────────────────────────────────

        public static PokemonInstanceDTO Capture(PokemonInstance inst)
        {
            PokemonInstanceDTO dto = new();
            if (inst == null) return dto;

            dto.SpeciesId    = inst.Species != null ? inst.Species.SpeciesId : null;
            dto.Level        = inst.Level;
            dto.CurrentHP    = inst.CurrentHP;
            dto.CurrentXP    = inst.CurrentXP;
            dto.TraumaStacks = inst.TraumaStacks;

            dto.CurrentMoveIds = MoveIds(inst.CurrentMoves);
            dto.MasteryMoveId  = inst.MasteryMove != null ? inst.MasteryMove.MoveId : null;
            dto.LearnedMoveIds = MoveIds(inst.LearnedMoves);

            dto.AbilityId  = inst.Ability != null ? inst.Ability.AbilityId : null;
            dto.HeldItemId = inst.HeldItem != null ? inst.HeldItem.Id : null;

            dto.StatStages = CaptureStages(inst.StatStages);

            dto.PrimaryStatus                 = inst.PrimaryStatus;
            dto.SecondaryStatus               = inst.SecondaryStatus;
            dto.PrimaryStatusTurnsRemaining   = inst.PrimaryStatusTurnsRemaining;
            dto.SecondaryStatusTurnsRemaining = inst.SecondaryStatusTurnsRemaining;

            dto.CurrentStage     = inst.CurrentStage;
            dto.SelectedBranchId = inst.SelectedBranch != null ? inst.SelectedBranch.BranchId : null;

            return dto;
        }

        public static List<PokemonInstanceDTO> CaptureBox(IReadOnlyList<PokemonInstance> box)
        {
            if (box == null) return null;
            List<PokemonInstanceDTO> list = new(box.Count);
            for (int i = 0; i < box.Count; i++)
                list.Add(Capture(box[i]));
            return list;
        }

        // ── Rebuild ──────────────────────────────────────────────────────────────────

        // Rebuilds a live PokemonInstance via the factory (pooled) with every field restored and SO
        // references resolved. Returns null if the species cannot be resolved — a team member with no
        // species is unusable, so it is dropped (logged by the registry) rather than half-built.
        public PokemonInstance Rebuild(RunContentRegistry registry, PokemonInstanceFactory factory)
        {
            EvolutionBranchSO branch = registry?.ResolveBranch(SelectedBranchId);

            // Branch-first species resolution: EvolutionExecutor sets Species = SelectedBranch.Evolved-
            // Species, so an evolved member's current species IS its branch's evolved form. This
            // disambiguates final-form sub-branch builds (e.g. Blastoise A1/A2) that intentionally
            // share a SpeciesId. Un-evolved members (no branch) fall back to the unique SpeciesId.
            PokemonSpeciesSO species = branch != null && branch.EvolvedSpecies != null
                ? branch.EvolvedSpecies
                : registry?.ResolveSpecies(SpeciesId);
            if (species == null) return null;

            PokemonInstance inst = factory != null ? factory.RentEmpty() : new PokemonInstance();
            inst.Species      = species;
            inst.Level        = Level;
            inst.CurrentHP    = CurrentHP;
            inst.CurrentXP    = CurrentXP;
            inst.TraumaStacks = TraumaStacks;

            AddMoves(inst.CurrentMoves, CurrentMoveIds, registry);
            inst.MasteryMove = registry?.ResolveMove(MasteryMoveId);
            AddMoves(inst.LearnedMoves, LearnedMoveIds, registry);

            inst.Ability  = registry?.ResolveAbility(AbilityId);
            inst.HeldItem = registry?.ResolveHeldItem(HeldItemId);

            inst.StatStages.Clear();
            if (StatStages != null)
                for (int i = 0; i < StatStages.Count; i++)
                    inst.StatStages[StatStages[i].Stat] = StatStages[i].Stages;

            inst.PrimaryStatus                 = PrimaryStatus;
            inst.SecondaryStatus               = SecondaryStatus;
            inst.PrimaryStatusTurnsRemaining   = PrimaryStatusTurnsRemaining;
            inst.SecondaryStatusTurnsRemaining = SecondaryStatusTurnsRemaining;

            inst.CurrentStage   = CurrentStage;
            inst.SelectedBranch = branch;

            return inst;
        }

        // Rebuilds a Box's worth of instances; null/unresolvable members are dropped.
        public static List<PokemonInstance> RebuildBox(
            List<PokemonInstanceDTO> dtos, RunContentRegistry registry, PokemonInstanceFactory factory)
        {
            if (dtos == null) return null;
            List<PokemonInstance> list = new(dtos.Count);
            for (int i = 0; i < dtos.Count; i++)
            {
                PokemonInstance inst = dtos[i]?.Rebuild(registry, factory);
                if (inst != null) list.Add(inst);
            }
            return list;
        }

        // ── Helpers ──────────────────────────────────────────────────────────────────

        private static List<string> MoveIds(IReadOnlyList<MoveSO> moves)
        {
            if (moves == null) return null;
            List<string> ids = new(moves.Count);
            for (int i = 0; i < moves.Count; i++)
                ids.Add(moves[i] != null ? moves[i].MoveId : null);
            return ids;
        }

        private static void AddMoves(List<MoveSO> target, List<string> ids, RunContentRegistry registry)
        {
            target.Clear();
            if (ids == null) return;
            for (int i = 0; i < ids.Count; i++)
            {
                MoveSO move = registry?.ResolveMove(ids[i]);
                if (move != null) target.Add(move);
            }
        }

        private static List<StatStageEntry> CaptureStages(IReadOnlyDictionary<Stat, int> stages)
        {
            if (stages == null || stages.Count == 0) return null;
            List<StatStageEntry> list = new(stages.Count);
            foreach (KeyValuePair<Stat, int> kv in stages)
                list.Add(new StatStageEntry { Stat = kv.Key, Stages = kv.Value });
            return list;
        }
    }

    // Per gap #43 — serializable representation of one PokemonInstance.StatStages entry
    // (JsonUtility cannot serialize Dictionary<Stat,int> directly).
    [Serializable]
    public struct StatStageEntry
    {
        public Stat Stat;
        public int Stages;
    }
}
