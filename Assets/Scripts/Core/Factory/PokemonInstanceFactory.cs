using System.Collections.Generic;

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
            instance.LearnedMoves.Clear();
            // Per §5.12.1 (CL-006) — seed the Learned Move Pool from the species' LEVEL-GATED learnset
            // (KnownMovesAtLevel; legacy fallback to BaseLearnset when no LevelUpLearnset is authored).
            // The active 4 (CurrentMoves) are filled by the creating caller from the same source. The
            // pool grows via level-ups (LevelUpResolver), evolution, TMs, Tutors; never shrinks.
            if (species != null)
            {
                List<MoveSO> known = species.KnownMovesAtLevel(level);
                for (int i = 0; i < known.Count; i++)
                    if (known[i] != null && !instance.LearnedMoves.Contains(known[i]))
                        instance.LearnedMoves.Add(known[i]);
            }
            instance.MasteryMove = species?.MasteryMove;
            // Per §5.12.3 (CL-008) — abilities are no longer auto-granted at creation.
            // They are earned via the Dojo (§7.14). Ability starts null; Dojo sets it.
            instance.Ability = null;
            instance.HeldItem = null;
            instance.StatStages.Clear();
            instance.PrimaryStatus = StatusCondition.None;
            instance.SecondaryStatus = StatusCondition.None;
            instance.CurrentStage = EvolutionStage.Basic;
            instance.SelectedBranch = default;
            return instance;
        }

        // Per §9.8 gap #43 — rent a cleared instance for save-restore. The caller (PokemonInstanceDTO
        // .Rebuild) sets every field explicitly from the saved snapshot, so no species seeding is done
        // here; Reset() guarantees no stale state leaks from a pooled instance.
        public PokemonInstance RentEmpty()
        {
            PokemonInstance instance = _pool.Rent();
            instance.Reset();
            return instance;
        }

        public void Release(PokemonInstance instance)
        {
            instance.Reset();
            _pool.Return(instance);
        }
    }
}
