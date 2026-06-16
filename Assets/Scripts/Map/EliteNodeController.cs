using System;
using System.Collections.Generic;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Map
{
    // Per §7.5.1 + Epic 9 Task 9.4 (CL-024) — the Elite Trainer node controller.
    //
    // A thin run-layer shell over Epic-8 EliteTrainerController. The Elite fields 2 Pokémon (2-phase),
    // rewards 3 Rare relics (player chooses 1 of 3), and grants 25 XP + 300₽.
    //
    //   • BuildCombat: EliteTrainerController.BuildCombatSetup (sequential 2-phase spawn) + run Badges.
    //   • ResolveCombat: on Victory, if 3 Rare relics → apply choice callback (or auto-pick first if null);
    //     apply chosen relic + ₽/XP, Complete (→ MapView). On Defeat → PlayerWiped (run-failure).
    public sealed class EliteNodeController : NodeController
    {
        private readonly EliteTrainerSO _eliteSO;
        private readonly PokemonInstanceFactory _factory;
        private readonly EconomyConfigSO _economy; // §8.3.3 — Coin Pouch

        private EliteTrainerController _elite;
        private Func<IReadOnlyList<RelicSO>, RelicSO> _relicChoiceCallback; // Optional: UI injects choice logic.

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

        // Per §7.5.1 (CL-024) — optional callback for 1-of-3 Rare-relic choice. UI layer injects this.
        // If null, controller auto-picks the first relic (headless tests).
        public void SetRelicChoiceCallback(Func<IReadOnlyList<RelicSO>, RelicSO> callback)
        {
            _relicChoiceCallback = callback;
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
            // #44 — apply Iron Will (enemy HP) / Dense Fog (intent hide) difficulty modifiers.
            setup = CombatController.ApplyDifficultyModifiers(setup, RunState.ActiveDifficultyModifiers);
            return setup;
        }

        // Per §7.5.1 (CL-024) + §3.3.1 / §2.3 + R3-5 — resolve reward + 1-of-3 Rare-relic choice.
        public TrainerRewardBundle ResolveCombat(CombatController.CombatOutcome outcome, int finalLeadIndex)
        {
            TrainerRewardBundle bundle = _elite.ResolveReward(outcome);

            if (outcome == CombatController.CombatOutcome.Victory)
            {
                // Per §7.5.1 (CL-024) — if bundle holds 3 Rare relics, open choice UI (or auto-pick).
                if (bundle.RelicDrops != null && bundle.RelicDrops.Count == 3)
                {
                    RelicSO chosenRelic = PickOneRelic(bundle.RelicDrops);
                    // Build a new bundle with ONLY the chosen relic + ₽/XP.
                    bundle = new TrainerRewardBundle
                    {
                        RelicDrops = new List<RelicSO> { chosenRelic },
                        TrainerXP = bundle.TrainerXP,
                        PokeDollars = bundle.PokeDollars,
                        ConsumableDrops = bundle.ConsumableDrops,
                        BadgeAwards = bundle.BadgeAwards
                    };
                }

                RewardApplier.Apply(RunState, bundle, _economy);
            }

            // Per R3-5 — persist final combat LeadIndex so team order survives node→MapView.
            RunState.LeadIndex = finalLeadIndex;

            Complete(outcome == CombatController.CombatOutcome.Defeat
                ? NodeOutcome.PlayerWiped
                : NodeOutcome.Cleared);
            return bundle;
        }

        // Per §7.5.1 (CL-024) — 1-of-3 Rare-relic choice. Calls the injected callback (UI layer),
        // or auto-picks first if no callback (headless tests). Returns the chosen relic.
        private RelicSO PickOneRelic(IReadOnlyList<RelicSO> offer)
        {
            if (offer == null || offer.Count == 0) return null;

            // If a choice callback is injected (runtime UI), use it.
            if (_relicChoiceCallback != null)
                return _relicChoiceCallback(offer) ?? offer[0];

            // Headless path (EditMode tests, no callback): auto-pick first.
            return offer[0];
        }
    }
}
