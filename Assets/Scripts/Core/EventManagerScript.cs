using UnityEngine;
using Game.Health;

namespace Game.Core
{
    /// <summary>
    /// Central event hub using C# events (multicast delegates).
    ///
    /// Events are declared `static` so any class can subscribe/publish through
    /// the class name — no instance reference required. The C# `event` keyword
    /// guarantees the delegate can only be invoked from within this class, so
    /// external code can only subscribe (+=) or unsubscribe (-=), never fire or
    /// overwrite the delegate directly. Publishers raise events through the
    /// static Raise*() helpers below, keeping the actual Invoke() in this class.
    ///
    /// Place this component on the GameManager object in the scene.
    /// </summary>
    public class EventManagerScript : MonoBehaviour
    {
        public delegate void DamageDealtHandler(DamageDealt e);
        public delegate void EntityDiedHandler(EntityDied e);
        public delegate void JumpHandler();
        public delegate void LandHandler();
        public delegate void FootstepHandler();

        public static event DamageDealtHandler OnDamageDealt;
        public static event EntityDiedHandler OnEntityDied;
        public static event JumpHandler OnJump;
        public static event LandHandler OnLand;
        public static event FootstepHandler OnFootstep;

        // ── Raise helpers (the only place events are actually invoked) ───────

        public static void RaiseDamageDealt(DamageDealt e) => OnDamageDealt?.Invoke(e);
        public static void RaiseEntityDied(EntityDied e) => OnEntityDied?.Invoke(e);
        public static void RaiseJump() => OnJump?.Invoke();
        public static void RaiseLand() => OnLand?.Invoke();
        public static void RaiseFootstep() => OnFootstep?.Invoke();

        /// <summary>Detach every subscriber. Call on scene load to avoid stale references.</summary>
        public static void ClearAll()
        {
            OnDamageDealt = null;
            OnEntityDied = null;
            OnJump = null;
            OnLand = null;
            OnFootstep = null;
        }
    }
}
