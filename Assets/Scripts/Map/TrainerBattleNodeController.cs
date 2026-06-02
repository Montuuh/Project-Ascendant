using System;
using System.Collections.Generic;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Map
{
    // Per §7.4 + Epic 9 Task 9.4 — the Trainer Battle node controller.
    //
    //   • OnEnter (9.4.1): pick one archetype from the Region's eligible pool (§7.4.3), seeded.
    //   • BuildCombat (9.4.2): delegate to Epic-8 TrainerBattleController.BuildCombatSetup and
    //     populate CombatSetup.ActiveBadges from RunState.EarnedBadges (§4.4.5 — earned Badges
    //     apply in every subsequent combat, e.g. Boulder Badge §4.4.5.1). The HSM CombatState runs it.
    //   • ResolveCombat (9.4.3/9.4.4): on Victory apply the reward bundle into the run and Complete
    //     (→ MapView); on Defeat Complete PlayerWiped (→ run-failure, §3.3.6).
    public sealed class TrainerBattleNodeController : NodeController
    {
        private readonly IReadOnlyList<TrainerArchetypeSO> _pool;
        private readonly PokemonInstanceFactory _factory;
        private readonly GameRNG _selectionRng; // archetype pick (map-content stream)
        private readonly GameRNG _lootRng;      // reward rolls (LootRNG, §9.7.2)
        private readonly EconomyConfigSO _economy; // §8.3.3 — Coin Pouch ₽ multiplier

        private TrainerBattleController _battle;

        public TrainerArchetypeSO Archetype { get; private set; }

        public TrainerBattleNodeController(
            MapNode node,
            RunStateSO runState,
            IReadOnlyList<TrainerArchetypeSO> archetypePool,
            PokemonInstanceFactory factory,
            GameRNG selectionRng,
            GameRNG lootRng,
            EconomyConfigSO economy = null)
            : base(node, runState)
        {
            _pool         = archetypePool ?? throw new ArgumentNullException(nameof(archetypePool));
            _factory      = factory;
            _selectionRng = selectionRng ?? throw new ArgumentNullException(nameof(selectionRng));
            _lootRng      = lootRng;
            _economy      = economy;
        }

        protected override void OnEnter()
        {
            Archetype = PickArchetype();
            _battle   = new TrainerBattleController(Archetype, _factory, _lootRng);
        }

        // 9.4.2 — build the combat for the HSM CombatState, with run Badges active.
        public CombatController.CombatSetup BuildCombat(
            List<PokemonInstance> playerTeam,
            int initialLeadIndex,
            List<ConsumableSO> baseInventory,
            FieldState initialField,
            BattleConfigSO battleConfig,
            GameRNG combatRng)
        {
            CombatController.CombatSetup setup = _battle.BuildCombatSetup(
                playerTeam, initialLeadIndex, baseInventory, initialField, battleConfig, combatRng);
            setup.ActiveBadges = RunState.EarnedBadges; // §4.4.5 — in-run Badge effects
            return setup;
        }

        // 9.4.3/9.4.4 — resolve the combat: apply rewards on Victory, persist final LeadIndex, then Complete.
        // Per §3.3.1 / §2.3 + R3-5 — the post-combat LeadIndex must persist back to RunStateSO.
        public TrainerRewardBundle ResolveCombat(CombatController.CombatOutcome outcome, int finalLeadIndex)
        {
            TrainerRewardBundle bundle = _battle.ResolveReward(outcome);
            if (outcome == CombatController.CombatOutcome.Victory)
                RewardApplier.Apply(RunState, bundle, _economy);

            // Per R3-5 — persist final combat LeadIndex so team order survives node→MapView.
            RunState.LeadIndex = finalLeadIndex;

            Complete(outcome == CombatController.CombatOutcome.Defeat
                ? NodeOutcome.PlayerWiped
                : NodeOutcome.Cleared);
            return bundle;
        }

        // Per §7.4.3 — uniform seeded pick from the Region's eligible archetype pool.
        private TrainerArchetypeSO PickArchetype()
        {
            List<(TrainerArchetypeSO value, float weight)> opts = new(_pool.Count);
            for (int i = 0; i < _pool.Count; i++)
                if (_pool[i] != null) opts.Add((_pool[i], 1f));
            return opts.Count > 0 ? _selectionRng.PickWeighted(opts) : null;
        }
    }
}
