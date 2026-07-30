using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// Unified inter-module communication hub.
    /// Combines an event bus (publish/subscribe) and a service locator
    /// (register/get) into a single entry point so modules never reference
    /// each other directly.
    ///
    /// ── Event Bus ──────────────────────────────────────────
    ///   Publish / Subscribe / Unsubscribe
    ///   Events are lightweight structs declared by the producing module:
    ///     GameHub.Publish(new DamageDealt(target, 25f, DamageType.Heavy, this));
    ///     GameHub.Subscribe&lt;DamageDealt&gt;(OnDamageDealt);
    ///
    /// ── Service Locator ────────────────────────────────────
    ///   RegisterService / GetService
    ///   For singleton-style services that modules need to query:
    ///     GameHub.RegisterService&lt;IGameState&gt;(this);
    ///     var state = GameHub.GetService&lt;IGameState&gt;();
    /// </summary>
    public static class GameHub
    {
        private static readonly Dictionary<Type, List<Delegate>> s_Subscribers = new();

        // ── Event Bus ──────────────────────────────────────────

        /// <summary>Register a handler for event type <typeparamref name="T"/>.</summary>
        public static void Subscribe<T>(Action<T> handler) where T : struct
        {
            var key = typeof(T);
            if (!s_Subscribers.TryGetValue(key, out var list))
            {
                list = new List<Delegate>();
                s_Subscribers[key] = list;
            }
            list.Add(handler);
        }

        /// <summary>Remove a previously-registered handler.</summary>
        public static void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            if (s_Subscribers.TryGetValue(typeof(T), out var list))
                list.Remove(handler);
        }

        /// <summary>Broadcast an event to all subscribers of type <typeparamref name="T"/>.</summary>
        public static void Publish<T>(T eventData) where T : struct
        {
            if (!s_Subscribers.TryGetValue(typeof(T), out var list))
                return;

            var snapshot = list.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
                ((Action<T>)snapshot[i]).Invoke(eventData);
        }

        // ── Service Locator ────────────────────────────────────

        private static readonly Dictionary<Type, object> s_Services = new();

        /// <summary>Register a service instance for type <typeparamref name="T"/>.</summary>
        public static void RegisterService<T>(T service) where T : class
        {
            s_Services[typeof(T)] = service;
        }

        /// <summary>Retrieve a previously-registered service, or null.</summary>
        public static T GetService<T>() where T : class
        {
            s_Services.TryGetValue(typeof(T), out var service);
            return service as T;
        }

        // ── Cleanup ────────────────────────────────────────────

        /// <summary>Clear all event subscriptions (call on scene unload).</summary>
        public static void ClearSubscribers() => s_Subscribers.Clear();

        /// <summary>Clear all registered services (call on scene unload).</summary>
        public static void ClearServices() => s_Services.Clear();

        /// <summary>Clear everything — call at the start of a scene to avoid stale references.</summary>
        public static void Reset()
        {
            s_Subscribers.Clear();
            s_Services.Clear();
        }
    }
}
