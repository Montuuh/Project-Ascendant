using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.8 + VS gap #43 — resolves the stable content IDs stored in RunStateDTO back to the
    // authored ScriptableObject assets on load. JsonUtility serializes nested SO references as
    // unstable instanceIDs (broken across sessions/builds); the DTO persists IDs instead and this
    // registry rebuilds the object graph. Built from RunContentCatalogSO (the single authored
    // content source for a run, §9.2), including content reachable transitively (the Gym Leader's
    // Badge reward). Pools the catalog does not own (difficulty modifiers chosen at the Hub) are
    // added explicitly via the Register* methods before LoadRun.
    public sealed class RunContentRegistry
    {
        private readonly Dictionary<string, RelicSO> _relics = new();
        private readonly Dictionary<string, ConsumableSO> _consumables = new();
        private readonly Dictionary<string, HeldItemSO> _heldItems = new();
        private readonly Dictionary<string, TMSO> _tms = new();
        private readonly Dictionary<string, EvolutionItemSO> _evolutionItems = new();
        private readonly Dictionary<string, BadgeSO> _badges = new();
        private readonly Dictionary<string, DifficultyModifierSO> _difficultyModifiers = new();
        private readonly Dictionary<string, RegionModifierSO> _regionModifiers = new();
        private readonly Dictionary<string, LeagueBoonSO> _boons = new();

        // Team content (PokemonInstance refs, gap #43 team persistence) — populated transitively
        // from the catalog's species roots (starters + wild biome pools) via RegisterSpeciesGraph.
        private readonly Dictionary<string, PokemonSpeciesSO> _species = new();
        private readonly Dictionary<string, MoveSO> _moves = new();
        private readonly Dictionary<string, AbilitySO> _abilities = new();
        private readonly Dictionary<string, EvolutionBranchSO> _branches = new();

        // Build a registry from the authored run catalog. Includes content reachable transitively
        // (the Gym Leader's Badge reward). Pools the catalog does not own — difficulty modifiers
        // (chosen at the Hub) and any region modifiers / boons — must be added via Register*.
        public static RunContentRegistry FromCatalog(RunContentCatalogSO catalog)
        {
            RunContentRegistry reg = new();
            if (catalog == null) return reg;

            reg.RegisterRelics(catalog.Relics);
            reg.RegisterConsumables(catalog.Consumables);
            reg.RegisterConsumable(catalog.Pokeball);
            reg.RegisterConsumable(catalog.Potion);
            reg.RegisterHeldItems(catalog.HeldItems);
            reg.RegisterTMs(catalog.TMs);
            if (catalog.Gym != null) reg.RegisterBadge(catalog.Gym.BadgeReward);

            // Team content (gap #43): every species a run can hold reaches transitively from the
            // starter pool and the wild biome species pools — walk both for moves/abilities/branches.
            reg.RegisterSpeciesGraphs(catalog.Starters);
            if (catalog.WildConfig != null && catalog.WildConfig.RegionBiomes != null)
                foreach (BiomeWeight bw in catalog.WildConfig.RegionBiomes)
                    if (bw.Biome != null) reg.RegisterSpeciesGraphs(bw.Biome.SpeciesPool);

            return reg;
        }

        // Transitively register a species and everything reachable from it: learnsets, ability,
        // mastery move, and each evolution branch (granted ability, new/upgrade moves, sub-branches,
        // evolved species). Cycle-safe via the species/branch dictionaries acting as the visited set.
        public void RegisterSpeciesGraph(PokemonSpeciesSO species)
        {
            if (species == null || string.IsNullOrEmpty(species.SpeciesId)) return;
            if (_species.ContainsKey(species.SpeciesId)) return; // already walked
            _species[species.SpeciesId] = species;

            RegisterMoves(species.BaseLearnset);
            RegisterMoves(species.TutorLearnset);
            RegisterTMs(species.TMCompatibility);
            // Per §5.12.3 (CL-008) — abilities are in the Dojo learner pool, not PrimaryAbility.
            RegisterAbilities(species.AvailableAbilities);
            RegisterAbility(species.PrimaryAbility); // legacy field; null on VS species
            RegisterMove(species.MasteryMove);

            if (species.Branches != null)
                foreach (EvolutionBranchSO branch in species.Branches)
                    RegisterBranchGraph(branch);
        }

        public void RegisterSpeciesGraphs(IEnumerable<PokemonSpeciesSO> list)
        {
            if (list == null) return;
            foreach (PokemonSpeciesSO s in list) RegisterSpeciesGraph(s);
        }

        private void RegisterBranchGraph(EvolutionBranchSO branch)
        {
            if (branch == null || string.IsNullOrEmpty(branch.BranchId)) return;
            if (_branches.ContainsKey(branch.BranchId)) return; // already walked
            _branches[branch.BranchId] = branch;

            RegisterAbility(branch.GrantedAbility);
            RegisterMoves(branch.NewMoves);
            if (branch.MoveUpgrades != null)
                foreach (MoveUpgradePair pair in branch.MoveUpgrades)
                {
                    RegisterMove(pair.OldMove);
                    RegisterMove(pair.NewMove);
                }
            if (branch.SubBranches != null)
                foreach (EvolutionBranchSO sub in branch.SubBranches)
                    RegisterBranchGraph(sub);

            RegisterSpeciesGraph(branch.EvolvedSpecies); // recurse into the evolved form
        }

        public void RegisterSpecies(PokemonSpeciesSO so)  { Add(_species, so, so != null ? so.SpeciesId : null); }
        public void RegisterMove(MoveSO so)               { Add(_moves, so, so != null ? so.MoveId : null); }
        public void RegisterAbility(AbilitySO so)         { Add(_abilities, so, so != null ? so.AbilityId : null); }
        public void RegisterBranch(EvolutionBranchSO so)  { Add(_branches, so, so != null ? so.BranchId : null); }

        public void RegisterMoves(IEnumerable<MoveSO> list)        { AddRange(list, RegisterMove); }
        public void RegisterAbilities(IEnumerable<AbilitySO> list) { AddRange(list, RegisterAbility); }

        // ── Registration (null-safe; entries with empty IDs are skipped) ─────────────

        public void RegisterRelic(RelicSO so)               { Add(_relics, so, so != null ? so.Id : null); }
        public void RegisterConsumable(ConsumableSO so)     { Add(_consumables, so, so != null ? so.Id : null); }
        public void RegisterHeldItem(HeldItemSO so)         { Add(_heldItems, so, so != null ? so.Id : null); }
        public void RegisterTM(TMSO so)                     { Add(_tms, so, so != null ? so.Id : null); }
        public void RegisterEvolutionItem(EvolutionItemSO so) { Add(_evolutionItems, so, so != null ? so.Id : null); }
        public void RegisterBadge(BadgeSO so)               { Add(_badges, so, so != null ? so.BadgeId : null); }
        public void RegisterDifficultyModifier(DifficultyModifierSO so) { Add(_difficultyModifiers, so, so != null ? so.ModifierId : null); }
        public void RegisterRegionModifier(RegionModifierSO so) { Add(_regionModifiers, so, so != null ? so.ModifierId : null); }
        public void RegisterBoon(LeagueBoonSO so)           { Add(_boons, so, so != null ? so.BoonId : null); }

        public void RegisterRelics(IEnumerable<RelicSO> list)               { AddRange(list, RegisterRelic); }
        public void RegisterConsumables(IEnumerable<ConsumableSO> list)     { AddRange(list, RegisterConsumable); }
        public void RegisterHeldItems(IEnumerable<HeldItemSO> list)         { AddRange(list, RegisterHeldItem); }
        public void RegisterTMs(IEnumerable<TMSO> list)                     { AddRange(list, RegisterTM); }
        public void RegisterEvolutionItems(IEnumerable<EvolutionItemSO> list) { AddRange(list, RegisterEvolutionItem); }
        public void RegisterBadges(IEnumerable<BadgeSO> list)               { AddRange(list, RegisterBadge); }
        public void RegisterDifficultyModifiers(IEnumerable<DifficultyModifierSO> list) { AddRange(list, RegisterDifficultyModifier); }
        public void RegisterRegionModifiers(IEnumerable<RegionModifierSO> list) { AddRange(list, RegisterRegionModifier); }
        public void RegisterBoons(IEnumerable<LeagueBoonSO> list)           { AddRange(list, RegisterBoon); }

        // ── Resolution (null/unknown IDs return null and log a warning) ──────────────

        public RelicSO ResolveRelic(string id)                       => Resolve(_relics, id, "Relic");
        public ConsumableSO ResolveConsumable(string id)             => Resolve(_consumables, id, "Consumable");
        public HeldItemSO ResolveHeldItem(string id)                 => Resolve(_heldItems, id, "HeldItem");
        public TMSO ResolveTM(string id)                             => Resolve(_tms, id, "TM");
        public EvolutionItemSO ResolveEvolutionItem(string id)       => Resolve(_evolutionItems, id, "EvolutionItem");
        public BadgeSO ResolveBadge(string id)                       => Resolve(_badges, id, "Badge");
        public DifficultyModifierSO ResolveDifficultyModifier(string id) => Resolve(_difficultyModifiers, id, "DifficultyModifier");
        public RegionModifierSO ResolveRegionModifier(string id)     => Resolve(_regionModifiers, id, "RegionModifier");
        public LeagueBoonSO ResolveBoon(string id)                   => Resolve(_boons, id, "Boon");
        public PokemonSpeciesSO ResolveSpecies(string id)            => Resolve(_species, id, "Species");
        public MoveSO ResolveMove(string id)                         => Resolve(_moves, id, "Move");
        public AbilitySO ResolveAbility(string id)                   => Resolve(_abilities, id, "Ability");
        public EvolutionBranchSO ResolveBranch(string id)            => Resolve(_branches, id, "EvolutionBranch");

        // Per §6.9 — every species reachable from the catalog roots (starters + wild biomes + all
        // evolution stages). Source of truth for the Pokédex roster + completion-% denominator.
        public IReadOnlyCollection<PokemonSpeciesSO> AllSpecies => _species.Values;

        // ── Internals ────────────────────────────────────────────────────────────────

        private static void Add<T>(Dictionary<string, T> map, T so, string id) where T : ScriptableObject
        {
            if (so == null || string.IsNullOrEmpty(id)) return;
            map[id] = so; // last-write-wins; authored IDs are expected unique
        }

        private static void AddRange<T>(IEnumerable<T> list, System.Action<T> add)
        {
            if (list == null) return;
            foreach (T so in list) add(so);
        }

        private static T Resolve<T>(Dictionary<string, T> map, string id, string label) where T : ScriptableObject
        {
            if (string.IsNullOrEmpty(id)) return null;
            if (map.TryGetValue(id, out T so)) return so;
            Debug.LogWarning($"[RunContentRegistry] Unknown {label} id '{id}' — not in catalog; skipped on load.");
            return null;
        }
    }
}
