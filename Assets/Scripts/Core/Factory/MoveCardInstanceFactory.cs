namespace ProjectAscendant.Core
{
    // Per §9.6.1 + §9.17 — MoveCardInstance factory.
    // No pool for VS — uses new+GC per §9.17 carve-out. Pool added post-VS if profiler shows pressure.
    //
    // Per Epic 5 Task 5.1.3 — the (move, owner, isMastery) overload is the
    // canonical construction path; SkillDeck.Build uses it. The single-arg
    // overload is retained for callers that don't track ownership (Epic 2
    // factory tests).
    public sealed class MoveCardInstanceFactory
    {
        public MoveCardInstance Create(MoveSO move)
        {
            return new MoveCardInstance { Move = move };
        }

        public MoveCardInstance Create(MoveSO move, PokemonInstance owner, bool isMasteryMove = false)
        {
            return new MoveCardInstance
            {
                Move = move,
                Owner = owner,
                IsMasteryMove = isMasteryMove,
            };
        }

        public void Release(MoveCardInstance instance)
        {
            if (instance == null) return;
            instance.Reset();
            // Per §9.17 — no pool for VS; GC collects.
        }
    }
}
