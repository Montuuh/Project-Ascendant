using System;
using System.Collections.Generic;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Map
{
    // Per §4.4.4 / §7.13 + Epic 9 Task 9.8 — the Gym Leader node (Layer 7, NodeType.Gym).
    //
    //   • OnEnter: build the Epic-8 GymLeaderController. NO pre-fight heal (9.8.2) — the guaranteed
    //     Pokémon Center at Layer 6 is the pre-Gym pit stop; the boss is faced at current HP (§2.4).
    //   • BuildCombat (9.8.1): GymLeaderController.BuildCombatSetup with the run's earned Badges
    //     active (§4.4.5) and the persistent Gym type field (set inside the controller, §4.4.4.3).
    //   • ResolveCombat (9.8.3): on Victory apply Badge + Rare relic + XP/₽ into the run, then
    //     Complete with NodeOutcome.RunEnded — the VS ends at Gym 1 (§7.13), so Gym victory drives
    //     the run-end transition (completes the Task 8.5.9 stub). On Defeat → PlayerWiped.
    public sealed class GymNodeController : NodeController
    {
        private readonly GymLeaderSO _gymSO;
        private readonly PokemonInstanceFactory _factory;
        private readonly EconomyConfigSO _economy; // §8.3.3 — Coin Pouch

        private GymLeaderController _gym;

        public GymLeaderSO GymSO => _gymSO;

        public GymNodeController(MapNode node, RunStateSO runState, GymLeaderSO gymSO, PokemonInstanceFactory factory,
            EconomyConfigSO economy = null)
            : base(node, runState)
        {
            _gymSO   = gymSO ?? throw new ArgumentNullException(nameof(gymSO));
            _factory = factory;
            _economy = economy;
        }

        protected override void OnEnter()
        {
            // 9.8.2 — no pre-fight heal: the Gym is fought at the team's current HP.
            _gym = new GymLeaderController(_gymSO, _factory);
        }

        public CombatController.CombatSetup BuildCombat(
            List<PokemonInstance> playerTeam,
            int initialLeadIndex,
            List<ConsumableSO> baseInventory,
            BattleConfigSO battleConfig,
            GameRNG combatRng)
        {
            // §4.4.5 — earned Badges active this fight; the Gym type field is set inside BuildCombatSetup.
            return _gym.BuildCombatSetup(
                playerTeam, initialLeadIndex, baseInventory, battleConfig, combatRng, RunState.EarnedBadges);
        }

        public TrainerRewardBundle ResolveCombat(CombatController.CombatOutcome outcome)
        {
            TrainerRewardBundle bundle = _gym.ResolveReward(outcome);

            if (outcome == CombatController.CombatOutcome.Victory)
            {
                RewardApplier.Apply(RunState, bundle, _economy); // Badge → EarnedBadges, Rare relic → HeldRelics
                Complete(NodeOutcome.RunEnded);         // §7.13 — VS ends at Gym 1 → run-end (8.5.9)
            }
            else
            {
                Complete(NodeOutcome.PlayerWiped);
            }
            return bundle;
        }
    }
}
