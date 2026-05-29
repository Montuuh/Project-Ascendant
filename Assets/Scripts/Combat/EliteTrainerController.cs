using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §7.5 + Epic 8 Task 8.4 — orchestrates an Elite Trainer node.
    //
    // Same sequential-spawn shape as TrainerBattleController (§7.4): the first
    // Pokémon is materialised up-front; the rest are handed to CombatController
    // one-at-a-time via IEnemyReinforcementProvider. Two differences:
    //   1. Each materialised Pokémon carries its authored PhaseCount (§4.4.3),
    //      so CombatController/BossPhaseTracker drive Phase-2 aggression.
    //   2. Reward is the SO's single GUARANTEED Uncommon relic + flat XP/₽
    //      (§7.5.1 / §7.12) — no LootRNG roll. Hence no GameRNG dependency.
    //
    // Lifecycle mirrors TrainerBattleController:
    //   var ctrl = new EliteTrainerController(eliteSO, pokemonFactory);
    //   var combat = ctrl.BuildCombatSetup(playerTeam, leadIdx, consumables,
    //                                      field, config, combatRng);
    //   var outcome = new CombatController(combat, agent).RunFullCombat();
    //   var reward = ctrl.ResolveReward(outcome);
    public sealed class EliteTrainerController : IEnemyReinforcementProvider
    {
        private readonly EliteTrainerSO _elite;
        private readonly PokemonInstanceFactory _pokemonFactory;
        private readonly Queue<ElitePokemonSlot> _remaining = new();

        public EliteTrainerSO Elite => _elite;

        // First Pokémon is materialised in BuildCombatSetup, so SpawnsExecuted
        // counts from there. RemainingInQueue lets UI/tests show "N left".
        public int SpawnsExecuted { get; private set; }
        public int RemainingInQueue => _remaining.Count;

        public EliteTrainerController(EliteTrainerSO elite,
                                      PokemonInstanceFactory pokemonFactory)
        {
            _elite = elite;
            _pokemonFactory = pokemonFactory;
            if (_elite == null || _elite.Composition == null) return;
            for (int i = 0; i < _elite.Composition.Count; i++)
                _remaining.Enqueue(_elite.Composition[i]);
        }

        // Pre-populates the CombatSetup with the Elite's first Pokémon and wires
        // this controller as the reinforcement provider for the rest.
        public CombatController.CombatSetup BuildCombatSetup(
            List<PokemonInstance> playerTeam,
            int initialLeadIndex,
            List<ConsumableSO> consumableInventory,
            FieldState initialField,
            BattleConfigSO config,
            GameRNG combatRng)
        {
            List<PokemonInstance> enemyTeam = new();
            PokemonInstance first = DequeueAndMaterialiseOne();
            if (first != null) enemyTeam.Add(first);

            return new CombatController.CombatSetup
            {
                PlayerTeam = playerTeam,
                InitialLeadIndex = initialLeadIndex,
                EnemyTeam = enemyTeam,
                ConsumableInventory = consumableInventory,
                InitialField = initialField,
                Config = config,
                Rng = combatRng,
                Reinforcements = this,
            };
        }

        // IEnemyReinforcementProvider — next Pokémon (single-element list) when
        // the current enemy is wiped. Empty list → CombatController completes
        // Victory. The new Pokémon carries its own PhaseCount.
        public List<PokemonInstance> RequestReinforcements(CombatController.CombatState state)
        {
            List<PokemonInstance> next = new();
            PokemonInstance p = DequeueAndMaterialiseOne();
            if (p != null) next.Add(p);
            return next;
        }

        // Per §7.5.1 / §7.12 — guaranteed reward, no RNG. Returns Empty for any
        // outcome other than Victory. The single relic is the authored
        // GuaranteedRelic (designer-verified Uncommon-tier via the audit test).
        public TrainerRewardBundle ResolveReward(CombatController.CombatOutcome outcome)
        {
            TrainerRewardBundle bundle = TrainerRewardBundle.Empty;
            if (outcome != CombatController.CombatOutcome.Victory) return bundle;
            if (_elite == null) return bundle;

            bundle.TrainerXP = _elite.TrainerXPReward;
            bundle.PokeDollars = _elite.PokeDollarReward;
            if (_elite.GuaranteedRelic != null)
                bundle.RelicDrops.Add(_elite.GuaranteedRelic);

            return bundle;
        }

        // ── Internals ────────────────────────────────────────────────────────

        private PokemonInstance DequeueAndMaterialiseOne()
        {
            if (_remaining.Count == 0) return null;
            if (_pokemonFactory == null) return null;
            ElitePokemonSlot slot = _remaining.Dequeue();
            if (slot.Species == null) return null;

            PokemonInstance inst = _pokemonFactory.Create(slot.Species, slot.Level);

            // Per §4.4.3 — carry the authored phase depth onto the instance so
            // BossPhaseTracker can escalate this Pokémon. Floor at 1 so a 0/
            // unauthored slot behaves like an ordinary single-phase Pokémon.
            inst.PhaseCount = slot.PhaseCount < 1 ? 1 : slot.PhaseCount;

            // Mirror TrainerBattleController/PokemonInstanceFactory: the factory
            // does not auto-fill CurrentMoves, so copy the species learnset
            // (capped at the §3.7 active-4 slot count) to give the AI candidate
            // intents. MasteryMove is wired by the factory.
            if (slot.Species.BaseLearnset != null)
            {
                int max = slot.Species.BaseLearnset.Count < 4
                    ? slot.Species.BaseLearnset.Count : 4;
                for (int i = 0; i < max; i++)
                {
                    MoveSO m = slot.Species.BaseLearnset[i];
                    if (m != null) inst.CurrentMoves.Add(m);
                }
            }

            SpawnsExecuted++;
            return inst;
        }
    }
}
