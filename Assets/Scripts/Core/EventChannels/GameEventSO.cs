using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.4.1.1 — ScriptableObject event channel base.
    // Create concrete subclasses with [CreateAssetMenu] for each event type.
    // Raise takes a snapshot copy of listeners before invoking,
    // making it safe against Register/Unregister calls during dispatch.
    public abstract class GameEventSO<T> : ScriptableObject
    {
        private readonly List<Action<T>> _listeners = new();

        public void Raise(T payload)
        {
            Action<T>[] snapshot = _listeners.ToArray();
            foreach (Action<T> listener in snapshot)
                listener(payload);
        }

        public void Register(Action<T> listener) => _listeners.Add(listener);

        public void Unregister(Action<T> listener) => _listeners.Remove(listener);
    }
}
