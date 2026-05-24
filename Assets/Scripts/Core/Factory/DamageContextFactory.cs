namespace ProjectAscendant.Core
{
    // Per §9.6.1 — pooled factory for DamageContextData (mutable working context).
    // See DamageContextData.cs for the distinction from the DamageContext readonly struct.
    public sealed class DamageContextFactory
    {
        private readonly Pool<DamageContextData> _pool = new(initialCapacity: 8);

        public int PoolFreeCount => _pool.FreeCount;

        public DamageContextData Rent()
        {
            DamageContextData data = _pool.Rent();
            data.Reset();
            return data;
        }

        public void Return(DamageContextData data)
        {
            data.Reset();
            _pool.Return(data);
        }
    }
}
