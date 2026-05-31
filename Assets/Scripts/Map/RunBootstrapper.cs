using System;
using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Map
{
    // Per §9.2 + Epic 9 runtime wiring — builds a live RunController from a RunContentCatalogSO.
    // The single reusable run-construction path: RunLauncher (runtime), the smoke harness, and any
    // future "New Run" UI all go through here, so content assembly lives in one place.
    public static class RunBootstrapper
    {
        // Assembles the RunContext + factory from the catalog and returns a wired RunController.
        // `dispatch` routes the RunController's GameEvents into the HSM (GameStateMachine.HandleEvent).
        public static RunController CreateRunController(
            RunContentCatalogSO catalog,
            RunStateSO run,
            PokemonInstanceFactory pokemonFactory,
            RNGStreams streams,
            Action<GameEvent> dispatch,
            out RunContext context)
        {
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));
            if (catalog.Economy == null) throw new ArgumentException("Catalog has no EconomyConfig.", nameof(catalog));

            context = BuildContext(catalog, run, pokemonFactory, streams);

            NodeControllerFactory factory = new();
            context.RegisterBuilders(factory);

            return new RunController(context, factory, dispatch);
        }

        // Seeds the Box with one starter and commits it as the Active Team (so a real team exists
        // before the first node). Copies the species learnset into CurrentMoves (the factory does
        // not auto-fill them) so the starter has cards to play in real combat. Returns the instance.
        public static PokemonInstance SeedStarter(RunContext context, PokemonSpeciesSO starter, int level)
        {
            if (context == null || starter == null) return null;
            PokemonInstance inst = context.PokemonFactory.Create(starter, level);

            // Per §3.7 — active 4 moves. Mirror the encounter controllers: copy BaseLearnset (cap 4).
            if (starter.BaseLearnset != null)
            {
                int max = starter.BaseLearnset.Count < 4 ? starter.BaseLearnset.Count : 4;
                for (int i = 0; i < max; i++)
                    if (starter.BaseLearnset[i] != null) inst.CurrentMoves.Add(starter.BaseLearnset[i]);
            }

            context.Box.Members.Add(inst);
            context.Loadout.Confirm(new List<int> { 0 }, 0);
            return inst;
        }

        private static RunContext BuildContext(
            RunContentCatalogSO catalog, RunStateSO run, PokemonInstanceFactory factory, RNGStreams streams)
        {
            Box box = new(catalog.Economy.BoxCapacity);
            List<RelicSO> relics = catalog.Relics ?? new List<RelicSO>();

            return new RunContext
            {
                Run = run,
                Box = box,
                Loadout = new LoadoutManager(run, box),
                PokemonFactory = factory,
                Streams = streams,
                Economy = catalog.Economy,
                MapConfig = catalog.MapConfig,
                BattleConfig = catalog.BattleConfig,
                ProgressionConfig = catalog.ProgressionConfig,
                WildConfig = catalog.WildConfig,
                Pokeball = catalog.Pokeball,
                BoxOverflow = new AutoSkipBoxOverflowHandler(),
                ArchetypePool = catalog.Archetypes,
                EliteSO = catalog.Elite,
                ShopConfig = catalog.ShopConfig,
                ShopPools = new RegionShopNodeController.ShopItemPools
                {
                    Consumables = catalog.Consumables,
                    CommonRelics = Filter(relics, RarityTier.Common),
                    UncommonRelics = Filter(relics, RarityTier.Uncommon),
                    Pokeball = catalog.Pokeball,
                    HeldItems = catalog.HeldItems,
                    TMs = catalog.TMs,
                },
                MysteryPool = catalog.MysteryEvents,
                MysteryConfig = catalog.MysteryConfig,
                MysteryItems = new MysteryEventNodeController.MysteryItemRefs
                {
                    StoneRelicPool = relics,
                    Potion = catalog.Potion,
                    TutorPlaceholder = catalog.Potion,
                },
                GymSO = catalog.Gym,
            };
        }

        private static List<RelicSO> Filter(List<RelicSO> relics, RarityTier rarity)
        {
            List<RelicSO> result = new();
            for (int i = 0; i < relics.Count; i++)
                if (relics[i] != null && relics[i].Rarity == rarity) result.Add(relics[i]);
            return result;
        }
    }
}
