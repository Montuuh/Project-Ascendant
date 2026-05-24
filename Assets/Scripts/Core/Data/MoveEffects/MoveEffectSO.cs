using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.3.2.2 + Epic 3.1.6 — abstract base for secondary move effects.
    // Primary damage is handled by BasePower + damage formula (§4.1.1).
    // MoveEffectSO covers riders, buffs, status application, card draw, etc.
    // Concrete execution wired in Epic 4 (Combat System).
    public abstract class MoveEffectSO : ScriptableObject { }
}
