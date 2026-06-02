namespace ProjectAscendant.Core
{
    // Per §9.6.2 — factory for PokemonInstance. Registered with Services in Bootstrap.
    // Pool pre-warms 16 instances — enough for a full Box of active Pokémon.
    public sealed class PokemonInstanceFactory
    {
        private readonly Pool<PokemonInstance> _pool = new(initialCapacity: 16);

        public int PoolFreeCount => _pool.FreeCount;

        // Per §9.6.2 — creates and initialises a PokemonInstance from species data.
        // Per §2.4.2 — freshly-created Pokémon start at full HP (canonical MaxHP).
        public PokemonInstance Create(PokemonSpeciesSO species, int level)
        {
            PokemonInstance instance = _pool.Rent();
            instance.Species = species;
            instance.Level = level;
            // Per R3-1 fix — seed CurrentHP from PokemonVitals.MaxHP (canonical),
            // not the stale ComputeMaxHP stub. Fresh recruits + wild enemies enter
            // at full HP per §2.4 / §7.3.4.1.
            instance.CurrentHP = PokemonVitals.MaxHP(instance);
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
    }
}
