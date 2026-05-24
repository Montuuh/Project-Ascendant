using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAscendant.Core
{
    // Simple service locator — dependency injection point for core systems.
    // Populated during Bootstrap.Start() before any game scene loads.
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Initialise()
        {
            _services.Clear();
            Debug.Log("[ServiceLocator] Initialised.");
        }

        public static void Register<T>(T service) where T : class
        {
            _services[typeof(T)] = service;
        }

        public static T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out object service))
                return (T)service;

            Debug.LogError($"[ServiceLocator] Service not registered: {typeof(T).Name}");
            return null;
        }

        public static bool Has<T>() where T : class => _services.ContainsKey(typeof(T));
    }
}
