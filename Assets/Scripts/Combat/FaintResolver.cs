using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per Epic 4 Task 4.8 + §3.3.5 — central faint resolution API.
    //
    // Stateless. Operates on the inputs the CombatController will pass in:
    // the team list (read-only), the Skill Deck + discard pile (mutable),
    // and the fainted PokemonInstance itself. This keeps Epic 4 free of
    // any Deck/Team class dependency (those land in Epics 5 and 6).
    //
    // Hard rules enforced:
    //   • CurrentHP == 0 IS the fainted state — never an IsFainted flag (§2.4.1).
    //   • Faint overrides Freeze position-lock (§3.3.5.1). IsSlotLockedForSwap
    //     returns false when the occupant is fainted, regardless of status.
    //   • Card purge removes the fainted Pokémon's contributed moves from BOTH
    //     the active skill deck AND the discard pile (§4.8.4).
    //   • +1 Trauma stack applied at moment of faint (§6.2.2).
    //   • All-Faint check triggers Defeat (§3.3.6).
    public static class FaintResolver
    {
        // True iff CurrentHP == 0 — the single source of truth (§2.4.1).
        public static bool IsFainted(PokemonInstance p)
        {
            return p != null && p.CurrentHP == 0;
        }

        // Per §3.3.5.1 — faint precedence over Freeze position-lock.
        // Returns true only when the occupant is alive AND status-locked.
        // Empty slots are NOT locked (return false).
        public static bool IsSlotLockedForSwap(PokemonInstance occupant)
        {
            if (occupant == null) return false;
            if (IsFainted(occupant)) return false; // §3.3.5.1
            return StatusModifiers.IsPositionLocked(occupant.PrimaryStatus);
        }

        // Per §4.8.5 — apply +1 Trauma stack at the moment of faint.
        // Idempotency: caller must invoke exactly once per faint event.
        // Returns the new stack count (0 if input is null).
        public static int ApplyTraumaOnFaint(PokemonInstance fainted)
        {
            if (fainted == null) return 0;
            fainted.TraumaStacks += 1;
            return fainted.TraumaStacks;
        }

        // Per §4.8.4 — purge every card whose Owner == fainted from both lists.
        // Iterates from the back to keep indices stable during removal.
        // Returns the total number of cards removed across both piles.
        public static int PurgeCards(PokemonInstance fainted,
                                     IList<CardEntry> skillDeck,
                                     IList<CardEntry> discardPile)
        {
            if (fainted == null) return 0;
            int removed = 0;
            if (skillDeck != null)
            {
                for (int i = skillDeck.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(skillDeck[i].Owner, fainted))
                    {
                        skillDeck.RemoveAt(i);
                        removed++;
                    }
                }
            }
            if (discardPile != null)
            {
                for (int i = discardPile.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(discardPile[i].Owner, fainted))
                    {
                        discardPile.RemoveAt(i);
                        removed++;
                    }
                }
            }
            return removed;
        }

        // Per §3.3.6 — defeat triggers when every active-team member is fainted.
        // Empty / null entries DO count as "absent" (treated as faint-equivalent
        // for defeat purposes): a team of all-null is defeated.
        public static bool IsAllFainted(IReadOnlyList<PokemonInstance> activeTeam)
        {
            if (activeTeam == null || activeTeam.Count == 0) return true;
            for (int i = 0; i < activeTeam.Count; i++)
            {
                PokemonInstance p = activeTeam[i];
                if (p == null) continue;          // empty slot — counts as absent
                if (p.CurrentHP > 0) return false; // any survivor → not defeated
            }
            return true;
        }

        // Per §4.8.1 — the candidate list for the Lead-replacement prompt:
        // every non-null, non-fainted bench member. The player picks one of
        // these at NO AP cost (CombatController enforces the no-cost rule).
        public static List<PokemonInstance> EligibleLeadReplacements(
            IReadOnlyList<PokemonInstance> activeTeam, PokemonInstance currentLead)
        {
            List<PokemonInstance> result = new();
            if (activeTeam == null) return result;
            for (int i = 0; i < activeTeam.Count; i++)
            {
                PokemonInstance p = activeTeam[i];
                if (p == null) continue;
                if (ReferenceEquals(p, currentLead)) continue;
                if (IsFainted(p)) continue;
                result.Add(p);
            }
            return result;
        }
    }
}
