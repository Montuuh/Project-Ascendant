using System.Collections.Generic;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Map
{
    // Per §9.5 + Epic 9 run-flow integration — the dependency bundle for a single run. Holds the
    // run's mutable state (RunState, Box, Loadout) plus the authored content + configs the node
    // controllers need, and wires every NodeType into a NodeControllerFactory. Constructed by the
    // run-setup layer (runtime: Bootstrap from Addressables-loaded content; tests: in-memory).
    public sealed class RunContext
    {
        // ── Runtime state ────────────────────────────────────────────────────
        public RunStateSO Run;
        public Box Box;
        public LoadoutManager Loadout;
        public PokemonInstanceFactory PokemonFactory;
        public RNGStreams Streams;

        // Per §7.2 v2 — the generated map (set by RunController after generation). Needed to resolve gyms.
        public RegionMap CurrentMap;

        // ── Global config ────────────────────────────────────────────────────
        public EconomyConfigSO Economy;
        public MapGenerationConfigSO MapConfig;
        public BattleConfigSO BattleConfig; // combat tuning (used when launching real fights)
        public ProgressionConfigSO ProgressionConfig; // §5.2 — per-Pokémon XP + leveling
        public MetaProgressionSO Meta;             // §6.10 — persistent cross-run state (run-end commit target)
        public MetaProgressionConfigSO MetaConfig; // §6.3 — Trainer XP/Level/Token tuning
        public PokedexProgressSO Pokedex;        // §6.9 — per-species kill counts + tiers (Task 11.8)
        public int PokedexTotalSpecies;            // §6.9 — total roster size (completion-% denominator)
        public IReadOnlyList<DifficultyModifierSO> DifficultyChoices; // §6.8 — selectable at the Hub (Task 11.6)

        // ── Wild (9.3) ───────────────────────────────────────────────────────
        public WildEncounterConfigSO WildConfig;
        public ConsumableSO Pokeball;
        public IBoxOverflowHandler BoxOverflow;

        // ── Trainer / Elite (9.4) ────────────────────────────────────────────
        public IReadOnlyList<TrainerArchetypeSO> ArchetypePool;
        public EliteTrainerSO EliteSO;

        // ── Shop (9.6) ───────────────────────────────────────────────────────
        public RegionShopConfigSO ShopConfig;
        public RegionShopNodeController.ShopItemPools ShopPools;

        // ── Mystery (9.7) ────────────────────────────────────────────────────
        public IReadOnlyList<MysteryEventSO> MysteryPool;
        public MysteryConfigSO MysteryConfig;
        public MysteryEventNodeController.MysteryItemRefs MysteryItems;

        // ── Gym (9.8) ────────────────────────────────────────────────────────
        [System.Obsolete("Use GymPool for v2 multi-gym support. Kept for backward compat.")]
        public GymLeaderSO GymSO;

        // Per §7.2 v2 — gym pool for 2-gym fork. Resolved per-node via Map.ChosenGyms.
        public IReadOnlyList<GymLeaderSO> GymPool;

        // Per §7.2 v2 — resolve the gym for a given node from the generated map's chosen gyms.
        // Falls back to the single GymSO if GymPool is insufficient or node has no valid gym index.
        private GymLeaderSO ResolveGym(MapNode node)
        {
            if (CurrentMap != null && CurrentMap.ChosenGyms != null && node.GymIndex >= 0 && node.GymIndex < CurrentMap.ChosenGyms.Count)
                return CurrentMap.ChosenGyms[node.GymIndex];

            // Fallback: single gym (backward compat).
#pragma warning disable CS0618 // Type or member is obsolete
            if (GymSO != null) return GymSO;
#pragma warning restore CS0618

            // Last fallback: GymPool[0] if available.
            if (GymPool != null && GymPool.Count > 0) return GymPool[0];

            return null; // Caller must handle null gym.
        }

        // Registers a builder for every NodeType. Each closure captures the deps this context owns,
        // so the HSM/RunController can resolve a node without a hardcoded switch (§9.6).
        public void RegisterBuilders(NodeControllerFactory factory)
        {
            factory.Register(NodeType.Wild, (node, run) => new WildAreaNodeController(
                node, run, WildConfig, PokemonFactory, Pokeball, Streams.EncounterRNG, Box, BoxOverflow));

            factory.Register(NodeType.Trainer, (node, run) => new TrainerBattleNodeController(
                node, run, ArchetypePool, PokemonFactory, Streams.LootRNG, Streams.LootRNG, Economy));

            factory.Register(NodeType.Elite, (node, run) => new EliteNodeController(
                node, run, EliteSO, PokemonFactory, Economy));

            factory.Register(NodeType.Center, (node, run) => new PokemonCenterNodeController(
                node, run, Box, Economy));

            factory.Register(NodeType.Shop, (node, run) => new RegionShopNodeController(
                node, run, ShopConfig, Streams.LootRNG, ShopPools));

            factory.Register(NodeType.Mystery, (node, run) => new MysteryEventNodeController(
                node, run, MysteryPool, MysteryConfig, Streams.MysteryRNG, MysteryItems, Box, Economy));

            factory.Register(NodeType.Gym, (node, run) => {
                // Per §7.2 v2 — resolve gym from the map's chosen gyms via node.GymIndex.
                GymLeaderSO gym = ResolveGym(node);
                return new GymNodeController(node, run, gym, PokemonFactory, Economy);
            });

            // Per §7.14 (CL-009) — Dojo node: teach moves / abilities for Poké Dollars.
            factory.Register(NodeType.Dojo, (node, run) => new DojoNodeController(
                node, run, Box, Economy));
        }
    }
}
