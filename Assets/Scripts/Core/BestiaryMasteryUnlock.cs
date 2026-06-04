namespace ProjectAscendant.Core
{
    // Per §4.3.9.1 — the Bestiary's payoff: defeating a species enough times to reach the
    // Master tier (50 / 25 / 10 / 10 kills by rarity, tunable on BestiaryProgressSO) permanently
    // unlocks that species' Mastery Move into MetaProgressionSO.UnlockedMasteryMoveIds — exactly
    // the §4.3.9.1 Master reward ("Unlocks a unique Mastery Move … added to this species' pool").
    //
    // This is the second VS unlock path for Mastery, complementing the evolution path (§4.3.9.2):
    //   • evolve a Pokémon            → unlock its evolved form's Mastery   (EvolutionExecutor)
    //   • master a species in combat  → unlock that species' Mastery        (here)
    //
    // Pure C#; the caller persists Meta at the §6.10 trigger (run-end SaveMeta). Idempotent.
    public static class BestiaryMasteryUnlock
    {
        // Returns true iff this call newly unlocked the species' Mastery Move (caller should log it).
        public static bool TryUnlockMastery(BestiaryProgressSO bestiary, MetaProgressionSO meta,
                                            PokemonSpeciesSO species)
        {
            if (bestiary == null || meta == null || species == null) return false;
            if (species.MasteryMove == null || string.IsNullOrEmpty(species.MasteryMove.MoveId)) return false;
            if (bestiary.TierFor(species.SpeciesId) < BestiaryMasteryTier.Master) return false;
            return meta.UnlockMastery(species.MasteryMove.MoveId);
        }
    }
}
