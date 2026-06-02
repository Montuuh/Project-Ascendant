using System.Collections.Generic;
using ProjectAscendant.Core;

namespace ProjectAscendant.Progression
{
    // Per §5.10 (approved 2026-06-02, pending Notion lock) — service layer for managing the Learned
    // Move Pool + active-4 configuration. Pure C# logic layer; UI never mutates PokemonInstance.LearnedMoves
    // or CurrentMoves directly — all changes go through this service. Enforces:
    //   • Active 4 must be chosen from the pool (validation).
    //   • Mastery slot is immutable and not part of active-4 selection (§4.3.9.2).
    //   • Pool additions are deduplicated (no duplicate entries).
    //   • Evolution in-place upgrades preserve active slots (§5.10.3).
    //   • Paid reconfigure gate (deduct from RunState; §5.10.2).
    public static class MoveLoadoutService
    {
        // §5.10.3 — in-place upgrade: replaces oldMove with newMove in the pool. If oldMove was in the
        // active 4, newMove takes that slot automatically. Returns false if oldMove is not in the pool.
        public static bool UpgradePoolMove(PokemonInstance pokemon, MoveSO oldMove, MoveSO newMove)
        {
            if (pokemon == null || oldMove == null || newMove == null) return false;
            bool changed = false;

            // Replace in the pool if present.
            int poolIdx = pokemon.LearnedMoves.IndexOf(oldMove);
            if (poolIdx >= 0) { pokemon.LearnedMoves[poolIdx] = newMove; changed = true; }

            // If the old move was active, reslot the new move into that same active slot (§5.10.3).
            // Defensive: an evolution upgrade of an ACTIVE move must take effect even if the pool and
            // active sets ever drift (e.g. CurrentMoves seeded independently of LearnedMoves) — the
            // active upgrade is the player-visible promise of evolution and must never silently no-op.
            int activeIdx = pokemon.CurrentMoves.IndexOf(oldMove);
            if (activeIdx >= 0) { pokemon.CurrentMoves[activeIdx] = newMove; changed = true; }

            // Maintain the CurrentMoves ⊆ pool invariant: the upgraded move must exist in the pool.
            if (changed && !pokemon.LearnedMoves.Contains(newMove)) pokemon.LearnedMoves.Add(newMove);

            return changed; // false only when oldMove was neither in the pool nor active
        }

        // §5.10.1 — add a move to the pool. Deduplicates: if the move is already in the pool, no-op.
        // Does NOT add to CurrentMoves automatically (player configures active 4 explicitly).
        public static void AddToPool(PokemonInstance pokemon, MoveSO move)
        {
            if (pokemon == null || move == null) return;
            if (pokemon.LearnedMoves.Contains(move)) return; // dedup per §5.10.1
            pokemon.LearnedMoves.Add(move);
        }

        // §5.10.2 — validate that a move is in the pool. Returns false if the move is null or not learned.
        public static bool IsInPool(PokemonInstance pokemon, MoveSO move)
        {
            if (pokemon == null || move == null) return false;
            return pokemon.LearnedMoves.Contains(move);
        }

        // §5.10.2 + §4.3.9.2 — validate that exactly 4 moves are provided, all are in the pool, and the
        // Mastery slot is not included (Mastery is a separate field, immutable). Returns false if invalid.
        public static bool ValidateActiveLoadout(PokemonInstance pokemon, List<MoveSO> proposedActive)
        {
            if (pokemon == null || proposedActive == null) return false;
            if (proposedActive.Count != 4) return false; // must be exactly 4

            // Check each move is in the pool and is not the Mastery move (§4.3.9.2)
            for (int i = 0; i < proposedActive.Count; i++)
            {
                MoveSO m = proposedActive[i];
                if (m == null) return false;
                if (!pokemon.LearnedMoves.Contains(m)) return false; // not in pool
                if (m == pokemon.MasteryMove) return false; // Mastery is immutable, not part of active 4
            }

            return true;
        }

        // §5.10.2 — set the active 4 from a validated loadout. Replaces CurrentMoves entirely.
        // Returns false if the loadout is invalid (use ValidateActiveLoadout first).
        public static bool SetActiveMoves(PokemonInstance pokemon, List<MoveSO> newActive)
        {
            if (!ValidateActiveLoadout(pokemon, newActive)) return false;

            pokemon.CurrentMoves.Clear();
            for (int i = 0; i < newActive.Count; i++)
                pokemon.CurrentMoves.Add(newActive[i]);

            return true;
        }

        // §5.10.2 — swap one active move for a pool move. Validates that fromPool is in the pool and
        // toReplaceInActive is in CurrentMoves (and not the Mastery). Returns false if invalid.
        public static bool SwapActiveMove(PokemonInstance pokemon, MoveSO fromPool, MoveSO toReplaceInActive)
        {
            if (pokemon == null || fromPool == null || toReplaceInActive == null) return false;
            if (!pokemon.LearnedMoves.Contains(fromPool)) return false; // not in pool
            if (fromPool == pokemon.MasteryMove) return false; // can't activate Mastery (§4.3.9.2)

            int idx = pokemon.CurrentMoves.IndexOf(toReplaceInActive);
            if (idx < 0) return false; // move to replace is not in CurrentMoves

            pokemon.CurrentMoves[idx] = fromPool;
            return true;
        }

        // §5.10 (approved 2026-06-02, pending Notion lock) — paid reconfigure gate. Deducts the
        // MoveReconfigCost from RunState.PokeDollars. Returns true if affordable and deducted; false
        // if the player cannot afford it (no mutation). UI calls this BEFORE allowing reconfiguration;
        // if it returns false, the reconfigure UI is disabled/greyed. Free at Centers and post-evolution
        // (callers skip this check in those contexts per §5.10.2).
        public static bool DeductReconfigCost(RunStateSO runState, EconomyConfigSO economy)
        {
            if (runState == null || economy == null) return false;
            int cost = economy.MoveReconfigCost;
            if (runState.PokeDollars < cost) return false; // unaffordable
            runState.PokeDollars -= cost;
            return true;
        }
    }
}
