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
                // §6.10 — persistent meta loaded from disk (fresh instance if none); §6.3 config from the
                // catalog, falling back to a spec-default instance so run-end commit works pre-authoring.
                // Per Bug #11 — LoadMeta returns null when no save exists; instantiate a fresh SO in that case.
                Meta = SaveSystem.LoadMeta() ?? ScriptableObject.CreateInstance<MetaProgressionSO>(),
                MetaConfig = catalog.MetaProgressionConfig != null
                    ? catalog.MetaProgressionConfig
                    : ScriptableObject.CreateInstance<MetaProgressionConfigSO>(),
                Bestiary = SaveSystem.LoadBestiary() ?? ScriptableObject.CreateInstance<BestiaryProgressSO>(),
                DifficultyChoices = BuildDifficultyChoices(),
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

        // Per §6.8.2 / Task 11.6 — the 3 VS difficulty modifiers, built as runtime instances (the
        // authored assets aren't catalog-wired yet; same default-instance pattern as MetaConfig).
        // XP multipliers per §6.8.2. Mechanical effects: One Path (routes) is live; Dense Fog (hide
        // intents) + Iron Will (enemy HP) are flagged for the combat-threading follow-up (gap #44).
        private static List<DifficultyModifierSO> BuildDifficultyChoices()
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
