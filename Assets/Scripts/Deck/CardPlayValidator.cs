using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Deck
{
    // Per §3.3 / §3.6 + Epic 5 Task 5.6.1 — pure-function validation of skill
    // card play against the Move Tag Taxonomy (Role / Range / PositionalModifier).
    //
    // Stateless. The full CardPlayService (Task 5.4) calls into this and then
    // performs side effects (AP spend, hand→discard, damage resolve). Tests
    // hit this surface directly for fast, scene-free coverage.
    //
    // Hard rules enforced:
    //   • Melee cards play only from the Lead slot — unless they carry
    //     Step-Forward (§3.3.2), which promotes the bench owner to Lead at
    //     resolution.
    //   • Ranged cards play from any slot (§3.3 / §3.6).
    //   • Step-Backward is Melee-only by spec (§3.3.4); but the play-position
    //     check treats it like a plain Melee card — SB swaps AFTER the effect,
    //     so the owner must already be Lead when played.
    //   • Cards from a fainted or absent owner are unplayable (§3.4).
    //   • A move's owner must still be in the active team — if a Pokémon was
    //     released between Build and Play, its cards leave play (defensive).
    public static class CardPlayValidator
    {
        // Why an enum: returning a single bool hides which rule failed, and the
        // UI needs the reason to grey + tooltip the card (Task 5.5.2). Each
        // value maps to a specific §-reference.
        public enum PlayResult
        {
            Playable,
            NullCard,                  // defensive: card or its Move is null
            OwnerAbsent,               // owner is null or not in active team
            OwnerFainted,              // owner.CurrentHP == 0 (§2.4.1)
            MeleeFromBenchNoSF,        // §3.3.2 — Melee + bench + no Step-Forward
        }

        // Per §3.3 + §3.6 — the full eligibility check. Composes the three
        // rules in order of cheapness so the first failure short-circuits.
        public static PlayResult Validate(MoveCardInstance card,
                                          IReadOnlyList<PokemonInstance> activeTeam,
                                          int leadIndex)
        {
            if (card == null || card.Move == null) return PlayResult.NullCard;
            PokemonInstance owner = card.Owner;
            if (owner == null) return PlayResult.OwnerAbsent;
            int ownerIdx = IndexOf(activeTeam, owner);
            if (ownerIdx < 0) return PlayResult.OwnerAbsent;
            if (owner.CurrentHP == 0) return PlayResult.OwnerFainted;

            if (card.Move.Range == MoveRange.Melee
                && ownerIdx != leadIndex
                && card.Move.Modifier != PositionalModifier.StepForward)
            {
                return PlayResult.MeleeFromBenchNoSF;
            }
            return PlayResult.Playable;
        }

        // Convenience for UI hover-state queries: true iff the card would pass
        // the position check. Skips owner-absent / owner-fainted (those are
        // shown via different UI affordances). Used by CardPlayValidatorTests
        // to isolate the play-position rule.
        public static bool IsPositionEligible(MoveSO move, int ownerSlot, int leadIndex)
        {
            if (move == null) return false;
            if (move.Range == MoveRange.Ranged) return true;
            if (ownerSlot == leadIndex) return true;
            return move.Modifier == PositionalModifier.StepForward;
        }

        // Per §3.3.1 — manual-swap defensive discount. Pure math: returns the
        // base AP cost minus 1 (floored at 0) iff the move is Defensive AND a
        // discount is currently available. Does NOT consume the discount —
        // call ConsumeDefensiveDiscount separately.
        //
        // Splitting compute-from-consume keeps the UI hover preview accurate
        // without having to roll back state.
        public static int ApplyDefensiveDiscount(int baseAPCost, MoveSO move,
                                                 bool discountAvailable)
        {
            if (move == null || !discountAvailable) return baseAPCost;
            if (move.Role != MoveRole.Defensive) return baseAPCost;
            int reduced = baseAPCost - 1;
            return reduced < 0 ? 0 : reduced;
        }

        // Per §3.3.1 — the discount is consumed by the first Defensive-tagged
        // card played after a manual swap, regardless of whether the reduction
        // actually saved AP (a cost-0 card still "uses" the discount slot).
        // SF/SB swaps don't grant the discount (TryManualSwap handles the set).
        public static bool ShouldConsumeDefensiveDiscount(MoveSO move,
                                                          bool discountAvailable)
        {
            if (move == null || !discountAvailable) return false;
            return move.Role == MoveRole.Defensive;
        }

        // Linear lookup is fine — active team is at most 3 Pokémon.
        private static int IndexOf(IReadOnlyList<PokemonInstance> team, PokemonInstance p)
        {
            if (team == null) return -1;
            for (int i = 0; i < team.Count; i++)
                if (ReferenceEquals(team[i], p)) return i;
            return -1;
        }
    }
}
