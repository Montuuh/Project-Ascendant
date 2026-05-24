using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.4.1.2 — Code-side static EventBus.
    // Use for internal phase-driver events and strongly-typed system-to-system signals.
    // Handlers MUST be synchronous — async void is forbidden (§9.4.2).
    // Subscription order is deterministic: handlers fire in registration order.
    public static class EventBus
    {
        private static readonly Dictionary<Type, Delegate> _subscribers = new();

        // Called in Bootstrap before any scene loads. Clears all subscriptions.
        public static void Initialise()
        {
            _subscribers.Clear();
            Debug.Log("[EventBus] Initialised.");
        }

        // Subscribe<T>: appends handler to the multicast chain for payload type T.
        public static void Subscribe<T>(Action<T> handler)
        {
            Type type = typeof(T);
            _subscribers[type] = _subscribers.TryGetValue(type, out Delegate existing)
                ? Delegate.Combine(existing, handler)
                : handler;
        }

        // Unsubscribe<T>: removes handler. No-op if not registered.
        public static void Unsubscribe<T>(Action<T> handler)
        {
            Type type = typeof(T);
            if (!_subscribers.TryGetValue(type, out Delegate existing)) return;

            Delegate remaining = Delegate.Remove(existing, handler);
            if (remaining == null)
                _subscribers.Remove(type);
            else
                _subscribers[type] = remaining;
        }

        // Publish<T>: invokes all handlers registered for T.
        // Multicast delegates are immutable — safe against subscribe/unsubscribe during dispatch.
        public static void Publish<T>(T payload)
        {
            if (_subscribers.TryGetValue(typeof(T), out Delegate del))
                (del as Action<T>)?.Invoke(payload);
        }

        // Clear all subscriptions. Called between runs via Bootstrap.
        public static void Clear() => _subscribers.Clear();
    }
}
