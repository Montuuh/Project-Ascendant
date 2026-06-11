using System;
using System.Collections.Generic;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Map
{
    // Per §7.3 + Epic 9 Task 9.3 — the Wild Pokémon Area node controller.
    //
    // Responsibilities (the run-layer shell around the Epic 8 WildEncounterController):
    //   • OnEnter (9.3.1/9.3.2): pick a biome (weighted, §7.3.1) and compose the 3-species offer
    //     (2 Common + 1 Uncommon, ~10% Rare swap, §7.3.2) by rarity-filtering the biome pool;
    //     publish WildSpeciesOfferedContext for the encounter UI.
    //   • SelectSpecies (9.3.3): build the CombatSetup for the chosen species with the free Pokéball
    //     injected (catching active). The HSM CombatState runs the actual combat.
    //   • ResolveCombat: interpret the combat outcome + caught target, route the recruit into the
    //     Box (§2.3.1 overflow via IBoxOverflowHandler), and Complete the node.
    //
    // All randomness uses the EncounterRNG stream (§9.7.2). The node owns the §7.3.2 composition;
    // catching itself (§7.3.4) lives in CombatController. The wild level band is §7.3.5.
    public sealed class WildAreaNodeController : NodeController
    {
        private readonly WildEncounterConfigSO _config;
        private readonly PokemonInstanceFactory _factory;
        private readonly ConsumableSO _pokeball;
        private readonly GameRNG _encounterRng;
        private readonly Box _box;
        private readonly IBoxOverflowHandler _overflowHandler;

        private WildEncounterController _wild;
        private readonly List<PokemonSpeciesSO> _choices = new();

        public BiomeSO SelectedBiome { get; private set; }
        public IReadOnlyList<PokemonSpeciesSO> Choices => _choices;

        public WildAreaNodeController(
            MapNode node,
            RunStateSO runState,
            WildEncounterConfigSO config,
            PokemonInstanceFactory factory,
            ConsumableSO pokeball,
            GameRNG encounterRng,
            Box box,
            IBoxOverflowHandler overflowHandler)
            : base(node, runState)
        {
            _config          = config ?? throw new ArgumentNullException(nameof(config));
            _factory         = factory;
            _pokeball        = pokeball;
            _encounterRng    = encounterRng ?? throw new ArgumentNullException(nameof(encounterRng));
            _box             = box ?? throw new ArgumentNullException(nameof(box));
            _overflowHandler = overflowHandler;
        }

        protected override void OnEnter()
        {
            SelectedBiome = PickBiome();
            _wild = new WildEncounterController(SelectedBiome, _pokeball, _factory, _encounterRng);

            _choices.Clear();
            _choices.AddRange(ComposeOffer(SelectedBiome));

            // 9.3.2 — surface the choices for the encounter UI (Pillar 1: visible up-front).
            EventBus.Publish(new WildSpeciesOfferedContext(Node.Layer, Node.Lane, _choices));
        }

        // 9.3.3 — build the catching combat for the chosen offer index. The wild level is rolled
        // from the Region band (§7.3.5) on the EncounterRNG. Returns default if the index is invalid.
        public CombatController.CombatSetup SelectSpecies(
            int choiceIndex,
            List<PokemonInstance> playerTeam,
            int initialLeadIndex,
            List<ConsumableSO> baseInventory,
            FieldState initialField,
            BattleConfigSO battleConfig,
            GameRNG combatRng)
        {
            if (choiceIndex < 0 || choiceIndex >= _choices.Count)
                throw new ArgumentOutOfRangeException(nameof(choiceIndex));

            PokemonSpeciesSO chosen = _choices[choiceIndex];
            int level = RollWildLevel();
            return _wild.BuildCombatSetup(
                chosen, level, playerTeam, initialLeadIndex,
                baseInventory, initialField, battleConfig, combatRng,
                RunState != null ? RunState.PokeballCount : 0); // §7.3.4 (Option 1) — gate the catch card
        }

        // Interprets the combat result and routes the recruit into the Box (§2.3.1), then Completes.
        // PlayerWiped → NodeOutcome.PlayerWiped (run-failure); Caught/WildFainted → Cleared.
        // Per §3.3.1 / §2.3 + R3-5 — persist final combat LeadIndex so team order survives.
        public WildEncounterResult ResolveCombat(
            CombatController.CombatOutcome outcome,
            PokemonInstance caughtTarget,
            int finalLeadIndex)
        {
            WildEncounterResult result = _wild.ResolveOutcome(
                outcome, caughtTarget, _box.Members, _box.Capacity, _overflowHandler);

            // Per R3-5 — persist final combat LeadIndex so team order survives node→MapView.
            RunState.LeadIndex = finalLeadIndex;

            NodeOutcome nodeOutcome = result.Outcome == WildEncounterResult.WildOutcome.PlayerWiped
                ? NodeOutcome.PlayerWiped
                : NodeOutcome.Cleared;
            Complete(nodeOutcome);
            return result;
        }

        // ── Internals ────────────────────────────────────────────────────────

        // Per §7.3.1 (+ CL-018 / Q21) — weighted biome sample from the Region's eligible set. When the
        // Naturalist's Lens Region Modifier is active, the steered biome is made dominant (its weight is
        // boosted) — overriding the default primary — while other biomes still appear. Null if none.
        private BiomeSO PickBiome()
        {
            if (_config.RegionBiomes == null || _config.RegionBiomes.Count == 0) return null;

            bool steer = RunState != null
                && RegionModifierResolver.GrantsBiomeSteer(RunState.ActiveRegionModifiers);
            BiomeSO chosen = RunState != null ? RunState.NaturalistLensBiome : null;
            float boost = RunState != null
                ? RegionModifierResolver.BiomeSteerBoost(RunState.ActiveRegionModifiers) : 1f;

            List<(BiomeSO value, float weight)> opts =
                WildAreaBiomeWeighting.BuildOptions(_config.RegionBiomes, steer, chosen, boost);
            return opts.Count > 0 ? _encounterRng.PickWeighted(opts) : null;
        }

        // Per §7.3.2 — CommonChoices Commons + (RareSwapChance ? Rares : Uncommons) for the
        // remaining slots, drawn distinctly by rarity from the biome pool.
        private List<PokemonSpeciesSO> ComposeOffer(BiomeSO biome)
        {
            List<PokemonSpeciesSO> offer = new();
            if (biome == null || biome.SpeciesPool == null) return offer;

            List<PokemonSpeciesSO> commons = FilterByRarity(biome.SpeciesPool, RarityTier.Common);
            List<PokemonSpeciesSO> uncommons = FilterByRarity(biome.SpeciesPool, RarityTier.Uncommon);
            List<PokemonSpeciesSO> rares = FilterByRarity(biome.SpeciesPool, RarityTier.Rare);

            offer.AddRange(PickDistinct(commons, _config.CommonChoices));

            // ~10% of nodes replace the Uncommon slot with a Rare (§7.3.2). Roll BEFORE picking so
            // the draw is deterministic on the EncounterRNG.
            bool rareSwap = _encounterRng.Range01() < _config.RareSwapChance && rares.Count > 0;
            List<PokemonSpeciesSO> secondBucket = rareSwap ? rares : uncommons;
            offer.AddRange(PickDistinct(secondBucket, _config.UncommonChoices));

            return offer;
        }

        private int RollWildLevel()
        {
            int min = _config.WildLevelMin;
            int max = _config.WildLevelMax;
            if (max < min) max = min;
            return _encounterRng.Range(min, max + 1); // §7.3.5 inclusive band
        }

        private static List<PokemonSpeciesSO> FilterByRarity(
            List<PokemonSpeciesSO> pool, RarityTier rarity)
        {
            List<PokemonSpeciesSO> result = new();
            for (int i = 0; i < pool.Count; i++)
                if (pool[i] != null && pool[i].WildRarity == rarity)
                    result.Add(pool[i]);
            return result;
        }

        // Distinct uniform picks (weight=1) from a bucket; returns the whole bucket if smaller.
        private List<PokemonSpeciesSO> PickDistinct(List<PokemonSpeciesSO> bucket, int count)
        {
            List<PokemonSpeciesSO> picks = new();
            if (count <= 0 || bucket.Count == 0) return picks;

            List<PokemonSpeciesSO> remaining = new(bucket);
            int target = count < remaining.Count ? count : remaining.Count;
            for (int i = 0; i < target; i++)
            {
                List<(PokemonSpeciesSO value, float weight)> opts = new(remaining.Count);
                for (int j = 0; j < remaining.Count; j++) opts.Add((remaining[j], 1f));
                PokemonSpeciesSO picked = _encounterRng.PickWeighted(opts);
                picks.Add(picked);
                remaining.Remove(picked);
            }
            return picks;
        }
    }
}
