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
        // Per §5.10 (approved 2026-06-02, pending Notion lock) — the full pool of learned moves.
        // Active 4 are chosen from this pool. Grows via evolution, TMs, and Move Tutors. Never shrinks.
        // Evolution in-place upgrades replace entries by reference. Seed from BaseLearnset on creation.
        public readonly List<MoveSO> LearnedMoves = new(8); // start 4, grow to ~5-8 per §5.10
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

        // ── Boss-encounter runtime (Gym ace, §4.4.3 / §4.4.4.3, Task 8.5) ─────
        // All inert for ordinary Pokémon (PhaseCount 1). Driven by
        // CombatController's phase-transition director + ResolveDamage.

        // Highest phase this Pokémon has reached this combat. Used for one-shot
        // phase-transition detection (evolution @ Phase 2, last-stand @ Phase 3).
        public int LastObservedPhase = 1;

        // Per §4.4.3 Phase 3 — Sturdy: the ace survives the first otherwise-
        // lethal hit at 1 HP. HasSturdy authored on the ace; consumed once.
        public bool HasSturdy;
        public bool SturdyConsumed;

        // Per §8.3.7 (CL-021) — Battle Hardened Legendary: a combat-start damage-absorbing Shield (set
        // each combat in CombatController.Start, consumed before HP in the damage pipeline). Combat-
        // transient — reset at the start of every combat.
        public int ShieldHP;

        // Per §4.4.4.3 — mid-fight evolution: the ace evolves into this species
        // on entering Phase 2 (HP <= 50%). Null = no mid-fight evolution.
        // Per CL-013, Gym aces never set this (rival/Champion only).
        public PokemonSpeciesSO MidFightEvolutionTarget;
        public bool HasEvolvedMidFight;

        // Per §4.4.4.4 (CL-013) — the Gym ace's Phase-2 signature archetype. None for
        // non-ace / non-Gym Pokémon. Drives CombatController's Phase-2 behaviour.
        public Phase2Archetype Phase2Archetype;

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
            LearnedMoves.Clear();
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
            LastObservedPhase = 1;
            HasSturdy = false;
            SturdyConsumed = false;
            MidFightEvolutionTarget = null;
            HasEvolvedMidFight = false;
            Phase2Archetype = Phase2Archetype.None;
            ShieldHP = 0; // §8.3.7 (CL-021) — combat-transient; never carried between nodes on a save-restore
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
