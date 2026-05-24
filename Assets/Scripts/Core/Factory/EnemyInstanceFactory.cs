namespace ProjectAscendant.Core
{
    // Per §9.6.1 — pooled factory for EnemyInstance. Small pool: max 3 enemies per combat.
    public sealed class EnemyInstanceFactory
    {
        private readonly Pool<EnemyInstance> _pool = new(initialCapacity: 4, maxCapacity: 8);

        public int PoolFreeCount => _pool.FreeCount;

        public EnemyInstance Create(string enemyId, int maxHP)
        {
            EnemyInstance instance = _pool.Rent();
            instance.EnemyId = enemyId;
            instance.CurrentHP = maxHP;
            instance.MaxHP = maxHP;
            instance.IntentQueue.Clear();
            return instance;
        }

        public void Release(EnemyInstance instance)
        {
            instance.Reset();
            _pool.Return(instance);
        }
    }
}
