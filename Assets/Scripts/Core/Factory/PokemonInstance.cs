using System.Collections.Generic;

namespace ProjectAscendant.Core
{
    // Per §9.3.2.6 — runtime Pokémon state. Plain C# class; poolable via PokemonInstanceFactory.
    // Lifecycle: Box entry = factory.Create(); Box release = factory.Release().
    public sealed class PokemonInstance
    {
        public PokemonSpeciesSO Species;
        public int Level;
        public int CurrentHP;              // 0 = fainted — never add IsFainted. (§2.4.1)
        public int CurrentXP;
        public int TraumaStacks;           // §6.2
        public readonly List<MoveSO> CurrentMoves = new(4); // 4 slots; mutated by evolution/TM
        public MoveSO MasteryMove;         // 5th slot — immutable per §4.3.9.2
        public AbilitySO Ability;
        public HeldItemSO HeldItem;
        public readonly Dictionary<Stat, int> StatStages = new();
        public StatusCondition PrimaryStatus;
        public StatusCondition SecondaryStatus; // Confusion only at launch

        // Per §4.2 + Task 4.5.1 — duration tracking.
        // Sentinel: int.MaxValue = permanent (Burn/Poison). 0 = no status.
        // Otherwise the remaining turn count (decremented by TickAtEndOfTurn).
        public int PrimaryStatusTurnsRemaining;
        public int SecondaryStatusTurnsRemaining;

        // Per §4.3.3 — per-encounter cooldown tracking for AI intent gating.
        // Key = MoveSO ref; value = remaining turn count (>0 = on cooldown).
        // Populated by CombatController after a Move-bearing intent resolves
        // for moves whose CooldownTurns > 0. Decremented end-of-turn via
        // TickMoveCooldowns. Cleared in Reset() so each combat starts fresh.
        public readonly Dictionary<MoveSO, int> MoveCooldowns = new();

        public EvolutionStage CurrentStage;
        // Per §5.3.3 — SO reference to the chosen branch; null until first evolution.
        public EvolutionBranchSO SelectedBranch;

        // Per §4.4.3 — boss/Elite multi-phase depth. 1 (default) = no phase
        // behaviour: ordinary wild/trainer Pokémon never escalate. 2 = the
        // standard two-phase template (Elite Trainers, §7.5.1). 3 = ace
        // three-phase template (Gym aces, Epic 8 Task 8.5). Set at materialise
        // time by the encounter controller; phases themselves are derived from
        // CurrentHP via BossPhaseTracker — there is no stored "current phase".
        public int PhaseCount = 1;

        // Called by PokemonInstanceFactory.Release() before returning to pool.
        // Clears collections in-place to avoid re-allocating them on reuse.
        public void Reset()
        {
            Species = null;
            Level = 0;
            CurrentHP = 0;
            CurrentXP = 0;
            TraumaStacks = 0;
            CurrentMoves.Clear();
            MasteryMove = null;
            Ability = null;
            HeldItem = null;
            StatStages.Clear();
            PrimaryStatus = StatusCondition.None;
            SecondaryStatus = StatusCondition.None;
            PrimaryStatusTurnsRemaining = 0;
            SecondaryStatusTurnsRemaining = 0;
            MoveCooldowns.Clear();
            CurrentStage = EvolutionStage.Basic;
            SelectedBranch = default;
            PhaseCount = 1; // §4.4.3 — pooled instances default back to no-phase
        }

        // Per §4.3.3 — set/check/tick helpers for AI cooldown gate.
        // Set: only called for moves whose MoveSO.CooldownTurns > 0.
        // IsOnCooldown: false if move is null, missing, or count <= 0.
        // Tick: decrement every tracked move by 1; remove entries that hit 0
        // so the dictionary stays bounded by the active cooldown set.
        public void SetMoveCooldown(MoveSO move, int turns)
        {
            if (move == null || turns <= 0) return;
            MoveCooldowns[move] = turns;
        }

        public bool IsMoveOnCooldown(MoveSO move)
        {
            if (move == null) return false;
            return MoveCooldowns.TryGetValue(move, out int remaining) && remaining > 0;
        }

        public void TickMoveCooldowns()
        {
            if (MoveCooldowns.Count == 0) return;
            // Iterate via a temp key list to avoid mutate-during-enumeration.
            // Allocation is small (active cooldown count is bounded by hand size).
            List<MoveSO> keys = new(MoveCooldowns.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                MoveSO k = keys[i];
                int next = MoveCooldowns[k] - 1;
                if (next <= 0) MoveCooldowns.Remove(k);
                else MoveCooldowns[k] = next;
            }
        }
    }
}
