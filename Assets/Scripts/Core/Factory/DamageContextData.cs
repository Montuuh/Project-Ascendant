namespace ProjectAscendant.Core
{
    // Per §9.6.1 — mutable working context for damage computation. Poolable.
    // Distinct from the DamageContext readonly struct in EventPayloads.cs (the EventBus payload).
    // Combat fills this during resolution, then calls ToEventPayload() to publish the immutable struct.
    public sealed class DamageContextData
    {
        public int SlotIndex;
        public int BaseDamage;
        public int FinalDamage;
        public bool IsCrit;
        public bool IsStab;
        public float TypeMultiplier;

        public void Reset()
        {
            SlotIndex = -1;
            BaseDamage = 0;
            FinalDamage = 0;
            IsCrit = false;
            IsStab = false;
            TypeMultiplier = 1f;
        }

        // Converts this mutable working context into the immutable EventBus payload.
        public DamageContext ToEventPayload() =>
            new DamageContext(SlotIndex, BaseDamage, FinalDamage, IsCrit, IsStab, TypeMultiplier);
    }
}
