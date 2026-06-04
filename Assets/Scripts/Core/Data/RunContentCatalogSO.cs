using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.2 + Epic 9 runtime wiring — the single authored content catalog a run is built from.
    // Aggregates every config + content pool the node controllers need, as direct serialized
    // references (NOT Resources.Load, which is forbidden §9.x; a direct asset ref is idiomatic).
    // Authored by VS_RunCatalogSeeder; referenced by RunLauncher in the Boot scene. Addressables can
    // wrap this later for streaming, but a single directly-referenced catalog is correct for the VS.
    [CreateAssetMenu(fileName = "RunContentCatalog", menuName = "Project Ascendant/Config/Run Content Catalog")]
    public class RunContentCatalogSO : ScriptableObject
    {
        [Header("Configs")]
        public MapGenerationConfigSO MapConfig;
        public EconomyConfigSO Economy;
        public WildEncounterConfigSO WildConfig;
        public RegionShopConfigSO ShopConfig;
        public MysteryConfigSO MysteryConfig;
        public BattleConfigSO BattleConfig;
        public ProgressionConfigSO ProgressionConfig; // §5.2 — per-Pokémon XP + leveling
        public MetaProgressionConfigSO MetaProgressionConfig; // §6.3 — Trainer XP/Level/Token tuning

        [Header("Key Consumables")]
        public ConsumableSO Pokeball;
        public ConsumableSO Potion;

        [Header("Encounters")]
        public List<TrainerArchetypeSO> Archetypes;
        public EliteTrainerSO Elite;

        [Tooltip("Per §7.2 v2 — gym pool for 2-gym fork. Falls back to single Gym if <2 entries.")]
        public List<GymLeaderSO> GymPool;

        [Tooltip("Fallback single gym (backward compat). Used if GymPool has <2 entries.")]
        public GymLeaderSO Gym;

        public List<MysteryEventSO> MysteryEvents;

        [Header("Item Pools")]
        public List<RelicSO> Relics;            // split by Rarity at run-build time
        public List<ConsumableSO> Consumables;
        public List<HeldItemSO> HeldItems;
        public List<TMSO> TMs;

        [Header("Starters")]
        // Per §2.1.1 — the unlocked starter pool the player picks from at run start.
        public List<PokemonSpeciesSO> Starters;
    }
}
