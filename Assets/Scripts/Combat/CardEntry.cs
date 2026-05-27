using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per Epic 4 Task 4.8.4 + Epic 5 (forward) — minimal card-entry tuple
    // pairing a MoveSO with the PokemonInstance that contributed it to the
    // shared Skill Deck.
    //
    // Epic 5 (Deck) will likely promote this to a richer CardInstance class
    // with cooldowns, transient buffs, etc. — at that point, callers update
    // the pooled list type and FaintResolver.PurgeCards stays unchanged
    // (it only reads Owner).
    //
    // Plain struct, value-semantic; safe to store in IList<CardEntry>.
    public struct CardEntry
    {
        public MoveSO Move;
        public PokemonInstance Owner;

        public CardEntry(MoveSO move, PokemonInstance owner)
        {
            Move = move;
            Owner = owner;
        }
    }
}
