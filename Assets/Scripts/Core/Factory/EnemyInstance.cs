using System.Collections.Generic;

namespace ProjectAscendant.Core
{
    // Per §9.6.1 — runtime enemy state. Poolable via EnemyInstanceFactory (small pool).
    // TODO: Epic 8 — expand with EnemyDataSO reference, multi-phase boss support (§4.4.3.1).
    public sealed class EnemyInstance
    {
        public string EnemyId;                        // TODO: Epic 8 — replace with EnemyDataSO
        public int CurrentHP;
        public int MaxHP;
        public readonly List<IntentData> IntentQueue = new(); // §4.3.2 — intents target slots

        public void Reset()
        {
            EnemyId = null;
            CurrentHP = 0;
            MaxHP = 0;
            IntentQueue.Clear();
        }
    }
}
