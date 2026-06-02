using ProjectAscendant.Core;

namespace ProjectAscendant.Progression
{
    // Per §5.4.1 + Epic 10 Task 10.6 — apply a TM to a Pokémon from the Map View (out of combat).
    // Per §5.10 (approved 2026-06-02, pending Notion lock): adds the TM's move to the Learned Move Pool
    // (deduplicates if already present). The Mastery slot is a separate field and is inherently exempt
    // (§4.3.9.2). The TM is consumed (single-use) on success. Pure C#.
    public static class TMApplicator
    {
        // §5.4.1 — a TM may only be applied to a species in its CompatibleSpecies list.
        public static bool IsCompatible(TMSO tm, PokemonInstance mon)
            => tm?.CompatibleSpecies != null && mon?.Species != null && tm.CompatibleSpecies.Contains(mon.Species);

        // §5.10.1 — adds the TM's move to the Learned Move Pool (dedups if already learned). Consumes
        // the TM. Returns false (no change) on incompatibility or if the move is already in the pool.
        public static bool Apply(RunStateSO run, TMSO tm, PokemonInstance mon)
        {
            if (run == null || tm == null || tm.MoveTeach == null || mon == null) return false;
            if (!IsCompatible(tm, mon)) return false;
            if (mon.LearnedMoves.Contains(tm.MoveTeach)) return false; // already in pool (§5.10.1 dedup)

            MoveLoadoutService.AddToPool(mon, tm.MoveTeach);
            run.OwnedTMs?.Remove(tm); // §5.4.1 — single-use
            return true;
        }
    }
}
