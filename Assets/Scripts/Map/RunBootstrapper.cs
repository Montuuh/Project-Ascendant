using System;
using System.Collections.Generic;
using UnityEngine;
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

            // Per §5.12.1 (CL-006) — active 4 from the level-gated learnset (cap 4). Base forms start
            // with 2; the pool/active grow as the starter levels (LevelUpResolver).
            List<MoveSO> known = starter.KnownMovesAtLevel(level);
            int max = known.Count < 4 ? known.Count : 4;
            for (int i = 0; i < max; i++)
                if (known[i] != null) inst.CurrentMoves.Add(known[i]);

            context.Box.Members.Add(inst);
            context.Loadout.Confirm(new List<int> { 0 }, 0);
            return inst;
        }

        private static RunContext BuildContext(
            RunContentCatalogSO catalog, RunStateSO run, PokemonInstanceFactory factory, RNGStreams streams)
        {
            Box box = new(catalog.Economy.BoxCapacity);
            List<RelicSO> relics = catalog.Relics ?? new List<RelicSO>();

            // The Region Shop has a dedicated Pokéball slot (§7.7.1), so exclude the Pokéball from the
            // general consumable pool — otherwise it double-lists (a Consumable slot AND the Pokéball slot).
            List<ConsumableSO> shopConsumables = new();
            if (catalog.Consumables != null)
                foreach (ConsumableSO c in catalog.Consumables)
                    if (c != null && c != catalog.Pokeball && c.Id != "pokeball") shopConsumables.Add(c);

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
                // §6.10 — persistent meta loaded from disk (fresh instance if none); §6.3 config from the
                // catalog, falling back to a spec-default instance so run-end commit works pre-authoring.
                // Per Bug #11 — LoadMeta returns null when no save exists; instantiate a fresh SO in that case.
                Meta = SaveSystem.LoadMeta() ?? ScriptableObject.CreateInstance<MetaProgressionSO>(),
                MetaConfig = ResolveMetaConfig(catalog),
                Pokedex = SaveSystem.LoadPokedex() ?? ScriptableObject.CreateInstance<PokedexProgressSO>(),
                // §6.9 — total roster size (starters + wild biomes + every evolution stage) for the
                // Pokédex completion-% denominator + milestone rewards.
                PokedexTotalSpecies = RunContentRegistry.FromCatalog(catalog).AllSpecies.Count,
                DifficultyChoices = BuildDifficultyChoices(),
                WildConfig = catalog.WildConfig,
                Pokeball = catalog.Pokeball,
                BoxOverflow = new AutoSkipBoxOverflowHandler(),
                ArchetypePool = catalog.Archetypes,
                EliteSO = catalog.Elite,
                ShopConfig = catalog.ShopConfig,
                ShopPools = new RegionShopNodeController.ShopItemPools
                {
                    Consumables = shopConsumables,
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
                GymPool = catalog.GymPool ?? new List<GymLeaderSO>(),
            };
        }

        private static List<RelicSO> Filter(List<RelicSO> relics, RarityTier rarity)
        {
            List<RelicSO> result = new();
            for (int i = 0; i < relics.Count; i++)
                if (relics[i] != null && relics[i].Rarity == rarity) result.Add(relics[i]);
            return result;
        }

        // §6.3 / §6.3.5 (CL-019 — Q18) — the Trainer-XP/Level/Token config. An authored catalog asset is
        // used verbatim; with none authored we fall back to a transient spec-default seeded with the
        // §6.3.5 Battle Pass reward track (so level-ups grant their milestones + Tokens). The authored
        // asset is never mutated.
        private static MetaProgressionConfigSO ResolveMetaConfig(RunContentCatalogSO catalog)
        {
            if (catalog.MetaProgressionConfig != null) return catalog.MetaProgressionConfig;
            MetaProgressionConfigSO cfg = ScriptableObject.CreateInstance<MetaProgressionConfigSO>();
            cfg.LevelMilestones = BattlePassTrack.BuildDefaultMilestones();
            return cfg;
        }

        // Per §6.8.2 / Task 11.6 — the 3 VS difficulty modifiers, built as runtime instances (the
        // authored assets aren't catalog-wired yet; same default-instance pattern as MetaConfig).
        // XP multipliers per §6.8.2. Mechanical effects: One Path (routes) is live; Dense Fog (hide
        // intents) + Iron Will (enemy HP) are flagged for the combat-threading follow-up (gap #44).
        // Public so the resume path (RunLauncher) can register these in the RunContentRegistry —
        // saved ActiveDifficultyModifiers are resolved by ID against this same authored set.
        public static List<DifficultyModifierSO> BuildDifficultyChoices()
        {
            return new List<DifficultyModifierSO>
            {
                Dm("iron_will", "Iron Will", "Enemies have +20% Max HP.", xp: 1.15f, enemy: 1.20f, hide: false, routes: 3),
                Dm("dense_fog", "Dense Fog", "Enemy intents start hidden.", xp: 1.15f, enemy: 1f, hide: true, routes: 3),
                Dm("one_path", "One Path", "Only one route forward at each junction.", xp: 1.10f, enemy: 1f, hide: false, routes: 1),
            };
        }

        private static DifficultyModifierSO Dm(string id, string name, string desc,
                                               float xp, float enemy, bool hide, int routes)
        {
            DifficultyModifierSO m = ScriptableObject.CreateInstance<DifficultyModifierSO>();
            m.ModifierId = id; m.DisplayName = name; m.Description = desc;
            m.TrainerXPMultiplier = xp; m.EnemyStatMultiplier = enemy;
            m.HideAllEnemyIntents = hide; m.MaxRouteBranchChoices = routes;
            return m;
        }
    }
}
