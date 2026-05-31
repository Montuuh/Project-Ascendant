using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §4.3.4 + Epic 4 Task 4.7.C — pure target-resolution helpers for
    // intents that resolve against the player's team layout.
    //
    // PlayerTeam holds the active Pokémon by stable identity slot; the Lead is whichever slot
    // State.LeadIndex points at (it floats as the player swaps — see SwapManager). A "Lead-targeted"
    // intent re-resolves through Intent.EffectiveTargetSlot(LeadIndex), so callers pass the CURRENT
    // lead slot here. null entries are empty slots; fainted entries have CurrentHP == 0.
    //
    // Resolution semantics (the load-bearing rules):
    //   • Cleave NEVER fizzles. Hits every occupied non-fainted slot.
    //     If only 1 alive occupant remains, hits exactly that one. (§4.3.4.1)
    //   • Backstrike fizzles silently on empty/fainted slot — does NOT
    //     redirect to Lead. (§4.3.4.1)
    public static class IntentTargeting
    {
        // Per §4.3.4 — Cleave target set. Returns the slot indices of every
        // alive occupant in the player team. Empty list iff team is entirely
        // empty or fully fainted (caller should have triggered defeat already).
        public static List<int> ResolveCleaveTargets(IReadOnlyList<PokemonInstance> playerTeam)
        {
            List<int> targets = new();
            if (playerTeam == null) return targets;
            for (int i = 0; i < playerTeam.Count; i++)
            {
                PokemonInstance p = playerTeam[i];
                if (p == null) continue;
                if (p.CurrentHP <= 0) continue;
                targets.Add(i);
            }
            return targets;
        }

        // Per §4.3.4.1 — Backstrike target slot. Returns the requested slot
        // index iff a NON-FAINTED occupant is currently in it; returns -1
        // (fizzle) on empty or fainted occupant. Never redirects to Lead.
        public static int ResolveBackstrikeTarget(int requestedSlot,
                                                  IReadOnlyList<PokemonInstance> playerTeam)
        {
            if (playerTeam == null) return -1;
            if (requestedSlot < 0 || requestedSlot >= playerTeam.Count) return -1;
            PokemonInstance occupant = playerTeam[requestedSlot];
            if (occupant == null) return -1;
            if (occupant.CurrentHP <= 0) return -1;
            return requestedSlot;
        }

        // Per §4.3.2 — for any slot-targeted intent (Attack/Status), resolve
        // the slot's CURRENT occupant. Returns null on empty/fainted slot
        // (caller decides whether that constitutes a fizzle — Status fizzles,
        // Attack fizzles, Backstrike has its own helper above).
        public static PokemonInstance ResolveSlotOccupant(int slot,
                                                          IReadOnlyList<PokemonInstance> playerTeam)
        {
            if (playerTeam == null) return null;
            if (slot < 0 || slot >= playerTeam.Count) return null;
            PokemonInstance p = playerTeam[slot];
            if (p == null || p.CurrentHP <= 0) return null;
            return p;
        }
    }
}
