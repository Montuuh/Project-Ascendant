namespace ProjectAscendant.Core
{
    // Per §9.6.1 — runtime card representation used by Deck/Hand systems.
    // Per §9.17 — MoveCardInstance uses new+GC for VS (pool deferred post-VS per profiling).
    //
    // Per Epic 5 Task 5.1.3 — every card tracks its OwnerPokemonId so the
    // §3.3.5 faint purge can remove the fainted Pokémon's contributed cards
    // from deck + discard + hand. The "Id" in the GDD wording is satisfied
    // by a direct PokemonInstance reference here — purge uses ReferenceEquals
    // (see FaintResolver / SkillDeck.PurgeOwner). If a stable serializable
    // identifier is needed later (save mid-combat, replay), add an int Id
    // alongside Owner; the reference stays load-bearing for runtime purge.
    //
    // IsMasteryMove flags the +1 card per mastered Pokémon (§3.4 / §4.3.9).
    // The deck builder sets this; UI / relic hooks read it to differentiate
    // Mastery cards (different art frame, etc.).
    public sealed class MoveCardInstance
    {
        public MoveSO Move;
        public PokemonInstance Owner;
        public bool IsMasteryMove;
        public bool IsExhausted;

        public void Reset()
        {
            Move = null;
            Owner = null;
            IsMasteryMove = false;
            IsExhausted = false;
        }
    }
}
