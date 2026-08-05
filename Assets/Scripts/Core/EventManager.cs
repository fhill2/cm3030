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
    /// Naming: every event derives from one stem —
    ///   delegate  &lt;Stem&gt;Handler
    ///   field     On&lt;Stem&gt;
    ///   payload   &lt;Stem&gt;Args   (data-carrying events only)
    ///   raise     Raise&lt;Stem&gt;
    ///
    /// Place this component on the GameManager object in the scene.
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        /// <summary>Minimum seconds between attacks for any actor (player and enemy).</summary>
        public const float AttackWindow = 0.75f;

        public delegate void DamageHandler(DamageArgs e);
        public delegate void DeathHandler(DeathArgs e);
        public delegate void HitHandler(HitArgs e);
        public delegate void BlockHandler(BlockArgs e);

        public static event DamageHandler OnDamage;
        public static event DeathHandler OnDeath;
        public static event HitHandler OnHit;
        public static event BlockHandler OnBlock;

        // ── Raise helpers (the only place events are actually invoked) ───────

        public static void RaiseDamage(DamageArgs e) => OnDamage?.Invoke(e);
        public static void RaiseDeath(DeathArgs e) => OnDeath?.Invoke(e);
        public static void RaiseHit(HitArgs e) => OnHit?.Invoke(e);
        public static void RaiseBlock(BlockArgs e) => OnBlock?.Invoke(e);

        /// <summary>Detach every subscriber. Call on scene load to avoid stale references.</summary>
        public static void ClearAll()
        {
            OnDamage = null;
            OnDeath = null;
            OnHit = null;
            OnBlock = null;
        }
    }
}
