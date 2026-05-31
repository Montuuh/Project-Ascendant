using ProjectAscendant.Core;

namespace ProjectAscendant.Progression
{
    // Per §5.4.1 + Epic 10 Task 10.6 — apply a TM to a Pokémon from the Map View (out of combat).
    // VS replace-a-slot model (the §5.10 additive Learned Move Pool is unbuilt, gap #36): the taught
    // move replaces a chosen CurrentMoves slot. The Mastery slot is a separate field and is therefore
    // inherently exempt (§4.3.9.2). The TM is consumed (single-use) on success. Pure C#.
    public static class TMApplicator
    {
        // §5.4.1 — a TM may only be applied to a species in its CompatibleSpecies list.
        public static bool IsCompatible(TMSO tm, PokemonInstance mon)
            => tm?.CompatibleSpecies != null && mon?.Species != null && tm.CompatibleSpecies.Contains(mon.Species);

        // Replaces CurrentMoves[slotIndex] with the TM's move and consumes the TM. Returns false (no
        // change) on incompatibility, a bad slot, or if the move is already known.
        public static bool Apply(RunStateSO run, TMSO tm, PokemonInstance mon, int slotIndex)
        {
            if (run == null || tm == null || tm.MoveTeach == null || mon == null) return false;
            if (!IsCompatible(tm, mon)) return false;
            if (slotIndex < 0 || slotIndex >= mon.CurrentMoves.Count) return false;
            if (mon.CurrentMoves.Contains(tm.MoveTeach)) return false; // no duplicate

            mon.CurrentMoves[slotIndex] = tm.MoveTeach;
            run.OwnedTMs?.Remove(tm); // §5.4.1 — single-use
            return true;
        }
    }
}
