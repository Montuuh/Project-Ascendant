using ProjectAscendant.Core;

namespace ProjectAscendant.Combat
{
    // Per §3.3.1 + Epic 6 Task 6.1 — manual-swap cost ladder + counter
    // discipline. Stateless utility; mutates the supplied CombatState only
    // through the TryManualSwap entry point so the contract is testable
    // without spinning up a full CombatController.
    //
    // Hard rules enforced (all from §3.3.1 / §3.3.5.1):
    //   • Manual swap cost ladder: 1st = 1 AP, 2nd = 2 AP, 3rd = 3 AP …
    //     (counter resets to 0 each DrawPhase — the controller owns reset).
    //   • Only manual swaps increment SwapCounter. SF / SB go through
    //     CardPlayService and never reach this code path.
    //   • Only manual swaps arm DefensiveSwapDiscountAvailable.
    //   • Faint precedence: a fainted Lead is NOT position-locked even if
    //     they were Frozen (§3.3.5.1). FaintResolver.IsSlotLockedForSwap
    //     returns false for fainted occupants, so Frozen+fainted Lead can
    //     legally swap — but in practice the controller's faint-replacement
    //     path runs first and the manual-swap branch never sees that case.
    //   • Target validation: bench slot must be in range, not the current
    //     Lead, occupant must exist and be non-fainted.
    public static class SwapManager
    {
        // Cost of the Nth manual swap of the turn (1-indexed): swapCounter
        // is the number of manual swaps ALREADY made this turn (0 at turn
        // start). The next swap costs `swapCounter + 1` AP.
        // Per §3.3.1 — 1st=1AP, 2nd=2AP, 3rd=3AP. No upper bound documented;
        // the formula continues monotonically (Nth = N AP).
        public static int NextSwapCost(int swapCounter)
        {
            if (swapCounter < 0) swapCounter = 0;
            return swapCounter + 1;
        }

        // Eligibility for a manual swap to `benchSlot`. Pure check — no
        // mutations. Used by UI hover preview and by TryManualSwap to gate.
        // currentLead is supplied separately because the state may carry
        // null PlayerTeam[LeadIndex] entries (defensive — empty slots).
        public static bool CanManualSwap(int currentLeadIndex,
                                         int benchSlot,
                                         System.Collections.Generic.IReadOnlyList<PokemonInstance> playerTeam,
                                         int currentAP,
                                         int swapCounter)
        {
            if (playerTeam == null) return false;
            if (benchSlot < 0 || benchSlot >= playerTeam.Count) return false;
            if (benchSlot == currentLeadIndex) return false;

            // Per §3.3.5.1 — Frozen Lead position-lock blocks manual swap
            // (unless fainted, in which case faint precedence applies and
            // the controller routes through PickLeadReplacement, not here).
            PokemonInstance lead =
                currentLeadIndex >= 0 && currentLeadIndex < playerTeam.Count
                    ? playerTeam[currentLeadIndex]
                    : null;
            if (FaintResolver.IsSlotLockedForSwap(lead)) return false;

            PokemonInstance target = playerTeam[benchSlot];
            if (target == null) return false;
            if (target.CurrentHP <= 0) return false;

            int cost = NextSwapCost(swapCounter);
            if (cost > currentAP) return false;

            return true;
        }

        // Per §3.3.1 — performs the manual swap. Mutates state ONLY if the
        // swap is legal; on success: subtracts cost from CurrentAP, bumps
        // SwapCounter, repoints LeadIndex, and arms the defensive discount.
        //
        // Returns true if the swap happened, false if rejected. Callers
        // should treat false as a non-fatal no-op (the action loop keeps
        // going so the player can pick something else).
        public static bool TryManualSwap(CombatController.CombatState state, int benchSlot)
        {
            if (state == null) return false;
            if (!CanManualSwap(state.LeadIndex, benchSlot, state.PlayerTeam,
                               state.CurrentAP, state.SwapCounter))
                return false;

            int cost = NextSwapCost(state.SwapCounter);
            // §8.3.4 Tactician's Coin — the first manual swap each combat costs 0 AP.
            if (state.ManualSwapsThisCombat == 0 && RelicResolver.Holds(state.ActiveRelics, "tacticians_coin"))
                cost = 0;
            state.CurrentAP -= cost;
            state.SwapCounter += 1;
            state.ManualSwapsThisCombat += 1;
            state.LeadIndex = benchSlot;

            // Per §3.3.1 — manual swap arms the defensive-swap discount for
            // the first Defensive card played this turn. SF/SB do NOT call
            // this method, so they correctly don't set this flag.
            state.DefensiveSwapDiscountAvailable = true;

            // §8.3.3 Defense Curl Charm — every 3rd manual swap, +1 Defense on the new Lead.
            if (RelicResolver.Holds(state.ActiveRelics, "defense_curl_charm")
                && state.ManualSwapsThisCombat % 3 == 0
                && benchSlot >= 0 && benchSlot < state.PlayerTeam.Count)
                StatStageManager.Modify(state.PlayerTeam[benchSlot], Stat.Defense, 1);

            // Per R4-4 — log manual swap.
            string newLeadName = state.PlayerTeam[benchSlot]?.Species?.DisplayName ?? "???";
            state.CombatLog.Add(new CombatController.CombatLogEntry(
                CombatController.CombatLogCategory.PlayerAction,
                $"Swapped to {newLeadName} (cost: {cost} AP)"));

            AbilityResolver.ApplyLeadEntryEffects(state); // §5.5.3.5 Intimidate
            return true;
        }
    }
}
