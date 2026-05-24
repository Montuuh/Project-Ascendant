using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.4 — Hybrid Event Bus stub.
    // Full SO-channel implementation comes in Epic 2.
    // This class satisfies the Bootstrap.Initialise() call without runtime errors.
    public static class EventBus
    {
        public static void Initialise()
        {
            // TODO: Epic 2 — register all SO event channels here (§9.4).
            Debug.Log("[EventBus] Initialised (stub).");
        }
    }
}
