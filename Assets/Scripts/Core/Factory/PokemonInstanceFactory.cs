namespace ProjectAscendant.Core
{
    // Per §9.6.2 — factory for PokemonInstance. Registered with Services in Bootstrap.
    // Pool pre-warms 16 instances — enough for a full Box of active Pokémon.
    public sealed class PokemonInstanceFactory
    {
        private readonly Pool<PokemonInstance> _pool = new(initialCapacity: 16);

        public int PoolFreeCount => _pool.FreeCount;

        // Per §9.6.2 — creates and initialises a PokemonInstance from species data.
        public PokemonInstance Create(PokemonSpeciesSO species, int level)
        {
            PokemonInstance instance = _pool.Rent();
            instance.Species = species;
            instance.Level = level;
            instance.CurrentHP = species != null ? ComputeMaxHP(species, level) : 0;
            instance.CurrentXP = 0;
            instance.TraumaStacks = 0;
            instance.CurrentMoves.Clear();
            instance.MasteryMove = species?.MasteryMove;
            instance.Ability = species?.PrimaryAbility;
            instance.HeldItem = null;
            instance.StatStages.Clear();
            instance.PrimaryStatus = StatusCondition.None;
            instance.SecondaryStatus = StatusCondition.None;
            instance.CurrentStage = EvolutionStage.Basic;
            instance.SelectedBranch = default;
            return instance;
        }

        public void Release(PokemonInstance instance)
        {
            instance.Reset();
            _pool.Return(instance);
        }

        // TODO: Epic 4 — replace with actual HP formula from §4.2.X using BattleConfigSO.
        // Stub: BaseHP + (Level * 2).
        private static int ComputeMaxHP(PokemonSpeciesSO species, int level) =>
            species.BaseStats.BaseHP + (level * 2);
    }
}
