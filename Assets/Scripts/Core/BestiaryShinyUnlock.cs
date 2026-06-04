namespace ProjectAscendant.Core
{
    // Per §4.3.9.1 (Veteran tier) — the second rung of the Pokédex tier-reward ladder: defeating a
    // species enough to reach the Veteran tier (30 / 15 / 5 / 5 kills by rarity, tunable) permanently
    // unlocks its Shiny variant, so every owned Pokémon of that species displays Shiny.
    //
    // Full ladder (§4.3.9.1):
    //   Familiar → type-chart reveal (BestiaryProgressSO.TypeChartRevealed)
    //   Veteran  → Shiny variant unlock        (here)
    //   Master   → Mastery Move unlock         (BestiaryMasteryUnlock)
    //
    // Pure C#; the caller persists Meta at the §6.10 trigger (run-end SaveMeta). Idempotent.
    public static class BestiaryShinyUnlock
    {
        // Returns true iff this call newly unlocked the species' Shiny variant (caller should log it).
        public static bool TryUnlockShiny(BestiaryProgressSO bestiary, MetaProgressionSO meta,
                                          PokemonSpeciesSO species)
        {
            if (bestiary == null || meta == null || species == null) return false;
            if (string.IsNullOrEmpty(species.SpeciesId)) return false;
            if (bestiary.TierFor(species.SpeciesId) < BestiaryMasteryTier.Veteran) return false;
            return meta.UnlockShiny(species.SpeciesId);
        }
    }
}
