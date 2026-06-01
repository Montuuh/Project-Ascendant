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

        // ── Global config ────────────────────────────────────────────────────
        public EconomyConfigSO Economy;
        public MapGenerationConfigSO MapConfig;
        public BattleConfigSO BattleConfig; // combat tuning (used when launching real fights)
        public ProgressionConfigSO ProgressionConfig; // §5.2 — per-Pokémon XP + leveling
        public MetaProgressionSO Meta;             // §6.10 — persistent cross-run state (run-end commit target)
        public MetaProgressionConfigSO MetaConfig; // §6.3 — Trainer XP/Level/Token tuning
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
        public GymLeaderSO GymSO;

        // Registers a builder for every NodeType. Each closure captures the deps this context owns,
        // so the HSM/RunController can resolve a node without a hardcoded switch (§9.6).
        public void RegisterBuilders(NodeControllerFactory factory)
        {
            factory.Register(NodeType.Wild, (node, run) => new WildAreaNodeController(
                node, run, WildConfig, PokemonFactory, Pokeball, Streams.EncounterRNG, Box, BoxOverflow));

            factory.Register(NodeType.Trainer, (node, run) => new TrainerBattleNodeController(
                node, run, ArchetypePool, PokemonFactory, Streams.LootRNG, Streams.LootRNG));

            factory.Register(NodeType.Elite, (node, run) => new EliteNodeController(
                node, run, EliteSO, PokemonFactory));

            factory.Register(NodeType.Center, (node, run) => new PokemonCenterNodeController(
                node, run, Box, Economy));

            factory.Register(NodeType.Shop, (node, run) => new RegionShopNodeController(
                node, run, ShopConfig, Streams.LootRNG, ShopPools));

            factory.Register(NodeType.Mystery, (node, run) => new MysteryEventNodeController(
                node, run, MysteryPool, MysteryConfig, Streams.MysteryRNG, MysteryItems, Box, Economy));

            factory.Register(NodeType.Gym, (node, run) => new GymNodeController(
                node, run, GymSO, PokemonFactory));
        }
    }
}
