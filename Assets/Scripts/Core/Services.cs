using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Per §9.14 — Service Locator for core runtime systems.
    // Populated in Bootstrap.Start() before any game scene loads.
    // All services must be registered before Get<T>() is valid.
    public static class Services
    {
        private static readonly Dictionary<Type, object> _registry = new();

        public static void Register<T>(T service) where T : class
        {
            if (service == null)
                throw new ArgumentNullException(nameof(service), $"[Services] Cannot register null for {typeof(T).Name}.");

            _registry[typeof(T)] = service;
        }

        public static T Get<T>() where T : class
        {
            if (_registry.TryGetValue(typeof(T), out object service))
                return (T)service;

            Debug.LogError($"[Services] '{typeof(T).Name}' is not registered. Call Register<T>() in Bootstrap first.");
            return null;
        }

        public static bool Has<T>() where T : class => _registry.ContainsKey(typeof(T));

        public static void Clear()
        {
            _registry.Clear();
        }
    }
}
