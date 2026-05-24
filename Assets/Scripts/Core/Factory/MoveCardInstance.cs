namespace ProjectAscendant.Core
{
    // Per §9.6.1 — runtime card representation used by Deck/Hand systems.
    // Per §9.17 — MoveCardInstance uses new+GC for VS (pool deferred post-VS per profiling).
    public sealed class MoveCardInstance
    {
        public MoveSO Move;
        public bool IsExhausted;

        public void Reset()
        {
            Move = null;
            IsExhausted = false;
        }
    }
}
