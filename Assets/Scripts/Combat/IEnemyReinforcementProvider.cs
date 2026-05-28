using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §7.4 + Epic 8 Task 8.2 — hook the CombatController consults when
    // the enemy team is fully fainted but the encounter is not yet over
    // (Trainer Battles with 1–2 sequential Pokémon, Elite 2-stage fights,
    // multi-Pokémon Gym Leader phases).
    //
    // CombatController calls RequestReinforcements from inside
    // HandleAnyFaints. If the result is null or empty, Outcome.Victory is
    // set normally. Otherwise the returned instances REPLACE the current
    // EnemyTeam contents in-place (slot indices reset to 0..N-1) and the
    // outcome stays InProgress. The new Pokémon do not act on the current
    // turn — the next IntentPhase rebuilds their intents fresh.
    //
    // Implementations are stateful (typically own a queue of remaining
    // archetype slots). Returning the same instance twice is the caller's
    // responsibility to avoid — the controller assumes ownership.
    public interface IEnemyReinforcementProvider
    {
        List<PokemonInstance> RequestReinforcements(CombatController.CombatState state);
    }
}
