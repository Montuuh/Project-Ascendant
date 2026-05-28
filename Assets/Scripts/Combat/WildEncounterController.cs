using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §7.3 + Epic 8 Task 8.1 — orchestrates a Wild Pokémon Area Node.
    //
    // Lifecycle (caller):
    //   var wild = new WildEncounterController(biome, pokeballSO,
    //                                          pokemonFactory, encounterRng);
    //   List<PokemonSpeciesSO> offer = wild.OfferSpeciesChoices(3);
    //   // ... player picks one ...
    //   var setup = wild.BuildCombatSetup(offer[chosenIdx], wildLevel,
    //                                     playerTeam, leadIdx, baseInv,
    //                                     field, config, combatRng);
    //   var cc = new CombatController(setup, agent);
    //   var outcome = cc.RunFullCombat();
    //   var result = wild.ResolveOutcome(outcome, cc.State.CaughtTarget,
    //                                    currentBox, boxCap, overflowHandler);
    //
    // Catch semantics live in CombatController.TryPlayConsumable + the
    // CatchConsumableEffectSO dispatch. This class is the encounter-shell
    // around the combat: species choice, ball injection, post-combat
    // recruit handling per §2.3.1.
    public sealed class WildEncounterController
    {
        private readonly BiomeSO _biome;
        private readonly ConsumableSO _pokeballSO;
        private readonly PokemonInstanceFactory _pokemonFactory;
        private readonly GameRNG _encounterRng;

        public BiomeSO Biome => _biome;

        public WildEncounterController(BiomeSO biome,
                                       ConsumableSO pokeballSO,
                                       PokemonInstanceFactory pokemonFactory,
                                       GameRNG encounterRng)
        {
            _biome = biome;
            _pokeballSO = pokeballSO;
            _pokemonFactory = pokemonFactory;
            _encounterRng = encounterRng;
        }

        // Per Task 8.1.1 + 8.1.2 — surface `count` distinct species choices
        // sampled from the biome's SpeciesPool via EncounterRNG. Choices are
        // unique within the offer (no duplicates) when the pool is large
        // enough; if the pool is smaller than `count`, returns the whole pool
        // in shuffled order. Returns empty list on null biome / empty pool.
        //
        // Uniform pick via GameRNG.PickWeighted weight=1 (the weighted
        // EncounterTableSO is used by Epic 9 map-spawn rolls, not by the
        // per-encounter species offer).
        public List<PokemonSpeciesSO> OfferSpeciesChoices(int count)
        {
            List<PokemonSpeciesSO> picks = new();
            if (count <= 0) return picks;
            if (_biome == null || _biome.SpeciesPool == null
                || _biome.SpeciesPool.Count == 0) return picks;
            if (_encounterRng == null) return picks;

            // Build a mutable working copy so we can remove-after-pick to
            // guarantee uniqueness. Trivial allocation — pool size is tiny.
            List<PokemonSpeciesSO> remaining = new(_biome.SpeciesPool.Count);
            for (int i = 0; i < _biome.SpeciesPool.Count; i++)
            {
                PokemonSpeciesSO s = _biome.SpeciesPool[i];
                if (s != null) remaining.Add(s);
            }

            int target = count < remaining.Count ? count : remaining.Count;
            for (int i = 0; i < target; i++)
            {
                List<(PokemonSpeciesSO value, float weight)> opts =
                    new(remaining.Count);
                for (int j = 0; j < remaining.Count; j++)
                    opts.Add((remaining[j], 1f));
                PokemonSpeciesSO picked = _encounterRng.PickWeighted(opts);
                picks.Add(picked);
                remaining.Remove(picked);
            }
            return picks;
        }

        // Per Task 8.1.3 — builds a CombatSetup with the chosen species as
        // the wild enemy and the free Pokéball appended to the combat-local
        // ConsumableInventory snapshot.
        //
        // Per §7.3.4.1 step 2 — "free Pokéball is 1-use; additional shop
        // Pokéballs are usable here." The snapshot is a copy (see
        // CombatController constructor); MarkUsed prevents re-draw within
        // combat; RestoreAll at CombatEnd is harmless because the player's
        // persistent inventory was never mutated.
        //
        // Wild Pokémon enters at full HP per §7.3.4.1 step 1. Player team
        // enters at current HP per §2.4. No reinforcements provider (wild
        // encounters are single-enemy).
        public CombatController.CombatSetup BuildCombatSetup(
            PokemonSpeciesSO chosenSpecies,
            int wildLevel,
            List<PokemonInstance> playerTeam,
            int initialLeadIndex,
            List<ConsumableSO> baseInventory,
            FieldState initialField,
            BattleConfigSO config,
            GameRNG combatRng)
        {
            List<PokemonInstance> enemyTeam = new();
            if (chosenSpecies != null && _pokemonFactory != null)
            {
                PokemonInstance wild = _pokemonFactory.Create(
                    chosenSpecies, wildLevel);
                // Mirror TrainerBattleController — copy BaseLearnset (4 cap)
                // into CurrentMoves so the AI has candidate intents to score.
                if (chosenSpecies.BaseLearnset != null)
                {
                    int max = chosenSpecies.BaseLearnset.Count < 4
                        ? chosenSpecies.BaseLearnset.Count : 4;
                    for (int i = 0; i < max; i++)
                    {
                        MoveSO m = chosenSpecies.BaseLearnset[i];
                        if (m != null) wild.CurrentMoves.Add(m);
                    }
                }
                enemyTeam.Add(wild);
            }

            // Per Task 8.1.3 — inject Pokéball into a fresh copy of the
            // inventory. Inserting into `baseInventory` directly would mutate
            // the caller's persistent list across combats; copy first.
            List<ConsumableSO> combatInventory = baseInventory != null
                ? new List<ConsumableSO>(baseInventory)
                : new List<ConsumableSO>();
            if (_pokeballSO != null) combatInventory.Add(_pokeballSO);

            return new CombatController.CombatSetup
            {
                PlayerTeam = playerTeam,
                InitialLeadIndex = initialLeadIndex,
                EnemyTeam = enemyTeam,
                ConsumableInventory = combatInventory,
                InitialField = initialField,
                Config = config,
                Rng = combatRng,
                // No reinforcements — wild encounters are single-enemy.
            };
        }

        // Per Task 8.1.5 + §2.3.1 — interpret combat outcome + the captured
        // target reference. On Caught, handle Box overflow per §2.3.1:
        //   • Box has room          → add candidate, BoxUpdated=true
        //   • Box at capacity       → call overflowHandler:
        //        Skip (return -1)   → discard candidate, BoxUpdated=false
        //        Swap (0..count-1)  → release that index, add candidate
        //
        // Box capacity defaults to box.Count + 1 if `boxCapacity <= 0`
        // (treated as unbounded for tests that don't care about §2.3.1).
        public WildEncounterResult ResolveOutcome(
            CombatController.CombatOutcome outcome,
            PokemonInstance caughtTarget,
            List<PokemonInstance> currentBox,
            int boxCapacity,
            IBoxOverflowHandler overflowHandler)
        {
            if (outcome == CombatController.CombatOutcome.Defeat)
                return WildEncounterResult.MakePlayerWiped();

            if (caughtTarget == null)
                return WildEncounterResult.MakeWildFainted();

            // From here: Outcome.Victory + CaughtTarget != null = Caught.
            // Box handling per §2.3.1.
            if (currentBox == null) currentBox = new List<PokemonInstance>();
            bool unbounded = boxCapacity <= 0;

            if (unbounded || currentBox.Count < boxCapacity)
            {
                currentBox.Add(caughtTarget);
                return WildEncounterResult.MakeCaught(
                    caughtTarget, overflowShown: false,
                    releasedIdx: -1, boxUpdated: true);
            }

            // Box is at capacity — prompt.
            if (overflowHandler == null)
            {
                // No handler wired → degrade to Skip (defensive).
                return WildEncounterResult.MakeCaught(
                    caughtTarget, overflowShown: true,
                    releasedIdx: -1, boxUpdated: false);
            }

            int releasedIdx = overflowHandler.OnBoxOverflow(currentBox, caughtTarget);
            if (releasedIdx < 0 || releasedIdx >= currentBox.Count)
            {
                // Skip or out-of-range → no change.
                return WildEncounterResult.MakeCaught(
                    caughtTarget, overflowShown: true,
                    releasedIdx: -1, boxUpdated: false);
            }

            currentBox[releasedIdx] = caughtTarget;
            return WildEncounterResult.MakeCaught(
                caughtTarget, overflowShown: true,
                releasedIdx: releasedIdx, boxUpdated: true);
        }
    }
}
