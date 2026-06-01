using System;
using System.Collections.Generic;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Map
{
    // Per §7.5 + Epic 9 Task 9.4 — the Elite Trainer node controller (Layer 3, NodeType.Elite).
    //
    // A thin run-layer shell over Epic-8 EliteTrainerController (gap #31 resolution: the Elite is
    // its own node type, not folded into the Trainer node — its 2-phase composition and guaranteed
    // Uncommon-relic reward are distinct from standard-trainer loot tables).
    //
    //   • BuildCombat: EliteTrainerController.BuildCombatSetup (sequential 2-phase spawn) + run Badges.
    //   • ResolveCombat: on Victory apply the guaranteed reward (§7.5.1/§7.12) and Complete (→ MapView);
    //     on Defeat Complete PlayerWiped (→ run-failure).
    public sealed class EliteNodeController : NodeController
    {
        private readonly EliteTrainerSO _eliteSO;
        private readonly PokemonInstanceFactory _factory;
        private readonly EconomyConfigSO _economy; // §8.3.3 — Coin Pouch

        private EliteTrainerController _elite;

        public EliteTrainerSO EliteSO => _eliteSO;

        public EliteNodeController(
            MapNode node,
            RunStateSO runState,
            EliteTrainerSO eliteSO,
            PokemonInstanceFactory factory,
            EconomyConfigSO economy = null)
            : base(node, runState)
        {
            _eliteSO = eliteSO ?? throw new ArgumentNullException(nameof(eliteSO));
            _factory = factory;
            _economy = economy;
        }

        protected override void OnEnter()
        {
            _elite = new EliteTrainerController(_eliteSO, _factory);
        }

        public CombatController.CombatSetup BuildCombat(
            List<PokemonInstance> playerTeam,
            int initialLeadIndex,
            List<ConsumableSO> baseInventory,
            FieldState initialField,
            BattleConfigSO battleConfig,
            GameRNG combatRng)
        {
            CombatController.CombatSetup setup = _elite.BuildCombatSetup(
                playerTeam, initialLeadIndex, baseInventory, initialField, battleConfig, combatRng);
            setup.ActiveBadges = RunState.EarnedBadges; // §4.4.5 — in-run Badge effects
            return setup;
        }

        public TrainerRewardBundle ResolveCombat(CombatController.CombatOutcome outcome)
        {
            TrainerRewardBundle bundle = _elite.ResolveReward(outcome);
            if (outcome == CombatController.CombatOutcome.Victory)
                RewardApplier.Apply(RunState, bundle, _economy);

            Complete(outcome == CombatController.CombatOutcome.Defeat
                ? NodeOutcome.PlayerWiped
                : NodeOutcome.Cleared);
            return bundle;
        }
    }
}
