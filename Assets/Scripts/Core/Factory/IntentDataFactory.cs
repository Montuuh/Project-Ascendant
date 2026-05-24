namespace ProjectAscendant.Core
{
    // Per §9.6.1 — pooled factory for IntentData.
    // Initial capacity of 8 covers 3 enemies × ~2 intents per turn with margin.
    public sealed class IntentDataFactory
    {
        private readonly Pool<IntentData> _pool = new(initialCapacity: 8);

        public int PoolFreeCount => _pool.FreeCount;

        public IntentData Create(int targetSlotIndex, int actionId, int priority = 0)
        {
            IntentData data = _pool.Rent();
            data.TargetSlotIndex = targetSlotIndex;
            data.ActionId = actionId;
            data.Priority = priority;
            return data;
        }

        public void Release(IntentData data)
        {
            data.Reset();
            _pool.Return(data);
        }
    }
}
