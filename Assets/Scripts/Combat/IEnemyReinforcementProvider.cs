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

        // Per §7.4.4 (OPEN) + R2-5 Task 5 — non-consuming preview of the next
        // wave for UI wave-queue telegraph. Returns a lightweight struct with
        // species + level data for the NEXT wave that RequestReinforcements
        // would spawn. CRITICAL: MUST NOT consume RNG or otherwise change
        // what RequestReinforcements will produce (determinism requirement).
        //
        // Returns empty/null when no reinforcements remain. For providers
        // with predefined rosters (Trainer/Elite/Gym) this is trivial (peek
        // the queue). For RNG-driven providers, pre-generate or cache the
        // wave so peek == eventual spawn.
        IReadOnlyList<ReinforcementPreview> PeekNextWave();
    }

    // Per §7.4.4 (OPEN) — lightweight wave preview for UI display. Species
    // + level suffice for the UI to render "Ivysaur Lv13" + intent-kind
    // placeholder (the full intent is not computed until spawn).
    public struct ReinforcementPreview
    {
        public PokemonSpeciesSO Species;
        public int Level;

        public ReinforcementPreview(PokemonSpeciesSO species, int level)
        {
            Species = species;
            Level = level;
        }
    }
}
