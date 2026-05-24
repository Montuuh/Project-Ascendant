using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §8.7 + Epic 3.1.7 — abstract base for consumable item effects.
    // Concrete execution wired in Epic 4 (Combat System).
    public abstract class ConsumableEffectSO : ScriptableObject { }
}
