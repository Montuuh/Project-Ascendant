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
        public EvolutionStage CurrentStage;
        public EvolutionBranch SelectedBranch;

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
            CurrentStage = EvolutionStage.Basic;
            SelectedBranch = default;
        }
    }
}
