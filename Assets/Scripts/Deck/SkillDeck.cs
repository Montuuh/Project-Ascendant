using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Deck
{
    // Per §3.4 + Epic 5 Task 5.1.1 — the shared per-combat Skill Deck.
    //
    // Built at combat start from each Active Pokémon's 4 moves (12 baseline)
    // plus their Mastery Move if one is unlocked (+1 per mastered Pokémon —
    // max 15 cards at full mastery). Cards are tagged with their owner so
    // §3.3.5 / §4.8.4 faint purge can sweep contributed cards out of the
    // deck and discard pile in O(N).
    //
    // Ownership rules:
    //   • SkillDeck owns the deck list and discard pile. UI / controller
    //     read via DeckView / DiscardView (read-only) or via Count properties.
    //   • The hand list is owned externally (CombatController) — Draw()
    //     returns a single card and the caller places it in their hand;
    //     Discard(card) re-enters the discard pile when a card is played
    //     or TurnEnd dumps the unplayed hand.
    //
    // Per §3.4 — "Cards played from hand go to the discard pile. When the
    // Skill Deck empties, the discard pile reshuffles into it." Draw()
    // handles the reshuffle inline so callers never see an empty deck while
    // discards remain.
    //
    // Per §9.17 — MoveCardInstance uses new+GC for VS (no pool); the
    // injected MoveCardInstanceFactory is the only allocation point and the
    // only release point. Tests can inject a factory; production wires via
    // Services.Resolve in CombatController construction.
    public sealed class SkillDeck
    {
        private readonly List<MoveCardInstance> _deck = new();
        private readonly List<MoveCardInstance> _discard = new();
        private readonly MoveCardInstanceFactory _factory;

        public SkillDeck(MoveCardInstanceFactory factory = null)
        {
            _factory = factory ?? new MoveCardInstanceFactory();
        }

        public int DeckCount => _deck.Count;
        public int DiscardCount => _discard.Count;

        // Read-only views for UI / tests. Mutating the underlying list via a
        // downcast is a caller bug; treat these as snapshots.
        public IReadOnlyList<MoveCardInstance> DeckView => _deck;
        public IReadOnlyList<MoveCardInstance> DiscardView => _discard;

        // Per §3.4 Task 5.1.1 + Task 5.1.3 — build deck from active team.
        // Releases any prior cards via factory, then walks each non-null
        // Pokémon's CurrentMoves (4 slots) + optional MasteryMove.
        // Skipped slots: null move references and null Pokémon.
        // `unlockedMasteryIds`: the MoveIds the player has unlocked via meta-progression (§4.3.9.2).
        // A Pokémon's Mastery card is added ONLY if its MoveId is in this set — null/empty ⇒ no
        // Mastery cards (locked by default). Wired from MetaProgressionSO.UnlockedMasteryMoveIds.
        public void Build(IList<PokemonInstance> activeTeam,
            System.Collections.Generic.ICollection<string> unlockedMasteryIds = null)
        {
            Clear();
            if (activeTeam == null) return;
            for (int i = 0; i < activeTeam.Count; i++)
            {
                PokemonInstance p = activeTeam[i];
                if (p == null) continue;
                for (int m = 0; m < p.CurrentMoves.Count; m++)
                {
                    MoveSO move = p.CurrentMoves[m];
                    if (move == null) continue;
                    _deck.Add(_factory.Create(move, p, isMasteryMove: false));
                }
                // Per §4.3.9.2 — the Mastery card is drawn only once the move is unlocked in meta.
                if (p.MasteryMove != null && unlockedMasteryIds != null && unlockedMasteryIds.Contains(p.MasteryMove.MoveId))
                    _deck.Add(_factory.Create(p.MasteryMove, p, isMasteryMove: true));
            }
        }

        // Per §3.2.2 + §3.4 — draw one card from the deck. Reshuffles the
        // discard pile back into the deck when the deck is empty. Returns
        // null only if both the deck AND the discard pile are empty
        // (true exhaustion — caller treats as a no-op draw).
        //
        // Drawn cards do NOT enter the discard pile here — the caller adds
        // them to the hand. The card returns to discard only when played
        // (Discard) or TurnEnd dumps unplayed hand cards back. This prevents
        // the self-feeding reshuffle loop where a drawn card returns to the
        // deck within the same turn.
        public MoveCardInstance Draw(GameRNG rng)
        {
            if (_deck.Count == 0) ReshuffleDiscardIntoDeck();
            if (_deck.Count == 0) return null;
            int idx = rng.Range(0, _deck.Count);
            MoveCardInstance card = _deck[idx];
            _deck.RemoveAt(idx);
            return card;
        }

        // Per §3.4 — played cards go to the discard pile.
        public void Discard(MoveCardInstance card)
        {
            if (card == null) return;
            _discard.Add(card);
        }

        // Per §3.4 — empty-deck reshuffle. Public so the controller can
        // trigger an early shuffle for testing / relic effects.
        public void ReshuffleDiscardIntoDeck()
        {
            if (_discard.Count == 0) return;
            _deck.AddRange(_discard);
            _discard.Clear();
        }

        // Per §3.3.5 + §4.8.4 — purge every card whose Owner == fainted from
        // BOTH the deck and the discard pile. Returns the total number of
        // cards removed across both lists. Releases purged instances back
        // through the factory (no-op for VS per §9.17, but the call site is
        // ready for post-VS pooling).
        //
        // Hand-state sweep is a separate concern owned by the controller
        // (Task 5.5.2 — greyed-out, not hidden). See FaintResolver.PurgeCards
        // for the legacy raw-list API still used by EditMode tests.
        public int PurgeOwner(PokemonInstance fainted)
        {
            if (fainted == null) return 0;
            int removed = 0;
            for (int i = _deck.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_deck[i].Owner, fainted))
                {
                    _factory.Release(_deck[i]);
                    _deck.RemoveAt(i);
                    removed++;
                }
            }
            for (int i = _discard.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_discard[i].Owner, fainted))
                {
                    _factory.Release(_discard[i]);
                    _discard.RemoveAt(i);
                    removed++;
                }
            }
            return removed;
        }

        // Release all cards and clear both lists. Called at combat end (or
        // by Build before a fresh setup).
        public void Clear()
        {
            for (int i = 0; i < _deck.Count; i++) _factory.Release(_deck[i]);
            for (int i = 0; i < _discard.Count; i++) _factory.Release(_discard[i]);
            _deck.Clear();
            _discard.Clear();
        }
    }
}
