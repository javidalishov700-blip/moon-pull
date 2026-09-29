using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonPull.Core
{
    /// <summary>
    /// Registry for the service layer (Ads, Analytics, Save, Audio, RemoteConfig, IAP, Localization, Haptics).
    /// Services are registered by interface so the Editor and tests can swap in mocks.
    /// </summary>
    public static class Services
    {
        private static readonly Dictionary<Type, object> Registry = new Dictionary<Type, object>();

        /// <summary>Registers or replaces the implementation for <typeparamref name="T"/>.</summary>
        public static void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            Registry[typeof(T)] = service;
        }

        /// <summary>Returns the registered service or throws with the missing type name.</summary>
        public static T Get<T>() where T : class
        {
            if (Registry.TryGetValue(typeof(T), out object service))
            {
                return (T)service;
            }

            throw new InvalidOperationException($"Service {typeof(T).Name} is not registered. Check the Boot installer order.");
        }

        /// <summary>Non-throwing lookup.</summary>
        public static bool TryGet<T>(out T service) where T : class
        {
            if (Registry.TryGetValue(typeof(T), out object found))
            {
                service = (T)found;
                return true;
            }

            service = null;
            return false;
        }

        /// <summary>True when <typeparamref name="T"/> has an implementation.</summary>
        public static bool Has<T>() where T : class => Registry.ContainsKey(typeof(T));

        /// <summary>Removes all registrations (tests and play-mode entry).</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Clear()
        {
            Registry.Clear();
        }
    }
}
