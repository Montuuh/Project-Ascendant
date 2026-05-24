using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §8.7 — abstract ScriptableObject hook. Subclasses implement data-bound effects
    // for relics (RelicSO.OnAcquireHook / OnEvent) and Held Items (HeldItemSO.OnEquipHook / OnEvent).
    // All hooks are authored in the Inspector; wiring to EventBus channels is done by HookSubscriber.
    public abstract class ScriptableHook : ScriptableObject
    {
        // Called by HookSubscriber when the subscribed event fires.
        // Implementations modify the provided EventContext; callers apply the result.
        public abstract void OnFire(EventContext context);
    }
}
