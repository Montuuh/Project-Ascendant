using System;
using System.Collections.Generic;
using ProjectAscendant.Core;
using ProjectAscendant.Combat;

namespace ProjectAscendant.Map
{
    // Per §7.5.2 (CL-024) — Elite Wild node controller (NodeType.EliteWild).
    // A boss-wild catch-vs-kill node: catch → recruit, defeat → single Rare relic.
    // Reuses WildAreaNodeController's catch flow (CL-014 gauge + recruit-into-Box via IBoxOverflowHandler).
    public sealed class EliteWildNodeController : NodeController
    {
        private readonly EliteWildSO _wildSO;
        private readonly PokemonInstanceFactory _factory;
        private readonly ConsumableSO _pokeball;
        private readonly GameRNG _encounterRng;
        private readonly Box _box;
        private readonly IBoxOverflowHandler _boxOverflow;
        private readonly EconomyConfigSO _economy;

        private EliteWildController _wild;

        public EliteWildSO WildSO => _wildSO;

        public EliteWildNodeController(
            MapNode node,
            RunStateSO runState,
            EliteWildSO wildSO,
            PokemonInstanceFactory factory,
            ConsumableSO pokeball,
            GameRNG encounterRng,
            Box box,
            IBoxOverflowHandler boxOverflow,
            EconomyConfigSO economy = null)
            : base(node, runState)
        {
            _wildSO = wildSO ?? throw new ArgumentNullException(nameof(wildSO));
            _factory = factory;
            _pokeball = pokeball;
            _encounterRng = encounterRng;
            _box = box;
            _boxOverflow = boxOverflow;
            _economy = economy;
        }

        protected override void OnEnter()
        {
            _wild = new EliteWildController(_wildSO, _factory);
        }

        public CombatController.CombatSetup BuildCombat(
            List<PokemonInstance> playerTeam,
            int initialLeadIndex,
            List<ConsumableSO> baseInventory,
            FieldState initialField,
            BattleConfigSO battleConfig,
            GameRNG combatRng)
        {
            CombatController.CombatSetup setup = _wild.BuildCombatSetup(
                playerTeam, initialLeadIndex, baseInventory, initialField, battleConfig, combatRng,
                _pokeball, RunState.PokeballCount);
            setup.ActiveBadges = RunState.EarnedBadges; // §4.4.5 — in-run Badge effects
            // #44 — apply Iron Will (enemy HP) / Dense Fog (intent hide) difficulty modifiers.
            setup = CombatController.ApplyDifficultyModifiers(setup, RunState.ActiveDifficultyModifiers);
            return setup;
        }

        // Per §7.5.2 — resolve catch-vs-kill outcome.
        //   • Caught → Victory + full XP + recruit into Box (via IBoxOverflowHandler).
        //   • Defeat (HP≤0, not caught) → Victory + apply single Rare relic.
        //   • PlayerWiped → NodeOutcome.PlayerWiped.
        public EliteWildRewardBundle ResolveCombat(
            CombatController.CombatOutcome outcome,
            int finalLeadIndex,
            bool wasCaught)
        {
            EliteWildRewardBundle bundle = _wild.ResolveReward(outcome, wasCaught);

            if (outcome == CombatController.CombatOutcome.Victory)
            {
                // Per §7.5.2 — catch → recruit into Box (reuse WildAreaNodeController's recruit pattern).
                if (bundle.WasCaught && bundle.CaughtInstance != null)
                {
                    // Per §7.3.4 (CL-014) — consume 1 Pokéball on catch.
                    if (RunState.PokeballCount > 0)
                        RunState.PokeballCount--;

                    // Recruit into Box. If full, consult overflow handler (same as WildController.ResolveOutcome pattern).
                    if (_box != null)
                    {
                        if (!_box.TryAdd(bundle.CaughtInstance) && _boxOverflow != null)
                        {
                            // Per §2.3.1 — overflow handler decides: skip (-1) or replace (index).
                            int replaceIdx = _boxOverflow.OnBoxOverflow(_box.Members, bundle.CaughtInstance);
                            if (replaceIdx >= 0 && replaceIdx < _box.Members.Count)
                            {
                                _box.Members.RemoveAt(replaceIdx);
                                _box.TryAdd(bundle.CaughtInstance);
                            }
                        }
                    }
                }

                // Per §7.5.2 — defeat (HP≤0, not caught) → apply single Rare relic + ₽.
                // Build a TrainerRewardBundle and use RewardApplier.
                if (!bundle.WasCaught)
                {
                    TrainerRewardBundle rewardBundle = TrainerRewardBundle.Empty;
                    rewardBundle.TrainerXP = bundle.TrainerXP;
                    rewardBundle.PokeDollars = bundle.PokeDollars;
                    if (bundle.DefeatRelic != null)
                        rewardBundle.RelicDrops.Add(bundle.DefeatRelic);
                    RewardApplier.Apply(RunState, rewardBundle, _economy);
                }
                else
                {
                    // Caught path still grants XP (§7.12 — full combat XP on catch).
                    TrainerRewardBundle rewardBundle = TrainerRewardBundle.Empty;
                    rewardBundle.TrainerXP = bundle.TrainerXP;
                    RewardApplier.Apply(RunState, rewardBundle, _economy);
                }
            }

            // Per §3.3.1 — persist final combat LeadIndex.
            RunState.LeadIndex = finalLeadIndex;

            Complete(outcome == CombatController.CombatOutcome.Defeat
                ? NodeOutcome.PlayerWiped
                : NodeOutcome.Cleared);

            return bundle;
        }
    }
}
