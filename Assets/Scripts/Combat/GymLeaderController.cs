using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §4.4.4 + Epic 8 Task 8.5 — orchestrates a Gym Leader boss fight.
    //
    // Same sequential-spawn shape as the Elite/trainer controllers (via
    // IEnemyReinforcementProvider), with the boss extras:
    //   • Materialises each Pokémon with its authored PhaseCount + the ace's
    //     HasSturdy (consumed by CombatController's phase-transition director,
    //     §4.4.3). Per CL-013 Gym aces no longer evolve mid-fight (§4.4.4.3).
    //   • Sets a persistent type field at encounter start (§4.4.4.3) — a VS
    //     placeholder marker (no damage multiplier yet; ⚠ gap #33).
    //   • Reward (§7.12 / §4.4.5): Badge + guaranteed Rare relic + 50 XP +
    //     500₽, no RNG. The Badge is applied run-wide by the run layer.
    public sealed class GymLeaderController : IEnemyReinforcementProvider
    {
        private readonly GymLeaderSO _gym;
        private readonly PokemonInstanceFactory _pokemonFactory;
        private readonly Queue<GymPokemonSlot> _remaining = new();

        public GymLeaderSO Gym => _gym;
        public int SpawnsExecuted { get; private set; }
        public int RemainingInQueue => _remaining.Count;

        public GymLeaderController(GymLeaderSO gym, PokemonInstanceFactory pokemonFactory)
        {
            _gym = gym;
            _pokemonFactory = pokemonFactory;
            if (_gym == null || _gym.Composition == null) return;
            for (int i = 0; i < _gym.Composition.Count; i++)
                _remaining.Enqueue(_gym.Composition[i]);
        }

        // Pre-populates the CombatSetup with the Gym's first Pokémon, sets the
        // persistent type field (§4.4.4.3), and passes through the player's
        // run-wide badges (optional — R1 has none yet).
        public CombatController.CombatSetup BuildCombatSetup(
            List<PokemonInstance> playerTeam,
            int initialLeadIndex,
            List<ConsumableSO> consumableInventory,
            BattleConfigSO config,
            GameRNG combatRng,
            List<BadgeSO> activeBadges = null)
        {
            List<PokemonInstance> enemyTeam = new();
            PokemonInstance first = DequeueAndMaterialiseOne();
            if (first != null) enemyTeam.Add(first);

            // Per §4.4.4.3 — type field set at encounter start, persists the
            // whole fight (VS placeholder: marker only, no damage effect).
            FieldState field = FieldState.Empty;
            if (_gym != null)
            {
                field.HasGymField = true;
                field.GymTypeField = _gym.GymType;
            }

            return new CombatController.CombatSetup
            {
                PlayerTeam = playerTeam,
                InitialLeadIndex = initialLeadIndex,
                EnemyTeam = enemyTeam,
                ConsumableInventory = consumableInventory,
                InitialField = field,
                Config = config,
                Rng = combatRng,
                Reinforcements = this,
                ActiveBadges = activeBadges,
                // Per §4.3.5 (CL-011/Option B) — Gym encounters always start with 1 Hidden intent per enemy.
                HideBaselineIntents = true,
            };
        }

        public List<PokemonInstance> RequestReinforcements(CombatController.CombatState state)
        {
            List<PokemonInstance> next = new();
            PokemonInstance p = DequeueAndMaterialiseOne();
            if (p != null) next.Add(p);
            return next;
        }

        // Per §7.4.4 (OPEN) + R2-5 Task 5 — peek the next wave without
        // consuming it. Gym roster is predefined (no RNG), so peek is
        // trivial: return the next queued slot's species + level.
        public IReadOnlyList<ReinforcementPreview> PeekNextWave()
        {
            List<ReinforcementPreview> preview = new();
            if (_remaining.Count == 0) return preview;
            // Gym battles spawn one at a time (sequential-spawn rule), so
            // the "next wave" is the single next Pokémon in the queue.
            GymPokemonSlot next = _remaining.Peek();
            if (next.Species != null)
                preview.Add(new ReinforcementPreview(next.Species, next.Level));
            return preview;
        }

        // Per §7.12 / §4.4.5 — guaranteed reward, no RNG. Badge + Rare relic +
        // 50 XP + 500₽. Empty for any non-Victory outcome. The run layer reads
        // BadgeAwards into RunStateSO.EarnedBadges and transitions to run-end
        // (Task 8.5.9 — HSM/run wiring is Epic 10/11).
        public TrainerRewardBundle ResolveReward(CombatController.CombatOutcome outcome)
        {
            TrainerRewardBundle bundle = TrainerRewardBundle.Empty;
            if (outcome != CombatController.CombatOutcome.Victory) return bundle;
            if (_gym == null) return bundle;

            bundle.TrainerXP = _gym.TrainerXPReward;
            bundle.PokeDollars = _gym.PokeDollarReward;
            if (_gym.GuaranteedRareRelic != null)
                bundle.RelicDrops.Add(_gym.GuaranteedRareRelic);
            if (_gym.BadgeReward != null)
                bundle.BadgeAwards.Add(_gym.BadgeReward);
            return bundle;
        }

        // ── Internals ────────────────────────────────────────────────────────

        private PokemonInstance DequeueAndMaterialiseOne()
        {
            if (_remaining.Count == 0 || _pokemonFactory == null) return null;
            GymPokemonSlot slot = _remaining.Dequeue();
            if (slot.Species == null) return null;

            PokemonInstance inst = _pokemonFactory.Create(slot.Species, slot.Level);

            // Per §4.4.3 — carry boss state onto the instance. Per CL-013 (§4.4.4.3)
            // Gym aces do NOT set MidFightEvolutionTarget — Gyms no longer evolve
            // mid-fight (the engine mechanic stays for rival/Champion).
            inst.PhaseCount = slot.PhaseCount < 1 ? 1 : slot.PhaseCount;
            inst.HasSturdy = slot.HasSturdy;

            // Mirror the other controllers: factory does not auto-fill moves,
            // so copy the species learnset (capped at the §3.7 active-4 count).
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
