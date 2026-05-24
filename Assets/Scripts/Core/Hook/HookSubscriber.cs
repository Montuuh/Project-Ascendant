using System;

namespace ProjectAscendant.Core
{
    // Per §8.7 — wires a ScriptableHook to an EventBus subscription.
    // Returns the handler delegate so callers can unsubscribe when the relic/item is removed.
    // Epic 4 will use this to wire RelicSO.OnEvent and HeldItemSO.OnEvent channels.
    public static class HookSubscriber
    {
        // Subscribe 'hook' to EventBus<T>. On event: convert payload to EventContext via
        // 'converter', call hook.OnFire(ctx). Returns the handler for later Unsubscribe.
        public static Action<T> Subscribe<T>(ScriptableHook hook, Func<T, EventContext> converter)
        {
            Action<T> handler = payload => hook.OnFire(converter(payload));
            EventBus.Subscribe<T>(handler);
            return handler;
        }
    }
}
