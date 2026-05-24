namespace ProjectAscendant.Core
{
    // Per §9.6.1 — enemy intent data. Poolable; one instance per enemy intent per turn.
    // Intents target POSITIONS (slot indices), not specific Pokémon. (§4.3.2)
    public sealed class IntentData
    {
        public int TargetSlotIndex;   // §4.3.2 — slot-based; whoever occupies it at resolution takes the hit
        public int ActionId;          // TODO: Epic 4 — replace with MoveSO/ActionSO reference
        public int Priority;

        public void Reset()
        {
            TargetSlotIndex = -1;
            ActionId = -1;
            Priority = 0;
        }
    }
}
