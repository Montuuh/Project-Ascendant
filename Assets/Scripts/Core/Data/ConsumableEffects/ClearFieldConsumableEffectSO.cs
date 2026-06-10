using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §4.3.8.6 (CL-012) — "Defog" Shop consumable. Clears the active field (any class:
    // Weather, Terrain, Hazard, and an enemy-owned Home Field) for the rest of combat — the
    // guaranteed counterplay to a hostile field. No parameters: it always clears everything.
    // (Named "Defog"; GDD §4.3.8.6 calls this consumable "Smoke Ball" — name reconciliation pending.)
    [CreateAssetMenu(fileName = "New Clear Field Effect", menuName = "Project Ascendant/Consumable Effects/Clear Field")]
    public class ClearFieldConsumableEffectSO : ConsumableEffectSO
    {
    }
}
