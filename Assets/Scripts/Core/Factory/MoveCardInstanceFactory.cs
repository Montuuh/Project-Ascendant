namespace ProjectAscendant.Core
{
    // Per §9.6.1 + §9.17 — MoveCardInstance factory.
    // No pool for VS — uses new+GC per §9.17 carve-out. Pool added post-VS if profiler shows pressure.
    public sealed class MoveCardInstanceFactory
    {
        public MoveCardInstance Create(MoveSO move)
        {
            return new MoveCardInstance { Move = move };
        }

        public void Release(MoveCardInstance instance)
        {
            instance.Reset();
            // Per §9.17 — no pool for VS; GC collects.
        }
    }
}
