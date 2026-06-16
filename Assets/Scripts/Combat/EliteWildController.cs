using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §7.5.2 (CL-024) — Elite Wild boss-wild combat controller.
    // A catchable boss Pokémon with a catch-vs-kill dilemma (§7.12):
    //   • Catch → weaken to Catchability gauge threshold (§7.3.4) + apply Pokéball → Victory + full XP + recruit.
    //   • Defeat (HP≤0, not caught) → Victory + single Rare relic (no choice).
    //   • Profile: boss-tier HP, 2-phase escalation (§4.4.3), NO evolution (wild, not trainer ace).
    //
    // Reuses WildAreaNodeController's catch flow (CL-014 gauge) + adds defeat → single-Rare-relic branch.
    public sealed class EliteWildController
    {
        private readonly EliteWildSO _wildSO;
        private readonly PokemonInstanceFactory _pokemonFactory;
        private PokemonInstance _instance;

        public EliteWildSO WildSO => _wildSO;
        public PokemonInstance Instance => _instance;

        public EliteWildController(EliteWildSO wildSO, PokemonInstanceFactory factory)
        {
            _wildSO = wildSO;
            _pokemonFactory = factory;
        }

        // Per §7.5.2 — build a boss-tier (2-phase, no evolution) wild combat.
        // Adds a Pokéball card if RunStateSO.PokeballCount > 0 (§7.3.4 counted scarcity).
        public CombatController.CombatSetup BuildCombatSetup(
            List<PokemonInstance> playerTeam,
            int initialLeadIndex,
            List<ConsumableSO> baseInventory,
            FieldState initialField,
            BattleConfigSO battleConfig,
            GameRNG combatRng,
            ConsumableSO pokeballConsumable,
            int pokeballCount)
        {
            if (_wildSO == null || _wildSO.Species == null)
                return default;

            // Materialise the boss wild with authored Level + PhaseCount.
            _instance = _pokemonFactory?.Create(_wildSO.Species, _wildSO.Level);
            if (_instance == null)
                return default;

            // Per §7.5.2 / §4.4.3 — 2-phase boss-tier defensive escalation.
            _instance.PhaseCount = _wildSO.PhaseCount;

            List<PokemonInstance> enemyTeam = new() { _instance };

            // Per §7.3.4 (CL-014) — add Pokéball card if run holds ≥1 ball.
            List<ConsumableSO> inventory = new(baseInventory);
            if (pokeballCount > 0 && pokeballConsumable != null)
                inventory.Add(pokeballConsumable);

            return new CombatController.CombatSetup
            {
                PlayerTeam = playerTeam,
                EnemyTeam = enemyTeam,
                InitialLeadIndex = initialLeadIndex,
                ConsumableInventory = inventory,
                InitialField = initialField, // caller provides neutral field if needed
                Config = battleConfig,
                Rng = combatRng,
            };
        }

        // Per §7.5.2 / §7.12 — resolve catch-vs-kill reward.
        //   • Caught (via WildCatchResolver) → Victory + full combat XP (CatchRewardXP) + recruit.
        //   • Defeat (HP≤0, not caught) → Victory + single Rare relic (DefeatRelic).
        //   • PlayerWiped → no reward.
        // The EliteWildNodeController handles recruit-into-Box via IBoxOverflowHandler (same as WildAreaNodeController).
        public EliteWildRewardBundle ResolveReward(CombatController.CombatOutcome outcome, bool wasCaught)
        {
            EliteWildRewardBundle bundle = new();
            if (outcome != CombatController.CombatOutcome.Victory)
                return bundle;

            if (wasCaught)
            {
                // Per §7.5.2 — catch → full XP (mirrors CL-003/CL-004) + recruit handled by node controller.
                bundle.TrainerXP = _wildSO != null ? _wildSO.CatchRewardXP : 0;
                bundle.WasCaught = true;
                bundle.CaughtInstance = _instance;
            }
            else
            {
                // Per §7.5.2 — defeat (HP≤0, not caught) → single Rare relic (no choice).
                if (_wildSO != null && _wildSO.DefeatRelic != null)
                    bundle.DefeatRelic = _wildSO.DefeatRelic;
                bundle.PokeDollars = _wildSO != null ? _wildSO.PokeDollarReward : 0;
            }

            return bundle;
        }
    }

    // Per §7.5.2 (CL-024) — Elite Wild reward bundle (catch-vs-kill).
    public struct EliteWildRewardBundle
    {
        public int TrainerXP;
        public int PokeDollars;
        public bool WasCaught;
        public PokemonInstance CaughtInstance;
        public RelicSO DefeatRelic; // single Rare relic on defeat
    }
}
