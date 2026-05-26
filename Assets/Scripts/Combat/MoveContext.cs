using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §4.1.1 + Epic 4 Task 4.2.1 — input bundle for DamageCalculator.Compute.
    // Immutable readonly struct to avoid allocations in hot combat paths.
    // Crit is passed in as a resolved bool — the crit-roll itself belongs to
    // Task 4.4 (Crit System) and the GameRNG combat stream, not the calculator.
    public readonly struct MoveContext
    {
        public readonly PokemonInstance Attacker;
        public readonly PokemonInstance Target;
        public readonly MoveSO Move;
        public readonly BattleConfigSO Config;

        // True if this play has crit. Forced true when move.AlwaysCrit is set
        // (see DamageCalculator). Per §4.1.3 — Crit is scarce, investment-gated;
        // the calculator does not roll for it.
        public readonly bool Crit;

        public MoveContext(
            PokemonInstance attacker,
            PokemonInstance target,
            MoveSO move,
            BattleConfigSO config,
            bool crit)
        {
            Attacker = attacker;
            Target = target;
            Move = move;
            Config = config;
            Crit = crit;
        }
    }
}
